# SportBooking

Ứng dụng đặt sân thể thao bằng ASP.NET Core MVC, Razor, ASP.NET Identity và SQL Server. Ba vai trò là khách hàng, chủ sân và quản trị viên. Lượt đặt mới thanh toán tại quầy hoặc chuyển khoản qua QR của chủ sân.

## Chạy trên database mới

Cài .NET 10 SDK và SQL Server LocalDB trên Windows hoặc SQL Server trên môi trường đích. Dùng tên database riêng khi chạy thử. Cấu hình `ConnectionStrings__DefaultConnection` qua biến môi trường hoặc kho bí mật. [Cách cấu hình database và Admin](docs/DATABASE_INITIALIZATION.md)

```powershell
dotnet restore SportBooking.csproj
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DatabaseInitialization__ApplyMigrations = 'true'
$env:DatabaseInitialization__SeedDemoData = 'false'
dotnet run --project SportBooking.csproj --launch-profile https
```

Thực hiện bước tạo Admin đầu tiên trong hướng dẫn khởi tạo trước khi đăng nhập. Development tự chạy migration theo cấu hình. Bản triển khai Production chạy migration riêng rồi mới khởi động. Để bật dữ liệu demo, đặt `DatabaseInitialization__SeedDemoData=true` trên database Development riêng. Các email demo đại diện là `admin@sportbooking.vn`, `owner1@sportbooking.vn` và `customer1@sportbooking.vn`.

## Nghiệp vụ và demo

- [Quy tắc đặt sân, thanh toán và báo cáo](docs/BUSINESS_RULES.md)
- [Chuyển khoản QR](docs/THANH_TOAN_QR.md) và [đối soát giao dịch VNPay cũ](docs/THANH_TOAN_VNPAY.md)
- [ERD và use case](docs/ARCHITECTURE.md)
- [Kịch bản bảo vệ ba vai trò](docs/DEMO.md)

## Kiểm thử

Cần Python, `sqlcmd` và Microsoft Edge để chạy các bộ browser hiện có. Mỗi ca dùng database kiểm thử riêng và dọn sau khi kết thúc. [Câu lệnh chạy đầy đủ](tests/README.md)

```powershell
dotnet build SportBooking.csproj -c Release
dotnet build SportBooking.csproj -c SB09
python -m pip install -r tests/requirements.txt
$env:SPORTBOOKING_TEST_DLL = (Resolve-Path 'bin/Release/net10.0/SportBooking.dll').Path
$env:SB09_VIEWPORT_DLL = $env:SPORTBOOKING_TEST_DLL
python -X utf8 tests/regression.py
python -X utf8 tests/time_boundaries.py
python -X utf8 tests/qr_transfer.py
python -X utf8 tests/sb09_e2e.py
```

Trong demo kiểm thử, Owner mô phỏng xác nhận đã nhận tiền trên app. Ứng dụng không kết nối tài khoản ngân hàng. Khi dùng ảnh QR thật, chủ sân chịu trách nhiệm đối chiếu tiền thực nhận và thực hiện hoàn tiền thủ công.
