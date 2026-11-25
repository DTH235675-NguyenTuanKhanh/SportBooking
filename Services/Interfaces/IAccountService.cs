// File: Services/Interfaces/IAccountService.cs
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Account;

namespace SportBooking.Services.Interfaces;

/// <summary>
/// Nghiệp vụ tài khoản: đăng ký, đăng nhập, quên/đặt lại/đổi mật khẩu.
/// Controller chỉ gọi service, không tự xử lý Identity.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Đăng ký tài khoản mới.
    /// - Customer: kích hoạt ngay, gán role Customer.
    /// - Owner: trạng thái ApprovalStatus = Pending, phải chờ Admin duyệt.
    /// </summary>
    Task<ServiceResult> RegisterAsync(RegisterViewModel model);

    /// <summary>
    /// Kiểm tra thông tin đăng nhập.
    /// Xử lý đầy đủ các trường hợp: sai mật khẩu, tài khoản bị khóa, chưa được duyệt.
    /// </summary>
    /// <returns>Kết quả kèm thông tin user nếu đăng nhập thành công.</returns>
    Task<ServiceResult<ApplicationUser>> ValidateLoginAsync(LoginViewModel model);

    /// <summary>
    /// Tạo yêu cầu quên mật khẩu và gửi email chứa link đặt lại.
    /// Luôn trả về thành công dù email có tồn tại hay không (chống dò tìm tài khoản).
    /// </summary>
    /// <param name="model">ViewModel chứa email.</param>
    /// <param name="resetUrlBuilder">Hàm dựng link đặt lại từ email + token (do Controller truyền vào vì cần biết scheme/host).</param>
    Task<ServiceResult> SendPasswordResetLinkAsync(ForgotPasswordViewModel model, Func<string, string, string> resetUrlBuilder);

    /// <summary>
    /// Đặt lại mật khẩu bằng token nhận từ email.
    /// </summary>
    Task<ServiceResult> ResetPasswordAsync(ResetPasswordViewModel model);

    /// <summary>
    /// Đổi mật khẩu khi đã đăng nhập (yêu cầu nhập đúng mật khẩu hiện tại).
    /// </summary>
    Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordViewModel model);

    /// <summary>
    /// Kiểm tra tài khoản có bị khóa hay không, trả về lý do nếu có.
    /// </summary>
    Task<(bool IsActive, string? Reason)> CheckAccountActiveAsync(string userId);
}