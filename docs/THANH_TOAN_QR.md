# Chuyển khoản qua QR của chủ sân

SportBooking có hai phương thức đặt mới là thanh toán tại quầy và chuyển khoản QR. Tiền chuyển trực tiếp vào tài khoản của chủ sân. Owner kiểm tra tiền thực nhận trong ngân hàng và cập nhật kết quả trong SportBooking.

## Cập nhật ứng dụng

1. Sao lưu database và chạy migration `AddOwnerBankTransfers` theo [hướng dẫn khởi tạo](DATABASE_INITIALIZATION.md). Migration thêm `OwnerTransferProfiles`, `BankTransfers`, `TransferReports` và `TransferImages`. Các cột bổ sung trên `Bookings` và `Payments` có thể để trống cho dữ liệu cũ.
2. Nếu đã nhận VNPay, giữ IPN/Return và khóa merchant cũ trong thời gian đối soát. [Quy trình chuyển tiếp VNPay](THANH_TOAN_VNPAY.md)
3. Owner đăng ký mới bật QR trong biểu mẫu. Owner đã có tài khoản mở **Hồ sơ → Thông tin chuyển khoản QR**. Điền ngân hàng, số tài khoản, chủ tài khoản và tải ảnh PNG/JPEG tối đa 2 MB, 2.048 × 2.048 px. Xem trước, quét thử và xác nhận ảnh trỏ đúng thông tin ngân hàng.

Một cấu hình dùng chung cho tất cả sân của Owner. App giải mã và lưu ảnh dưới dạng PNG theo ID ngẫu nhiên trong DB. QR cũ vẫn giữ theo lượt đặt đã cấp. QR và biên lai xem qua tuyến có đăng nhập và kiểm tra quyền sở hữu. Chỉ tải ảnh cần thiết cho đối soát, tránh đưa dữ liệu riêng tư không liên quan vào biên lai.

## Khách đặt và báo chuyển

1. Chọn sân, ngày, khung giờ và **Chuyển khoản qua QR**. Sân chưa bật QR nhận đặt tại quầy.
2. Trên trang chuyển khoản, kiểm tra ngân hàng, người nhận, số tài khoản, số tiền và mã nội dung. Có nút sao chép và tải ảnh QR. Kiểm tra người nhận trên ứng dụng ngân hàng trước khi chuyển.
3. Sau khi đã chuyển, bấm **Tôi đã chuyển khoản**. Có thể đính kèm biên lai PNG/JPEG. Theo dõi tại **Lịch đặt sân → Chuyển khoản**.

## Owner đối soát và hoàn tiền

1. Vào **Owner → Lịch đặt sân**, bật **Chuyển khoản chờ chủ sân xác nhận**. Mở đơn để xem khách, mã nội dung, số tiền, thời điểm báo và từng biên lai.
2. So sánh mã, số tiền và tài khoản thực nhận trong ứng dụng ngân hàng. Chọn **Xác nhận đã nhận đủ tiền** khi đã đối chiếu. Nếu chưa thấy, nhận sai số tiền hoặc cần hỏi thêm, chọn **Chưa tìm thấy / cần bổ sung** và nhập lý do. Khách có thể báo lại. Các báo cáo và ghi chú trước vẫn xem được.
3. Khi có khoản chờ hoàn, liên hệ khách và trả tiền bên ngoài app. Sau đó nhập ghi chú và xác nhận đã hoàn theo từng khung. Nếu nhận thiếu hoặc thừa, ghi lý do và đối soát phần chênh lệch trực tiếp với khách.

## Hạn và trạng thái

| Tình huống | Xử lý |
|---|---|
| Mới đặt | Giữ chỗ mặc định 15 phút, tối đa đến lúc khung đầu bắt đầu. |
| Báo lần đầu đúng hạn | Chờ Owner 15 phút kể từ lúc báo, tối đa đến lúc khung đầu bắt đầu. Báo lại giữ nguyên hạn. |
| Owner xác nhận đủ khi còn hạn | Cả nhóm được xác nhận. Mỗi khung được ghi nhận thanh toán đúng giá đã chốt. |
| Hết hạn khi chưa được xác nhận | App nhả khung giờ. Khách chỉ báo sau hạn nếu đã chuyển và cần đối soát. |
| Đối soát đủ sau hạn | Booking giữ trạng thái hết hạn. Payment chờ hoàn thủ công. |
| Khách hủy đúng chính sách sau khi trả | Khung đã hủy chờ hoàn. App hiển thị đã hoàn sau khi Owner xác nhận. |

Ảnh QR, biên lai và thông báo là dữ liệu đối soát. Việc ngân hàng ghi có do Owner xác minh. SportBooking không kết nối API ngân hàng hoặc API hoàn tiền.

## Chạy lại kiểm thử

```powershell
dotnet build SportBooking.csproj -c Release
dotnet build SportBooking.csproj -c SB09
python -m pip install -r tests/requirements.txt
$env:SPORTBOOKING_TEST_DLL = (Resolve-Path 'bin/Release/net10.0/SportBooking.dll').Path
python -X utf8 tests/regression.py
python -X utf8 tests/qr_transfer.py
python -X utf8 tests/time_boundaries.py
```

Ca QR tạo DB mới, đăng ký và duyệt tài khoản qua các biểu mẫu, dùng ảnh thử và kiểm tra SQL sau mỗi bước. Chạy bằng Edge headless. [Báo cáo nghiệm thu](qr-transfer/REPORT.md)
