// File: Services/Interfaces/IFileUploadService.cs
namespace SportBooking.Services.Interfaces;

/// <summary>
/// Dịch vụ upload file ảnh.
/// Chịu trách nhiệm kiểm tra tính hợp lệ (đuôi file, dung lượng, MIME) và lưu file
/// vào thư mục con trong wwwroot/uploads, trả về đường dẫn tương đối để lưu DB.
/// </summary>
public interface IFileUploadService
{
    /// <summary>
    /// Lưu ảnh vào thư mục con chỉ định.
    /// </summary>
    /// <param name="file">File người dùng upload.</param>
    /// <param name="subFolder">Thư mục con trong wwwroot/uploads (ví dụ: "avatars").</param>
    /// <returns>
    /// Tuple: (Thành công?, Đường dẫn tương đối, Thông báo lỗi).
    /// Đường dẫn trả về có dạng "/uploads/avatars/abc123.jpg".
    /// </returns>
    Task<(bool Success, string? RelativePath, string? ErrorMessage)> SaveImageAsync(IFormFile file, string subFolder);

    /// <summary>
    /// Xóa ảnh cũ theo đường dẫn tương đối. Không báo lỗi nếu file không tồn tại.
    /// </summary>
    /// <param name="relativePath">Đường dẫn tương đối, ví dụ "/uploads/avatars/abc.jpg".</param>
    void DeleteImage(string? relativePath);
}