// File: Services/Implementations/FileUploadService.cs
using Microsoft.Extensions.Options;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Implementation upload ảnh.
/// Kiểm tra 3 lớp: dung lượng, đuôi file, và chữ ký nhị phân (magic bytes) của file
/// để chống trường hợp đổi đuôi file thực thi thành .jpg.
/// Tên file được sinh mới hoàn toàn (GUID) để tránh ghi đè và path traversal.
/// </summary>
public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly FileUploadSettings _settings;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(
        IWebHostEnvironment environment,
        IOptions<FileUploadSettings> settings,
        ILogger<FileUploadService> logger)
    {
        _environment = environment;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(IFormFile file, string subFolder)
    {
        // ---------- 1. Kiểm tra file có tồn tại và có dữ liệu ----------
        if (file is null || file.Length == 0)
        {
            return (false, null, "Vui lòng chọn một file ảnh.");
        }

        // ---------- 2. Kiểm tra dung lượng ----------
        if (file.Length > _settings.MaxFileSizeBytes)
        {
            var maxMb = _settings.MaxFileSizeBytes / 1024d / 1024d;
            return (false, null, $"Dung lượng ảnh tối đa là {maxMb:0.#}MB.");
        }

        // ---------- 3. Kiểm tra đuôi file ----------
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension) ||
            !_settings.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            var allowed = string.Join(", ", _settings.AllowedExtensions);
            return (false, null, $"Chỉ chấp nhận các định dạng ảnh: {allowed}.");
        }

        // ---------- 4. Kiểm tra chữ ký nhị phân (magic bytes) ----------
        if (!await IsRealImageAsync(file, extension))
        {
            return (false, null, "File tải lên không phải là ảnh hợp lệ.");
        }

        try
        {
            // ---------- 5. Xác định thư mục lưu file ----------
            // Chỉ cho phép thư mục con đơn giản, chặn đường dẫn lạ.
            var safeSubFolder = SanitizeFolderName(subFolder);

            var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads", safeSubFolder);
            if (!Directory.Exists(uploadsRoot))
            {
                Directory.CreateDirectory(uploadsRoot);
            }

            // ---------- 6. Sinh tên file mới ----------
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsRoot, fileName);

            // ---------- 7. Ghi file xuống đĩa ----------
            await using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Đường dẫn tương đối dùng để lưu DB và hiển thị trên View.
            var relativePath = $"/uploads/{safeSubFolder}/{fileName}";

            _logger.LogInformation("Đã lưu ảnh upload: {RelativePath}", relativePath);

            return (true, relativePath, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi lưu file upload {FileName}.", file.FileName);
            return (false, null, "Có lỗi xảy ra khi lưu ảnh. Vui lòng thử lại.");
        }
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        try
        {
            // Chỉ xử lý các đường dẫn nằm trong /uploads để tránh xóa file hệ thống.
            if (!relativePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Từ chối xóa file ngoài thư mục uploads: {Path}", relativePath);
                return;
            }

            // Chuẩn hóa đường dẫn: bỏ dấu '/' đầu và thay '/' bằng dấu phân cách hệ điều hành.
            var trimmed = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_environment.WebRootPath, trimmed);

            // Xác nhận đường dẫn cuối cùng vẫn nằm trong wwwroot (chống path traversal).
            var webRootFull = Path.GetFullPath(_environment.WebRootPath);
            var targetFull = Path.GetFullPath(fullPath);
            if (!targetFull.StartsWith(webRootFull, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Đường dẫn không hợp lệ khi xóa ảnh: {Path}", relativePath);
                return;
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Đã xóa ảnh cũ: {RelativePath}", relativePath);
            }
        }
        catch (Exception ex)
        {
            // Không ném lỗi ra ngoài: xóa ảnh cũ thất bại không nên làm hỏng luồng nghiệp vụ.
            _logger.LogError(ex, "Lỗi khi xóa ảnh {RelativePath}.", relativePath);
        }
    }

    /// <summary>
    /// Kiểm tra nội dung file có đúng là ảnh dựa trên magic bytes.
    /// </summary>
    private static async Task<bool> IsRealImageAsync(IFormFile file, string extension)
    {
        // Đọc tối đa 12 byte đầu file để nhận dạng.
        var header = new byte[12];
        await using var stream = file.OpenReadStream();

        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length));
        if (bytesRead < 4)
        {
            return false;
        }

        // JPEG: FF D8 FF
        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        var isPng = bytesRead >= 8
                    && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47
                    && header[4] == 0x0D && header[5] == 0x0A && header[6] == 0x1A && header[7] == 0x0A;

        // WEBP: "RIFF" .... "WEBP"
        var isWebp = bytesRead >= 12
                     && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                     && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        return extension switch
        {
            ".jpg" or ".jpeg" => isJpeg,
            ".png" => isPng,
            ".webp" => isWebp,
            _ => false
        };
    }

    /// <summary>
    /// Làm sạch tên thư mục con: chỉ giữ chữ cái, chữ số, gạch ngang, gạch dưới.
    /// Ngăn chặn path traversal qua tham số subFolder.
    /// </summary>
    private static string SanitizeFolderName(string subFolder)
    {
        if (string.IsNullOrWhiteSpace(subFolder))
        {
            return "misc";
        }

        var cleaned = new string(subFolder
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());

        return string.IsNullOrWhiteSpace(cleaned) ? "misc" : cleaned;
    }
}