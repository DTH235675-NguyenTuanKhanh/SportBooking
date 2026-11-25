using Microsoft.EntityFrameworkCore;
using SkiaSharp;
using SportBooking.Data;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Account;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

public sealed class TransferProfileService(ApplicationDbContext context)
{
    public async Task<TransferProfileInput> GetAsync(string ownerId) =>
        await context.OwnerTransferProfiles.Where(p => p.OwnerId == ownerId).Select(p => new TransferProfileInput
        {
            Enabled = p.Enabled, BankName = p.BankName, AccountNumber = p.AccountNumber,
            AccountName = p.AccountName, HasCurrentQr = true
        }).SingleOrDefaultAsync() ?? new();

    public Task<byte[]?> GetQrAsync(string ownerId) => context.OwnerTransferProfiles
        .Where(p => p.OwnerId == ownerId).Select(p => p.QrImage.Png).SingleOrDefaultAsync();

    public async Task<ServiceResult> SaveAsync(string ownerId, TransferProfileInput input)
    {
        var current = await context.OwnerTransferProfiles.FindAsync(ownerId);
        var result = await PrepareAsync(ownerId, input, current);
        if (!result.Success) return ServiceResult.Fail(result.Errors, result.Message);
        if (input.Enabled && input.Qr is not null && result.Data is { } changed)
            context.TransferImages.Add(changed.QrImage);
        if (result.Data is not null && current is null) context.OwnerTransferProfiles.Add(result.Data);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Fail("Không lưu được thông tin chuyển khoản. Vui lòng tải lại trang và thử lại.");
        }
        return ServiceResult.Ok("Đã lưu thông tin chuyển khoản cho các lượt đặt mới.");
    }

    public static async Task<ServiceResult<OwnerTransferProfile?>> PrepareAsync(
        string ownerId, TransferProfileInput input, OwnerTransferProfile? current = null)
    {
        if (!input.Enabled)
        {
            if (current is not null) current.Enabled = false;
            return ServiceResult<OwnerTransferProfile?>.Ok(current);
        }

        var bank = input.BankName?.Trim();
        var number = input.AccountNumber?.Trim();
        var name = input.AccountName?.Trim();
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrEmpty(bank) || bank.Length > 80)
            errors[nameof(input.BankName)] = ["Nhập tên ngân hàng, tối đa 80 ký tự."];
        if (string.IsNullOrEmpty(number) || number.Length is < 3 or > 30 || !number.All(char.IsAsciiDigit))
            errors[nameof(input.AccountNumber)] = ["Số tài khoản phải gồm 3 đến 30 chữ số."];
        if (string.IsNullOrEmpty(name) || name.Length > 100)
            errors[nameof(input.AccountName)] = ["Nhập tên chủ tài khoản, tối đa 100 ký tự."];
        if (!input.Confirmed)
            errors[nameof(input.Confirmed)] = ["Hãy kiểm tra và xác nhận mã QR khớp thông tin tài khoản."];
        var qr = input.Qr is not null ? await TransferImageUpload.ReadAsync(input.Qr) : null;
        if (qr is null && (input.Qr is not null || current is null))
            errors[nameof(input.Qr)] = [TransferImageUpload.Error];
        if (errors.Count != 0)
            return ServiceResult<OwnerTransferProfile?>.Fail(errors, "Vui lòng kiểm tra lại thông tin chuyển khoản.");
        var profile = current ?? new OwnerTransferProfile { OwnerId = ownerId };
        profile.BankName = bank!;
        profile.AccountNumber = number!;
        profile.AccountName = name!;
        profile.Enabled = true;
        if (qr is not null)
        {
            profile.QrImage = qr;
            profile.QrImageId = qr.Id;
        }
        return ServiceResult<OwnerTransferProfile?>.Ok(profile);
    }
}

public static class TransferImageUpload
{
    public const string Error = "Chọn ảnh PNG hoặc JPEG hợp lệ, tối đa 2 MB và 2.048 × 2.048 px.";
    private const int MaxBytes = 2 * 1024 * 1024;
    private static readonly byte[] PngHeader = [137, 80, 78, 71, 13, 10, 26, 10];
    private static readonly byte[] JpegHeader = [255, 216, 255];

    public static async Task<TransferImage?> ReadAsync(IFormFile file)
    {
        if (file.Length is < 8 or > MaxBytes) return null;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        SKEncodedImageFormat? expected = extension switch
        {
            ".png" => SKEncodedImageFormat.Png,
            ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
            _ => null
        };
        if (expected is null) return null;
        try
        {
            using var stream = file.OpenReadStream();
            var prefix = new byte[8];
            await stream.ReadExactlyAsync(prefix);
            var signature = expected == SKEncodedImageFormat.Png ? PngHeader : JpegHeader;
            if (!prefix.AsSpan().StartsWith(signature)) return null;
            stream.Position = 0;
            using var codec = SKCodec.Create(stream);
            if (codec is null || codec.EncodedFormat != expected || codec.Info.Width is < 1 or > 2048
                || codec.Info.Height is < 1 or > 2048) return null;
            var pixels = new SKImageInfo(codec.Info.Width, codec.Info.Height);
            using var bitmap = new SKBitmap(pixels);
            if (codec.GetPixels(pixels, bitmap.GetPixels()) != SKCodecResult.Success) return null;
            using var output = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            return output is { Size: <= 8 * 1024 * 1024 } ? new TransferImage { Png = output.ToArray() } : null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
