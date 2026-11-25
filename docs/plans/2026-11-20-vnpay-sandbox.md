# Chuẩn bị nghiệm thu VNPay sandbox

1. Đối chiếu tài liệu VNPay PAY 2.1.0 với `PaymentController`, `VnPayService` và hồi quy hiện có. Kiểm tra sự hiện diện của cấu hình theo từng nguồn.
2. Thêm ca HTTP/SQL về IPN thiếu trường, giao dịch không tồn tại, khách hủy, giới hạn tiền và kết quả khi không qua Return URL. Kiểm tra ca mới trên bản hiện tại trước khi sửa.
3. Sửa kiểm tra IPN, giới hạn số tiền khi tạo booking online, giữ kết quả giao dịch khi tác vụ hết hạn chạy và chuẩn bị xử lý HTTPS sau proxy loopback tin cậy.
4. Build Release, chạy hồi quy trên LocalDB riêng và kiểm tra endpoint HTTPS cục bộ không có cookie. Ghi rõ phần kiểm tra được từ Internet.
5. Cập nhật hướng dẫn cấu hình, tài liệu đối chiếu DB và mẫu liên hệ VNPay. Chỉ nghiệm thu giao dịch thực khi có merchant sandbox và IPN công khai đã đăng ký.
