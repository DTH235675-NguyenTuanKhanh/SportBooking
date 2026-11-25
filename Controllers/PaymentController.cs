using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Services.Interfaces;

namespace SportBooking.Controllers;

[Authorize(Roles = AppConstants.Roles.Customer)]
public sealed class PaymentController : Controller
{
    private readonly IVnPayService _vnPay;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(IVnPayService vnPay, UserManager<ApplicationUser> users, ILogger<PaymentController> logger)
    {
        _vnPay = vnPay;
        _users = users;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> VnPayReturn()
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        // Return URL chỉ hiển thị kết quả. Chỉ IPN có chữ ký mới cập nhật trạng thái giao dịch.
        if (!User.IsInRole(AppConstants.Roles.Customer) || _users.GetUserId(User) is not { } customerId)
            return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("MyBookings", "Booking") });

        var result = await _vnPay.GetReturnBookingAsync(customerId, Request.Query);
        if (!result.Success)
        {
            TempData[AppConstants.TempDataKeys.Error] = result.Message;
            return RedirectToAction("MyBookings", "Booking");
        }
        TempData[AppConstants.TempDataKeys.Info] = "Đã nhận kết quả từ VNPay. Trạng thái được cập nhật khi cổng thanh toán xác nhận giao dịch.";
        return RedirectToAction("Detail", "Booking", new { id = result.Data });
    }

    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> VnPayIpn(CancellationToken cancellationToken)
    {
        VnPayNotificationResult result;
        try
        {
            result = await _vnPay.ProcessNotificationAsync(Request.Query, cancellationToken);
            if (result.RspCode is "00" or "02")
                _logger.LogInformation("VNPay IPN: {RspCode}, request {RequestId}, UTC {ReceivedAt}.",
                    result.RspCode, HttpContext.TraceIdentifier, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể xử lý IPN VNPay.");
            result = new("99", "Processing error");
        }
        return new JsonResult(result, new JsonSerializerOptions { PropertyNamingPolicy = null });
    }
}
