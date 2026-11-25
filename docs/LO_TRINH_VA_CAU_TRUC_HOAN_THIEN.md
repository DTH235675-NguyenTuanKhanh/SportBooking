# Lộ trình và cấu trúc dự kiến khi hoàn thiện SportBooking

Tài liệu lập sau đợt sửa lỗi ngày 06/11/2026. Đây là đề xuất cho các bước tiếp theo; những file có dấu `+` chưa được tạo. Dấu `~` chỉ file hiện có cần mở rộng. File không đánh dấu đã tồn tại ở thời điểm lập tài liệu.

Cập nhật ngày 21/11/2026. Lượt đặt mới dùng chuyển khoản QR của Owner hoặc thanh toán tại quầy. Mục VNPay bên dưới ghi lại lộ trình cũ. [Phương thức thanh toán hiện tại](THANH_TOAN_QR.md)

Giữ kiến trúc ASP.NET Core MVC đang có: Controller nhận yêu cầu, Service xử lý nghiệp vụ, Entity/ViewModel biểu diễn dữ liệu, Razor View hiển thị. Chưa cần tách thành nhiều project để hoàn thành phạm vi đồ án hiện tại.

**Điểm xuất phát**

Đã có tài khoản/phân quyền, hồ sơ, tìm kiếm sân, đặt sân và lịch sử, hủy, đánh giá của khách, quản lý booking tại quầy của Owner, duyệt Owner/quản lý người dùng/dashboard của Admin, tự hết hạn booking và luồng VNPay trong mã nguồn. Đợt sửa gần nhất đã biên dịch thành công và chạy đạt 22 ca kiểm thử hồi quy. VNPay mới được kiểm tra bằng thông báo giả lập có chữ ký, chưa nghiệm thu giao dịch qua máy chủ VNPay.

**Thứ tự thực hiện**

| Bước | Việc cần thực hiện | Vì sao làm ở bước này | Điều kiện xem là hoàn thành |
|---|---|---|---|
| 1 | Admin quản lý loại sân: danh sách, thêm, sửa, tìm kiếm, bật/tắt | Sân cần danh mục loại để liên kết; giúp ngừng phụ thuộc dữ liệu mẫu | Tạo loại mới dùng được khi thêm sân; loại đang được sân sử dụng không bị xóa cứng |
| 2 | Hoàn thiện khung giao diện Owner: menu, layout, trang vào khu quản lý | Làm một lần để tái sử dụng ở các trang tiếp theo | Menu đi tới đúng trang; chỉ Owner được duyệt truy cập được; khu vực quản lý dùng giao diện thống nhất |
| 3 | Owner quản lý cụm sân: tên, địa chỉ, liên hệ, ảnh, giờ hoạt động | Cụm sân là cha của sân con | Owner tạo/sửa/ngừng cụm của mình; không xem/sửa cụm người khác; không xóa mất lịch sử |
| 4 | Owner quản lý sân: thêm, sửa, tìm, ảnh, loại sân, bảo trì, xóa mềm | Cần cụm sân và danh mục loại từ bước 1, 3 | Sân mới xuất hiện ở phía khách khi đủ điều kiện; sân ngừng/xóa không nhận đơn; lịch sử cũ vẫn xem được |
| 5 | Quản lý khung giờ và giá | Cần có sân trước khi tạo lịch bán | Không có khung chồng lấn; giờ nằm trong giờ hoạt động; giá hợp lệ; sửa giờ/giá không làm sai booking đã chốt |
| 6 | Kiểm tra chu trình không phụ thuộc seed: Admin duyệt Owner → Owner tự tạo cụm/sân/giờ → khách đặt → thu tiền → hoàn thành → đánh giá | Xác nhận toàn bộ khối dữ liệu mới nối với các luồng đã có | Luồng chạy được với tài khoản và sân mới; giữ đúng giá và giờ đã đặt khi dữ liệu sân thay đổi |
| 7 | Nghiệm thu kết nối VNPay sandbox và hoàn tiền online | Booking và giá đã ổn định để đối chiếu tiền | Thanh toán thành công/thất bại/hủy/hết hạn qua cổng thật được kiểm tra; callback lặp an toàn; khoản hoàn được đối soát rồi mới ghi đã hoàn |
| 8 | Owner xem và phản hồi đánh giá | Tái sử dụng đánh giá khách đã có | Chỉ phản hồi đánh giá sân của mình; lưu thời gian; phản hồi hiển thị ở trang chi tiết sân |
| 9 | Báo cáo doanh thu Owner, rà soát thống kê Admin | Dựa trên dữ liệu thu tiền, hoàn tiền và trạng thái đã ổn định | Lọc theo thời gian/cụm/sân; có tổng thu, hoàn, doanh thu ròng theo quy tắc đã thống nhất; số liệu khớp giao dịch; xuất Excel nếu yêu cầu |
| 10 | Rà soát giao diện và thông báo trạng thái | Các luồng chính đã đủ để kiểm tra trải nghiệm trọn vẹn | Giao diện điện thoại dễ thao tác; lỗi rõ ràng; có màn hình rỗng, tải dữ liệu và chống bấm gửi lặp |
| 11 | Bổ sung test cho chức năng mới, chạy toàn bộ hồi quy và kiểm tra phân quyền/xóa mềm | Đây là đợt nghiệm thu tổng; mỗi bước trước vẫn cần kiểm tra ngay khi làm xong | Chạy được từ database mới; chặn truy cập chéo Owner; không mất lịch sử; số liệu tiền và trạng thái nhất quán |
| 12 | Chuẩn bị chạy thử/triển khai, tài liệu và kịch bản bảo vệ | Chốt cấu hình và bằng chứng sau khi chức năng ổn định | Có hướng dẫn cài đặt, migration/seed demo riêng, cấu hình email/thanh toán, ERD, use case cập nhật, bảng test và kịch bản demo |

