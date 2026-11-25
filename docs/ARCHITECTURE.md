# ERD và use case SportBooking

Mô hình lấy từ `ApplicationDbContext` và migration hiện tại. Các cột nhạy cảm của Identity và thành phần kỹ thuật được lược bớt để đọc sơ đồ.

```mermaid
erDiagram
    USERS ||--o{ USER_ROLES : "được cấp"
    ROLES ||--o{ USER_ROLES : "xác định"
    USERS ||--o{ SPORT_COMPLEXES : "sở hữu"
    USERS ||--o| OWNER_TRANSFER_PROFILES : "cấu hình QR"
    USERS ||--o{ BOOKINGS : "đặt"
    USERS ||--o{ REVIEWS : "viết"
    USERS ||--o{ NOTIFICATIONS : "nhận"
    SPORT_COMPLEXES ||--o{ COURTS : "chứa"
    SPORT_TYPES ||--o{ COURTS : "phân loại"
    COURTS ||--o{ TIME_SLOTS : "bán"
    COURTS ||--o{ BOOKINGS : "lịch sân"
    COURTS ||--o{ REVIEWS : "được đánh giá"
    TIME_SLOTS ||--o{ BOOKINGS : "tham chiếu"
    BOOKINGS ||--o{ PAYMENTS : "ghi nhận tiền"
    BOOKINGS ||--o| REVIEWS : "có đánh giá"
    BANK_TRANSFERS o|--o{ BOOKINGS : "nhóm QR"
    BANK_TRANSFERS ||--o{ TRANSFER_REPORTS : "thông báo từ khách"
    TRANSFER_IMAGES ||--o{ OWNER_TRANSFER_PROFILES : "QR hiện dùng"
    TRANSFER_IMAGES ||--o{ BANK_TRANSFERS : "QR tại lúc đặt"
    TRANSFER_IMAGES o|--o{ TRANSFER_REPORTS : "ảnh biên lai"

    BOOKINGS {
        int Id PK
        string BookingGroupCode
        string BankTransferGroupCode FK
        int CourtId FK
        int TimeSlotId FK
        datetime StartDateTime
        datetime EndDateTime
        decimal TotalAmount
        int Status
        int PaymentStatus
    }
    BANK_TRANSFERS {
        string GroupCode PK
        string Reference UK
        string OwnerId
        string CustomerId
        guid QrImageId FK
        decimal Amount
        int State
    }
    PAYMENTS {
        int Id PK
        int BookingId FK
        string TransactionCode UK
        decimal Amount
        datetime PaidAt
        datetime ManualRefundedAt
    }
    REVIEWS {
        int Id PK
        int BookingId FK
        string OwnerReply
        datetime OwnerReplyAt
        bool IsVisible
    }
```

Mỗi booking là một khung. Giờ và giá được chốt trên booking. `BankTransfer` giữ danh tính theo nhóm và tham chiếu ảnh QR tại lúc đặt. Các trường `OwnerId`, `CustomerId` trên nhóm chuyển khoản là dấu vết quyền sở hữu trong ứng dụng. `DemoSeedStates` ghi dấu hoàn tất seed.

## Use case theo vai trò

```mermaid
flowchart LR
    Customer["Khách hàng"] --> Search["Tìm sân và xem lịch"]
    Customer --> Checkout["Chọn khung và đặt lịch"]
    Checkout --> Counter["Thanh toán tại quầy"]
    Checkout --> QR["Xem QR và báo chuyển khoản"]
    Customer --> History["Xem lịch sử, hủy theo điều kiện"]
    Customer --> Review["Đánh giá booking đã hoàn thành"]
    Owner["Owner đã duyệt"] --> Inventory["Quản lý cụm, sân, khung giờ, giá"]
    Owner --> Bank["Cấu hình tài khoản và QR"]
    Owner --> Settlement["Xác nhận tại quầy, đối soát QR, hoàn thủ công"]
    Owner --> Complete["Hoàn thành booking sau giờ sử dụng"]
    Owner --> Reply["Phản hồi đánh giá sân của mình"]
    Owner --> Revenue["Báo cáo thu, hoàn và doanh thu ròng"]
    Admin["Admin"] --> Approval["Duyệt Owner và quản lý tài khoản"]
    Admin --> Catalog["Quản lý loại sân"]
    Admin --> Moderation["Ẩn hoặc hiện đánh giá"]
    Admin --> Overview["Xem thống kê toàn hệ thống"]
```

Admin, Owner và khách đăng nhập bằng Identity. Các thao tác ghi đi qua POST có CSRF. Owner quản lý dữ liệu theo quyền sở hữu ở server. [Trạng thái và quy tắc](BUSINESS_RULES.md)
