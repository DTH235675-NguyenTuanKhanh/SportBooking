using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportBooking.Data.Migrations
{
    /// <inheritdoc />
    public partial class VietHoaBangVaCot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "Roles");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "UX_Bookings_Court_Slot_Date_Active",
                table: "Bookings");

            migrationBuilder.RenameTable(
                name: "UserTokens",
                newName: "MaXacThucNguoiDung");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "NguoiDung");

            migrationBuilder.RenameTable(
                name: "UserRoles",
                newName: "VaiTroNguoiDung");

            migrationBuilder.RenameTable(
                name: "UserLogins",
                newName: "DangNhapNgoai");

            migrationBuilder.RenameTable(
                name: "UserClaims",
                newName: "KhaiBaoNguoiDung");

            migrationBuilder.RenameTable(
                name: "TransferReports",
                newName: "BaoCaoChuyenKhoan");

            migrationBuilder.RenameTable(
                name: "TransferImages",
                newName: "AnhChuyenKhoan");

            migrationBuilder.RenameTable(
                name: "TimeSlots",
                newName: "KhungGio");

            migrationBuilder.RenameTable(
                name: "SportTypes",
                newName: "LoaiTheThao");

            migrationBuilder.RenameTable(
                name: "SportComplexes",
                newName: "CumSan");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "VaiTro");

            migrationBuilder.RenameTable(
                name: "RoleClaims",
                newName: "KhaiBaoVaiTro");

            migrationBuilder.RenameTable(
                name: "Reviews",
                newName: "DanhGia");

            migrationBuilder.RenameTable(
                name: "Payments",
                newName: "ThanhToan");

            migrationBuilder.RenameTable(
                name: "OwnerTransferProfiles",
                newName: "HoSoNhanChuyenKhoan");

            migrationBuilder.RenameTable(
                name: "Notifications",
                newName: "ThongBao");

            migrationBuilder.RenameTable(
                name: "DemoSeedStates",
                newName: "TrangThaiNapMau");

            migrationBuilder.RenameTable(
                name: "Courts",
                newName: "San");

            migrationBuilder.RenameTable(
                name: "Bookings",
                newName: "DatSan");

            migrationBuilder.RenameTable(
                name: "BankTransfers",
                newName: "ChuyenKhoan");

            migrationBuilder.RenameColumn(
                name: "Value",
                table: "MaXacThucNguoiDung",
                newName: "GiaTri");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "MaXacThucNguoiDung",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "MaXacThucNguoiDung",
                newName: "NhaCungCapDangNhap");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "MaXacThucNguoiDung",
                newName: "MaNguoiDung");

            migrationBuilder.RenameColumn(
                name: "UserName",
                table: "NguoiDung",
                newName: "TenDangNhap");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "NguoiDung",
                newName: "ThoiDiemCapNhat");

            migrationBuilder.RenameColumn(
                name: "TwoFactorEnabled",
                table: "NguoiDung",
                newName: "BatXacThucHaiBuoc");

            migrationBuilder.RenameColumn(
                name: "SecurityStamp",
                table: "NguoiDung",
                newName: "DauBaoMat");

            migrationBuilder.RenameColumn(
                name: "PhoneNumberConfirmed",
                table: "NguoiDung",
                newName: "DaXacNhanDienThoai");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "NguoiDung",
                newName: "SoDienThoai");

            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "NguoiDung",
                newName: "MatKhauBam");

            migrationBuilder.RenameColumn(
                name: "NormalizedUserName",
                table: "NguoiDung",
                newName: "TenDangNhapChuanHoa");

            migrationBuilder.RenameColumn(
                name: "NormalizedEmail",
                table: "NguoiDung",
                newName: "ThuDienTuChuanHoa");

            migrationBuilder.RenameColumn(
                name: "LockoutEnd",
                table: "NguoiDung",
                newName: "HanKhoa");

            migrationBuilder.RenameColumn(
                name: "LockoutEnabled",
                table: "NguoiDung",
                newName: "ChoPhepKhoa");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "NguoiDung",
                newName: "DangHoatDong");

            migrationBuilder.RenameColumn(
                name: "FullName",
                table: "NguoiDung",
                newName: "HoTen");

            migrationBuilder.RenameColumn(
                name: "EmailConfirmed",
                table: "NguoiDung",
                newName: "DaXacNhanThuDienTu");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "NguoiDung",
                newName: "ThuDienTu");

            migrationBuilder.RenameColumn(
                name: "DateOfBirth",
                table: "NguoiDung",
                newName: "NgaySinh");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "NguoiDung",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "NguoiDung",
                newName: "DauPhienBan");

            migrationBuilder.RenameColumn(
                name: "BusinessPhone",
                table: "NguoiDung",
                newName: "DienThoaiKinhDoanh");

            migrationBuilder.RenameColumn(
                name: "BusinessName",
                table: "NguoiDung",
                newName: "TenCoSoKinhDoanh");

            migrationBuilder.RenameColumn(
                name: "AvatarUrl",
                table: "NguoiDung",
                newName: "DuongDanAnhDaiDien");

            migrationBuilder.RenameColumn(
                name: "ApprovalStatus",
                table: "NguoiDung",
                newName: "TrangThaiDuyet");

            migrationBuilder.RenameColumn(
                name: "ApprovalReviewedById",
                table: "NguoiDung",
                newName: "MaNguoiXetDuyet");

            migrationBuilder.RenameColumn(
                name: "ApprovalReviewedAt",
                table: "NguoiDung",
                newName: "ThoiDiemXetDuyet");

            migrationBuilder.RenameColumn(
                name: "ApprovalRejectionReason",
                table: "NguoiDung",
                newName: "LyDoTuChoiDuyet");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "NguoiDung",
                newName: "DiaChi");

            migrationBuilder.RenameColumn(
                name: "AccessFailedCount",
                table: "NguoiDung",
                newName: "SoLanDangNhapThatBai");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "NguoiDung",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "VaiTroNguoiDung",
                newName: "MaVaiTro");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "VaiTroNguoiDung",
                newName: "MaNguoiDung");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "DangNhapNgoai",
                newName: "MaNguoiDung");

            migrationBuilder.RenameColumn(
                name: "ProviderDisplayName",
                table: "DangNhapNgoai",
                newName: "TenHienThiNhaCungCap");

            migrationBuilder.RenameColumn(
                name: "ProviderKey",
                table: "DangNhapNgoai",
                newName: "KhoaNhaCungCap");

            migrationBuilder.RenameColumn(
                name: "LoginProvider",
                table: "DangNhapNgoai",
                newName: "NhaCungCapDangNhap");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "KhaiBaoNguoiDung",
                newName: "MaNguoiDung");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "KhaiBaoNguoiDung",
                newName: "GiaTriKhaiBao");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "KhaiBaoNguoiDung",
                newName: "LoaiKhaiBao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "KhaiBaoNguoiDung",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "ReportedAt",
                table: "BaoCaoChuyenKhoan",
                newName: "ThoiDiemBao");

            migrationBuilder.RenameColumn(
                name: "ReceiptId",
                table: "BaoCaoChuyenKhoan",
                newName: "MaAnhBienLai");

            migrationBuilder.RenameColumn(
                name: "Outcome",
                table: "BaoCaoChuyenKhoan",
                newName: "KetQuaXuLy");

            migrationBuilder.RenameColumn(
                name: "HandlingNote",
                table: "BaoCaoChuyenKhoan",
                newName: "GhiChuXuLy");

            migrationBuilder.RenameColumn(
                name: "HandledById",
                table: "BaoCaoChuyenKhoan",
                newName: "MaNguoiXuLy");

            migrationBuilder.RenameColumn(
                name: "HandledAt",
                table: "BaoCaoChuyenKhoan",
                newName: "ThoiDiemXuLy");

            migrationBuilder.RenameColumn(
                name: "GroupCode",
                table: "BaoCaoChuyenKhoan",
                newName: "MaNhom");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "BaoCaoChuyenKhoan",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "Png",
                table: "AnhChuyenKhoan",
                newName: "DuLieuAnhPng");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "AnhChuyenKhoan",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "KhungGio",
                newName: "ThoiDiemCapNhat");

            migrationBuilder.RenameColumn(
                name: "StartTime",
                table: "KhungGio",
                newName: "GioBatDau");

            migrationBuilder.RenameColumn(
                name: "PriceWeekend",
                table: "KhungGio",
                newName: "GiaCuoiTuan");

            migrationBuilder.RenameColumn(
                name: "PriceWeekday",
                table: "KhungGio",
                newName: "GiaNgayThuong");

            migrationBuilder.RenameColumn(
                name: "PriceHoliday",
                table: "KhungGio",
                newName: "GiaNgayLe");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "KhungGio",
                newName: "DangHoatDong");

            migrationBuilder.RenameColumn(
                name: "EndTime",
                table: "KhungGio",
                newName: "GioKetThuc");

            migrationBuilder.RenameColumn(
                name: "DurationMinutes",
                table: "KhungGio",
                newName: "SoPhut");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "KhungGio",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "CourtId",
                table: "KhungGio",
                newName: "MaSan");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "KhungGio",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "LoaiTheThao",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "LoaiTheThao",
                newName: "DangHoatDong");

            migrationBuilder.RenameColumn(
                name: "IconClass",
                table: "LoaiTheThao",
                newName: "LopBieuTuong");

            migrationBuilder.RenameColumn(
                name: "DisplayOrder",
                table: "LoaiTheThao",
                newName: "ThuTuHienThi");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "LoaiTheThao",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "LoaiTheThao",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "LoaiTheThao",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "CumSan",
                newName: "ThoiDiemCapNhat");

            migrationBuilder.RenameColumn(
                name: "Province",
                table: "CumSan",
                newName: "TinhThanh");

            migrationBuilder.RenameColumn(
                name: "PhoneNumber",
                table: "CumSan",
                newName: "SoDienThoai");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "CumSan",
                newName: "MaChuSan");

            migrationBuilder.RenameColumn(
                name: "OpeningTime",
                table: "CumSan",
                newName: "GioMoCua");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "CumSan",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "CumSan",
                newName: "DangHoatDong");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "CumSan",
                newName: "DuongDanAnh");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "CumSan",
                newName: "ThuDienTu");

            migrationBuilder.RenameColumn(
                name: "District",
                table: "CumSan",
                newName: "QuanHuyen");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "CumSan",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "CumSan",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "ClosingTime",
                table: "CumSan",
                newName: "GioDongCua");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "CumSan",
                newName: "DiaChi");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "CumSan",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "NormalizedName",
                table: "VaiTro",
                newName: "TenChuanHoa");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "VaiTro",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "ConcurrencyStamp",
                table: "VaiTro",
                newName: "DauPhienBan");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "VaiTro",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "KhaiBaoVaiTro",
                newName: "MaVaiTro");

            migrationBuilder.RenameColumn(
                name: "ClaimValue",
                table: "KhaiBaoVaiTro",
                newName: "GiaTriKhaiBao");

            migrationBuilder.RenameColumn(
                name: "ClaimType",
                table: "KhaiBaoVaiTro",
                newName: "LoaiKhaiBao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "KhaiBaoVaiTro",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "DanhGia",
                newName: "ThoiDiemCapNhat");

            migrationBuilder.RenameColumn(
                name: "Rating",
                table: "DanhGia",
                newName: "SoSao");

            migrationBuilder.RenameColumn(
                name: "OwnerReplyAt",
                table: "DanhGia",
                newName: "ThoiDiemPhanHoi");

            migrationBuilder.RenameColumn(
                name: "OwnerReply",
                table: "DanhGia",
                newName: "PhanHoiChuSan");

            migrationBuilder.RenameColumn(
                name: "IsVisible",
                table: "DanhGia",
                newName: "DangHienThi");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "DanhGia",
                newName: "MaKhachHang");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "DanhGia",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "CourtId",
                table: "DanhGia",
                newName: "MaSan");

            migrationBuilder.RenameColumn(
                name: "Comment",
                table: "DanhGia",
                newName: "NhanXet");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "DanhGia",
                newName: "MaLuotDat");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "DanhGia",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "ThanhToan",
                newName: "ThoiDiemCapNhat");

            migrationBuilder.RenameColumn(
                name: "TransactionCode",
                table: "ThanhToan",
                newName: "MaGiaoDich");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "ThanhToan",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "ResponseMessage",
                table: "ThanhToan",
                newName: "NoiDungPhanHoi");

            migrationBuilder.RenameColumn(
                name: "ResponseCode",
                table: "ThanhToan",
                newName: "MaPhanHoi");

            migrationBuilder.RenameColumn(
                name: "RawData",
                table: "ThanhToan",
                newName: "DuLieuGoc");

            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "ThanhToan",
                newName: "ThoiDiemThuTien");

            migrationBuilder.RenameColumn(
                name: "Method",
                table: "ThanhToan",
                newName: "PhuongThuc");

            migrationBuilder.RenameColumn(
                name: "ManualRefundedById",
                table: "ThanhToan",
                newName: "MaNguoiHoanThuCong");

            migrationBuilder.RenameColumn(
                name: "ManualRefundedAt",
                table: "ThanhToan",
                newName: "ThoiDiemHoanThuCong");

            migrationBuilder.RenameColumn(
                name: "ManualRefundNote",
                table: "ThanhToan",
                newName: "GhiChuHoanThuCong");

            migrationBuilder.RenameColumn(
                name: "GatewayTransactionId",
                table: "ThanhToan",
                newName: "MaGiaoDichCong");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ThanhToan",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "BookingId",
                table: "ThanhToan",
                newName: "MaLuotDat");

            migrationBuilder.RenameColumn(
                name: "BankCode",
                table: "ThanhToan",
                newName: "MaNganHang");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "ThanhToan",
                newName: "SoTien");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ThanhToan",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "QrImageId",
                table: "HoSoNhanChuyenKhoan",
                newName: "MaAnhQr");

            migrationBuilder.RenameColumn(
                name: "Enabled",
                table: "HoSoNhanChuyenKhoan",
                newName: "DaBat");

            migrationBuilder.RenameColumn(
                name: "BankName",
                table: "HoSoNhanChuyenKhoan",
                newName: "TenNganHang");

            migrationBuilder.RenameColumn(
                name: "AccountNumber",
                table: "HoSoNhanChuyenKhoan",
                newName: "SoTaiKhoan");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "HoSoNhanChuyenKhoan",
                newName: "TenChuTaiKhoan");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "HoSoNhanChuyenKhoan",
                newName: "MaChuSan");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "ThongBao",
                newName: "MaNguoiDung");

            migrationBuilder.RenameColumn(
                name: "Url",
                table: "ThongBao",
                newName: "DuongDan");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "ThongBao",
                newName: "Loai");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "ThongBao",
                newName: "TieuDe");

            migrationBuilder.RenameColumn(
                name: "ReadAt",
                table: "ThongBao",
                newName: "ThoiDiemDoc");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "ThongBao",
                newName: "NoiDung");

            migrationBuilder.RenameColumn(
                name: "IsRead",
                table: "ThongBao",
                newName: "DaDoc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "ThongBao",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "ThongBao",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "CompletedAtUtc",
                table: "TrangThaiNapMau",
                newName: "ThoiDiemHoanThanhUtc");

            migrationBuilder.RenameColumn(
                name: "Key",
                table: "TrangThaiNapMau",
                newName: "Khoa");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "San",
                newName: "ThoiDiemCapNhat");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "San",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "SportTypeId",
                table: "San",
                newName: "MaLoaiTheThao");

            migrationBuilder.RenameColumn(
                name: "SportComplexId",
                table: "San",
                newName: "MaCumSan");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "San",
                newName: "Ten");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "San",
                newName: "DaXoaMem");

            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "San",
                newName: "DuongDanAnh");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "San",
                newName: "MoTa");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "San",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "San",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "DatSan",
                newName: "ThoiDiemCapNhat");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "DatSan",
                newName: "TongTien");

            migrationBuilder.RenameColumn(
                name: "TimeSlotId",
                table: "DatSan",
                newName: "MaKhungGio");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "DatSan",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "StartDateTime",
                table: "DatSan",
                newName: "ThoiDiemBatDau");

            migrationBuilder.RenameColumn(
                name: "RowVersion",
                table: "DatSan",
                newName: "PhienBanDong");

            migrationBuilder.RenameColumn(
                name: "PaymentStatus",
                table: "DatSan",
                newName: "TrangThaiThanhToan");

            migrationBuilder.RenameColumn(
                name: "PaymentMethod",
                table: "DatSan",
                newName: "PhuongThucThanhToan");

            migrationBuilder.RenameColumn(
                name: "PaymentDeadline",
                table: "DatSan",
                newName: "HanThanhToan");

            migrationBuilder.RenameColumn(
                name: "Note",
                table: "DatSan",
                newName: "GhiChu");

            migrationBuilder.RenameColumn(
                name: "EndDateTime",
                table: "DatSan",
                newName: "ThoiDiemKetThuc");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "DatSan",
                newName: "MaKhachHang");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "DatSan",
                newName: "ThoiDiemTao");

            migrationBuilder.RenameColumn(
                name: "CourtId",
                table: "DatSan",
                newName: "MaSan");

            migrationBuilder.RenameColumn(
                name: "ConfirmedAt",
                table: "DatSan",
                newName: "ThoiDiemXacNhan");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "DatSan",
                newName: "ThoiDiemHoanThanh");

            migrationBuilder.RenameColumn(
                name: "CancelledBy",
                table: "DatSan",
                newName: "BenHuy");

            migrationBuilder.RenameColumn(
                name: "CancelledAt",
                table: "DatSan",
                newName: "ThoiDiemHuy");

            migrationBuilder.RenameColumn(
                name: "CancellationReason",
                table: "DatSan",
                newName: "LyDoHuy");

            migrationBuilder.RenameColumn(
                name: "BookingGroupCode",
                table: "DatSan",
                newName: "MaNhomDatSan");

            migrationBuilder.RenameColumn(
                name: "BookingDate",
                table: "DatSan",
                newName: "NgaySuDung");

            migrationBuilder.RenameColumn(
                name: "BookingCode",
                table: "DatSan",
                newName: "MaDatSan");

            migrationBuilder.RenameColumn(
                name: "BankTransferGroupCode",
                table: "DatSan",
                newName: "MaNhomChuyenKhoan");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "DatSan",
                newName: "Ma");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "ChuyenKhoan",
                newName: "TrangThai");

            migrationBuilder.RenameColumn(
                name: "ReportDeadline",
                table: "ChuyenKhoan",
                newName: "HanBaoChuyenKhoan");

            migrationBuilder.RenameColumn(
                name: "Reference",
                table: "ChuyenKhoan",
                newName: "MaThamChieu");

            migrationBuilder.RenameColumn(
                name: "QrImageId",
                table: "ChuyenKhoan",
                newName: "MaAnhQr");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "ChuyenKhoan",
                newName: "MaChuSan");

            migrationBuilder.RenameColumn(
                name: "LastReportedAt",
                table: "ChuyenKhoan",
                newName: "LanBaoCuoi");

            migrationBuilder.RenameColumn(
                name: "CustomerId",
                table: "ChuyenKhoan",
                newName: "MaKhachHang");

            migrationBuilder.RenameColumn(
                name: "ComplexId",
                table: "ChuyenKhoan",
                newName: "MaCumSan");

            migrationBuilder.RenameColumn(
                name: "BankName",
                table: "ChuyenKhoan",
                newName: "TenNganHang");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "ChuyenKhoan",
                newName: "SoTien");

            migrationBuilder.RenameColumn(
                name: "AccountNumber",
                table: "ChuyenKhoan",
                newName: "SoTaiKhoan");

            migrationBuilder.RenameColumn(
                name: "AccountName",
                table: "ChuyenKhoan",
                newName: "TenChuTaiKhoan");

            migrationBuilder.RenameColumn(
                name: "GroupCode",
                table: "ChuyenKhoan",
                newName: "MaNhom");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "NguoiDung",
                column: "TenDangNhapChuanHoa",
                unique: true,
                filter: "[TenDangNhapChuanHoa] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "VaiTro",
                column: "TenChuanHoa",
                unique: true,
                filter: "[TenChuanHoa] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "DanhGia",
                sql: "[SoSao] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "UX_Bookings_Court_Slot_Date_Active",
                table: "DatSan",
                columns: new[] { "MaSan", "MaKhungGio", "NgaySuDung" },
                unique: true,
                filter: "[TrangThai] IN (0, 1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "RoleNameIndex",
                table: "VaiTro");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "NguoiDung");

            migrationBuilder.DropIndex(
                name: "UX_Bookings_Court_Slot_Date_Active",
                table: "DatSan");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "DanhGia");

            migrationBuilder.RenameTable(
                name: "VaiTroNguoiDung",
                newName: "UserRoles");

            migrationBuilder.RenameTable(
                name: "VaiTro",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "TrangThaiNapMau",
                newName: "DemoSeedStates");

            migrationBuilder.RenameTable(
                name: "ThongBao",
                newName: "Notifications");

            migrationBuilder.RenameTable(
                name: "ThanhToan",
                newName: "Payments");

            migrationBuilder.RenameTable(
                name: "San",
                newName: "Courts");

            migrationBuilder.RenameTable(
                name: "NguoiDung",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "MaXacThucNguoiDung",
                newName: "UserTokens");

            migrationBuilder.RenameTable(
                name: "LoaiTheThao",
                newName: "SportTypes");

            migrationBuilder.RenameTable(
                name: "KhungGio",
                newName: "TimeSlots");

            migrationBuilder.RenameTable(
                name: "KhaiBaoVaiTro",
                newName: "RoleClaims");

            migrationBuilder.RenameTable(
                name: "KhaiBaoNguoiDung",
                newName: "UserClaims");

            migrationBuilder.RenameTable(
                name: "HoSoNhanChuyenKhoan",
                newName: "OwnerTransferProfiles");

            migrationBuilder.RenameTable(
                name: "DatSan",
                newName: "Bookings");

            migrationBuilder.RenameTable(
                name: "DanhGia",
                newName: "Reviews");

            migrationBuilder.RenameTable(
                name: "DangNhapNgoai",
                newName: "UserLogins");

            migrationBuilder.RenameTable(
                name: "CumSan",
                newName: "SportComplexes");

            migrationBuilder.RenameTable(
                name: "ChuyenKhoan",
                newName: "BankTransfers");

            migrationBuilder.RenameTable(
                name: "BaoCaoChuyenKhoan",
                newName: "TransferReports");

            migrationBuilder.RenameTable(
                name: "AnhChuyenKhoan",
                newName: "TransferImages");

            migrationBuilder.RenameColumn(
                name: "MaVaiTro",
                table: "UserRoles",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "MaNguoiDung",
                table: "UserRoles",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "TenChuanHoa",
                table: "Roles",
                newName: "NormalizedName");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "Roles",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "DauPhienBan",
                table: "Roles",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Roles",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemHoanThanhUtc",
                table: "DemoSeedStates",
                newName: "CompletedAtUtc");

            migrationBuilder.RenameColumn(
                name: "Khoa",
                table: "DemoSeedStates",
                newName: "Key");

            migrationBuilder.RenameColumn(
                name: "TieuDe",
                table: "Notifications",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "Notifications",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemDoc",
                table: "Notifications",
                newName: "ReadAt");

            migrationBuilder.RenameColumn(
                name: "NoiDung",
                table: "Notifications",
                newName: "Message");

            migrationBuilder.RenameColumn(
                name: "MaNguoiDung",
                table: "Notifications",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Loai",
                table: "Notifications",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "DuongDan",
                table: "Notifications",
                newName: "Url");

            migrationBuilder.RenameColumn(
                name: "DaDoc",
                table: "Notifications",
                newName: "IsRead");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Notifications",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "Payments",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemThuTien",
                table: "Payments",
                newName: "PaidAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "Payments",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemHoanThuCong",
                table: "Payments",
                newName: "ManualRefundedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemCapNhat",
                table: "Payments",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "SoTien",
                table: "Payments",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "PhuongThuc",
                table: "Payments",
                newName: "Method");

            migrationBuilder.RenameColumn(
                name: "NoiDungPhanHoi",
                table: "Payments",
                newName: "ResponseMessage");

            migrationBuilder.RenameColumn(
                name: "MaPhanHoi",
                table: "Payments",
                newName: "ResponseCode");

            migrationBuilder.RenameColumn(
                name: "MaNguoiHoanThuCong",
                table: "Payments",
                newName: "ManualRefundedById");

            migrationBuilder.RenameColumn(
                name: "MaNganHang",
                table: "Payments",
                newName: "BankCode");

            migrationBuilder.RenameColumn(
                name: "MaLuotDat",
                table: "Payments",
                newName: "BookingId");

            migrationBuilder.RenameColumn(
                name: "MaGiaoDichCong",
                table: "Payments",
                newName: "GatewayTransactionId");

            migrationBuilder.RenameColumn(
                name: "MaGiaoDich",
                table: "Payments",
                newName: "TransactionCode");

            migrationBuilder.RenameColumn(
                name: "GhiChuHoanThuCong",
                table: "Payments",
                newName: "ManualRefundNote");

            migrationBuilder.RenameColumn(
                name: "DuLieuGoc",
                table: "Payments",
                newName: "RawData");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Payments",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "Courts",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "Courts",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemCapNhat",
                table: "Courts",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "Courts",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "Courts",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "MaLoaiTheThao",
                table: "Courts",
                newName: "SportTypeId");

            migrationBuilder.RenameColumn(
                name: "MaCumSan",
                table: "Courts",
                newName: "SportComplexId");

            migrationBuilder.RenameColumn(
                name: "DuongDanAnh",
                table: "Courts",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "DaXoaMem",
                table: "Courts",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Courts",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TrangThaiDuyet",
                table: "Users",
                newName: "ApprovalStatus");

            migrationBuilder.RenameColumn(
                name: "ThuDienTuChuanHoa",
                table: "Users",
                newName: "NormalizedEmail");

            migrationBuilder.RenameColumn(
                name: "ThuDienTu",
                table: "Users",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemXetDuyet",
                table: "Users",
                newName: "ApprovalReviewedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "Users",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemCapNhat",
                table: "Users",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "TenDangNhapChuanHoa",
                table: "Users",
                newName: "NormalizedUserName");

            migrationBuilder.RenameColumn(
                name: "TenDangNhap",
                table: "Users",
                newName: "UserName");

            migrationBuilder.RenameColumn(
                name: "TenCoSoKinhDoanh",
                table: "Users",
                newName: "BusinessName");

            migrationBuilder.RenameColumn(
                name: "SoLanDangNhapThatBai",
                table: "Users",
                newName: "AccessFailedCount");

            migrationBuilder.RenameColumn(
                name: "SoDienThoai",
                table: "Users",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "NgaySinh",
                table: "Users",
                newName: "DateOfBirth");

            migrationBuilder.RenameColumn(
                name: "MatKhauBam",
                table: "Users",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "MaNguoiXetDuyet",
                table: "Users",
                newName: "ApprovalReviewedById");

            migrationBuilder.RenameColumn(
                name: "LyDoTuChoiDuyet",
                table: "Users",
                newName: "ApprovalRejectionReason");

            migrationBuilder.RenameColumn(
                name: "HoTen",
                table: "Users",
                newName: "FullName");

            migrationBuilder.RenameColumn(
                name: "HanKhoa",
                table: "Users",
                newName: "LockoutEnd");

            migrationBuilder.RenameColumn(
                name: "DuongDanAnhDaiDien",
                table: "Users",
                newName: "AvatarUrl");

            migrationBuilder.RenameColumn(
                name: "DienThoaiKinhDoanh",
                table: "Users",
                newName: "BusinessPhone");

            migrationBuilder.RenameColumn(
                name: "DiaChi",
                table: "Users",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "DauPhienBan",
                table: "Users",
                newName: "ConcurrencyStamp");

            migrationBuilder.RenameColumn(
                name: "DauBaoMat",
                table: "Users",
                newName: "SecurityStamp");

            migrationBuilder.RenameColumn(
                name: "DangHoatDong",
                table: "Users",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "DaXacNhanThuDienTu",
                table: "Users",
                newName: "EmailConfirmed");

            migrationBuilder.RenameColumn(
                name: "DaXacNhanDienThoai",
                table: "Users",
                newName: "PhoneNumberConfirmed");

            migrationBuilder.RenameColumn(
                name: "ChoPhepKhoa",
                table: "Users",
                newName: "LockoutEnabled");

            migrationBuilder.RenameColumn(
                name: "BatXacThucHaiBuoc",
                table: "Users",
                newName: "TwoFactorEnabled");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "GiaTri",
                table: "UserTokens",
                newName: "Value");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "UserTokens",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "NhaCungCapDangNhap",
                table: "UserTokens",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "MaNguoiDung",
                table: "UserTokens",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ThuTuHienThi",
                table: "SportTypes",
                newName: "DisplayOrder");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "SportTypes",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "SportTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "SportTypes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "LopBieuTuong",
                table: "SportTypes",
                newName: "IconClass");

            migrationBuilder.RenameColumn(
                name: "DangHoatDong",
                table: "SportTypes",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "SportTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "TimeSlots",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemCapNhat",
                table: "TimeSlots",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "SoPhut",
                table: "TimeSlots",
                newName: "DurationMinutes");

            migrationBuilder.RenameColumn(
                name: "MaSan",
                table: "TimeSlots",
                newName: "CourtId");

            migrationBuilder.RenameColumn(
                name: "GioKetThuc",
                table: "TimeSlots",
                newName: "EndTime");

            migrationBuilder.RenameColumn(
                name: "GioBatDau",
                table: "TimeSlots",
                newName: "StartTime");

            migrationBuilder.RenameColumn(
                name: "GiaNgayThuong",
                table: "TimeSlots",
                newName: "PriceWeekday");

            migrationBuilder.RenameColumn(
                name: "GiaNgayLe",
                table: "TimeSlots",
                newName: "PriceHoliday");

            migrationBuilder.RenameColumn(
                name: "GiaCuoiTuan",
                table: "TimeSlots",
                newName: "PriceWeekend");

            migrationBuilder.RenameColumn(
                name: "DangHoatDong",
                table: "TimeSlots",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "TimeSlots",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "MaVaiTro",
                table: "RoleClaims",
                newName: "RoleId");

            migrationBuilder.RenameColumn(
                name: "LoaiKhaiBao",
                table: "RoleClaims",
                newName: "ClaimType");

            migrationBuilder.RenameColumn(
                name: "GiaTriKhaiBao",
                table: "RoleClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "RoleClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "MaNguoiDung",
                table: "UserClaims",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "LoaiKhaiBao",
                table: "UserClaims",
                newName: "ClaimType");

            migrationBuilder.RenameColumn(
                name: "GiaTriKhaiBao",
                table: "UserClaims",
                newName: "ClaimValue");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "UserClaims",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TenNganHang",
                table: "OwnerTransferProfiles",
                newName: "BankName");

            migrationBuilder.RenameColumn(
                name: "TenChuTaiKhoan",
                table: "OwnerTransferProfiles",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "SoTaiKhoan",
                table: "OwnerTransferProfiles",
                newName: "AccountNumber");

            migrationBuilder.RenameColumn(
                name: "MaAnhQr",
                table: "OwnerTransferProfiles",
                newName: "QrImageId");

            migrationBuilder.RenameColumn(
                name: "DaBat",
                table: "OwnerTransferProfiles",
                newName: "Enabled");

            migrationBuilder.RenameColumn(
                name: "MaChuSan",
                table: "OwnerTransferProfiles",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "TrangThaiThanhToan",
                table: "Bookings",
                newName: "PaymentStatus");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "Bookings",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "TongTien",
                table: "Bookings",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemXacNhan",
                table: "Bookings",
                newName: "ConfirmedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "Bookings",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemKetThuc",
                table: "Bookings",
                newName: "EndDateTime");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemHuy",
                table: "Bookings",
                newName: "CancelledAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemHoanThanh",
                table: "Bookings",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemCapNhat",
                table: "Bookings",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemBatDau",
                table: "Bookings",
                newName: "StartDateTime");

            migrationBuilder.RenameColumn(
                name: "PhuongThucThanhToan",
                table: "Bookings",
                newName: "PaymentMethod");

            migrationBuilder.RenameColumn(
                name: "PhienBanDong",
                table: "Bookings",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "NgaySuDung",
                table: "Bookings",
                newName: "BookingDate");

            migrationBuilder.RenameColumn(
                name: "MaSan",
                table: "Bookings",
                newName: "CourtId");

            migrationBuilder.RenameColumn(
                name: "MaNhomDatSan",
                table: "Bookings",
                newName: "BookingGroupCode");

            migrationBuilder.RenameColumn(
                name: "MaNhomChuyenKhoan",
                table: "Bookings",
                newName: "BankTransferGroupCode");

            migrationBuilder.RenameColumn(
                name: "MaKhungGio",
                table: "Bookings",
                newName: "TimeSlotId");

            migrationBuilder.RenameColumn(
                name: "MaKhachHang",
                table: "Bookings",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "MaDatSan",
                table: "Bookings",
                newName: "BookingCode");

            migrationBuilder.RenameColumn(
                name: "LyDoHuy",
                table: "Bookings",
                newName: "CancellationReason");

            migrationBuilder.RenameColumn(
                name: "HanThanhToan",
                table: "Bookings",
                newName: "PaymentDeadline");

            migrationBuilder.RenameColumn(
                name: "GhiChu",
                table: "Bookings",
                newName: "Note");

            migrationBuilder.RenameColumn(
                name: "BenHuy",
                table: "Bookings",
                newName: "CancelledBy");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Bookings",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "Reviews",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemPhanHoi",
                table: "Reviews",
                newName: "OwnerReplyAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemCapNhat",
                table: "Reviews",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "SoSao",
                table: "Reviews",
                newName: "Rating");

            migrationBuilder.RenameColumn(
                name: "PhanHoiChuSan",
                table: "Reviews",
                newName: "OwnerReply");

            migrationBuilder.RenameColumn(
                name: "NhanXet",
                table: "Reviews",
                newName: "Comment");

            migrationBuilder.RenameColumn(
                name: "MaSan",
                table: "Reviews",
                newName: "CourtId");

            migrationBuilder.RenameColumn(
                name: "MaLuotDat",
                table: "Reviews",
                newName: "BookingId");

            migrationBuilder.RenameColumn(
                name: "MaKhachHang",
                table: "Reviews",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "DangHienThi",
                table: "Reviews",
                newName: "IsVisible");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "Reviews",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TenHienThiNhaCungCap",
                table: "UserLogins",
                newName: "ProviderDisplayName");

            migrationBuilder.RenameColumn(
                name: "MaNguoiDung",
                table: "UserLogins",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "KhoaNhaCungCap",
                table: "UserLogins",
                newName: "ProviderKey");

            migrationBuilder.RenameColumn(
                name: "NhaCungCapDangNhap",
                table: "UserLogins",
                newName: "LoginProvider");

            migrationBuilder.RenameColumn(
                name: "TinhThanh",
                table: "SportComplexes",
                newName: "Province");

            migrationBuilder.RenameColumn(
                name: "ThuDienTu",
                table: "SportComplexes",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemTao",
                table: "SportComplexes",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemCapNhat",
                table: "SportComplexes",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "Ten",
                table: "SportComplexes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "SoDienThoai",
                table: "SportComplexes",
                newName: "PhoneNumber");

            migrationBuilder.RenameColumn(
                name: "QuanHuyen",
                table: "SportComplexes",
                newName: "District");

            migrationBuilder.RenameColumn(
                name: "MoTa",
                table: "SportComplexes",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "MaChuSan",
                table: "SportComplexes",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "GioMoCua",
                table: "SportComplexes",
                newName: "OpeningTime");

            migrationBuilder.RenameColumn(
                name: "GioDongCua",
                table: "SportComplexes",
                newName: "ClosingTime");

            migrationBuilder.RenameColumn(
                name: "DuongDanAnh",
                table: "SportComplexes",
                newName: "ImageUrl");

            migrationBuilder.RenameColumn(
                name: "DiaChi",
                table: "SportComplexes",
                newName: "Address");

            migrationBuilder.RenameColumn(
                name: "DangHoatDong",
                table: "SportComplexes",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "SportComplexes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TrangThai",
                table: "BankTransfers",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "TenNganHang",
                table: "BankTransfers",
                newName: "BankName");

            migrationBuilder.RenameColumn(
                name: "TenChuTaiKhoan",
                table: "BankTransfers",
                newName: "AccountName");

            migrationBuilder.RenameColumn(
                name: "SoTien",
                table: "BankTransfers",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "SoTaiKhoan",
                table: "BankTransfers",
                newName: "AccountNumber");

            migrationBuilder.RenameColumn(
                name: "MaThamChieu",
                table: "BankTransfers",
                newName: "Reference");

            migrationBuilder.RenameColumn(
                name: "MaKhachHang",
                table: "BankTransfers",
                newName: "CustomerId");

            migrationBuilder.RenameColumn(
                name: "MaCumSan",
                table: "BankTransfers",
                newName: "ComplexId");

            migrationBuilder.RenameColumn(
                name: "MaChuSan",
                table: "BankTransfers",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "MaAnhQr",
                table: "BankTransfers",
                newName: "QrImageId");

            migrationBuilder.RenameColumn(
                name: "LanBaoCuoi",
                table: "BankTransfers",
                newName: "LastReportedAt");

            migrationBuilder.RenameColumn(
                name: "HanBaoChuyenKhoan",
                table: "BankTransfers",
                newName: "ReportDeadline");

            migrationBuilder.RenameColumn(
                name: "MaNhom",
                table: "BankTransfers",
                newName: "GroupCode");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemXuLy",
                table: "TransferReports",
                newName: "HandledAt");

            migrationBuilder.RenameColumn(
                name: "ThoiDiemBao",
                table: "TransferReports",
                newName: "ReportedAt");

            migrationBuilder.RenameColumn(
                name: "MaNhom",
                table: "TransferReports",
                newName: "GroupCode");

            migrationBuilder.RenameColumn(
                name: "MaNguoiXuLy",
                table: "TransferReports",
                newName: "HandledById");

            migrationBuilder.RenameColumn(
                name: "MaAnhBienLai",
                table: "TransferReports",
                newName: "ReceiptId");

            migrationBuilder.RenameColumn(
                name: "KetQuaXuLy",
                table: "TransferReports",
                newName: "Outcome");

            migrationBuilder.RenameColumn(
                name: "GhiChuXuLy",
                table: "TransferReports",
                newName: "HandlingNote");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "TransferReports",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "DuLieuAnhPng",
                table: "TransferImages",
                newName: "Png");

            migrationBuilder.RenameColumn(
                name: "Ma",
                table: "TransferImages",
                newName: "Id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Bookings_Court_Slot_Date_Active",
                table: "Bookings",
                columns: new[] { "CourtId", "TimeSlotId", "BookingDate" },
                unique: true,
                filter: "[Status] IN (0, 1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reviews_Rating",
                table: "Reviews",
                sql: "[Rating] BETWEEN 1 AND 5");
        }
    }
}
