# Kiểm thử hồi quy

Bộ test khởi chạy ứng dụng thật, gọi HTTP và kiểm tra trạng thái SQL Server sau thao tác. Mỗi lần chạy tạo một database `SportBookingRegression_<mã ngẫu nhiên>` riêng rồi dừng ứng dụng và xóa đúng database đó. Không ghi vào `SportBookingDb` hiện có.

Cần .NET 10 SDK, SQL Server LocalDB `MSSQLLocalDB`, `sqlcmd`, Python và thư viện trong `requirements.txt`.

Chạy từ thư mục gốc dự án:

```powershell
dotnet build SportBooking.csproj
python -m pip install -r tests/requirements.txt
python -X utf8 tests/regression.py
```

Nếu ứng dụng đang chạy từ DLL Debug và giữ khóa file đầu ra, build Release và trỏ bộ test
vào DLL vừa build mà không dừng ứng dụng đang mở:

```powershell
dotnet build SportBooking.csproj -c Release
$env:SPORTBOOKING_TEST_DLL = (Resolve-Path 'bin/Release/net10.0/SportBooking.dll').Path
python -X utf8 tests/regression.py
```

`ui_flow.py` và `sports_theme.py` cũng dùng biến `SPORTBOOKING_TEST_DLL` này.

Ca nghiệm thu SB-09 chạy riêng với `dotnet build SportBooking.csproj -c SB09` và
`python -X utf8 tests/sb09_e2e.py`. Ca này dùng Microsoft Edge, một database mới tắt demo
và lưu ảnh cùng kết quả đối chiếu SQL. Xem [hướng dẫn SB-09](../docs/sb-09/REPORT.md).

`python -X utf8 tests/time_boundaries.py` cũng dùng bản build `SB09` và LocalDB riêng. Ca này
kiểm tra cuối tuần, chuyển ngày Việt Nam ở mốc UTC và hết hạn giữ chỗ. Khi có
`SB09_VIEWPORT_DLL` trỏ vào DLL Release, ca SB-09 nối tiếp bằng kiểm tra ba vai trò
ở 375/768/1440 px trên Release. Xem [báo cáo và hướng dẫn SB-17/SB-18](../docs/sb-17-18/REPORT.md).

Các ca bao gồm khóa/mở tài khoản và thu hồi phiên, thông báo khóa tạm, link đặt lại mật khẩu và dùng lại token, đổi mật khẩu và giữ phiên hiện tại, lọc giờ, đặt đồng thời, quyền hoàn tiền tại quầy, hạn hủy, trạng thái cụm/Owner, cùng các nhánh thanh toán VNPay.

`StartupRegression` kiểm tra database mới chỉ có schema/role khi tắt demo, Admin đầu tiên từ cấu hình, cấu hình thiếu/sai, không nâng quyền tài khoản thường, demo đầy đủ và chạy lại an toàn, tắt demo giữ dữ liệu, Production từ chối demo, migration thiếu/thất bại và rollback khi seed demo lỗi. Chạy riêng: `python -X utf8 tests/regression.py StartupRegression`. Xem [hướng dẫn khởi tạo](../docs/DATABASE_INITIALIZATION.md).

Quản lý loại sân: kiểm tra quyền Admin, chống CSRF, thêm/sửa/xóa, tên trùng, tên rỗng, thứ tự không hợp lệ, lọc trạng thái và chặn xóa loại đang có sân. Chạy riêng: `python -X utf8 tests/regression.py Regression.test_admin_sport_types`.

Khu Owner: kiểm tra tải trang tổng quan và menu lịch đặt, liên kết ra ngoài Area, chuyển hướng sau đăng nhập và chặn tài khoản không có quyền hoặc chưa duyệt. Chạy riêng: `python -X utf8 tests/regression.py Regression.test_owner_dashboard_and_menu`.

Quản lý cụm sân Owner: thêm/sửa/ngừng hoạt động, lọc và phân trang, kiểm tra giờ mở cửa/liên hệ/URL ảnh, chống CSRF, chống giả mạo OwnerId và truy cập cụm sân người khác. Chạy riêng: `python -X utf8 tests/regression.py Regression.test_owner_complex_management`.

Ca `Regression.test_owner_complex_management` còn kiểm tra email liên hệ trên danh sách, dữ liệu giữ nguyên khi biểu mẫu lỗi, giờ chính xác đến phút, lọc theo địa chỉ, phân trang 11 cụm và Owner chưa duyệt gửi POST. Ca `Regression.test_owner_complex_concurrent_pause_and_booking` cho Owner ngừng cụm đồng thời với khách đặt sân, kiểm tra lịch sử và khả năng đặt lại sau khi mở cụm. Ca `Regression.test_owner_time_slots_and_prices` kiểm tra thu hẹp giờ dù booking nằm trên khung đã ngừng bán.

Quản lý sân trong cụm: thêm/sửa/trạng thái, lọc và phân trang theo cụm, chống giả mạo cụm và sân của Owner khác, kiểm tra loại sân, giữ loại hiện tại đã ngừng hoạt động, loại trừ sân xóa mềm và chặn Owner chưa duyệt. Chạy riêng: `python -X utf8 tests/regression.py Regression.test_owner_courts`.

