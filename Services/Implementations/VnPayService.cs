using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SportBooking.Data;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Interfaces;

namespace SportBooking.Services.Implementations;

/// <summary>Đối soát IPN/Return cho giao dịch VNPay phát sinh trước khi chuyển sang QR.</summary>
public sealed class VnPayService : IVnPayService
{
    private readonly ApplicationDbContext _context;
    private readonly VnPaySettings _settings;

    public VnPayService(ApplicationDbContext context, IOptions<VnPaySettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    private bool HasCredentials => !string.IsNullOrWhiteSpace(_settings.TmnCode)
        && !string.IsNullOrWhiteSpace(_settings.HashSecret);

    private static string CanonicalData(IEnumerable<KeyValuePair<string, string>> values) => string.Join("&",
        values.Where(p => !string.IsNullOrEmpty(p.Value)).OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));

    private string Sign(string data) => Convert.ToHexStringLower(
        HMACSHA512.HashData(Encoding.UTF8.GetBytes(_settings.HashSecret), Encoding.UTF8.GetBytes(data)));

    private bool HasValidSignature(IQueryCollection query)
    {
        // Tắt nhận đơn online mới vẫn phải tiếp nhận kết quả các giao dịch đã tạo.
        if (!HasCredentials || query.Any(p => p.Key.StartsWith("vnp_", StringComparison.Ordinal) && p.Value.Count != 1))
            return false;
        var received = query["vnp_SecureHash"].ToString();
        if (received.Length != 128) return false;
        var fields = query.Where(p => p.Key.StartsWith("vnp_", StringComparison.Ordinal)
                                      && p.Key is not ("vnp_SecureHash" or "vnp_SecureHashType"))
            .Select(p => new KeyValuePair<string, string>(p.Key, p.Value.ToString()));
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(received),
                Convert.FromHexString(Sign(CanonicalData(fields))));
        }
        catch (FormatException) { return false; }
    }

    public async Task<VnPayNotificationResult> ProcessNotificationAsync(IQueryCollection query, CancellationToken cancellationToken)
    {
        if (!HasValidSignature(query) || query["vnp_TmnCode"] != _settings.TmnCode)
            return new("97", "Invalid signature");
        var reference = query["vnp_TxnRef"].ToString();
        if (!Guid.TryParseExact(reference, "N", out _)) return new("01", "Order not found");
        var amount = query["vnp_Amount"].ToString();
        if (amount.Length > 12 || !long.TryParse(amount, NumberStyles.None, CultureInfo.InvariantCulture, out var cents) || cents <= 0)
            return new("04", "Invalid amount");
        var responseCode = query["vnp_ResponseCode"].ToString();
        var transactionStatus = query["vnp_TransactionStatus"].ToString();
        var transactionNo = query["vnp_TransactionNo"].ToString();
        var bankCode = query["vnp_BankCode"].ToString();
        var orderInfo = query["vnp_OrderInfo"].ToString();
        var success = responseCode == "00" && transactionStatus == "00";
        if (responseCode.Length != 2 || !responseCode.All(char.IsAsciiDigit)
            || transactionStatus.Length != 2 || !transactionStatus.All(char.IsAsciiDigit)
            || transactionNo.Length is < 1 or > 15 || !transactionNo.All(char.IsAsciiDigit)
            || (success && transactionNo.All(c => c == '0'))
            || bankCode.Length is < 3 or > 20 || !bankCode.All(char.IsAsciiLetterOrDigit)
            || string.IsNullOrWhiteSpace(orderInfo) || orderInfo.Length > 255)
            return new("99", "Invalid response");

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            _context.ChangeTracker.Clear();
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            var prefix = reference + "-";
            var payments = await _context.Payments.Include(p => p.Booking)
                .Where(p => p.Method == PaymentMethod.VnPay && p.TransactionCode.StartsWith(prefix))
                .OrderBy(p => p.Id).ToListAsync(cancellationToken);
            if (payments.Count == 0) return new VnPayNotificationResult("01", "Order not found");
            if (payments.Sum(p => p.Amount) * 100 != cents || payments.Any(p => p.Amount != p.Booking.TotalAmount))
                return new VnPayNotificationResult("04", "Invalid amount");
            var first = payments[0].Booking;
            if (payments.Select(p => p.BookingId).Distinct().Count() != payments.Count
                || payments.Any(p => p.Booking.PaymentMethod != PaymentMethod.VnPay
                    || p.Booking.BookingGroupCode != first.BookingGroupCode || p.Booking.CustomerId != first.CustomerId
                    || p.Booking.CourtId != first.CourtId || p.Booking.BookingDate != first.BookingDate))
                return new VnPayNotificationResult("99", "Invalid booking group");
            if (payments.Any(p => p.Status is PaymentStatus.Paid or PaymentStatus.RefundPending or PaymentStatus.Refunded
                                  || (p.Status == PaymentStatus.Failed && p.ResponseCode != "EXPIRED")))
                return new VnPayNotificationResult("02", "Order already processed");

            var now = SportBooking.Helpers.ScheduleGuard.Now;
            var recordedAt = DateTime.UtcNow;
            var bookings = payments.Select(p => p.Booking).ToList();
            var canConfirm = bookings.All(b => b.Status == BookingStatus.Pending && b.PaymentStatus == PaymentStatus.Unpaid
                && b.PaymentDeadline.HasValue && b.PaymentDeadline > now && b.StartDateTime > now);
            foreach (var payment in payments)
            {
                payment.GatewayTransactionId = transactionNo;
                payment.BankCode = bankCode;
                payment.ResponseCode = responseCode;
                payment.ResponseMessage = success ? "VNPay xác nhận thanh toán thành công." : "Giao dịch VNPay không thành công.";
                payment.UpdatedAt = recordedAt;
                payment.Status = success ? (canConfirm ? PaymentStatus.Paid : PaymentStatus.RefundPending) : PaymentStatus.Failed;
                payment.PaidAt = success ? recordedAt : null;
                var booking = payment.Booking;
                booking.PaymentStatus = payment.Status;
                booking.UpdatedAt = recordedAt;
                if (success && canConfirm)
                {
                    booking.Status = BookingStatus.Confirmed;
                    booking.ConfirmedAt = recordedAt;
                }
                else
                {
                    // Không khôi phục chỗ đã hết hạn: chỗ đó có thể đã được khách khác đặt.
                    booking.Status = BookingStatus.Expired;
                    booking.CancelledAt ??= recordedAt;
                    booking.CancelledBy = "System";
                    booking.CancellationReason = success
                        ? "Nhận thanh toán sau khi hết hạn giữ chỗ; cần hoàn tiền."
                        : "Thanh toán VNPay không thành công.";
                }
            }
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new VnPayNotificationResult("00", "Confirm success");
        });
    }

    public async Task<ServiceResult<int>> GetReturnBookingAsync(string customerId, IQueryCollection query)
    {
        if (!HasValidSignature(query) || query["vnp_TmnCode"] != _settings.TmnCode)
            return ServiceResult<int>.Fail("Kết quả thanh toán không hợp lệ.");
        var reference = query["vnp_TxnRef"].ToString();
        if (!Guid.TryParseExact(reference, "N", out _)) return ServiceResult<int>.Fail("Không tìm thấy giao dịch.");
        var prefix = reference + "-";
        var id = await _context.Payments.AsNoTracking()
            .Where(p => p.Method == PaymentMethod.VnPay && p.TransactionCode.StartsWith(prefix)
                        && p.Booking.CustomerId == customerId)
            .Select(p => (int?)p.BookingId).FirstOrDefaultAsync();
        return id.HasValue ? ServiceResult<int>.Ok(id.Value)
            : ServiceResult<int>.Fail("Không tìm thấy giao dịch thuộc tài khoản của bạn.");
    }
}