Thứ tự phụ thuộc chính: **Loại sân → Cụm sân → Sân → Khung giờ/giá → Đặt sân → Thanh toán/hoàn tiền → Báo cáo**.

Trong bước 5 cần thống nhất ba quy tắc: cho đặt các khung rời nhau hay bắt buộc liên tiếp; xử lý khi sửa giờ đã có người đặt; có áp giá ngày lễ hay không. Nếu có giá lễ thì bổ sung danh mục ngày lễ trước khi cho khách sử dụng bảng giá này. Không hiển thị một loại giá chưa được áp dụng thực tế.

**Phạm vi bổ sung có điều kiện**

- MoMo: làm sau khi VNPay nghiệm thu nếu đề cương yêu cầu cả hai cổng. Nếu chỉ yêu cầu một cổng thanh toán online, VNPay đã đủ hướng triển khai.
- Thông báo trong ứng dụng: entity Notification đã có nhưng chưa có luồng đầy đủ. Có thể thêm sau bước 8 nếu đề cương yêu cầu hoặc còn thời gian; không phải một use case độc lập trong sơ đồ đã gửi.
- Quản lý ngày lễ: cần nếu muốn áp giá lễ. Nếu không nằm trong phạm vi chấm, nên thống nhất chỉ dùng giá ngày thường/cuối tuần.

**Bảng cấu trúc theo chức năng**

