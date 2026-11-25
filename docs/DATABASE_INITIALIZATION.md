# Khởi tạo database

`DatabaseInitialization` có ba cờ độc lập:

| Cờ | Mặc định chung | Tác dụng |
|---|---:|---|
| `ApplyMigrations` | `false` | Chạy EF Core migration trước khi tạo role. Development bật `true` trong `appsettings.Development.json`. |
| `CreateInitialAdmin` | `false` | Tạo Admin đầu tiên từ `InitialAdmin:Email` và `InitialAdmin:Password`. |
| `SeedDemoData` | `false` | Tạo tài khoản, loại sân, cụm, sân, khung, booking, payment, review và notification mẫu. Production từ chối khởi động nếu bật. |

Sau khi database có cấu trúc, ứng dụng luôn bảo đảm ba role `Admin`, `Owner`, `Customer`, kể cả khi
tắt cả Admin khởi tạo và demo. Tắt demo **không xóa** dữ liệu đã có. Nếu migration hoặc khởi tạo
được yêu cầu nhưng lỗi, ứng dụng dừng; xem lỗi khởi động và xử lý trước khi phục vụ request.

## Database mới, không có demo

Đặt `ConnectionStrings__DefaultConnection` cho database đích. Khi phát triển với LocalDB,
`appsettings.Development.json` đã bật migration; chạy `dotnet run` sẽ tạo schema và role,
không tạo tài khoản hay dữ liệu mẫu. Trong triển khai, sao lưu database rồi chạy migration thủ công:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:ConnectionStrings__DefaultConnection = '<chuoi-ket-noi-database-dich>'
dotnet ef database update --project SportBooking.csproj --startup-project SportBooking.csproj
dotnet run --no-launch-profile
```

Không bật `ApplyMigrations` ở Production nếu quy trình triển khai chạy migration riêng. Nếu tắt
migration nhưng schema chưa có, ứng dụng dừng thay vì chạy với database thiếu bảng.

## Admin đầu tiên

Chỉ dùng email mới dành cho Admin. Ví dụ với biến môi trường trong phiên PowerShell triển khai:

```powershell
$env:DatabaseInitialization__CreateInitialAdmin = 'true'
$env:InitialAdmin__Email = 'admin@example.test'
$env:InitialAdmin__Password = '<mat-khau-rieng-du-manh>'
dotnet run --no-launch-profile
```

Thay các giá trị trong dấu `<...>` bằng giá trị thật, không lưu mật khẩu trong `appsettings*.json`.
Ở Development có thể dùng .NET User Secrets: `dotnet user-secrets set 'InitialAdmin:Email' 'admin@example.test'`
và `dotnet user-secrets set 'InitialAdmin:Password' '<mat-khau-rieng-du-manh>'`.
Sau khi đăng nhập Admin thành công, **tắt** `CreateInitialAdmin` và xóa mật khẩu khỏi môi trường
khởi động hoặc User Secrets. Lần chạy lại không đổi mật khẩu/trạng thái Admin đã có. Nếu email đã
thuộc tài khoản thường, khởi tạo dừng và không tự gán role Admin.

## Demo và chạy kiểm thử

Chỉ bật demo trên database phát triển/kiểm thử riêng:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DatabaseInitialization__SeedDemoData = 'true'
dotnet run
```

Lần seed mới ghi dữ liệu và dấu mốc `DemoSeedStates` trong cùng giao dịch. Chạy lại không tạo bản
ghi trùng hoặc đặt lại mật khẩu. Database cũ đã chứa dấu vết demo nhưng chưa có dấu mốc sẽ bị
từ chối seed lại để tránh nhân đôi hoặc che dữ liệu dở dang; hãy giữ `SeedDemoData=false`, sao lưu
và đối chiếu các bảng trước khi quyết định chuyển đổi dấu mốc.

```powershell
dotnet build SportBooking.csproj
python -m pip install -r tests/requirements.txt
python -X utf8 tests/regression.py
```

Bộ test tự bật migration và demo khi cần, dùng LocalDB riêng rồi chỉ xóa database thử do chính
lần chạy tạo. SMTP (`EmailSettings__Password`) và khóa cổng thanh toán cũ
(`VnPaySettings__HashSecret`, `MomoSettings__SecretKey`, `MomoSettings__AccessKey`) lấy từ
biến môi trường hoặc kho bí mật. Cấu hình chung để trống các khóa và tắt gửi email ra console.
Development bật `EmailSettings:UseConsoleSender=true` để kiểm thử email không cần SMTP.
Migration `AddOwnerBankTransfers` thêm các bảng QR và giữ booking/payment đã có. QR mới dùng
thông tin do Owner nhập. [Hướng dẫn chuyển khoản QR](THANH_TOAN_QR.md)
