// File: Program.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Data;
using SportBooking.Models.Entities;
using SportBooking.Models.ViewModels.Shared;
using SportBooking.Services.Implementations;
using SportBooking.Services.Expiration;
using SportBooking.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

#if SPORTBOOKING_E2E
SportBooking.Helpers.ScheduleGuard.ConfigureTestClock(builder.Environment);
#endif

// =========================================================================
// 1. CẤU HÌNH DATABASE
// =========================================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối 'DefaultConnection' trong appsettings.json.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        // Tự động thử lại khi kết nối SQL Server tạm thời gián đoạn.
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);

        // Đặt timeout cho câu lệnh truy vấn.
        sqlOptions.CommandTimeout(60);
    });

    // Do not log EF parameter values: startup creates Identity users and payment data.
});

// =========================================================================
// 2. CẤU HÌNH IDENTITY
// =========================================================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // --- Quy tắc mật khẩu: tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt ---
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;

    // --- Khóa tài khoản tạm thời khi nhập sai 5 lần ---
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // --- Yêu cầu email là duy nhất, chưa cần xác thực email ---
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Đường dẫn chuyển hướng khi chưa đăng nhập / không đủ quyền.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "SportBooking.Auth";
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Kiểm tra SecurityStamp ở mỗi request để phiên đăng nhập bị vô hiệu
// ngay sau khi Admin khóa tài khoản hoặc thay đổi thông tin bảo mật.
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.Zero;
});

// =========================================================================
// 3. BIND CẤU HÌNH NGHIỆP VỤ TỪ appsettings.json
// =========================================================================
builder.Services.Configure<BookingSettings>(
    builder.Configuration.GetSection(BookingSettings.SectionName));

builder.Services.Configure<VnPaySettings>(
    builder.Configuration.GetSection(VnPaySettings.SectionName));

builder.Services.Configure<MomoSettings>(
    builder.Configuration.GetSection(MomoSettings.SectionName));

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection(EmailSettings.SectionName));

builder.Services.Configure<FileUploadSettings>(
    builder.Configuration.GetSection(FileUploadSettings.SectionName));

// =========================================================================
// 3.1. ĐĂNG KÝ CÁC SERVICE NGHIỆP VỤ (Dependency Injection)
// =========================================================================
// --- Email: chọn implementation theo cấu hình EmailSettings.UseConsoleSender ---
// Ở môi trường phát triển, dùng ConsoleEmailService để không cần SMTP thật
// mà vẫn lấy được link đặt lại mật khẩu từ log.
var emailSettings = builder.Configuration
    .GetSection(EmailSettings.SectionName)
    .Get<EmailSettings>() ?? new EmailSettings();

if (emailSettings.UseConsoleSender)
{
    builder.Services.AddScoped<IEmailService, ConsoleEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, SmtpEmailService>();
}

// --- Upload file ảnh ---
builder.Services.AddScoped<IFileUploadService, FileUploadService>();

// --- Nghiệp vụ tài khoản và hồ sơ ---
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// --- Nghiệp vụ sân và đặt sân ---
builder.Services.AddScoped<ICourtService, CourtService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddScoped<TransferProfileService>();
builder.Services.AddScoped<BankTransferService>();
builder.Services.AddScoped<IBookingExpirationService, BookingExpirationService>();
builder.Services.AddScoped<IOwnerBookingService, OwnerBookingService>();
builder.Services.AddScoped<IOwnerReviewService, OwnerReviewService>();
builder.Services.AddScoped<IOwnerRevenueService, OwnerRevenueService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddHostedService<BookingExpirationBackgroundService>();

// =========================================================================
// 4. MVC + RAZOR RUNTIME COMPILATION
// =========================================================================
builder.Services.AddControllersWithViews();

// Cho phép Razor view được biên dịch lúc runtime (tiện khi phát triển).
builder.Services.AddRazorPages();

// Hỗ trợ đọc thông tin người dùng hiện tại từ HttpContext.
builder.Services.AddHttpContextAccessor();

// =========================================================================
// 5. SESSION (dùng cho giỏ khung giờ đang chọn, thông báo, v.v.)
// =========================================================================
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = "SportBooking.Session";
});

// =========================================================================
// 6. XỬ LÝ LỖI TOÀN CỤC
// =========================================================================
// Tùy chỉnh trang lỗi mặc định: 404 -> /Home/NotFound, 500 -> /Home/ServerError.
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// =========================================================================
// 7. PIPELINE XỬ LÝ REQUEST
// =========================================================================
// Chỉ nhận thông tin từ reverse proxy cùng máy (loopback mặc định).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    // Môi trường production: dùng trang lỗi thân thiện, không lộ stack trace.
    app.UseExceptionHandler("/Home/ServerError");
    app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Phục vụ file tĩnh trong wwwroot (bao gồm ảnh upload).
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// =========================================================================
// 8. ROUTING: Areas + Route mặc định
// =========================================================================
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =========================================================================
// 9. DATABASE INIT: migration, required roles, optional first Admin, optional demo
// =========================================================================
var applyMigrations = builder.Configuration.GetValue<bool>("DatabaseInitialization:ApplyMigrations");
var seedDemo = builder.Configuration.GetValue<bool>("DatabaseInitialization:SeedDemoData");
var createInitialAdmin = builder.Configuration.GetValue<bool>("DatabaseInitialization:CreateInitialAdmin");
if (seedDemo && app.Environment.IsProduction())
    throw new InvalidOperationException("DatabaseInitialization:SeedDemoData không được bật trong Production.");

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        if (applyMigrations)
        {
            try
            {
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migration đã hoàn tất.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Database migration thất bại; ứng dụng không khởi động. "
                    + "Kiểm tra kết nối, quyền database và các migration đang chờ.", ex);
            }
        }

        await DbSeeder.SeedRolesAsync(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(), logger);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        if (createInitialAdmin)
            await DbSeeder.SeedInitialAdminAsync(context, userManager,
                builder.Configuration["InitialAdmin:Email"], builder.Configuration["InitialAdmin:Password"], logger);
        if (seedDemo)
            await DbSeeder.SeedDemoAsync(context, userManager, logger);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Khởi tạo database thất bại; ứng dụng dừng. "
            + "Nếu tắt ApplyMigrations, hãy chạy migration thủ công trước khi khởi động.");
        throw;
    }
}

app.Run();