| Chức năng | Controller dự kiến | Service dự kiến | View/ViewModel |
|---|---|---|---|
| Loại sân | `Areas/Admin/Controllers/SportTypesController.cs` | `ISportTypeService`, `SportTypeService` | `Admin/Views/SportTypes`, `ViewModels/Admin/SportTypeViewModels.cs` |
| Cụm sân | `Areas/Owner/Controllers/ComplexesController.cs` | `ISportComplexService`, `SportComplexService` | `Owner/Views/Complexes`, `ViewModels/Owner/ComplexViewModels.cs` |
| Sân của Owner | `Areas/Owner/Controllers/CourtsController.cs` | `IOwnerCourtService`, `OwnerCourtService` | `Owner/Views/Courts`, `ViewModels/Owner/OwnerCourtViewModels.cs` |
| Khung giờ/giá | `Areas/Owner/Controllers/TimeSlotsController.cs` | `ITimeSlotService`, `TimeSlotService` | `Owner/Views/TimeSlots`, `ViewModels/Owner/TimeSlotViewModels.cs` |
| Đặt sân phía khách | `Controllers/BookingController.cs` | `IBookingService`, `BookingService` | `Views/Booking`, `ViewModels/Booking` đã có |
| Booking của Owner | `Areas/Owner/Controllers/BookingsController.cs` | `IOwnerBookingService`, `OwnerBookingService` | `Owner/Views/Bookings` đã có |
| Thanh toán/hoàn online | `Controllers/PaymentController.cs`, `Owner/Controllers/PaymentsController.cs` | `IVnPayService`, `VnPayService`, `IRefundService`, `RefundService` | `Owner/Views/Payments`, `ViewModels/Owner/OwnerPaymentViewModels.cs` |
| Phản hồi đánh giá | `Areas/Owner/Controllers/ReviewsController.cs` | `IOwnerReviewService`, `OwnerReviewService` | `Owner/Views/Reviews`, `ViewModels/Owner/OwnerReviewViewModels.cs` |
| Báo cáo Owner | `Areas/Owner/Controllers/ReportsController.cs` | `IOwnerReportService`, `OwnerReportService` | `Owner/Views/Reports`, `ViewModels/Owner/OwnerReportViewModels.cs` |
| Quản trị hiện có | Các controller trong `Areas/Admin` | `IAdminService`, `AdminService` | Giữ các trang Dashboard, OwnerApproval, UserManagement, ReviewManagement |
| Thông báo, nếu làm | `Controllers/NotificationsController.cs` | `INotificationService`, `NotificationService` | `Views/Notifications`, `ViewModels/Notification` |

`OwnerCourtService` xử lý quyền sửa sân của Owner; `CourtService` hiện có tiếp tục phục vụ tìm kiếm/xem sân công khai. Giữ cách phân biệt này để tránh trộn quyền quản lý với truy vấn cho khách.

**Cây file dự kiến khi hoàn thành phạm vi chính**

Gốc dự án: `D:\TK\DA.NET\QLSanTheThao\SportBooking`.