Xóa mềm sân và lịch sử booking: `Regression.test_z_owner_soft_delete_keeps_booking_history` kiểm tra nút xóa, CSRF, quyền Owner, tài khoản chưa duyệt/ngừng hoạt động, booking đang giữ chỗ, gửi lại yêu cầu, ẩn sân khỏi danh sách/tìm kiếm/đặt mới, lịch sử và chi tiết của khách/Owner, snapshot giờ/tiền và đánh giá. `Regression.test_z_owner_delete_races_booking_creation` chạy xóa sân đồng thời với đặt sân. `Regression.test_z_deleted_court_keeps_refund_workflow` kiểm tra chặn xóa booking đã xác nhận, hủy rồi xóa, Owner đúng quyền vẫn hoàn tiền tại quầy.

Khung giờ và giá: `python -X utf8 tests/regression.py Regression.test_owner_time_slots_and_prices`. Kiểm tra quyền sở hữu/CSRF, thêm/sửa/ngừng bán, biểu mẫu sửa giữ nguyên giá, giờ cùng ngày/trong giờ mở cửa, khung chồng lấn, giá nguyên VNĐ, đặt các khung liền nhau, chặn đổi giờ có booking, giữ snapshot, xác nhận lại khi giờ/giá thay đổi và tạo booking đồng thời với sửa giờ.

Test tạo booking gửi TotalAmount và SlotQuotes từ bảng giá hiện tại, tương tự biểu mẫu người dùng. Đây là dữ liệu để phát hiện lựa chọn cũ; server luôn kiểm tra lại giờ, giá và chỗ trống từ database trong giao dịch.

Ca `Regression.test_z_overlap_after_schedule_change` dựng lịch cũ bị đổi ngoài luồng Owner để kiểm tra phòng vệ: booking đã chốt vẫn hiển thị giờ/giá cũ, lưới giờ và tìm sân không báo trống cho khoảng chồng lấn trên ID khung khác, POST đặt sân bị chặn.

Sau khi bỏ checkout VNPay, ca tạo URL/tiếp tục thanh toán được thay bằng kiểm tra endpoint cũ đã đóng và lịch sử vẫn đọc được. Ca ngưỡng tiền VNPay được gỡ vì không còn luồng gửi tiền sang cổng. Ca Return/IPN còn áp dụng dùng Payment legacy dựng trong DB thử và chữ ký giả lập. Bài kiểm tra biên giờ kiểm tra hết hạn trên phương thức QR hiện tại.

`python -X utf8 tests/qr_transfer.py` chạy luồng QR trên Release qua Edge headless, từ tài khoản và DB sạch tắt demo. Test dùng ảnh tổng hợp để kiểm tra upload, quyền xem ảnh, đối soát, đồng thời và dữ liệu trước/sau. [Hướng dẫn QR](../docs/THANH_TOAN_QR.md)

## Kiểm thử giao diện bằng trình duyệt

`ui_flow.py` dùng Playwright với Microsoft Edge headless và cùng cơ chế database LocalDB riêng của `regression.py`. Mỗi lượt tự tạo database ngẫu nhiên rồi xóa sau khi hoàn tất, kể cả khi kiểm thử thất bại. Cần Microsoft Edge và các thư viện trong `requirements.txt`.

```powershell
dotnet build SportBooking.csproj
python -m pip install -r tests/requirements.txt
python -X utf8 tests/ui_flow.py
```

Trong phiên triển khai này Playwright chỉ được tải vào thư mục tạm; để chạy lại bằng bản đó:

```powershell
$env:PYTHONPATH = "$env:TEMP\sportbooking-ui-python"
python -X utf8 tests/ui_flow.py
```

Các ca kiểm tra bộ lọc và cascade tỉnh/quận (kể cả tải chậm/lỗi), phân trang/quay lại giữ điều kiện, đăng nhập trở về ngày đã chọn, menu/bộ lọc mobile, 375/768/1440px, chữ tiếng Việt, target chính 44px, keyboard và focus lỗi, chọn giờ liền/rời/giới hạn, hover selected, lịch AJAX đổi ngày nhanh/lỗi/thử lại, trạng thái thật, đổi giá yêu cầu xác nhận lại, loading/chống gửi lặp và đặt 2 khung tại quầy thành công với tổng snapshot SQL đúng. Có thêm dữ liệu dài, giá tối đa, ảnh lấy từ URL nội bộ, reduced motion, landscape và chữ 200%.

Ảnh và kết quả nằm tại `docs/ui-review/screenshots/` và `docs/ui-review/browser-results.json`. Bộ kiểm thử dùng fixture hoa văn SVG có sẵn để xác minh nhánh ảnh; đó không phải ảnh chụp sân. Không gửi thanh toán đến VNPay hoặc email thật.

## Theme thể thao tối và ảnh nền

```powershell
python -X utf8 tests/sports_theme.py
```

Bộ test dùng database tạm riêng và Edge headless, kiểm tra 6 ảnh/logo cục bộ, picker giữ `SportTypeId`, thao tác bàn phím, chuyển ảnh/pause/manual/reduced-motion, pause khi ảnh tải chậm, 375/768/1440px, tương phản selected/skip link, đặt sân tại quầy và fallback select khi không có JavaScript. Kết quả và ảnh lưu trong `docs/ui-review/sports-theme/`. Nếu đang dùng Playwright được tải vào thư mục tạm trong phiên này, đặt `PYTHONPATH` như hướng dẫn phía trên.
