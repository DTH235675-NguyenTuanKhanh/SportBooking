# Hoàn thiện và nghiệm thu SportBooking

1. Đối chiếu mã nguồn và ca kiểm thử hiện có. Bản Release ban đầu đã build. Hai khoảng trống nghiệp vụ xác định từ mã nguồn là phản hồi đánh giá và báo cáo doanh thu của chủ sân.
2. Dùng Review.OwnerReply và OwnerReplyAt để lưu phản hồi. Phân quyền theo sân, giữ lịch sử sân xóa mềm và tôn trọng trạng thái ẩn của Admin.
3. Tính tổng thu, tiền đã hoàn và doanh thu ròng từ Payment theo ngày hạch toán Việt Nam. Dùng dấu vết ghi nhận tiền hiện có, bổ sung mốc hoàn tại quầy và thể hiện phần lịch sử thiếu chứng từ. Đối chiếu Admin với cùng nguồn tiền.
4. Nối kịch bản DB sạch cho cả hai phương thức. Sau khi hoàn thành booking và đánh giá, Owner phản hồi, đối chiếu báo cáo, khởi động lại và kiểm tra dữ liệu. Chạy các bộ hồi quy trên bản cuối.
5. Bổ sung README, quy tắc nghiệp vụ, ERD, use case, hướng dẫn bảo vệ và workbook tiến độ phản ánh bằng chứng thu được. Ghi rõ kết quả mô phỏng và điều kiện bên ngoài nếu có.