```text
SportBooking/
├── SportBooking.slnx
├── SportBooking.csproj
├── Program.cs                                      ~ đăng ký các service mới
├── appsettings.json                               ~ cấu hình chung, không chứa khóa triển khai thật
├── appsettings.Development.json
├── nuget.config
├── README.md                                      + cách cài và chạy dự án
│
├── Areas/
│   ├── Admin/
│   │   ├── Controllers/
│   │   │   ├── DashboardController.cs
│   │   │   ├── OwnerApprovalController.cs
│   │   │   ├── UserManagementController.cs
│   │   │   ├── ReviewManagementController.cs
│   │   │   └── SportTypesController.cs             +
│   │   └── Views/
│   │       ├── _ViewImports.cshtml
│   │       ├── _ViewStart.cshtml
│   │       ├── Shared/_Layout.cshtml
│   │       ├── Dashboard/Index.cshtml
│   │       ├── OwnerApproval/{Index,Detail}.cshtml
│   │       ├── UserManagement/{Index,Detail}.cshtml
│   │       ├── ReviewManagement/Index.cshtml
│   │       └── SportTypes/                        +
│   │           ├── Index.cshtml
│   │           ├── Create.cshtml
│   │           ├── Edit.cshtml
│   │           └── _Form.cshtml
│   │
│   └── Owner/
│       ├── Controllers/
│       │   ├── DashboardController.cs             + trang vào khu Owner
│       │   ├── ComplexesController.cs             +
│       │   ├── CourtsController.cs                +
│       │   ├── TimeSlotsController.cs             +
│       │   ├── BookingsController.cs
│       │   ├── PaymentsController.cs              + giao dịch và yêu cầu hoàn tiền
│       │   ├── ReviewsController.cs               +
│       │   └── ReportsController.cs               +
│       └── Views/
│           ├── _ViewImports.cshtml
│           ├── _ViewStart.cshtml                  +
│           ├── Shared/_Layout.cshtml              +
│           ├── Dashboard/Index.cshtml             +
│           ├── Complexes/                         +
│           │   ├── Index.cshtml
│           │   ├── Detail.cshtml
│           │   ├── Create.cshtml
│           │   ├── Edit.cshtml
│           │   └── _Form.cshtml
│           ├── Courts/                            +
│           │   ├── Index.cshtml
│           │   ├── Detail.cshtml
│           │   ├── Create.cshtml
│           │   ├── Edit.cshtml
│           │   └── _Form.cshtml
│           ├── TimeSlots/                         +
│           │   ├── Index.cshtml
│           │   ├── Create.cshtml
│           │   ├── Edit.cshtml
│           │   └── _Form.cshtml
│           ├── Bookings/Index.cshtml
│           ├── Payments/{Index,Detail}.cshtml     +
│           ├── Reviews/Index.cshtml               +
│           └── Reports/Index.cshtml               +
│
├── Controllers/
│   ├── HomeController.cs
│   ├── AccountController.cs
│   ├── ProfileController.cs
│   ├── CourtController.cs
│   ├── BookingController.cs
│   └── PaymentController.cs
│
├── Models/
│   ├── Entities/
│   │   ├── ApplicationUser.cs
│   │   ├── SportType.cs
│   │   ├── SportComplex.cs
│   │   ├── Court.cs
│   │   ├── TimeSlot.cs
│   │   ├── Booking.cs
│   │   ├── Payment.cs
│   │   ├── RefundRequest.cs                       + trạng thái yêu cầu/đối soát hoàn online
│   │   ├── Review.cs
│   │   ├── Notification.cs
│   │   └── Enums.cs                               ~ giữ nguyên giá trị enum cũ
│   ├── ErrorViewModel.cs
│   └── ViewModels/
│       ├── Account/
│       │   ├── RegisterViewModel.cs
│       │   ├── LoginViewModel.cs
│       │   ├── ForgotPasswordViewModel.cs
│       │   ├── ResetPasswordViewModel.cs
│       │   └── ChangePasswordViewModel.cs
│       ├── Profile/ProfileViewModel.cs
│       ├── Home/HomeViewModel.cs
│       ├── Court/
│       │   ├── CourtSearchViewModel.cs
│       │   ├── CourtDetailViewModel.cs
│       │   └── CourtCardViewModel.cs
│       ├── Booking/
│       │   ├── BookingCreateViewModel.cs
│       │   ├── BookingListViewModel.cs
│       │   ├── BookingDetailViewModel.cs
│       │   ├── BookingCancelViewModel.cs
│       │   └── ReviewCreateViewModel.cs
│       ├── Owner/
│       │   ├── OwnerDashboardViewModel.cs          +
│       │   ├── ComplexViewModels.cs                +
│       │   ├── OwnerCourtViewModels.cs             +
│       │   ├── TimeSlotViewModels.cs               +
│       │   ├── OwnerBookingListViewModel.cs
│       │   ├── OwnerPaymentViewModels.cs           +
│       │   ├── OwnerReviewViewModels.cs            +
│       │   └── OwnerReportViewModels.cs            +
│       ├── Admin/
│       │   ├── AdminViewModels.cs
│       │   └── SportTypeViewModels.cs              +
│       └── Shared/
│           ├── AppSettings.cs
│           ├── PaginationViewModel.cs
│           └── StarRatingViewModel.cs
│
├── Services/
│   ├── Interfaces/
│   │   ├── IServiceResult.cs
│   │   ├── IAccountService.cs
│   │   ├── IProfileService.cs
│   │   ├── ICourtService.cs
│   │   ├── IBookingService.cs
│   │   ├── IOwnerBookingService.cs
│   │   ├── IBookingExpirationService.cs
│   │   ├── IAdminService.cs
│   │   ├── IVnPayService.cs
│   │   ├── IEmailService.cs
│   │   ├── IFileUploadService.cs
│   │   ├── ISportTypeService.cs                    +
│   │   ├── ISportComplexService.cs                 +
│   │   ├── IOwnerCourtService.cs                   +
│   │   ├── ITimeSlotService.cs                     +
│   │   ├── IRefundService.cs                       +
│   │   ├── IOwnerReviewService.cs                  +
│   │   └── IOwnerReportService.cs                  +
│   ├── Implementations/
│   │   ├── AccountService.cs
│   │   ├── ProfileService.cs
│   │   ├── CourtService.cs
│   │   ├── BookingService.cs                       ~ lịch sử/giá/giờ sau sửa hoặc xóa sân
│   │   ├── OwnerBookingService.cs
│   │   ├── BookingExpirationService.cs
│   │   ├── AdminService.cs
│   │   ├── VnPayService.cs                         ~ tích hợp truy vấn/hoàn online khi triển khai
│   │   ├── SmtpEmailService.cs
│   │   ├── ConsoleEmailService.cs
│   │   ├── FileUploadService.cs
│   │   ├── SportTypeService.cs                     +
│   │   ├── SportComplexService.cs                  +
│   │   ├── OwnerCourtService.cs                    +
│   │   ├── TimeSlotService.cs                      +
│   │   ├── RefundService.cs                        +
│   │   ├── OwnerReviewService.cs                   +
│   │   └── OwnerReportService.cs                   +
│   └── Expiration/
│       └── BookingExpirationBackgroundService.cs
│
├── Data/
│   ├── ApplicationDbContext.cs                     ~ cấu hình thực thể mới
│   ├── DbSeeder.cs                                 ~ tách dữ liệu demo khỏi chạy thật
│   └── Migrations/
│       ├── 20261025135603_PhaseA_Baseline.cs
│       ├── 20261025135603_PhaseA_Baseline.Designer.cs
│       ├── <các migration mới do EF tạo>.cs         + chỉ khi schema thay đổi
│       └── ApplicationDbContextModelSnapshot.cs
│
├── Helpers/
│   ├── VietnamPhoneAttribute.cs
│   ├── PasswordStrengthAttribute.cs
│   ├── DisplayHelper.cs
│   └── BusinessTime.cs                             + thống nhất giờ nghiệp vụ Việt Nam
├── Constants/AppConstants.cs
├── Properties/launchSettings.json
│
├── Views/
│   ├── _ViewImports.cshtml
│   ├── _ViewStart.cshtml
│   ├── Home/{Index,Privacy}.cshtml
│   ├── Account/
│   │   ├── Register.cshtml
│   │   ├── Login.cshtml
│   │   ├── ForgotPassword.cshtml
│   │   ├── ForgotPasswordConfirmation.cshtml
│   │   ├── ResetPassword.cshtml
│   │   ├── ResetPasswordConfirmation.cshtml
│   │   ├── ChangePassword.cshtml
│   │   └── AccessDenied.cshtml
│   ├── Profile/{Index,Edit}.cshtml
│   ├── Court/{Search,Detail}.cshtml
│   ├── Booking/
│   │   ├── Create.cshtml
│   │   ├── Confirmation.cshtml
│   │   ├── MyBookings.cshtml
│   │   ├── Detail.cshtml
│   │   ├── Cancel.cshtml
│   │   └── Review.cshtml
│   └── Shared/
│       ├── _Layout.cshtml
│       ├── _Layout.cshtml.css
│       ├── _Alerts.cshtml
│       ├── _CourtCard.cshtml
│       ├── _Pagination.cshtml
│       ├── _StarRating.cshtml
│       ├── _ValidationScriptsPartial.cshtml
│       ├── Error.cshtml
│       ├── NotFound.cshtml
│       └── ServerError.cshtml
│
├── wwwroot/
│   ├── css/site.css
│   ├── js/site.js
│   ├── images/hero-pattern.svg
│   ├── lib/                                       các thư viện giao diện hiện có
│   ├── favicon.ico
│   └── uploads/                                   + sinh khi upload, không đưa ảnh thật vào seed
│       ├── avatars/
│       ├── complexes/
│       └── courts/
│
├── tests/
│   ├── README.md
│   ├── requirements.txt
│   ├── regression.py                              ~ bổ sung ca cho các nghiệp vụ mới
│   ├── support.py                                 + dùng chung fixture/test server khi tách test
│   ├── owner_management.py                        + quyền sở hữu, CRUD, xóa mềm, giờ chồng lấn
│   ├── reports_reviews.py                         + số liệu, phản hồi, dữ liệu đã ẩn
│   └── refunds.py                                 + hoàn lặp, hoàn một phần, kết quả không rõ
│
└── docs/
    ├── LO_TRINH_VA_CAU_TRUC_HOAN_THIEN.md
    ├── THANH_TOAN_VNPAY.md
    ├── DATABASE.md                                + ERD và giải thích quan hệ
    ├── USE_CASE.md                                + đối chiếu sơ đồ với chức năng cuối
    ├── BUSINESS_RULES.md                          + quy tắc đặt/hủy/giá/doanh thu
    ├── TEST_PLAN.md                               + ca thử và kết quả nghiệm thu
    ├── DEPLOYMENT.md                              + cấu hình chạy thử/triển khai
    └── DEMO.md                                    + tài khoản demo và kịch bản bảo vệ
```

