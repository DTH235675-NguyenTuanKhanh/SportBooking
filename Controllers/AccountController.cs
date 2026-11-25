// File: Controllers/AccountController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Account;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Interfaces;

namespace SportBooking.Controllers;

/// <summary>
/// Controller xử lý đăng ký, đăng nhập, đăng xuất, quên/đặt lại/đổi mật khẩu.
/// Controller mỏng: nghiệp vụ chính nằm trong AccountService.
/// </summary>
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IAccountService accountService,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment hostEnvironment,
        IOptions<EmailSettings> emailSettings,
        ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _signInManager = signInManager;
        _userManager = userManager;
        _hostEnvironment = hostEnvironment;
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    // =========================================================================
    // 1. ĐĂNG KÝ
    // =========================================================================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(3_000_000)]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _accountService.RegisterAsync(model);

        if (!result.Success)
        {
            // Hiển thị lỗi gắn với từng trường.
            foreach (var (field, messages) in result.Errors)
            {
                foreach (var msg in messages)
                {
                    if (string.IsNullOrEmpty(field))
                    {
                        ModelState.AddModelError(string.Empty, msg);
                    }
                    else
                    {
                        ModelState.AddModelError(field, msg);
                    }
                }
            }

            // Nếu không có lỗi gắn trường cụ thể, hiển thị thông báo chung.
            if (result.Errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }

            return View(model);
        }

        // Đăng ký thành công: hiển thị thông báo và chuyển về trang đăng nhập.
        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login), new { returnUrl });
    }

    // =========================================================================
    // 2. ĐĂNG NHẬP
    // =========================================================================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Gọi service kiểm tra thông tin đăng nhập.
        var result = await _accountService.ValidateLoginAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        var user = result.Data!;

        // Đăng nhập thông qua SignInManager (tạo cookie authentication).
        await _signInManager.SignInAsync(user, isPersistent: model.RememberMe);

        _logger.LogInformation("User {Email} đã đăng nhập thành công.", user.Email);

        // Chuyển hướng theo role.
        return RedirectAfterLogin(user, returnUrl);
    }

    // =========================================================================
    // 3. ĐĂNG XUẤT
    // =========================================================================
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userName = User.Identity?.Name;

        await _signInManager.SignOutAsync();

        _logger.LogInformation("User {UserName} đã đăng xuất.", userName);

        TempData["SuccessMessage"] = "Bạn đã đăng xuất thành công.";
        return RedirectToAction("Index", "Home");
    }

    // =========================================================================
    // 4. QUÊN MẬT KHẨU
    // =========================================================================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        // Ở môi trường phát triển, email chỉ ghi ra log nên hiển thị gợi ý cho người kiểm thử.
        ViewData["ShowDevHint"] = _hostEnvironment.IsDevelopment() && _emailSettings.UseConsoleSender;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Hàm dựng link đặt lại mật khẩu, truyền vào service.
        string BuildResetUrl(string email, string token)
            => Url.Action(nameof(ResetPassword), "Account", new { email, token }, Request.Scheme)!;

        var result = await _accountService.SendPasswordResetLinkAsync(model, BuildResetUrl);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    // =========================================================================
    // 5. ĐẶT LẠI MẬT KHẨU
    // =========================================================================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        // Link từ email đã URL-encode token, Controller tự động decode khi nhận query string.
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ.";
            return RedirectToAction(nameof(Login));
        }

        var model = new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _accountService.ResetPasswordAsync(model);

        if (!result.Success)
        {
            foreach (var (field, messages) in result.Errors)
            {
                foreach (var msg in messages)
                {
                    ModelState.AddModelError(field, msg);
                }
            }

            if (result.Errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    // =========================================================================
    // 6. ĐỔI MẬT KHẨU (đã đăng nhập)
    // =========================================================================
    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User)!;
        var result = await _accountService.ChangePasswordAsync(userId, model);

        if (!result.Success)
        {
            foreach (var (field, messages) in result.Errors)
            {
                foreach (var msg in messages)
                {
                    ModelState.AddModelError(field, msg);
                }
            }

            if (result.Errors.Count == 0)
            {
                ModelState.AddModelError(string.Empty, result.Message);
            }

            return View(model);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction("Index", "Profile");
    }

    // =========================================================================
    // 7. ACCESS DENIED (khi không đủ quyền truy cập)
    // =========================================================================
    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    // =========================================================================
    // HELPER: Chuyển hướng sau đăng nhập dựa trên role
    // =========================================================================
    private IActionResult RedirectAfterLogin(ApplicationUser user, string? returnUrl)
    {
        // Nếu có returnUrl hợp lệ (cùng domain), ưu tiên chuyển về đó.
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        // Chuyển hướng mặc định theo role.
        var roles = _userManager.GetRolesAsync(user).Result;

        if (roles.Contains(AppConstants.Roles.Admin))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (roles.Contains(AppConstants.Roles.Owner))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Owner" });
        }

        // Mặc định: Customer và các role khác về trang chủ.
        return RedirectToAction("Index", "Home");
    }
}
