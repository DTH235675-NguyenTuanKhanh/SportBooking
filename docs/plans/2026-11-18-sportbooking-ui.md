# SportBooking UI Implementation Plan

**Goal:** Hoàn thiện luồng Tìm sân → Chi tiết → Đặt sân trên ứng dụng MVC thật ở 375, 768, 1440px.

**Architecture:** Giữ Razor binding, route, antiforgery, AJAX và dịch vụ tính tiền. Chuẩn hóa CSS tokens và partial dùng chung; cải thiện phản hồi phía trình duyệt, không đổi quy tắc bán khung.

**Tech Stack:** ASP.NET Core MVC .NET 10, Razor, Bootstrap 5, Bootstrap Icons, jQuery, Be Vietnam Pro.

## Thiết kế đã lựa chọn

- Giữ giao diện hiện tại và sửa CSS đơn lẻ: ít thay đổi nhưng không giải quyết cấu trúc tóm tắt và bộ lọc.
- **Chọn hướng dịch vụ đặt lịch:** nền sáng, xanh đậm, khoảng cách vừa phải, card có viền nhẹ, tiến trình ba bước và tóm tắt rõ ràng. Phù hợp với dữ liệu và stack hiện có.
- Hướng thể thao quảng bá với hero lớn, sắc đỏ và chuyển động mạnh: không phù hợp tác vụ so sánh giờ/giá, không áp dụng.

Tra cứu `ui-ux-pro-max`: `sports court booking clear light` trả về hướng đội thể thao không phù hợp; thử lại `service booking marketplace clear` trả về Soft UI Evolution và funnel ba bước phù hợp ở cấp cấu trúc. Chỉ áp dụng phân cấp, viền/bóng nhẹ, progressive disclosure. Giữ xanh và Be Vietnam Pro theo yêu cầu. Dataset không có stack Razor/Bootstrap; kết quả HTML/Tailwind chỉ tham khảo nguyên tắc focus của HTML, không thêm framework.

## Các bước

1. Đọc layout khách/Owner/Admin, partial, ViewModel/controller/service, BUSINESS_RULES; build và chạy database `SportBookingUiReview_20261118`; chụp trước sửa.
2. Lưu `design-system/sportbooking/MASTER.md`: vai trò, tokens, component, trạng thái, responsive, accessibility và lý do.
3. Sửa `_Layout`, `_CourtCard`, `_Pagination`, `_Alerts`, bổ sung partial bộ lọc, trạng thái rỗng, lưới giờ, tóm tắt. Giữ liên kết điều hướng ngược và bộ lọc trong query string.
4. Tìm sân: tiêu đề tác vụ, bộ lọc collapse mobile có nút áp dụng rõ, số kết quả/nhãn lọc, card và phân trang.
5. Chi tiết: ảnh ổn định, điều hướng nội dung, giá VNĐ/khung, bảng giá reflow mobile, giải thích không nhận đặt theo trạng thái thực.
6. Đặt sân: form grid có summary desktop/mobile, checkbox bàn phím, trạng thái bằng chữ, lỗi theo trường/tổng hợp, loading/chống gửi lặp và chống lịch AJAX cũ ghi đè.
7. Build `dotnet build SportBooking.csproj --no-restore`; chạy test HTTP/SQL hiện có phù hợp. Viết test Playwright cho filtering, phân trang, quay lại, keyboard, AJAX race/failure, khung liền/rời, báo giá đổi và gửi thành công trên dữ liệu riêng.
8. Kiểm tra overflow, target, contrast, font, ảnh, reduced motion và nội dung dài/rỗng. Chụp kết quả mobile/tablet/desktop, lưu báo cáo `docs/ui-review/REPORT.md`.

## Ghi chú môi trường

Không có `.git` trong checkout hoặc thư mục cha; không tạo worktree/commit. LocalDB và trình duyệt cần chạy ngoài sandbox. Playwright được tải vào thư mục tạm, không cài thư viện toàn hệ thống. Không kết nối VNPay/SMTP thật; chỉ đặt thử tại quầy.