Ký hiệu `{Index,Detail}.cshtml` là cách viết gọn hai file `Index.cshtml` và `Detail.cshtml`. Các thư mục `bin`, `obj`, `.vs`, cấu hình riêng của IDE và file sinh tạm không phải phần cấu trúc mã nguồn bàn giao.

**File bổ sung nếu mở rộng phạm vi**

| Phần mở rộng | File dự kiến |
|---|---|
| MoMo | `Services/Interfaces/IMomoService.cs`, `Services/Implementations/MomoService.cs`; mở rộng `PaymentController.cs`, cấu hình và test tương ứng |
| Ngày lễ | `Models/Entities/Holiday.cs`, `Areas/Admin/Controllers/HolidaysController.cs`, `Services/Interfaces/IHolidayService.cs`, `Services/Implementations/HolidayService.cs`, `Models/ViewModels/Admin/HolidayViewModels.cs`, các view dưới `Areas/Admin/Views/Holidays`, migration và test tương ứng |
| Thông báo | `Controllers/NotificationsController.cs`, `Services/Interfaces/INotificationService.cs`, `Services/Implementations/NotificationService.cs`, `Models/ViewModels/Notification/NotificationViewModels.cs`, `Views/Notifications/Index.cshtml`, `Views/Shared/_NotificationMenu.cshtml`, test tương ứng |

