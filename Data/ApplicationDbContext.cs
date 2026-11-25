// File: Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SportBooking.Models.Entities;

namespace SportBooking.Data;

/// <summary>
/// DbContext chính của ứng dụng.
/// Kế thừa IdentityDbContext để tích hợp ASP.NET Core Identity với ApplicationUser.
/// Toàn bộ cấu hình bảng/cột/index được khai báo tập trung trong OnModelCreating (Fluent API).
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ========== DbSet ==========

    /// <summary>Loại sân thể thao.</summary>
    public DbSet<SportType> SportTypes => Set<SportType>();

    /// <summary>Cụm sân.</summary>
    public DbSet<SportComplex> SportComplexes => Set<SportComplex>();

    /// <summary>Sân con.</summary>
    public DbSet<Court> Courts => Set<Court>();

    /// <summary>Khung giờ và giá.</summary>
    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();

    /// <summary>Lượt đặt sân.</summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>Giao dịch thanh toán.</summary>
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<OwnerTransferProfile> OwnerTransferProfiles => Set<OwnerTransferProfile>();
    public DbSet<BankTransfer> BankTransfers => Set<BankTransfer>();
    public DbSet<TransferReport> TransferReports => Set<TransferReport>();
    public DbSet<TransferImage> TransferImages => Set<TransferImage>();

    /// <summary>Đánh giá của khách.</summary>
    public DbSet<Review> Reviews => Set<Review>();

    /// <summary>Thông báo.</summary>
    public DbSet<Notification> Notifications => Set<Notification>();

    /// <summary>Completion marker for optional demo data.</summary>
    public DbSet<DemoSeedState> DemoSeedStates => Set<DemoSeedState>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Bắt buộc gọi base để Identity cấu hình các bảng AspNetUsers, AspNetRoles...
        base.OnModelCreating(builder);

        ConfigureIdentityTables(builder);
        ConfigureApplicationUser(builder);
        ConfigureSportType(builder);
        ConfigureSportComplex(builder);
        ConfigureCourt(builder);
        ConfigureTimeSlot(builder);
        ConfigureBooking(builder);
        ConfigurePayment(builder);
        ConfigureBankTransfer(builder);
        ConfigureReview(builder);
        ConfigureNotification(builder);
        builder.Entity<DemoSeedState>(entity =>
        {
            entity.ToTable("DemoSeedStates");
            entity.HasKey(e => e.Key);
            entity.Property(e => e.Key).HasMaxLength(100);
            entity.Property(e => e.CompletedAtUtc).IsRequired();
        });
        VietnameseSchema.Configure(builder);
    }

    private static void ConfigureBankTransfer(ModelBuilder builder)
    {
        builder.Entity<OwnerTransferProfile>(entity =>
        {
            entity.HasOne<ApplicationUser>().WithOne().HasForeignKey<OwnerTransferProfile>(p => p.OwnerId);
            entity.HasOne(p => p.QrImage).WithMany().OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<BankTransfer>(entity =>
        {
            entity.Property(t => t.OwnerId).HasMaxLength(450);
            entity.Property(t => t.CustomerId).HasMaxLength(450);
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.HasIndex(t => t.Reference).IsUnique();
            entity.HasOne(t => t.QrImage).WithMany().OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(t => t.Bookings).WithOne(b => b.BankTransfer)
                .HasForeignKey(b => b.BankTransferGroupCode).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(t => t.Reports).WithOne(r => r.Transfer)
                .HasForeignKey(r => r.GroupCode).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<TransferReport>().HasOne(r => r.Receipt).WithMany().OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Đổi tên các bảng Identity cho gọn và nhất quán với phần còn lại của hệ thống.
    /// </summary>
    private static void ConfigureIdentityTables(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(e => e.ToTable("Users"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>(e => e.ToTable("Roles"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>(e => e.ToTable("UserRoles"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>(e => e.ToTable("UserClaims"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>(e => e.ToTable("UserLogins"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>(e => e.ToTable("UserTokens"));
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>(e => e.ToTable("RoleClaims"));
    }

    private static void ConfigureApplicationUser(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(u => u.Address)
                  .HasMaxLength(300);

            entity.Property(u => u.AvatarUrl)
                  .HasMaxLength(500);

            entity.Property(u => u.BusinessName)
                  .HasMaxLength(200);

            entity.Property(u => u.BusinessPhone)
                  .HasMaxLength(20);

            entity.Property(u => u.ApprovalReviewedById)
                  .HasMaxLength(450);

            entity.Property(u => u.ApprovalRejectionReason)
                  .HasMaxLength(1000);

            entity.Property(u => u.IsActive)
                  .HasDefaultValue(true);

            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Index hỗ trợ Admin tra cứu nhanh người dùng theo trạng thái duyệt.
            entity.HasIndex(u => u.ApprovalStatus)
                  .HasDatabaseName("IX_Users_ApprovalStatus");

            entity.HasIndex(u => u.IsActive)
                  .HasDatabaseName("IX_Users_IsActive");
        });
    }

    private static void ConfigureSportType(ModelBuilder builder)
    {
        builder.Entity<SportType>(entity =>
        {
            entity.ToTable("SportTypes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Description)
                  .HasMaxLength(500);

            entity.Property(e => e.IconClass)
                  .HasMaxLength(100);

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Tên loại sân không được trùng.
            entity.HasIndex(e => e.Name)
                  .IsUnique()
                  .HasDatabaseName("UX_SportTypes_Name");
        });
    }

    private static void ConfigureSportComplex(ModelBuilder builder)
    {
        builder.Entity<SportComplex>(entity =>
        {
            entity.ToTable("SportComplexes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Address)
                  .IsRequired()
                  .HasMaxLength(300);

            entity.Property(e => e.Province)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.District)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(e => e.Description)
                  .HasMaxLength(1000);

            entity.Property(e => e.ImageUrl)
                  .HasMaxLength(500);

            entity.Property(e => e.PhoneNumber)
                  .HasMaxLength(20);

            entity.Property(e => e.Email)
                  .HasMaxLength(256);

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Owner 1 - n SportComplex. Xóa Owner thì không cho xóa cứng cụm sân (Restrict).
            entity.HasOne(e => e.Owner)
                  .WithMany(u => u.SportComplexes)
                  .HasForeignKey(e => e.OwnerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OwnerId)
                  .HasDatabaseName("IX_SportComplexes_OwnerId");

            entity.HasIndex(e => new { e.Province, e.District })
                  .HasDatabaseName("IX_SportComplexes_Location");
        });
    }

    private static void ConfigureCourt(ModelBuilder builder)
    {
        builder.Entity<Court>(entity =>
        {
            entity.ToTable("Courts");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(e => e.Description)
                  .HasMaxLength(1000);

            entity.Property(e => e.ImageUrl)
                  .HasMaxLength(500);

            entity.Property(e => e.Status)
                  .HasConversion<int>();

            entity.Property(e => e.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // SportComplex 1 - n Court
            entity.HasOne(e => e.SportComplex)
                  .WithMany(c => c.Courts)
                  .HasForeignKey(e => e.SportComplexId)
                  .OnDelete(DeleteBehavior.Cascade);

            // SportType 1 - n Court (không cho xóa loại sân nếu còn sân dùng)
            entity.HasOne(e => e.SportType)
                  .WithMany(t => t.Courts)
                  .HasForeignKey(e => e.SportTypeId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.SportComplexId)
                  .HasDatabaseName("IX_Courts_SportComplexId");

            entity.HasIndex(e => e.SportTypeId)
                  .HasDatabaseName("IX_Courts_SportTypeId");

            // Index tổng hợp phục vụ tìm kiếm sân đang hoạt động theo loại + trạng thái.
            entity.HasIndex(e => new { e.Status, e.IsDeleted })
                  .HasDatabaseName("IX_Courts_Status_IsDeleted");

            // Global query filter: mặc định ẩn sân đã xóa mềm.
            // Dùng .IgnoreQueryFilters() khi cần truy vấn cả sân đã xóa.
            entity.HasQueryFilter(e => !e.IsDeleted);
        });
    }

    private static void ConfigureTimeSlot(ModelBuilder builder)
    {
        builder.Entity<TimeSlot>(entity =>
        {
            entity.ToTable("TimeSlots");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.PriceWeekday)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PriceWeekend)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.PriceHoliday)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.IsActive)
                  .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Court 1 - n TimeSlot
            entity.HasOne(e => e.Court)
                  .WithMany(c => c.TimeSlots)
                  .HasForeignKey(e => e.CourtId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Một sân không thể có 2 khung giờ trùng giờ bắt đầu.
            entity.HasIndex(e => new { e.CourtId, e.StartTime })
                  .IsUnique()
                  .HasDatabaseName("UX_TimeSlots_Court_Start");

            entity.HasIndex(e => e.CourtId)
                  .HasDatabaseName("IX_TimeSlots_CourtId");
        });
    }

    private static void ConfigureBooking(ModelBuilder builder)
    {
        builder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.BookingCode)
                  .IsRequired()
                  .HasMaxLength(30);

            entity.Property(e => e.BookingGroupCode)
                  .IsRequired()
                  .HasMaxLength(30);

            entity.Property(e => e.TotalAmount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Status)
                  .HasConversion<int>();

            entity.Property(e => e.PaymentStatus)
                  .HasConversion<int>();

            entity.Property(e => e.PaymentMethod)
                  .HasConversion<int>();

            entity.Property(e => e.Note)
                  .HasMaxLength(500);

            entity.Property(e => e.CancellationReason)
                  .HasMaxLength(500);

            entity.Property(e => e.CancelledBy)
                  .HasMaxLength(50);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // RowVersion: SQL Server tự sinh giá trị rowversion, EF dùng để phát hiện xung đột.
            entity.Property(e => e.RowVersion)
                  .IsRowVersion()
                  .IsConcurrencyToken();

            entity.Property(e => e.BookingDate)
                  .HasColumnType("date");

            // Customer 1 - n Booking (không cho xóa user nếu còn booking)
            entity.HasOne(e => e.Customer)
                  .WithMany(u => u.Bookings)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Court 1 - n Booking
            entity.HasOne(e => e.Court)
                  .WithMany(c => c.Bookings)
                  .HasForeignKey(e => e.CourtId)
                  .OnDelete(DeleteBehavior.Restrict);

            // TimeSlot 1 - n Booking
            entity.HasOne(e => e.TimeSlot)
                  .WithMany(t => t.Bookings)
                  .HasForeignKey(e => e.TimeSlotId)
                  .OnDelete(DeleteBehavior.Restrict);

            // ===== CHỐNG ĐẶT TRÙNG (tầng database) =====
            // Unique index CÓ ĐIỀU KIỆN: chỉ áp dụng cho các bản ghi đang giữ chỗ.
            // Trạng thái giữ chỗ: Pending (0), Confirmed (1), Completed (2).
            // Cancelled (3), Expired (4), NoShow (5) không nằm trong filter
            // => khung giờ được giải phóng và có thể đặt lại.
            entity.HasIndex(e => new { e.CourtId, e.TimeSlotId, e.BookingDate })
                  .IsUnique()
                  .HasFilter("[TrangThai] IN (0, 1, 2)")
                  .HasDatabaseName("UX_Bookings_Court_Slot_Date_Active");

            // Mã booking duy nhất toàn hệ thống.
            entity.HasIndex(e => e.BookingCode)
                  .IsUnique()
                  .HasDatabaseName("UX_Bookings_BookingCode");

            entity.HasIndex(e => e.CustomerId)
                  .HasDatabaseName("IX_Bookings_CustomerId");

            // Index phục vụ BackgroundService quét booking quá hạn.
            entity.HasIndex(e => new { e.Status, e.PaymentDeadline })
                  .HasDatabaseName("IX_Bookings_Status_PaymentDeadline");

            // Index phục vụ báo cáo doanh thu theo thời gian.
            entity.HasIndex(e => new { e.BookingDate, e.Status })
                  .HasDatabaseName("IX_Bookings_Date_Status");

            entity.HasIndex(e => e.BookingGroupCode)
                  .HasDatabaseName("IX_Bookings_BookingGroupCode");
        });
    }

    private static void ConfigurePayment(ModelBuilder builder)
    {
        builder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.TransactionCode)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.Amount)
                  .HasColumnType("decimal(18,2)");

            entity.Property(e => e.Method)
                  .HasConversion<int>();

            entity.Property(e => e.Status)
                  .HasConversion<int>();

            entity.Property(e => e.GatewayTransactionId)
                  .HasMaxLength(100);

            entity.Property(e => e.BankCode)
                  .HasMaxLength(50);

            entity.Property(e => e.ResponseCode)
                  .HasMaxLength(50);

            entity.Property(e => e.ResponseMessage)
                  .HasMaxLength(500);

            entity.Property(e => e.RawData)
                  .HasMaxLength(4000);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Booking 1 - n Payment
            entity.HasOne(e => e.Booking)
                  .WithMany(b => b.Payments)
                  .HasForeignKey(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Mã giao dịch nội bộ không được trùng (phục vụ đối soát IPN).
            entity.HasIndex(e => e.TransactionCode)
                  .IsUnique()
                  .HasDatabaseName("UX_Payments_TransactionCode");

            entity.HasIndex(e => e.BookingId)
                  .HasDatabaseName("IX_Payments_BookingId");
        });
    }

    private static void ConfigureReview(ModelBuilder builder)
    {
        builder.Entity<Review>(entity =>
        {
            entity.ToTable("Reviews");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Comment)
                  .HasMaxLength(1000);

            entity.Property(e => e.OwnerReply)
                  .HasMaxLength(1000);

            entity.Property(e => e.IsVisible)
                  .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            // Booking 1 - 1 Review (mỗi booking chỉ đánh giá 1 lần)
            entity.HasOne(e => e.Booking)
                  .WithOne(b => b.Review)
                  .HasForeignKey<Review>(e => e.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Court 1 - n Review
            entity.HasOne(e => e.Court)
                  .WithMany(c => c.Reviews)
                  .HasForeignKey(e => e.CourtId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Customer 1 - n Review
            entity.HasOne(e => e.Customer)
                  .WithMany(u => u.Reviews)
                  .HasForeignKey(e => e.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.BookingId)
                  .IsUnique()
                  .HasDatabaseName("UX_Reviews_BookingId");

            entity.HasIndex(e => e.CourtId)
                  .HasDatabaseName("IX_Reviews_CourtId");

            // Ràng buộc số sao 1-5 ở tầng database.
            entity.ToTable(t => t.HasCheckConstraint("CK_Reviews_Rating", "[SoSao] BETWEEN 1 AND 5"));
        });
    }

    private static void ConfigureNotification(ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(e => e.Message)
                  .IsRequired()
                  .HasMaxLength(1000);

            entity.Property(e => e.Url)
                  .HasMaxLength(500);

            entity.Property(e => e.Type)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(e => e.IsRead)
                  .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Notifications)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.IsRead })
                  .HasDatabaseName("IX_Notifications_User_Read");
        });
    }
}
