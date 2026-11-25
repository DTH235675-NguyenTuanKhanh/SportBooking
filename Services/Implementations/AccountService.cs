// File: Services/Implementations/AccountService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Account;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>
/// Nghiệp vụ tài khoản, làm việc trực tiếp với ASP.NET Core Identity (UserManager).
/// Toàn bộ thao tác đều bất đồng bộ và có ghi log để phục vụ điều tra sự cố.
/// </summary>
public class AccountService : IAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountService> _logger;
    private readonly ApplicationDbContext _context;

    public AccountService(
        UserManager<ApplicationUser> userManager,
        IEmailService emailService,
        ILogger<AccountService> logger,
        ApplicationDbContext context)
    {
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
        _context = context;
    }

    // =========================================================================
    // 1. ĐĂNG KÝ
    // =========================================================================
    public async Task<ServiceResult> RegisterAsync(RegisterViewModel model)
    {
        // Kiểm tra email đã tồn tại chưa (Identity cũng chặn, nhưng kiểm tra sớm để báo lỗi đẹp).
        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null)
        {
            return ServiceResult.Fail(new Dictionary<string, string[]>
            {
                [nameof(RegisterViewModel.Email)] = new[] { "Email này đã được sử dụng. Vui lòng chọn email khác." }
            }, "Email đã tồn tại.");
        }

        // Xác định role và trạng thái dựa trên loại tài khoản.
        var isOwner = model.AccountType == "Owner";
        var roleName = isOwner ? AppConstants.Roles.Owner : AppConstants.Roles.Customer;

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true, // Bỏ qua xác thực email cho đồ án; bật lại nếu triển khai thật.
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            DateOfBirth = model.DateOfBirth,
            Address = model.Address?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            ApprovalStatus = isOwner
                ? OwnerApprovalStatus.Pending   // Chủ sân phải chờ Admin duyệt
                : OwnerApprovalStatus.NotApplicable
        };

        if (isOwner)
        {
            user.BusinessName = model.BusinessName?.Trim();
            user.BusinessPhone = model.BusinessPhone?.Trim();
        }

        OwnerTransferProfile? transfer = null;
        if (isOwner)
        {
            var prepared = await TransferProfileService.PrepareAsync(user.Id, model.Transfer);
            if (!prepared.Success)
                return ServiceResult.Fail(prepared.Errors.ToDictionary(e => $"Transfer.{e.Key}", e => e.Value), prepared.Message);
            transfer = prepared.Data;
        }

        var result = await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var created = await _userManager.CreateAsync(user, model.Password);
            if (!created.Succeeded)
                return ServiceResult.Fail(MapIdentityErrors(created.Errors), "Đăng ký không thành công. Vui lòng kiểm tra lại thông tin.");
            var roleResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                _logger.LogError("Không gán được role {Role} khi đăng ký: {Errors}", roleName,
                    string.Join(", ", roleResult.Errors.Select(e => e.Code)));
                return ServiceResult.Fail("Đăng ký không thành công do lỗi hệ thống. Vui lòng thử lại sau.");
            }
            if (transfer is not null)
            {
                _context.OwnerTransferProfiles.Add(transfer);
                await _context.SaveChangesAsync();
            }
            await transaction.CommitAsync();
            return ServiceResult.Ok();
        });
        if (!result.Success) return result;

        _logger.LogInformation("Đã đăng ký tài khoản {Email} với role {Role} (ApprovalStatus: {Status}).",
            model.Email, roleName, user.ApprovalStatus);

        var message = isOwner
            ? "Đăng ký thành công! Tài khoản Chủ sân của bạn đang chờ Admin phê duyệt. " +
              "Bạn sẽ nhận được thông báo khi tài khoản được kích hoạt."
            : "Đăng ký thành công! Bạn có thể đăng nhập và bắt đầu đặt sân ngay bây giờ.";

        return ServiceResult.Ok(message);
    }

    // =========================================================================
    // 2. ĐĂNG NHẬP
    // =========================================================================
    public async Task<ServiceResult<ApplicationUser>> ValidateLoginAsync(LoginViewModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        // Không tìm thấy user: trả thông báo chung để tránh lộ thông tin tài khoản nào tồn tại.
        if (user is null)
        {
            _logger.LogWarning("Đăng nhập thất bại: không tìm thấy email {Email}.", model.Email);
            return ServiceResult<ApplicationUser>.Fail("Email hoặc mật khẩu không đúng.");
        }

        // ---- Kiểm tra tài khoản bị hệ thống khóa (do Admin khóa) ----
        if (!user.IsActive)
        {
            _logger.LogWarning("Đăng nhập bị từ chối: tài khoản {Email} đã bị khóa.", model.Email);
            return ServiceResult<ApplicationUser>.Fail(
                "Tài khoản của bạn đã bị khóa bởi quản trị viên. " +
                "Vui lòng liên hệ bộ phận hỗ trợ để được giải quyết.");
        }

        // ---- Kiểm tra tài khoản Chủ sân chưa được duyệt ----
        if (user.ApprovalStatus == OwnerApprovalStatus.Pending)
        {
            _logger.LogWarning("Đăng nhập bị từ chối: tài khoản Chủ sân {Email} đang chờ duyệt.", model.Email);
            return ServiceResult<ApplicationUser>.Fail(
                "Tài khoản Chủ sân của bạn đang chờ Admin phê duyệt. " +
                "Vui lòng quay lại sau khi tài khoản được kích hoạt.");
        }

        if (user.ApprovalStatus == OwnerApprovalStatus.Rejected)
        {
            _logger.LogWarning("Đăng nhập bị từ chối: tài khoản Chủ sân {Email} đã bị từ chối.", model.Email);
            return ServiceResult<ApplicationUser>.Fail(
                "Yêu cầu đăng ký tài khoản Chủ sân của bạn đã bị từ chối. " +
                "Vui lòng liên hệ bộ phận hỗ trợ để biết thêm chi tiết.");
        }

        // ---- Kiểm tra tài khoản có đang bị khóa tạm thời do nhập sai nhiều lần ----
        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remainingMinutes = lockoutEnd.HasValue
                ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes))
                : 15;

            _logger.LogWarning("Đăng nhập bị từ chối: tài khoản {Email} đang bị khóa tạm thời.", model.Email);

            return ServiceResult<ApplicationUser>.Fail(
                $"Tài khoản đã bị khóa tạm thời do nhập sai mật khẩu quá nhiều lần. " +
                $"Vui lòng thử lại sau {remainingMinutes} phút.");
        }

        // ---- Kiểm tra mật khẩu ----
        var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);

        if (!passwordValid)
        {
            // Tăng bộ đếm số lần nhập sai; Identity sẽ tự khóa khi đạt ngưỡng MaxFailedAccessAttempts (5).
            await _userManager.AccessFailedAsync(user);

            if (await _userManager.IsLockedOutAsync(user))
            {
                return ServiceResult<ApplicationUser>.Fail(
                    "Tài khoản đã bị khóa tạm thời do nhập sai mật khẩu quá nhiều lần. Vui lòng thử lại sau 15 phút.");
            }

            var failedCount = await _userManager.GetAccessFailedCountAsync(user);
            var remaining = 5 - failedCount;

            _logger.LogWarning("Đăng nhập thất bại: sai mật khẩu cho {Email} (lần {Count}).", model.Email, failedCount);

            if (remaining > 0)
            {
                return ServiceResult<ApplicationUser>.Fail(
                    $"Email hoặc mật khẩu không đúng. Bạn còn {remaining} lần thử trước khi tài khoản bị khóa tạm thời.");
            }

            return ServiceResult<ApplicationUser>.Fail(
                "Email hoặc mật khẩu không đúng. Tài khoản của bạn đã bị khóa tạm thời 15 phút do nhập sai quá nhiều lần.");
        }

        // ---- Đăng nhập thành công: reset bộ đếm số lần sai ----
        if (await _userManager.GetAccessFailedCountAsync(user) > 0)
        {
            await _userManager.ResetAccessFailedCountAsync(user);
        }

        _logger.LogInformation("Đăng nhập thành công: {Email}.", model.Email);

        return ServiceResult<ApplicationUser>.Ok(user);
    }

    // =========================================================================
    // 3. QUÊN MẬT KHẨU
    // =========================================================================
    public async Task<ServiceResult> SendPasswordResetLinkAsync(
        ForgotPasswordViewModel model,
        Func<string, string, string> resetUrlBuilder)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);

        // Bảo mật: dù email không tồn tại vẫn trả về thành công như nhau,
        // tránh kẻ tấn công dò tìm danh sách email đã đăng ký trong hệ thống.
        if (user is null)
        {
            _logger.LogWarning("Yêu cầu quên mật khẩu cho email không tồn tại: {Email}.", model.Email);
            return ServiceResult.Ok(
                "Nếu email này tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu. " +
                "Vui lòng kiểm tra hộp thư của bạn.");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Yêu cầu quên mật khẩu cho tài khoản bị khóa: {Email}.", model.Email);
            return ServiceResult.Fail(
                "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ bộ phận hỗ trợ để được giúp đỡ.");
        }

        // Sinh token đặt lại mật khẩu bằng Identity.
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Url.Action mã hóa query string; truyền token gốc để tránh mã hóa hai lần.
        var resetLink = resetUrlBuilder(user.Email!, token);

        try
        {
            await _emailService.SendPasswordResetEmailAsync(
                user.Email!,
                user.FullName,
                resetLink);

            _logger.LogInformation("Đã gửi email đặt lại mật khẩu cho {Email}.", model.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không gửi được email đặt lại mật khẩu cho {Email}.", model.Email);
            return ServiceResult.Fail(
                "Không gửi được email đặt lại mật khẩu. Vui lòng thử lại sau hoặc liên hệ bộ phận hỗ trợ.");
        }

        return ServiceResult.Ok(
            "Chúng tôi đã gửi hướng dẫn đặt lại mật khẩu đến email của bạn. " +
            "Vui lòng kiểm tra hộp thư (kể cả thư mục Spam).");
    }

    // =========================================================================
    // 4. ĐẶT LẠI MẬT KHẨU BẰNG TOKEN
    // =========================================================================
    public async Task<ServiceResult> ResetPasswordAsync(ResetPasswordViewModel model)
    {
        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            // Token gắn với user, nên nếu không tìm thấy user thì token chắc chắn không hợp lệ.
            _logger.LogWarning("Đặt lại mật khẩu thất bại: không tìm thấy user {Email}.", model.Email);
            return ServiceResult.Fail("Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
        }

        // Identity tự giải mã token (Controller đã URL-decode trước khi truyền vào).
        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

        if (!result.Succeeded)
        {
            var errors = MapIdentityErrors(result.Errors);
            _logger.LogWarning("Đặt lại mật khẩu thất bại cho {Email}: {Errors}",
                model.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));

            // Lỗi token hết hạn/không hợp lệ là trường hợp phổ biến nhất.
            if (result.Errors.Any(e => e.Code == "InvalidToken"))
            {
                return ServiceResult.Fail(
                    "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. " +
                    "Vui lòng yêu cầu gửi lại email đặt lại mật khẩu.");
            }

            return ServiceResult.Fail(errors, "Đặt lại mật khẩu không thành công.");
        }

        // Đặt lại mật khẩu thành công: mở khóa tài khoản nếu đang bị khóa tạm thời
        // và reset bộ đếm nhập sai, để người dùng có thể đăng nhập ngay.
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);

        _logger.LogInformation("Đặt lại mật khẩu thành công cho {Email}.", model.Email);

        return ServiceResult.Ok("Mật khẩu đã được đặt lại thành công. Bạn có thể đăng nhập bằng mật khẩu mới.");
    }

    // =========================================================================
    // 5. ĐỔI MẬT KHẨU (đã đăng nhập)
    // =========================================================================
    public async Task<ServiceResult> ChangePasswordAsync(string userId, ChangePasswordViewModel model)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return ServiceResult.Fail("Không tìm thấy tài khoản. Vui lòng đăng nhập lại.");
        }

        // Không cho đặt mật khẩu mới trùng mật khẩu cũ.
        if (model.CurrentPassword == model.NewPassword)
        {
            return ServiceResult.Fail(new Dictionary<string, string[]>
            {
                [nameof(ChangePasswordViewModel.NewPassword)] =
                    new[] { "Mật khẩu mới phải khác mật khẩu hiện tại." }
            }, "Mật khẩu mới không hợp lệ.");
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            var errors = MapIdentityErrors(result.Errors);
            _logger.LogWarning("Đổi mật khẩu thất bại cho {Email}: {Errors}",
                user.Email,
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return ServiceResult.Fail(errors, "Đổi mật khẩu không thành công.");
        }

        _logger.LogInformation("Đổi mật khẩu thành công cho {Email}.", user.Email);

        return ServiceResult.Ok("Đổi mật khẩu thành công.");
    }

    // =========================================================================
    // 6. KIỂM TRA TRẠNG THÁI TÀI KHOẢN
    // =========================================================================
    public async Task<(bool IsActive, string? Reason)> CheckAccountActiveAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return (false, "Không tìm thấy tài khoản.");
        }

        if (!user.IsActive)
        {
            return (false, "Tài khoản của bạn đã bị khóa bởi quản trị viên.");
        }

        return (true, null);
    }

    // =========================================================================
    // HELPER
    // =========================================================================
    /// <summary>
    /// Chuyển đổi lỗi Identity (tiếng Anh) sang thông báo tiếng Việt,
    /// gắn vào đúng tên trường để hiển thị cạnh input tương ứng.
    /// </summary>
    private static Dictionary<string, string[]> MapIdentityErrors(IEnumerable<IdentityError> errors)
    {
        var result = new Dictionary<string, string[]>();

        foreach (var error in errors)
        {
            // Xác định trường bị lỗi dựa trên mã lỗi của Identity.
            var field = error.Code switch
            {
                "DuplicateEmail" or "DuplicateUserName" => nameof(RegisterViewModel.Email),
                "PasswordTooShort" or "PasswordRequiresUpper" or "PasswordRequiresLower"
                    or "PasswordRequiresDigit" or "PasswordRequiresNonAlphanumeric"
                    or "PasswordRequiresUniqueChars" => nameof(RegisterViewModel.Password),
                "PasswordMismatch" => nameof(ChangePasswordViewModel.CurrentPassword),
                "InvalidEmail" or "InvalidUserName" => nameof(RegisterViewModel.Email),
                _ => string.Empty
            };

            var message = error.Code switch
            {
                "DuplicateEmail" or "DuplicateUserName" => "Email này đã được sử dụng.",
                "PasswordTooShort" => "Mật khẩu quá ngắn.",
                "PasswordRequiresUpper" => "Mật khẩu phải có ít nhất 1 chữ hoa.",
                "PasswordRequiresLower" => "Mật khẩu phải có ít nhất 1 chữ thường.",
                "PasswordRequiresDigit" => "Mật khẩu phải có ít nhất 1 chữ số.",
                "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải có ít nhất 1 ký tự đặc biệt.",
                "PasswordMismatch" => "Mật khẩu hiện tại không đúng.",
                "InvalidEmail" => "Email không hợp lệ.",
                "InvalidUserName" => "Tên đăng nhập không hợp lệ.",
                "InvalidToken" => "Liên kết không hợp lệ hoặc đã hết hạn.",
                _ => "Thông tin không hợp lệ. Vui lòng kiểm tra lại."
            };

            if (string.IsNullOrEmpty(field))
            {
                // Lỗi không gắn được với trường cụ thể: dùng khóa rỗng để hiển thị ở đầu form.
                result[string.Empty] = result.TryGetValue(string.Empty, out var existing)
                    ? existing.Append(message).ToArray()
                    : new[] { message };
            }
            else
            {
                result[field] = result.TryGetValue(field, out var existing)
                    ? existing.Append(message).ToArray()
                    : new[] { message };
            }
        }

        return result;
    }
}