Quy tắc bán khung giờ và giá đã chốt ngày 14/11/2026 tại [BUSINESS_RULES.md](BUSINESS_RULES.md): đặt nhiều khung phải liền nhau, chặn sửa giờ khi còn booking đang giữ chỗ chưa kết thúc, chưa hỗ trợ qua nửa đêm và chưa áp dụng giá lễ. Dùng tài liệu này làm tiêu chí triển khai và nghiệm thu quản lý giờ/giá.

**Điểm cần giữ đúng khi triển khai**

- Kiểm tra quyền sở hữu tại server ở mọi thao tác quản lý, không chỉ ẩn nút trên giao diện.
- Xóa/ngừng sân không làm biến mất booking và đánh giá cũ. Booking đã chốt phải hiển thị đúng giờ, giá của thời điểm đặt; cần lưu thông tin lịch sử phù hợp thay vì chỉ đọc khung giờ hiện tại.
- Thay đổi ngày/giờ/giá phải kiểm tra ảnh hưởng tới lịch đã đặt, không cập nhật ngược số tiền đã chốt.
- Doanh thu cần có một định nghĩa rõ: theo ngày thu tiền hay ngày sử dụng; hoàn tiền theo ngày nào. Không cộng các khoản chưa thu hoặc trừ hai lần khoản hoàn.
- Hoàn tiền có trạng thái yêu cầu, kết quả cổng và đối soát. Không ghi “Đã hoàn tiền” chỉ vì đã bấm gửi yêu cầu.
- Migration tạo từ thay đổi model; không sửa migration cũ đã áp dụng để thay đổi schema hiện có.
- Test và tài liệu được cập nhật theo từng chức năng, đợt cuối chỉ tổng kiểm tra và chuẩn bị bàn giao.
