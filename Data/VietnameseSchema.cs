using Microsoft.EntityFrameworkCore;

namespace SportBooking.Data;

internal static class VietnameseSchema
{
    private static readonly Dictionary<string, string> Tables = new()
    {
        ["Roles"] = "VaiTro",
        ["RoleClaims"] = "KhaiBaoVaiTro",
        ["Users"] = "NguoiDung",
        ["UserClaims"] = "KhaiBaoNguoiDung",
        ["UserLogins"] = "DangNhapNgoai",
        ["UserRoles"] = "VaiTroNguoiDung",
        ["UserTokens"] = "MaXacThucNguoiDung",
        ["BankTransfers"] = "ChuyenKhoan",
        ["Bookings"] = "DatSan",
        ["Courts"] = "San",
        ["DemoSeedStates"] = "TrangThaiNapMau",
        ["Notifications"] = "ThongBao",
        ["OwnerTransferProfiles"] = "HoSoNhanChuyenKhoan",
        ["Payments"] = "ThanhToan",
        ["Reviews"] = "DanhGia",
        ["SportComplexes"] = "CumSan",
        ["SportTypes"] = "LoaiTheThao",
        ["TimeSlots"] = "KhungGio",
        ["TransferImages"] = "AnhChuyenKhoan",
        ["TransferReports"] = "BaoCaoChuyenKhoan",
    };

