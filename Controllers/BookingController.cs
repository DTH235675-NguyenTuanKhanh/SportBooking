// File: Controllers/BookingController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SportBooking.Constants;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Booking;
using SportBooking.Services.Interfaces;

namespace SportBooking.Controllers;

/// <summary>
/// Controller xử lý đặt sân và quản lý booking của khách hàng.
/// Toàn bộ action yêu cầu đăng nhập với role Customer.
/// </summary>
[Authorize(Roles = AppConstants.Roles.Customer)]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<BookingController> _logger;

    public BookingController(
        IBookingService bookingService,
        UserManager<ApplicationUser> userManager,
        ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _userManager = userManager;
        _logger = logger;
    }

    // =========================================================================
    // 1. TRANG ĐẶT SÂN
    // =========================================================================
    /// <summary>
    /// GET: /Booking/Create?courtId=5&amp;bookingDate=2026-01-01
    /// Hiển thị lưới chọn khung giờ và form đặt sân.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Create(int courtId, DateTime? bookingDate = null, string? returnUrl = null)
    {
        ViewData["SearchReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Search", "Court");
        if (courtId <= 0)
        {
            TempData[AppConstants.TempDataKeys.Error] = "Sân không hợp lệ.";
            return RedirectToAction("Search", "Court");
        }

        var result = await _bookingService.PrepareBookingFormAsync(courtId, bookingDate);

        if (!result.Success)
        {
            TempData[AppConstants.TempDataKeys.Error] = result.Message;
            return RedirectToAction("Detail", "Court", new { id = courtId });
        }

        return View(result.Data);
    }

    /// <summary>
    /// POST: /Booking/Create
    /// Xử lý đặt sân.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel model, string? returnUrl = null)
    {
        ViewData["SearchReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Search", "Court");
        var customerId = _userManager.GetUserId(User);
        if (string.IsNullOrEmpty(customerId))
        {
            return RedirectToAction("Login", "Account");
        }

        // Xóa các lỗi validation của các trường chỉ dùng để hiển thị (không gửi từ form).
        ModelState.Remove(nameof(model.CourtName));
        ModelState.Remove(nameof(model.ComplexName));

        if (!ModelState.IsValid)
        {
            // Nạp lại lưới khung giờ để hiển thị lại form kèm lỗi.
            var prepared = await _bookingService.PrepareBookingFormAsync(model.CourtId, model.BookingDate);
            if (prepared.Success)
            {
                model.CourtName = prepared.Data!.CourtName;
                model.ComplexName = prepared.Data!.ComplexName;
                model.ComplexAddress = prepared.Data!.ComplexAddress;
                model.SportTypeName = prepared.Data!.SportTypeName;
                model.CourtImageUrl = prepared.Data!.CourtImageUrl;
                model.TimeSlots = prepared.Data!.TimeSlots;
                model.TransferAvailable = prepared.Data.TransferAvailable;
            }

            return View(model);
        }

        var result = await _bookingService.CreateBookingAsync(customerId, model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            var prepared = await _bookingService.PrepareBookingFormAsync(model.CourtId, model.BookingDate);
            if (prepared.Success)
            {
                model.CourtName = prepared.Data!.CourtName;
                model.ComplexName = prepared.Data!.ComplexName;
                model.ComplexAddress = prepared.Data!.ComplexAddress;
                model.SportTypeName = prepared.Data!.SportTypeName;
                model.CourtImageUrl = prepared.Data!.CourtImageUrl;
                model.TimeSlots = prepared.Data!.TimeSlots;
                model.TransferAvailable = prepared.Data.TransferAvailable;
            }

            return View(model);
        }

        var created = result.Data!;

        if (created.PaymentMethod == "BankTransfer")
            return RedirectToAction("Details", "BankTransfer", new { code = created.BookingGroupCode });

        // Thanh toán tại quầy: chuyển tới trang xác nhận kết quả.
        TempData[AppConstants.TempDataKeys.Success] = result.Message;
        return RedirectToAction(nameof(Confirmation), new { groupCode = created.BookingGroupCode });
    }

    /// <summary>
    /// GET: /Booking/Confirmation?groupCode=GRP20260101-0001
    /// Trang xác nhận kết quả đặt sân.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Confirmation(string groupCode)
    {
        if (string.IsNullOrWhiteSpace(groupCode))
        {
            return RedirectToAction(nameof(MyBookings));
        }

        var customerId = _userManager.GetUserId(User)!;

        // Lấy danh sách booking theo mã nhóm để hiển thị toàn bộ khung giờ đã đặt.
        var filter = new BookingListViewModel
        {
            Keyword = groupCode,
            Page = 1,
            PageSize = 20
        };

        var list = await _bookingService.GetCustomerBookingsAsync(customerId, filter);

        if (list.Bookings.Count == 0)
        {
            TempData[AppConstants.TempDataKeys.Error] = "Không tìm thấy thông tin đặt sân.";
            return RedirectToAction(nameof(MyBookings));
        }

        ViewBag.GroupCode = groupCode;
        return View(list.Bookings);
    }

    // =========================================================================
    // 2. API: LẤY TRẠNG THÁI KHUNG GIỜ (AJAX)
    // =========================================================================
    /// <summary>
    /// GET: /Booking/GetTimeSlots?courtId=5&amp;date=2026-01-01
    /// Trả về JSON trạng thái các khung giờ để render lại lưới chọn giờ bằng AJAX.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTimeSlots(int courtId, string date)
    {
        if (courtId <= 0 || !DateTime.TryParse(date, out var bookingDate))
        {
            return BadRequest(new { success = false, message = "Tham số không hợp lệ." });
        }

        var result = await _bookingService.GetTimeSlotAvailabilityAsync(courtId, bookingDate);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.Message });
        }

        // Trả về dữ liệu tối giản để client render lưới khung giờ.
        var slots = result.Data!.Select(s => new
        {
            id = s.TimeSlotId,
            quote = s.Quote,
            startTime = s.StartTime.ToString(@"hh\:mm"),
            endTime = s.EndTime.ToString(@"hh\:mm"),
            timeRange = s.TimeRangeText,
            price = s.Price,
            priceFormatted = s.Price.ToString("#,##0") + " ₫",
            status = (int)s.Status,
            statusClass = s.StatusClass,
            selectable = s.IsSelectable
        });

        return Json(new { success = true, slots });
    }

    // =========================================================================
    // 3. LỊCH SỬ ĐẶT SÂN
    // =========================================================================
    /// <summary>
    /// GET: /Booking/MyBookings
    /// Danh sách booking của khách hàng với bộ lọc và phân trang.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> MyBookings([FromQuery] BookingListViewModel filter)
    {
        var customerId = _userManager.GetUserId(User)!;

        var result = await _bookingService.GetCustomerBookingsAsync(customerId, filter);
        return View(result);
    }

    // =========================================================================
    // 4. CHI TIẾT BOOKING
    // =========================================================================
    /// <summary>
    /// GET: /Booking/Detail/5
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        var customerId = _userManager.GetUserId(User)!;

        // isOwnerOrAdmin = false: khách chỉ xem được booking của chính mình.
        var result = await _bookingService.GetBookingDetailAsync(id, customerId, isOwnerOrAdmin: false);

        if (!result.Success)
        {
            TempData[AppConstants.TempDataKeys.Error] = result.Message;
            return RedirectToAction(nameof(MyBookings));
        }

        return View(result.Data);
    }

    // =========================================================================
    // 5. HỦY BOOKING
    // =========================================================================
    /// <summary>
    /// GET: /Booking/Cancel/5
    /// Form nhập lý do hủy.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Cancel(int id)
    {
        var customerId = _userManager.GetUserId(User)!;

        var result = await _bookingService.PrepareCancelFormAsync(id, customerId);

        if (!result.Success)
        {
            TempData[AppConstants.TempDataKeys.Error] = result.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }

        return View(result.Data);
    }

    /// <summary>
    /// POST: /Booking/Cancel
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(BookingCancelViewModel model)
    {
        var customerId = _userManager.GetUserId(User)!;

        if (!ModelState.IsValid)
        {
            // Nạp lại thông tin hiển thị để render lại form.
            var prepared = await _bookingService.PrepareCancelFormAsync(model.BookingId, customerId);
            if (prepared.Success)
            {
                model.BookingCode = prepared.Data!.BookingCode;
                model.CourtName = prepared.Data!.CourtName;
                model.ComplexName = prepared.Data!.ComplexName;
                model.BookingDate = prepared.Data!.BookingDate;
                model.StartTime = prepared.Data!.StartTime;
                model.EndTime = prepared.Data!.EndTime;
                model.TotalAmount = prepared.Data!.TotalAmount;
                model.PaymentStatus = prepared.Data!.PaymentStatus;
                model.MinHoursBefore = prepared.Data!.MinHoursBefore;
            }

            return View(model);
        }

        var result = await _bookingService.CancelBookingAsync(
            model.BookingId,
            customerId,
            model.CancellationReason);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            var prepared = await _bookingService.PrepareCancelFormAsync(model.BookingId, customerId);
            if (prepared.Success)
            {
                model.BookingCode = prepared.Data!.BookingCode;
                model.CourtName = prepared.Data!.CourtName;
                model.ComplexName = prepared.Data!.ComplexName;
                model.BookingDate = prepared.Data!.BookingDate;
                model.StartTime = prepared.Data!.StartTime;
                model.EndTime = prepared.Data!.EndTime;
                model.TotalAmount = prepared.Data!.TotalAmount;
                model.PaymentStatus = prepared.Data!.PaymentStatus;
                model.MinHoursBefore = prepared.Data!.MinHoursBefore;
            }

            return View(model);
        }

        TempData[AppConstants.TempDataKeys.Success] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = model.BookingId });
    }

    // =========================================================================
    // 6. ĐÁNH GIÁ
    // =========================================================================
    /// <summary>
    /// GET: /Booking/Review/5
    /// Form đánh giá sân.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        var customerId = _userManager.GetUserId(User)!;

        var result = await _bookingService.PrepareReviewFormAsync(id, customerId);

        if (!result.Success)
        {
            TempData[AppConstants.TempDataKeys.Error] = result.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }

        return View(result.Data);
    }

    /// <summary>
    /// POST: /Booking/Review
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(ReviewCreateViewModel model)
    {
        var customerId = _userManager.GetUserId(User)!;

        // Xóa lỗi của các trường chỉ dùng để hiển thị.
        ModelState.Remove(nameof(model.CourtName));
        ModelState.Remove(nameof(model.BookingCode));

        if (!ModelState.IsValid)
        {
            var prepared = await _bookingService.PrepareReviewFormAsync(model.BookingId, customerId);
            if (prepared.Success)
            {
                model.CourtName = prepared.Data!.CourtName;
                model.CourtImageUrl = prepared.Data!.CourtImageUrl;
                model.BookingCode = prepared.Data!.BookingCode;
                model.BookingDate = prepared.Data!.BookingDate;
                model.StartTime = prepared.Data!.StartTime;
                model.EndTime = prepared.Data!.EndTime;
            }

            return View(model);
        }

        var result = await _bookingService.SubmitReviewAsync(customerId, model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);

            var prepared = await _bookingService.PrepareReviewFormAsync(model.BookingId, customerId);
            if (prepared.Success)
            {
                model.CourtName = prepared.Data!.CourtName;
                model.CourtImageUrl = prepared.Data!.CourtImageUrl;
                model.BookingCode = prepared.Data!.BookingCode;
                model.BookingDate = prepared.Data!.BookingDate;
                model.StartTime = prepared.Data!.StartTime;
                model.EndTime = prepared.Data!.EndTime;
            }

            return View(model);
        }

        TempData[AppConstants.TempDataKeys.Success] = result.Message;
        return RedirectToAction(nameof(Detail), new { id = model.BookingId });
    }
}
