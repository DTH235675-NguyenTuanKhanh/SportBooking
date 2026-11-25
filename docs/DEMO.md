# Kịch bản demo SportBooking

## Chuẩn bị

Dùng database mới, tắt demo, chạy migration và tạo Admin qua cấu hình. Dùng ảnh PNG/JPEG thử cho Owner, số tài khoản minh họa và hai trình duyệt riêng cho Owner, khách. [Cài đặt](../README.md) · [Khởi tạo Admin](DATABASE_INITIALIZATION.md)

## Trình diễn

1. **Admin** tạo loại sân hoạt động. Đăng ký Owner kèm ảnh QR ở cửa sổ riêng, thử đăng nhập khi chưa duyệt, quay về Admin duyệt.
2. **Owner** tạo cụm mở 07:00–22:00, thêm sân và hai khung liền nhau. Đặt giá ngày thường 180.000 ₫ và 220.000 ₫. Mở trang hồ sơ để cho thấy tài khoản và QR đã cấu hình.
3. **Khách** đăng ký, tìm sân, chọn hai khung và đặt tại quầy. Owner xác nhận thu tiền, khách kiểm tra giờ và tiền trong lịch sử. Chọn ngày khác, đặt một khung bằng QR và báo đã chuyển. Owner lọc đơn QR, kiểm tra thông tin và mô phỏng xác nhận tiền trên DB thử.
4. **Owner** hoàn thành booking sau giờ kết thúc. Khách đánh giá. Owner phản hồi và khách mở trang chi tiết sân để thấy phản hồi công khai.
5. **Owner** mở báo cáo, lọc theo ngày thực thu và sân. Đối chiếu tổng thu, tiền đã hoàn và doanh thu ròng. Dùng dữ liệu báo cáo trước và sau hoàn để minh họa cách tính. Admin có thống kê ròng từ cùng nguồn giao dịch.
6. Khởi động lại app, đăng nhập lại và kiểm tra lịch sử, trạng thái thanh toán, đánh giá, phản hồi và báo cáo.

Với demo thủ công trên Release, chỉ hoàn thành sau giờ kết thúc thật. Để trình diễn trọn luồng ngay, chạy `tests/sb09_e2e.py` theo README. Bản SB09 dùng đồng hồ riêng cho test. Không thay giờ hệ thống hoặc gửi tiền trong quá trình chạy. [Hướng dẫn kiểm thử](../tests/README.md)

## Hạn chế cần nêu khi bảo vệ

- Ảnh QR và biên lai xác định thông tin đối soát. Chủ sân kiểm tra tiền tại ngân hàng và thực hiện hoàn bên ngoài app.
- Bản báo cáo không tự suy đoán số tiền hoặc ngày của dữ liệu cũ thiếu chứng từ, có thông báo nếu gặp trong phạm vi sân đã chọn.
- VNPay chỉ còn xử lý giao dịch cũ. Khi vận hành với merchant đã dùng VNPay, cần đối chiếu giao dịch tồn trước khi đóng callback. [Chuyển tiếp VNPay](THANH_TOAN_VNPAY.md)