    private static readonly Dictionary<string, string> Columns = new()
    {
        ["Id"] = "Ma",
        ["AccessFailedCount"] = "SoLanDangNhapThatBai",
        ["AccountName"] = "TenChuTaiKhoan",
        ["AccountNumber"] = "SoTaiKhoan",
        ["Address"] = "DiaChi",
        ["Amount"] = "SoTien",
        ["ApprovalRejectionReason"] = "LyDoTuChoiDuyet",
        ["ApprovalReviewedAt"] = "ThoiDiemXetDuyet",
        ["ApprovalReviewedById"] = "MaNguoiXetDuyet",
        ["ApprovalStatus"] = "TrangThaiDuyet",
        ["AvatarUrl"] = "DuongDanAnhDaiDien",
        ["BankCode"] = "MaNganHang",
        ["BankName"] = "TenNganHang",
        ["BankTransferGroupCode"] = "MaNhomChuyenKhoan",
        ["BookingCode"] = "MaDatSan",
        ["BookingDate"] = "NgaySuDung",
        ["BookingGroupCode"] = "MaNhomDatSan",
        ["BookingId"] = "MaLuotDat",
        ["BusinessName"] = "TenCoSoKinhDoanh",
        ["BusinessPhone"] = "DienThoaiKinhDoanh",
        ["CancellationReason"] = "LyDoHuy",
        ["CancelledAt"] = "ThoiDiemHuy",
        ["CancelledBy"] = "BenHuy",
        ["ClaimType"] = "LoaiKhaiBao",
        ["ClaimValue"] = "GiaTriKhaiBao",
        ["ClosingTime"] = "GioDongCua",
        ["Comment"] = "NhanXet",
        ["CompletedAt"] = "ThoiDiemHoanThanh",
        ["CompletedAtUtc"] = "ThoiDiemHoanThanhUtc",
        ["ComplexId"] = "MaCumSan",
        ["ConcurrencyStamp"] = "DauPhienBan",
        ["ConfirmedAt"] = "ThoiDiemXacNhan",
        ["CourtId"] = "MaSan",
        ["CreatedAt"] = "ThoiDiemTao",
        ["CustomerId"] = "MaKhachHang",
        ["DateOfBirth"] = "NgaySinh",
        ["Description"] = "MoTa",
        ["DisplayOrder"] = "ThuTuHienThi",
        ["District"] = "QuanHuyen",
        ["DurationMinutes"] = "SoPhut",
        ["Email"] = "ThuDienTu",
        ["EmailConfirmed"] = "DaXacNhanThuDienTu",
        ["Enabled"] = "DaBat",
        ["EndDateTime"] = "ThoiDiemKetThuc",
        ["EndTime"] = "GioKetThuc",
        ["FullName"] = "HoTen",
        ["GatewayTransactionId"] = "MaGiaoDichCong",
        ["GroupCode"] = "MaNhom",
        ["HandledAt"] = "ThoiDiemXuLy",
        ["HandledById"] = "MaNguoiXuLy",
        ["HandlingNote"] = "GhiChuXuLy",
        ["IconClass"] = "LopBieuTuong",
        ["ImageUrl"] = "DuongDanAnh",
        ["IsActive"] = "DangHoatDong",
        ["IsDeleted"] = "DaXoaMem",
        ["IsRead"] = "DaDoc",
        ["IsVisible"] = "DangHienThi",
        ["Key"] = "Khoa",
        ["LastReportedAt"] = "LanBaoCuoi",
        ["LockoutEnabled"] = "ChoPhepKhoa",
        ["LockoutEnd"] = "HanKhoa",
        ["LoginProvider"] = "NhaCungCapDangNhap",
        ["ManualRefundNote"] = "GhiChuHoanThuCong",
        ["ManualRefundedAt"] = "ThoiDiemHoanThuCong",
        ["ManualRefundedById"] = "MaNguoiHoanThuCong",
        ["Message"] = "NoiDung",
        ["Method"] = "PhuongThuc",
        ["Name"] = "Ten",
        ["NormalizedEmail"] = "ThuDienTuChuanHoa",
        ["NormalizedName"] = "TenChuanHoa",
        ["NormalizedUserName"] = "TenDangNhapChuanHoa",
        ["Note"] = "GhiChu",
        ["OpeningTime"] = "GioMoCua",
        ["Outcome"] = "KetQuaXuLy",
        ["OwnerId"] = "MaChuSan",
        ["OwnerReply"] = "PhanHoiChuSan",
        ["OwnerReplyAt"] = "ThoiDiemPhanHoi",
        ["PaidAt"] = "ThoiDiemThuTien",
        ["PasswordHash"] = "MatKhauBam",
        ["PaymentDeadline"] = "HanThanhToan",
        ["PaymentMethod"] = "PhuongThucThanhToan",
        ["PaymentStatus"] = "TrangThaiThanhToan",
        ["PhoneNumber"] = "SoDienThoai",
        ["PhoneNumberConfirmed"] = "DaXacNhanDienThoai",
        ["Png"] = "DuLieuAnhPng",
        ["PriceHoliday"] = "GiaNgayLe",
        ["PriceWeekday"] = "GiaNgayThuong",
        ["PriceWeekend"] = "GiaCuoiTuan",
        ["ProviderDisplayName"] = "TenHienThiNhaCungCap",
        ["ProviderKey"] = "KhoaNhaCungCap",
        ["Province"] = "TinhThanh",
        ["QrImageId"] = "MaAnhQr",
        ["Rating"] = "SoSao",
        ["RawData"] = "DuLieuGoc",
        ["ReadAt"] = "ThoiDiemDoc",
        ["ReceiptId"] = "MaAnhBienLai",
        ["Reference"] = "MaThamChieu",
        ["ReportDeadline"] = "HanBaoChuyenKhoan",
        ["ReportedAt"] = "ThoiDiemBao",
        ["ResponseCode"] = "MaPhanHoi",
        ["ResponseMessage"] = "NoiDungPhanHoi",
        ["RoleId"] = "MaVaiTro",
        ["RowVersion"] = "PhienBanDong",
        ["SecurityStamp"] = "DauBaoMat",
        ["SportComplexId"] = "MaCumSan",
        ["SportTypeId"] = "MaLoaiTheThao",
        ["StartDateTime"] = "ThoiDiemBatDau",
        ["StartTime"] = "GioBatDau",
        ["State"] = "TrangThai",
        ["Status"] = "TrangThai",
        ["TimeSlotId"] = "MaKhungGio",
        ["Title"] = "TieuDe",
        ["TotalAmount"] = "TongTien",
        ["TransactionCode"] = "MaGiaoDich",
        ["TwoFactorEnabled"] = "BatXacThucHaiBuoc",
        ["Type"] = "Loai",
        ["UpdatedAt"] = "ThoiDiemCapNhat",
        ["Url"] = "DuongDan",
        ["UserId"] = "MaNguoiDung",
        ["UserName"] = "TenDangNhap",
        ["Value"] = "GiaTri",
    };

    internal static void Configure(ModelBuilder builder)
    {
        // Identity also exposes model types that do not have their own migrated tables.
        var entities = builder.Model.GetEntityTypes()
            .Where(entity => entity.GetTableName() is { } table && Tables.ContainsKey(table))
            .ToArray();
        // Preserve object names for existing keys and indexes while localizing tables and columns.
        foreach (var entity in entities)
        {
            foreach (var key in entity.GetKeys()) key.SetName(key.GetName());
            foreach (var foreignKey in entity.GetForeignKeys()) foreignKey.SetConstraintName(foreignKey.GetConstraintName());
            foreach (var index in entity.GetIndexes()) index.SetDatabaseName(index.GetDatabaseName());
        }

        foreach (var entity in entities)
        {
            var table = entity.GetTableName()!;
            entity.SetTableName(Tables[table]);
            foreach (var property in entity.GetProperties()) property.SetColumnName(Columns[property.Name]);
        }
    }
}
