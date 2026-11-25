using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SportBooking.Models.ViewModels.Account;

public sealed class TransferProfileInput
{
    [Display(Name = "Nhận chuyển khoản qua QR")]
    public bool Enabled { get; set; }
    [Display(Name = "Ngân hàng")]
    public string? BankName { get; set; }
    [Display(Name = "Số tài khoản")]
    public string? AccountNumber { get; set; }
    [Display(Name = "Tên chủ tài khoản")]
    public string? AccountName { get; set; }
    [Display(Name = "Ảnh mã QR")]
    public IFormFile? Qr { get; set; }
    [Display(Name = "Tôi đã kiểm tra mã QR dẫn đến đúng tài khoản ghi trên biểu mẫu.")]
    public bool Confirmed { get; set; }
    [BindNever] public bool HasCurrentQr { get; set; }
}
