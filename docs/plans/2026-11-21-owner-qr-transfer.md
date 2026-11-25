# Chuyển khoản QR cho SportBooking

1. Lưu cấu hình theo Owner, ảnh đã chuẩn hóa trong DB và thông tin chuyển khoản cố định theo nhóm booking. Dùng migration cộng thêm bảng/cột.
2. Dùng chung khóa lịch để gửi báo, đối soát và hết hạn. Lần báo đúng hạn đầu tiên cho tối đa thêm 15 phút chờ Owner, luôn cắt ở giờ bắt đầu. Sau hết hạn, chỉ đối soát và theo dõi hoàn thủ công.
3. Nối đăng ký, hồ sơ Owner, booking khách, xem biên lai có phân quyền, danh sách đối soát và hoàn thủ công. Dừng tạo giao dịch VNPay và giữ callback lịch sử.
4. Chạy hồi quy trên DB riêng, nghiệm thu tài khoản mới và kiểm tra bố cục trên Chromium. Ghi hướng dẫn chuyển đổi, vận hành và giới hạn xác minh ngân hàng.
