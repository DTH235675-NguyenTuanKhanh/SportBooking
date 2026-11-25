# Chuyển tiếp các giao dịch VNPay đã có

SportBooking ngừng tạo giao dịch VNPay mới từ phiên bản chuyển sang QR của chủ sân. Các lượt đặt và thanh toán VNPay vẫn đọc được trong lịch sử khách và Owner. [Luồng thanh toán hiện tại](THANH_TOAN_QR.md)

## Giữ callback trong thời gian đối soát

1. Trước khi cập nhật, ghi lại nhóm VNPay đang chờ, mã giao dịch và trạng thái phía gateway. Giữ tên miền và đường dẫn HTTPS đã đăng ký.
2. Giữ TmnCode và HashSecret của merchant cũ qua VnPaySettings__TmnCode và VnPaySettings__HashSecret hoặc kho bí mật. Callback GET /Payment/VnPayIpn tiếp tục kiểm tra chữ ký, merchant, mã giao dịch, số tiền và trạng thái. Return /Payment/VnPayReturn chỉ hiển thị cho đúng chủ tài khoản. Endpoint /Payment/Pay đã bỏ.
3. Theo dõi các giao dịch chưa xong và cửa sổ callback gửi lại của VNPay. Thành công đến sau hạn chuyển sang chờ hoàn. Đối chiếu khoản tiền với gateway và xử lý theo quy trình merchant. Chức năng hoàn tại quầy và hoàn QR chỉ xử lý payment của hai phương thức đó.
4. Khi tất cả mã có thể nhận callback đã được xử lý và đối chiếu, phối hợp VNPay dừng IPN cũ. Sau đó có thể thu hồi khóa và đóng tuyến công khai. Giữ dữ liệu lịch sử và migration.

Báo cáo sandbox trước khi đổi phương thức ghi nhận IPN giả lập và HTTPS cục bộ. [Bằng chứng VNPay trước đợt thay đổi](vnpay-sandbox/REPORT.md)
