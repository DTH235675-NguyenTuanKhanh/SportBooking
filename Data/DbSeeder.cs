// File: Data/DbSeeder.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SportBooking.Constants;
using SportBooking.Models.Entities;

namespace SportBooking.Data;

/// <summary>
/// Separates required Identity roles, the optional first Admin, and the optional demo dataset.
/// </summary>
public static class DbSeeder
{
    private const string DemoSeedKey = "SportBookingDemoV1";
    private const string DemoPassword = "Sport@123";

    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (var roleName in AppConstants.Roles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName)) continue;
            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded && !await roleManager.RoleExistsAsync(roleName))
            {
                throw new InvalidOperationException($"Không tạo được role {roleName}: "
                    + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            logger.LogInformation("Đã bảo đảm role {Role}.", roleName);
        }
    }

    public static async Task SeedInitialAdminAsync(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager, string? email, string? password, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("InitialAdmin:Email và InitialAdmin:Password phải được cấp qua biến môi trường hoặc User Secrets.");

        email = email.Trim();
        var candidate = new ApplicationUser
        {
            UserName = email, Email = email, EmailConfirmed = true,
            FullName = "Quản trị viên hệ thống", IsActive = true,
            ApprovalStatus = OwnerApprovalStatus.NotApplicable
        };
        foreach (var validator in userManager.PasswordValidators)
        {
            var validation = await validator.ValidateAsync(userManager, candidate, password);
            if (!validation.Succeeded)
                throw new InvalidOperationException("InitialAdmin:Password không đạt yêu cầu Identity: "
                    + string.Join(", ", validation.Errors.Select(e => e.Description)));
        }

        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            var existing = await userManager.FindByEmailAsync(email);
            if (existing is not null)
            {
                if (!await userManager.IsInRoleAsync(existing, AppConstants.Roles.Admin))
                    throw new InvalidOperationException("Email Admin khởi tạo đã thuộc tài khoản không có role Admin; không tự nâng quyền.");
                logger.LogInformation("Admin khởi tạo đã tồn tại; giữ nguyên mật khẩu và trạng thái tài khoản.");
                return;
            }

            var created = await userManager.CreateAsync(candidate, password);
            if (!created.Succeeded)
                throw new InvalidOperationException("Không tạo được Admin khởi tạo: "
                    + string.Join(", ", created.Errors.Select(e => e.Description)));
            var assigned = await userManager.AddToRoleAsync(candidate, AppConstants.Roles.Admin);
            if (!assigned.Succeeded)
                throw new InvalidOperationException("Không gán được role Admin khởi tạo: "
                    + string.Join(", ", assigned.Errors.Select(e => e.Description)));
            await transaction.CommitAsync();
            logger.LogInformation("Đã tạo Admin khởi tạo.");
        });
    }

    public static async Task SeedDemoAsync(ApplicationDbContext context,
        UserManager<ApplicationUser> userManager, ILogger logger)
    {
        await context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            context.ChangeTracker.Clear();
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (await context.DemoSeedStates.AnyAsync(s => s.Key == DemoSeedKey))
            {
                logger.LogInformation("Dữ liệu demo đã hoàn tất trước đó; không tạo lại.");
                return;
            }

            // Old versions had no completion marker. Do not mistake one complex for a full seed.
            var legacyDemo = await context.Users.AnyAsync(u => u.NormalizedEmail == "OWNER1@SPORTBOOKING.VN"
                || u.NormalizedEmail == "CUSTOMER1@SPORTBOOKING.VN")
                || await context.SportComplexes.AnyAsync(c => c.Name == "Cụm sân Thể Thao Hùng Phát - Quận 5");
            if (legacyDemo)
                throw new InvalidOperationException("Có dấu vết demo từ phiên bản cũ nhưng chưa có dấu mốc hoàn tất. "
                    + "Hãy sao lưu và đối chiếu dữ liệu trước khi bật seed demo; dữ liệu hiện có không bị sửa.");

            var users = await SeedUsersAsync(userManager, logger);
            await SeedBusinessDataAsync(context, users, logger);
            context.DemoSeedStates.Add(new DemoSeedState { Key = DemoSeedKey, CompletedAtUtc = DateTime.UtcNow });
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            logger.LogInformation("Đã khởi tạo dữ liệu demo đầy đủ.");
        });
    }

    // =========================================================================
    // 2. USERS
    // =========================================================================
    /// <summary>
    /// Kết quả seed user, dùng để tham chiếu ở bước seed dữ liệu nghiệp vụ.
    /// </summary>
    private sealed class SeededUsers
    {
        public ApplicationUser Admin { get; set; } = null!;
        public ApplicationUser Owner1 { get; set; } = null!;
        public ApplicationUser Owner2 { get; set; } = null!;
        public ApplicationUser Owner3Pending { get; set; } = null!;
        public ApplicationUser Customer1 { get; set; } = null!;
        public ApplicationUser Customer2 { get; set; } = null!;
        public ApplicationUser Customer3 { get; set; } = null!;
        public ApplicationUser Customer4Locked { get; set; } = null!;
    }

    private static async Task<SeededUsers> SeedUsersAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        var result = new SeededUsers
        {
            Admin = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "admin@sportbooking.vn",
                Email = "admin@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Quản trị viên hệ thống",
                PhoneNumber = "0901234567",
                Address = "1 Lê Duẩn, Quận 1, TP. Hồ Chí Minh",
                DateOfBirth = new DateTime(1990, 5, 20),
                IsActive = true,
                ApprovalStatus = OwnerApprovalStatus.NotApplicable,
                CreatedAt = DateTime.UtcNow.AddMonths(-12)
            }, AppConstants.Roles.Admin),

            Owner1 = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "owner1@sportbooking.vn",
                Email = "owner1@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Nguyễn Văn Hùng",
                PhoneNumber = "0912345678",
                Address = "25 Nguyễn Trãi, Quận 5, TP. Hồ Chí Minh",
                DateOfBirth = new DateTime(1985, 3, 12),
                BusinessName = "Công ty TNHH Thể Thao Hùng Phát",
                BusinessPhone = "028 3838 1234",
                IsActive = true,
                ApprovalStatus = OwnerApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddMonths(-10)
            }, AppConstants.Roles.Owner),

            Owner2 = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "owner2@sportbooking.vn",
                Email = "owner2@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Trần Thị Mai",
                PhoneNumber = "0923456789",
                Address = "88 Phạm Văn Đồng, TP. Thủ Đức, TP. Hồ Chí Minh",
                DateOfBirth = new DateTime(1988, 8, 25),
                BusinessName = "Cụm sân Thể Thao Mai Anh",
                BusinessPhone = "028 3722 5678",
                IsActive = true,
                ApprovalStatus = OwnerApprovalStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddMonths(-8)
            }, AppConstants.Roles.Owner),

            // Owner đang chờ duyệt - dùng để test chức năng duyệt tài khoản của Admin.
            Owner3Pending = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "owner3@sportbooking.vn",
                Email = "owner3@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Lê Quốc Bảo",
                PhoneNumber = "0934567890",
                Address = "12 Trần Phú, TP. Nha Trang, Khánh Hòa",
                DateOfBirth = new DateTime(1992, 11, 3),
                BusinessName = "Sân bóng Bảo Long",
                BusinessPhone = "0258 3555 999",
                IsActive = true,
                ApprovalStatus = OwnerApprovalStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }, AppConstants.Roles.Owner),

            Customer1 = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "customer1@sportbooking.vn",
                Email = "customer1@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Phạm Minh Tuấn",
                PhoneNumber = "0945678901",
                Address = "45 Võ Văn Ngân, TP. Thủ Đức, TP. Hồ Chí Minh",
                DateOfBirth = new DateTime(1998, 1, 15),
                IsActive = true,
                ApprovalStatus = OwnerApprovalStatus.NotApplicable,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            }, AppConstants.Roles.Customer),

            Customer2 = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "customer2@sportbooking.vn",
                Email = "customer2@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Hoàng Thị Lan",
                PhoneNumber = "0956789012",
                Address = "102 Cách Mạng Tháng 8, Quận 3, TP. Hồ Chí Minh",
                DateOfBirth = new DateTime(2000, 7, 22),
                IsActive = true,
                ApprovalStatus = OwnerApprovalStatus.NotApplicable,
                CreatedAt = DateTime.UtcNow.AddMonths(-5)
            }, AppConstants.Roles.Customer),

            Customer3 = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "customer3@sportbooking.vn",
                Email = "customer3@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Đỗ Anh Khoa",
                PhoneNumber = "0967890123",
                Address = "77 Nguyễn Văn Cừ, TP. Biên Hòa, Đồng Nai",
                DateOfBirth = new DateTime(1995, 4, 8),
                IsActive = true,
                ApprovalStatus = OwnerApprovalStatus.NotApplicable,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            }, AppConstants.Roles.Customer),

            // Tài khoản bị khóa - dùng để test chức năng khóa tài khoản và thông báo lý do.
            Customer4Locked = await CreateUserIfNotExistsAsync(userManager, logger, new ApplicationUser
            {
                UserName = "customer4@sportbooking.vn",
                Email = "customer4@sportbooking.vn",
                EmailConfirmed = true,
                FullName = "Bùi Thanh Sơn",
                PhoneNumber = "0978901234",
                Address = "33 Lý Thường Kiệt, TP. Huế",
                DateOfBirth = new DateTime(1993, 9, 30),
                IsActive = false,
                ApprovalStatus = OwnerApprovalStatus.NotApplicable,
                CreatedAt = DateTime.UtcNow.AddMonths(-4)
            }, AppConstants.Roles.Customer)
        };

        return result;
    }

    /// <summary>
    /// Tạo user nếu chưa tồn tại (theo email) và gán role.
    /// Trả về user đã có trong database.
    /// </summary>
    private static async Task<ApplicationUser> CreateUserIfNotExistsAsync(
        UserManager<ApplicationUser> userManager,
        ILogger logger,
        ApplicationUser user,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(user.Email!);
        if (existing is not null)
        {
            if (!await userManager.IsInRoleAsync(existing, role))
                throw new InvalidOperationException($"Email demo {user.Email} đã thuộc tài khoản không có role {role}; không tự nâng quyền.");
            return existing;
        }

        var createResult = await userManager.CreateAsync(user, DemoPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Không tạo được user {user.Email}: " +
                string.Join(", ", createResult.Errors.Select(e => e.Description)));
        }

        var assigned = await userManager.AddToRoleAsync(user, role);
        if (!assigned.Succeeded)
            throw new InvalidOperationException($"Không gán được role {role} cho tài khoản demo: "
                + string.Join(", ", assigned.Errors.Select(e => e.Description)));
        logger.LogInformation("Đã tạo user {Email} với role {Role}", user.Email, role);

        return user;
    }

    // =========================================================================
    // 3. DỮ LIỆU NGHIỆP VỤ
    // =========================================================================
    private static async Task SeedBusinessDataAsync(
        ApplicationDbContext context,
        SeededUsers users,
        ILogger logger)
    {
        // ---------- 3.1 Loại sân ----------
        var sportTypes = new List<SportType>
        {
            new()
            {
                Name = "Bóng đá mini",
                Description = "Sân bóng đá mini 5 người hoặc 7 người, mặt cỏ nhân tạo.",
                IconClass = "bi-dribbble",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Name = "Cầu lông",
                Description = "Sân cầu lông trong nhà, thảm tiêu chuẩn thi đấu.",
                IconClass = "bi-feather",
                DisplayOrder = 2,
                IsActive = true
            },
            new()
            {
                Name = "Tennis",
                Description = "Sân tennis ngoài trời và trong nhà, mặt sân cứng.",
                IconClass = "bi-circle",
                DisplayOrder = 3,
                IsActive = true
            },
            new()
            {
                Name = "Bóng rổ",
                Description = "Sân bóng rổ tiêu chuẩn, có thể chơi 3x3 hoặc 5x5.",
                IconClass = "bi-basketball",
                DisplayOrder = 4,
                IsActive = true
            },
            new()
            {
                Name = "Pickleball",
                Description = "Sân pickleball mới, phù hợp mọi lứa tuổi.",
                IconClass = "bi-hexagon",
                DisplayOrder = 5,
                IsActive = true
            },
            new()
            {
                Name = "Bóng chuyền",
                Description = "Sân bóng chuyền trong nhà và bãi biển.",
                IconClass = "bi-broadcast",
                DisplayOrder = 6,
                IsActive = true
            }
        };

        await context.SportTypes.AddRangeAsync(sportTypes);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} loại sân.", sportTypes.Count);

        var football = sportTypes.First(t => t.Name == "Bóng đá mini");
        var badminton = sportTypes.First(t => t.Name == "Cầu lông");
        var tennis = sportTypes.First(t => t.Name == "Tennis");
        var basketball = sportTypes.First(t => t.Name == "Bóng rổ");
        var pickleball = sportTypes.First(t => t.Name == "Pickleball");

        // ---------- 3.2 Cụm sân ----------
        var complexes = new List<SportComplex>
        {
            new()
            {
                OwnerId = users.Owner1.Id,
                Name = "Cụm sân Thể Thao Hùng Phát - Quận 5",
                Address = "128 Nguyễn Trãi, Phường 3",
                Province = "TP. Hồ Chí Minh",
                District = "Quận 5",
                Description = "Cụm sân thể thao đa năng với 8 sân bóng đá mini, 4 sân cầu lông và 2 sân tennis. " +
                              "Có bãi giữ xe rộng, phòng thay đồ, căng tin và hệ thống đèn LED chiếu sáng ban đêm.",
                PhoneNumber = "028 3838 1234",
                Email = "hungphat@sportbooking.vn",
                OpeningTime = new TimeSpan(6, 0, 0),
                ClosingTime = new TimeSpan(23, 0, 0),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-9)
            },
            new()
            {
                OwnerId = users.Owner1.Id,
                Name = "Cụm sân Thể Thao Hùng Phát - Thủ Đức",
                Address = "256 Võ Văn Ngân, Phường Bình Thọ",
                Province = "TP. Hồ Chí Minh",
                District = "TP. Thủ Đức",
                Description = "Cơ sở 2 của Hùng Phát, tập trung vào pickleball và bóng rổ. " +
                              "Sân mái che, hoạt động cả khi trời mưa.",
                PhoneNumber = "028 3722 8899",
                Email = "thuduc@sportbooking.vn",
                OpeningTime = new TimeSpan(5, 30, 0),
                ClosingTime = new TimeSpan(22, 30, 0),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-7)
            },
            new()
            {
                OwnerId = users.Owner2.Id,
                Name = "Cụm sân Mai Anh - Phạm Văn Đồng",
                Address = "88 Phạm Văn Đồng, Phường Hiệp Bình Chánh",
                Province = "TP. Hồ Chí Minh",
                District = "TP. Thủ Đức",
                Description = "Cụm sân cầu lông và tennis chất lượng cao, thảm Yonex chính hãng. " +
                              "Có huấn luyện viên cho thuê theo giờ.",
                PhoneNumber = "028 3722 5678",
                Email = "maianh@sportbooking.vn",
                OpeningTime = new TimeSpan(6, 0, 0),
                ClosingTime = new TimeSpan(22, 0, 0),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-6)
            },
            new()
            {
                OwnerId = users.Owner2.Id,
                Name = "Trung tâm Thể thao Mai Anh - Quận 1",
                Address = "15 Lê Thánh Tôn, Phường Bến Nghé",
                Province = "TP. Hồ Chí Minh",
                District = "Quận 1",
                Description = "Trung tâm thể thao cao cấp ngay trung tâm thành phố, " +
                              "phù hợp cho các giải đấu doanh nghiệp.",
                PhoneNumber = "028 3822 1111",
                Email = "quan1@sportbooking.vn",
                OpeningTime = new TimeSpan(6, 0, 0),
                ClosingTime = new TimeSpan(23, 30, 0),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-4)
            }
        };

        await context.SportComplexes.AddRangeAsync(complexes);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} cụm sân.", complexes.Count);

        var complexHungPhatQ5 = complexes[0];
        var complexHungPhatTD = complexes[1];
        var complexMaiAnhPVD = complexes[2];
        var complexMaiAnhQ1 = complexes[3];

        // ---------- 3.3 Sân con ----------
        var courts = new List<Court>();

        // Cụm Hùng Phát Q5: 4 sân bóng đá mini + 3 sân cầu lông + 2 sân tennis
        courts.AddRange(new[]
        {
            CreateCourt(complexHungPhatQ5.Id, football.Id, "Sân bóng số 1 (5 người)",
                "Sân 5 người cỏ nhân tạo, đèn LED, phù hợp đá tối.", CourtStatus.Active),
            CreateCourt(complexHungPhatQ5.Id, football.Id, "Sân bóng số 2 (5 người)",
                "Sân 5 người cỏ nhân tạo mới thay tháng trước.", CourtStatus.Active),
            CreateCourt(complexHungPhatQ5.Id, football.Id, "Sân bóng số 3 (7 người)",
                "Sân 7 người rộng rãi, có khán đài nhỏ.", CourtStatus.Active),
            CreateCourt(complexHungPhatQ5.Id, football.Id, "Sân bóng số 4 (7 người)",
                "Sân 7 người, đang bảo trì mặt cỏ đến cuối tháng.", CourtStatus.Maintenance),
            CreateCourt(complexHungPhatQ5.Id, badminton.Id, "Sân cầu lông A1",
                "Sân cầu lông trong nhà, thảm tiêu chuẩn.", CourtStatus.Active),
            CreateCourt(complexHungPhatQ5.Id, badminton.Id, "Sân cầu lông A2",
                "Sân cầu lông trong nhà, thảm tiêu chuẩn.", CourtStatus.Active),
            CreateCourt(complexHungPhatQ5.Id, badminton.Id, "Sân cầu lông A3",
                "Sân cầu lông gần quầy lễ tân, thuận tiện.", CourtStatus.Active),
            CreateCourt(complexHungPhatQ5.Id, tennis.Id, "Sân tennis T1",
                "Sân tennis mặt sân cứng ngoài trời.", CourtStatus.Active),
            CreateCourt(complexHungPhatQ5.Id, tennis.Id, "Sân tennis T2",
                "Sân tennis mặt sân cứng ngoài trời.", CourtStatus.Active)
        });

        // Cụm Hùng Phát Thủ Đức: 3 sân pickleball + 2 sân bóng rổ
        courts.AddRange(new[]
        {
            CreateCourt(complexHungPhatTD.Id, pickleball.Id, "Sân Pickleball P1",
                "Sân pickleball có mái che, chơi được cả khi mưa.", CourtStatus.Active),
            CreateCourt(complexHungPhatTD.Id, pickleball.Id, "Sân Pickleball P2",
                "Sân pickleball có mái che.", CourtStatus.Active),
            CreateCourt(complexHungPhatTD.Id, pickleball.Id, "Sân Pickleball P3",
                "Sân pickleball ngoài trời, view đẹp.", CourtStatus.Active),
            CreateCourt(complexHungPhatTD.Id, basketball.Id, "Sân bóng rổ B1",
                "Sân bóng rổ tiêu chuẩn 5x5, có mái che.", CourtStatus.Active),
            CreateCourt(complexHungPhatTD.Id, basketball.Id, "Sân bóng rổ B2",
                "Sân bóng rổ 3x3 ngoài trời.", CourtStatus.Active)
        });

        // Cụm Mai Anh Phạm Văn Đồng: 5 sân cầu lông + 3 sân tennis
        courts.AddRange(new[]
        {
            CreateCourt(complexMaiAnhPVD.Id, badminton.Id, "Sân cầu lông M1",
                "Thảm Yonex chính hãng, ánh sáng chuẩn thi đấu.", CourtStatus.Active),
            CreateCourt(complexMaiAnhPVD.Id, badminton.Id, "Sân cầu lông M2",
                "Thảm Yonex chính hãng.", CourtStatus.Active),
            CreateCourt(complexMaiAnhPVD.Id, badminton.Id, "Sân cầu lông M3",
                "Thảm Yonex chính hãng.", CourtStatus.Active),
            CreateCourt(complexMaiAnhPVD.Id, badminton.Id, "Sân cầu lông M4",
                "Sân gần cửa sổ, thoáng mát.", CourtStatus.Active),
            CreateCourt(complexMaiAnhPVD.Id, badminton.Id, "Sân cầu lông M5",
                "Sân dành cho thuê huấn luyện viên.", CourtStatus.Active),
            CreateCourt(complexMaiAnhPVD.Id, tennis.Id, "Sân tennis MT1",
                "Sân tennis trong nhà, mái vòm cao.", CourtStatus.Active),
            CreateCourt(complexMaiAnhPVD.Id, tennis.Id, "Sân tennis MT2",
                "Sân tennis trong nhà.", CourtStatus.Active),
            CreateCourt(complexMaiAnhPVD.Id, tennis.Id, "Sân tennis MT3",
                "Sân tennis ngoài trời.", CourtStatus.Active)
        });

        // Cụm Mai Anh Q1: 2 sân bóng đá + 4 sân cầu lông
        courts.AddRange(new[]
        {
            CreateCourt(complexMaiAnhQ1.Id, football.Id, "Sân bóng trung tâm 1",
                "Sân bóng 7 người trên sân thượng tòa nhà.", CourtStatus.Active),
            CreateCourt(complexMaiAnhQ1.Id, football.Id, "Sân bóng trung tâm 2",
                "Sân bóng 5 người trên sân thượng.", CourtStatus.Active),
            CreateCourt(complexMaiAnhQ1.Id, badminton.Id, "Sân cầu lông Q1-A",
                "Sân cầu lông cao cấp, điều hòa.", CourtStatus.Active),
            CreateCourt(complexMaiAnhQ1.Id, badminton.Id, "Sân cầu lông Q1-B",
                "Sân cầu lông cao cấp, điều hòa.", CourtStatus.Active),
            CreateCourt(complexMaiAnhQ1.Id, badminton.Id, "Sân cầu lông Q1-C",
                "Sân cầu lông cao cấp.", CourtStatus.Active),
            CreateCourt(complexMaiAnhQ1.Id, basketball.Id, "Sân bóng rổ Q1",
                "Sân bóng rổ trong nhà tiêu chuẩn.", CourtStatus.Active)
        });

        await context.Courts.AddRangeAsync(courts);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} sân.", courts.Count);

        // ---------- 3.4 Khung giờ & giá ----------
        var timeSlots = new List<TimeSlot>();
        foreach (var court in courts)
        {
            timeSlots.AddRange(BuildTimeSlotsForCourt(court));
        }

        await context.TimeSlots.AddRangeAsync(timeSlots);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} khung giờ.", timeSlots.Count);

        // ---------- 3.5 Booking mẫu ----------
        var bookings = BuildSampleBookings(courts, timeSlots, users);
        await context.Bookings.AddRangeAsync(bookings);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} booking mẫu.", bookings.Count);

        // ---------- 3.6 Thanh toán mẫu ----------
        var payments = BuildSamplePayments(bookings);
        await context.Payments.AddRangeAsync(payments);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} giao dịch thanh toán.", payments.Count);

        // ---------- 3.7 Đánh giá mẫu ----------
        var reviews = BuildSampleReviews(bookings, users);
        await context.Reviews.AddRangeAsync(reviews);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} đánh giá.", reviews.Count);

        // ---------- 3.8 Thông báo mẫu ----------
        var notifications = BuildSampleNotifications(bookings, users);
        await context.Notifications.AddRangeAsync(notifications);
        await context.SaveChangesAsync();
        logger.LogInformation("Đã seed {Count} thông báo.", notifications.Count);
    }

    /// <summary>Helper tạo Court với các giá trị mặc định hợp lý.</summary>
    private static Court CreateCourt(
        int complexId,
        int sportTypeId,
        string name,
        string description,
        CourtStatus status) => new()
        {
            SportComplexId = complexId,
            SportTypeId = sportTypeId,
            Name = name,
            Description = description,
            Status = status,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow.AddMonths(-5)
        };

    /// <summary>
    /// Sinh danh sách khung giờ cho một sân.
    /// - Sân bóng đá / bóng rổ: khung 60 phút.
    /// - Cầu lông / pickleball: khung 60 phút.
    /// - Tennis: khung 90 phút (thực tế thuê sân tennis thường 1.5 giờ).
    /// Giá được tính theo loại sân: sân trong nhà / cao cấp có giá cao hơn.
    /// </summary>
    private static List<TimeSlot> BuildTimeSlotsForCourt(Court court)
    {
        var slots = new List<TimeSlot>();

        // Xác định cấu hình giá theo tên sân & loại sân để dữ liệu seed trông thực tế.
        var (openHour, closeHour, durationMinutes, baseWeekday, baseWeekend, baseHoliday) =
            ResolveCourtPricing(court);

        var start = new TimeSpan(openHour, 0, 0);
        var close = new TimeSpan(closeHour, 0, 0);

        while (start + TimeSpan.FromMinutes(durationMinutes) <= close)
        {
            var end = start + TimeSpan.FromMinutes(durationMinutes);

            // Giờ cao điểm (17h-21h) tăng giá 30% để phản ánh thực tế.
            var isPeak = start.Hours >= 17 && start.Hours < 21;
            var peakMultiplier = isPeak ? 1.3m : 1m;

            // Làm tròn giá tới 10.000đ cho đẹp số.
            decimal RoundToTenThousand(decimal value)
                => Math.Round(value * peakMultiplier / 10000m, MidpointRounding.AwayFromZero) * 10000m;

            slots.Add(new TimeSlot
            {
                CourtId = court.Id,
                StartTime = start,
                EndTime = end,
                DurationMinutes = durationMinutes,
                PriceWeekday = RoundToTenThousand(baseWeekday),
                PriceWeekend = RoundToTenThousand(baseWeekend),
                PriceHoliday = RoundToTenThousand(baseHoliday),
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddMonths(-5)
            });

            start = end;
        }

        return slots;
    }

    /// <summary>
    /// Trả về cấu hình (giờ mở, giờ đóng, thời lượng khung, giá ngày thường, cuối tuần, lễ)
    /// dựa trên tên sân và loại sân.
    /// </summary>
    private static (int openHour, int closeHour, int durationMinutes,
        decimal weekday, decimal weekend, decimal holiday) ResolveCourtPricing(Court court)
    {
        // Giá tham khảo (VNĐ/giờ) theo loại sân - đã bao gồm phân biệt trong nhà/ngoài trời.
        if (court.Name.Contains("Pickleball", StringComparison.OrdinalIgnoreCase))
        {
            return (6, 22, 60, 120_000m, 150_000m, 180_000m);
        }

        if (court.Name.Contains("bóng rổ", StringComparison.OrdinalIgnoreCase))
        {
            return (7, 22, 60, 150_000m, 180_000m, 220_000m);
        }

        if (court.Name.Contains("bóng đá", StringComparison.OrdinalIgnoreCase)
            || court.Name.Contains("bóng trung tâm", StringComparison.OrdinalIgnoreCase))
        {
            // Sân 7 người đắt hơn sân 5 người.
            var isSevenAside = court.Name.Contains("7 người", StringComparison.OrdinalIgnoreCase);
            return isSevenAside
                ? (6, 23, 60, 350_000m, 420_000m, 500_000m)
                : (6, 23, 60, 250_000m, 300_000m, 360_000m);
        }

        if (court.Name.Contains("tennis", StringComparison.OrdinalIgnoreCase))
        {
            var isIndoor = court.Name.Contains("MT", StringComparison.OrdinalIgnoreCase)
                           || court.Name.Contains("trong nhà", StringComparison.OrdinalIgnoreCase);
            return isIndoor
                ? (6, 22, 90, 300_000m, 350_000m, 400_000m)
                : (6, 22, 90, 200_000m, 250_000m, 300_000m);
        }

        if (court.Name.Contains("cầu lông", StringComparison.OrdinalIgnoreCase))
        {
            // Sân cao cấp (Mai Anh / Q1) giá cao hơn sân thường.
            var isPremium = court.Name.Contains(" M", StringComparison.OrdinalIgnoreCase)
                            || court.Name.Contains("Q1", StringComparison.OrdinalIgnoreCase);
            return isPremium
                ? (5, 23, 60, 140_000m, 170_000m, 200_000m)
                : (5, 23, 60, 90_000m, 110_000m, 140_000m);
        }

        // Mặc định.
        return (6, 22, 60, 150_000m, 180_000m, 200_000m);
    }

    /// <summary>
    /// Tạo danh sách booking mẫu trải đều các trạng thái để test toàn bộ luồng nghiệp vụ.
    /// </summary>
    private static List<Booking> BuildSampleBookings(
        List<Court> courts,
        List<TimeSlot> timeSlots,
        SeededUsers users)
    {
        var bookings = new List<Booking>();
        var now = SportBooking.Helpers.ScheduleGuard.Now;
        var today = now.Date;

        // Helper lấy TimeSlot của sân theo giờ bắt đầu mong muốn.
        // Vì tennis dùng khung 90 phút (06:00, 07:30, 09:00...) nên không phải giờ nào
        // cũng tồn tại khung giờ bắt đầu đúng bằng tham số truyền vào.
        // Do đó ta lấy khung giờ có StartTime gần nhất về phía trước (không vượt quá giờ yêu cầu).
        TimeSlot Slot(Court court, int hour)
        {
            var courtSlots = timeSlots
                .Where(t => t.CourtId == court.Id)
                .OrderBy(t => t.StartTime)
                .ToList();

            // Ưu tiên khung giờ bắt đầu đúng bằng giờ yêu cầu.
            var exact = courtSlots.FirstOrDefault(t => t.StartTime.Hours == hour);
            if (exact is not null)
            {
                return exact;
            }

            // Nếu không có, lấy khung giờ có giờ bắt đầu lớn nhất nhưng vẫn <= giờ yêu cầu;
            // nếu giờ yêu cầu sớm hơn cả khung đầu tiên thì lấy khung đầu tiên.
            var fallback = courtSlots.LastOrDefault(t => t.StartTime.Hours <= hour)
                           ?? courtSlots.First();

            return fallback;
        }

        var seq = 1;
        string NextCode(DateTime date) => $"SB{date:yyyyMMdd}-{seq++:D4}";

        // --- Booking 1: Đã hoàn thành (Completed + Paid) - tuần trước, sân bóng số 1 ---
        var c1 = courts.First(c => c.Name == "Sân bóng số 1 (5 người)");
        var s1 = Slot(c1, 18);
        var d1 = today.AddDays(-7);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d1),
            BookingGroupCode = $"GRP{d1:yyyyMMdd}-0001",
            CustomerId = users.Customer1.Id,
            CourtId = c1.Id,
            TimeSlotId = s1.Id,
            BookingDate = DateOnly.FromDateTime(d1),
            StartDateTime = d1.Add(s1.StartTime),
            EndDateTime = d1.Add(s1.EndTime),
            TotalAmount = s1.PriceWeekday,
            Status = BookingStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.VnPay,
            ConfirmedAt = d1.AddDays(-8),
            CompletedAt = d1.Add(s1.EndTime),
            CreatedAt = d1.AddDays(-8),
            Note = "Đá giao hữu với công ty"
        });

        // --- Booking 2: Đã hoàn thành (Completed + Paid) - 5 ngày trước, cầu lông A1 ---
        var c2 = courts.First(c => c.Name == "Sân cầu lông A1");
        var s2 = Slot(c2, 19);
        var d2 = today.AddDays(-5);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d2),
            BookingGroupCode = $"GRP{d2:yyyyMMdd}-0002",
            CustomerId = users.Customer2.Id,
            CourtId = c2.Id,
            TimeSlotId = s2.Id,
            BookingDate = DateOnly.FromDateTime(d2),
            StartDateTime = d2.Add(s2.StartTime),
            EndDateTime = d2.Add(s2.EndTime),
            TotalAmount = s2.PriceWeekday,
            Status = BookingStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.AtCounter,
            ConfirmedAt = d2.AddDays(-5).AddHours(-2),
            CompletedAt = d2.Add(s2.EndTime),
            CreatedAt = d2.AddDays(-6)
        });

        // --- Booking 3: Đã hủy bởi khách (Cancelled) - 3 ngày trước, tennis T1 ---
        var c3 = courts.First(c => c.Name == "Sân tennis T1");
        var s3 = Slot(c3, 8);
        var d3 = today.AddDays(-3);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d3),
            BookingGroupCode = $"GRP{d3:yyyyMMdd}-0003",
            CustomerId = users.Customer3.Id,
            CourtId = c3.Id,
            TimeSlotId = s3.Id,
            BookingDate = DateOnly.FromDateTime(d3),
            StartDateTime = d3.Add(s3.StartTime),
            EndDateTime = d3.Add(s3.EndTime),
            TotalAmount = s3.PriceWeekday,
            Status = BookingStatus.Cancelled,
            PaymentStatus = PaymentStatus.Refunded,
            PaymentMethod = PaymentMethod.VnPay,
            CancellationReason = "Bận đột xuất, không sắp xếp được thời gian.",
            CancelledAt = d3.AddDays(-1),
            CancelledBy = "Customer",
            ConfirmedAt = d3.AddDays(-4),
            CreatedAt = d3.AddDays(-4)
        });

        // --- Booking 4: Vắng mặt (NoShow) - 2 ngày trước, bóng rổ B1 ---
        var c4 = courts.First(c => c.Name == "Sân bóng rổ B1");
        var s4 = Slot(c4, 20);
        var d4 = today.AddDays(-2);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d4),
            BookingGroupCode = $"GRP{d4:yyyyMMdd}-0004",
            CustomerId = users.Customer1.Id,
            CourtId = c4.Id,
            TimeSlotId = s4.Id,
            BookingDate = DateOnly.FromDateTime(d4),
            StartDateTime = d4.Add(s4.StartTime),
            EndDateTime = d4.Add(s4.EndTime),
            TotalAmount = s4.PriceWeekday,
            Status = BookingStatus.NoShow,
            PaymentStatus = PaymentStatus.Unpaid,
            PaymentMethod = PaymentMethod.AtCounter,
            ConfirmedAt = d4.AddDays(-3),
            CreatedAt = d4.AddDays(-3),
            Note = "Khách báo bận nhưng không hủy"
        });

        // --- Booking 5: Đã xác nhận (Confirmed + Paid) - hôm nay, sân bóng số 2 ---
        var c5 = courts.First(c => c.Name == "Sân bóng số 2 (5 người)");
        var s5 = Slot(c5, 19);
        var d5 = today;
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d5),
            BookingGroupCode = $"GRP{d5:yyyyMMdd}-0005",
            CustomerId = users.Customer2.Id,
            CourtId = c5.Id,
            TimeSlotId = s5.Id,
            BookingDate = DateOnly.FromDateTime(d5),
            StartDateTime = d5.Add(s5.StartTime),
            EndDateTime = d5.Add(s5.EndTime),
            TotalAmount = s5.PriceWeekday,
            Status = BookingStatus.Confirmed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.VnPay,
            ConfirmedAt = now.AddHours(-3),
            CreatedAt = now.AddHours(-3).AddMinutes(-10),
            Note = "Đội bóng cơ quan"
        });

        // --- Booking 6: Đã xác nhận, thanh toán tại quầy (Confirmed + Paid) - ngày mai, pickleball P1 ---
        var c6 = courts.First(c => c.Name == "Sân Pickleball P1");
        var s6 = Slot(c6, 7);
        var d6 = today.AddDays(1);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d6),
            BookingGroupCode = $"GRP{d6:yyyyMMdd}-0006",
            CustomerId = users.Customer3.Id,
            CourtId = c6.Id,
            TimeSlotId = s6.Id,
            BookingDate = DateOnly.FromDateTime(d6),
            StartDateTime = d6.Add(s6.StartTime),
            EndDateTime = d6.Add(s6.EndTime),
            TotalAmount = s6.PriceWeekday,
            Status = BookingStatus.Confirmed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.AtCounter,
            ConfirmedAt = now.AddHours(-1),
            CreatedAt = now.AddHours(-2)
        });

        // --- Booking 7: Chờ thanh toán online (Pending + Unpaid, còn hạn) - ngày mai, cầu lông M1 ---
        var c7 = courts.First(c => c.Name == "Sân cầu lông M1");
        var s7 = Slot(c7, 18);
        var d7 = today.AddDays(1);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d7),
            BookingGroupCode = $"GRP{d7:yyyyMMdd}-0007",
            CustomerId = users.Customer1.Id,
            CourtId = c7.Id,
            TimeSlotId = s7.Id,
            BookingDate = DateOnly.FromDateTime(d7),
            StartDateTime = d7.Add(s7.StartTime),
            EndDateTime = d7.Add(s7.EndTime),
            TotalAmount = s7.PriceWeekday,
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid,
            PaymentMethod = PaymentMethod.VnPay,
            PaymentDeadline = now.AddMinutes(10),
            CreatedAt = now.AddMinutes(-5)
        });

        // --- Booking 8: Chờ xác nhận tại quầy (Pending + Unpaid) - ngày mai, tennis MT1 ---
        var c8 = courts.First(c => c.Name == "Sân tennis MT1");
        var s8 = Slot(c8, 17);
        var d8 = today.AddDays(1);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d8),
            BookingGroupCode = $"GRP{d8:yyyyMMdd}-0008",
            CustomerId = users.Customer2.Id,
            CourtId = c8.Id,
            TimeSlotId = s8.Id,
            BookingDate = DateOnly.FromDateTime(d8),
            StartDateTime = d8.Add(s8.StartTime),
            EndDateTime = d8.Add(s8.EndTime),
            TotalAmount = s8.PriceWeekday,
            Status = BookingStatus.Pending,
            PaymentStatus = PaymentStatus.Unpaid,
            PaymentMethod = PaymentMethod.AtCounter,
            CreatedAt = now.AddHours(-4)
        });

        // --- Booking 9: Đã hết hạn do quá 15 phút không thanh toán (Expired) - hôm qua ---
        var c9 = courts.First(c => c.Name == "Sân bóng số 3 (7 người)");
        var s9 = Slot(c9, 20);
        var d9 = today.AddDays(-1);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d9),
            BookingGroupCode = $"GRP{d9:yyyyMMdd}-0009",
            CustomerId = users.Customer3.Id,
            CourtId = c9.Id,
            TimeSlotId = s9.Id,
            BookingDate = DateOnly.FromDateTime(d9),
            StartDateTime = d9.Add(s9.StartTime),
            EndDateTime = d9.Add(s9.EndTime),
            TotalAmount = s9.PriceWeekday,
            Status = BookingStatus.Expired,
            PaymentStatus = PaymentStatus.Failed,
            PaymentMethod = PaymentMethod.VnPay,
            PaymentDeadline = d9.AddHours(-19),
            CancelledAt = d9.AddHours(-19),
            CancelledBy = "System",
            CancellationReason = "Quá hạn thanh toán 15 phút.",
            CreatedAt = d9.AddHours(-20)
        });

        // --- Booking 10-12: Nhóm 3 khung giờ liên tiếp (đã xác nhận) - cuối tuần tới, sân bóng số 1 ---
        var d10 = NextWeekend(today);
        var groupCode = $"GRP{d10:yyyyMMdd}-0010";
        foreach (var hour in new[] { 17, 18, 19 })
        {
            var slot = Slot(c1, hour);
            var dateOnly = DateOnly.FromDateTime(d10);
            bookings.Add(new Booking
            {
                BookingCode = NextCode(d10),
                BookingGroupCode = groupCode,
                CustomerId = users.Customer2.Id,
                CourtId = c1.Id,
                TimeSlotId = slot.Id,
                BookingDate = dateOnly,
                StartDateTime = d10.Add(slot.StartTime),
                EndDateTime = d10.Add(slot.EndTime),
                TotalAmount = slot.PriceWeekend,
                Status = BookingStatus.Confirmed,
                PaymentStatus = PaymentStatus.Paid,
                PaymentMethod = PaymentMethod.VnPay,
                ConfirmedAt = now.AddHours(-6),
                CreatedAt = now.AddHours(-6).AddMinutes(-8),
                Note = "Đá giải nội bộ 3 tiếng liên tục"
            });
        }

        // --- Booking 13: Đã xác nhận cuối tuần, cầu lông M2 (để test đánh giá cuối tuần) ---
        var c13 = courts.First(c => c.Name == "Sân cầu lông M2");
        var s13 = Slot(c13, 9);
        var d13 = NextWeekend(today);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d13),
            BookingGroupCode = $"GRP{d13:yyyyMMdd}-0013",
            CustomerId = users.Customer1.Id,
            CourtId = c13.Id,
            TimeSlotId = s13.Id,
            BookingDate = DateOnly.FromDateTime(d13),
            StartDateTime = d13.Add(s13.StartTime),
            EndDateTime = d13.Add(s13.EndTime),
            TotalAmount = s13.PriceWeekend,
            Status = BookingStatus.Confirmed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.VnPay,
            ConfirmedAt = now.AddHours(-2),
            CreatedAt = now.AddHours(-2).AddMinutes(-12)
        });

        // --- Booking 14: Completed ở Mai Anh PVD - để có review cho cụm này ---
        var c14 = courts.First(c => c.Name == "Sân cầu lông M3");
        var s14 = Slot(c14, 20);
        var d14 = today.AddDays(-10);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d14),
            BookingGroupCode = $"GRP{d14:yyyyMMdd}-0014",
            CustomerId = users.Customer3.Id,
            CourtId = c14.Id,
            TimeSlotId = s14.Id,
            BookingDate = DateOnly.FromDateTime(d14),
            StartDateTime = d14.Add(s14.StartTime),
            EndDateTime = d14.Add(s14.EndTime),
            TotalAmount = s14.PriceWeekday,
            Status = BookingStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.VnPay,
            ConfirmedAt = d14.AddDays(-11),
            CompletedAt = d14.Add(s14.EndTime),
            CreatedAt = d14.AddDays(-11)
        });

        // --- Booking 15: Completed ở Mai Anh Q1 - để có review cho cụm Q1 ---
        var c15 = courts.First(c => c.Name == "Sân cầu lông Q1-A");
        var s15 = Slot(c15, 18);
        var d15 = today.AddDays(-12);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d15),
            BookingGroupCode = $"GRP{d15:yyyyMMdd}-0015",
            CustomerId = users.Customer2.Id,
            CourtId = c15.Id,
            TimeSlotId = s15.Id,
            BookingDate = DateOnly.FromDateTime(d15),
            StartDateTime = d15.Add(s15.StartTime),
            EndDateTime = d15.Add(s15.EndTime),
            TotalAmount = s15.PriceWeekday,
            Status = BookingStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.AtCounter,
            ConfirmedAt = d15.AddDays(-12),
            CompletedAt = d15.Add(s15.EndTime),
            CreatedAt = d15.AddDays(-13)
        });

        // --- Booking 16: Completed sân bóng trung tâm 1 (Mai Anh Q1) ---
        var c16 = courts.First(c => c.Name == "Sân bóng trung tâm 1");
        var s16 = Slot(c16, 19);
        var d16 = today.AddDays(-15);
        bookings.Add(new Booking
        {
            BookingCode = NextCode(d16),
            BookingGroupCode = $"GRP{d16:yyyyMMdd}-0016",
            CustomerId = users.Customer1.Id,
            CourtId = c16.Id,
            TimeSlotId = s16.Id,
            BookingDate = DateOnly.FromDateTime(d16),
            StartDateTime = d16.Add(s16.StartTime),
            EndDateTime = d16.Add(s16.EndTime),
            TotalAmount = s16.PriceWeekday,
            Status = BookingStatus.Completed,
            PaymentStatus = PaymentStatus.Paid,
            PaymentMethod = PaymentMethod.VnPay,
            ConfirmedAt = d16.AddDays(-16),
            CompletedAt = d16.Add(s16.EndTime),
            CreatedAt = d16.AddDays(-16)
        });

        return bookings;
    }

    /// <summary>Trả về ngày thứ Bảy của tuần kế tiếp.</summary>
    private static DateTime NextWeekend(DateTime from)
    {
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)from.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0)
        {
            daysUntilSaturday = 7;
        }
        return from.AddDays(daysUntilSaturday);
    }

    /// <summary>
    /// Tạo giao dịch thanh toán mẫu tương ứng với các booking đã thanh toán online.
    /// </summary>
    private static List<Payment> BuildSamplePayments(List<Booking> bookings)
    {
        var payments = new List<Payment>();
        var counter = 1;

        foreach (var booking in bookings)
        {
            // Chỉ sinh Payment cho các booking thanh toán qua VNPay.
            if (booking.PaymentMethod != PaymentMethod.VnPay)
            {
                continue;
            }

            var isSuccess = booking.PaymentStatus == PaymentStatus.Paid;

            payments.Add(new Payment
            {
                BookingId = booking.Id,
                TransactionCode = $"VNP{booking.BookingDate:yyyyMMdd}{counter++:D6}",
                Method = PaymentMethod.VnPay,
                Amount = booking.TotalAmount,
                Status = booking.PaymentStatus,
                GatewayTransactionId = isSuccess ? $"14{DateTime.UtcNow.Ticks % 100000000:D8}" : null,
                BankCode = isSuccess ? "NCB" : null,
                ResponseCode = isSuccess ? "00" : (booking.Status == BookingStatus.Expired ? "24" : "99"),
                ResponseMessage = isSuccess
                    ? "Giao dịch thành công"
                    : (booking.Status == BookingStatus.Expired
                        ? "Khách hàng không thanh toán"
                        : "Giao dịch thất bại"),
                RawData = "vnp_ResponseCode=" + (isSuccess ? "00" : "24"),
                PaidAt = isSuccess ? booking.ConfirmedAt ?? booking.CreatedAt.AddMinutes(3) : null,
                CreatedAt = booking.CreatedAt
            });
        }

        return payments;
    }

    /// <summary>
    /// Tạo đánh giá mẫu cho các booking đã hoàn thành.
    /// </summary>
    private static List<Review> BuildSampleReviews(List<Booking> bookings, SeededUsers users)
    {
        var reviews = new List<Review>();
        var completedBookings = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();

        // Nội dung đánh giá mẫu theo số sao.
        var comments = new Dictionary<int, string[]>
        {
            [5] = new[]
            {
                "Sân rất đẹp, mặt cỏ mới, nhân viên thân thiện. Sẽ quay lại!",
                "Chất lượng sân tuyệt vời, ánh sáng tốt, giá hợp lý.",
                "Đặt sân nhanh, xác nhận liền. Sân sạch sẽ, rất hài lòng."
            },
            [4] = new[]
            {
                "Sân ổn, giá cả phải chăng. Chỉ hơi đông vào giờ cao điểm.",
                "Chất lượng tốt, tuy nhiên bãi giữ xe hơi chật.",
                "Sân đẹp nhưng phòng thay đồ cần cải thiện thêm."
            },
            [3] = new[]
            {
                "Sân tạm ổn, ánh sáng hơi yếu vào buổi tối.",
                "Dịch vụ bình thường, giá hơi cao so với chất lượng."
            }
        };

        var ratingPattern = new[] { 5, 4, 5, 5, 3, 4, 5, 4 };
        var reviewIndex = 0;

        foreach (var booking in completedBookings)
        {
            var rating = ratingPattern[reviewIndex % ratingPattern.Length];
            var pool = comments[rating];
            var comment = pool[reviewIndex % pool.Length];

            var review = new Review
            {
                BookingId = booking.Id,
                CourtId = booking.CourtId,
                CustomerId = booking.CustomerId,
                Rating = rating,
                Comment = comment,
                IsVisible = true,
                CreatedAt = (booking.CompletedAt ?? booking.CreatedAt).AddHours(2)
            };

            // Một số đánh giá có phản hồi từ chủ sân để test chức năng phản hồi.
            if (reviewIndex % 3 == 0)
            {
                review.OwnerReply = "Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ. " +
                                    "Hẹn gặp lại bạn trong thời gian tới!";
                review.OwnerReplyAt = review.CreatedAt.AddDays(1);
            }

            reviews.Add(review);
            reviewIndex++;
        }

        return reviews;
    }

    /// <summary>
    /// Tạo thông báo mẫu cho người dùng để test chức năng chuông thông báo.
    /// </summary>
    private static List<Notification> BuildSampleNotifications(List<Booking> bookings, SeededUsers users)
    {
        var notifications = new List<Notification>();
        var now = DateTime.UtcNow;

        // Thông báo cho khách hàng về booking gần nhất.
        var latestConfirmed = bookings
            .Where(b => b.CustomerId == users.Customer1.Id && b.Status == BookingStatus.Confirmed)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefault();

        if (latestConfirmed is not null)
        {
            notifications.Add(new Notification
            {
                UserId = users.Customer1.Id,
                Title = "Đặt sân thành công",
                Message = $"Booking {latestConfirmed.BookingCode} đã được xác nhận. " +
                          "Vui lòng đến sân đúng giờ và mang theo mã đặt sân.",
                Url = "/Booking/History",
                Type = "booking",
                IsRead = false,
                CreatedAt = now.AddHours(-3)
            });
        }

        notifications.Add(new Notification
        {
            UserId = users.Customer2.Id,
            Title = "Nhắc nhở thanh toán tại quầy",
            Message = "Bạn có một lượt đặt sân thanh toán tại quầy vào ngày mai. " +
                      "Vui lòng thanh toán khi đến sân.",
            Url = "/Booking/History",
            Type = "payment",
            IsRead = false,
            CreatedAt = now.AddHours(-1)
        });

        notifications.Add(new Notification
        {
            UserId = users.Customer3.Id,
            Title = "Booking đã hết hạn",
            Message = "Lượt đặt sân của bạn đã bị hủy do quá hạn thanh toán 15 phút.",
            Url = "/Booking/History",
            Type = "system",
            IsRead = true,
            ReadAt = now.AddHours(-20),
            CreatedAt = now.AddHours(-21)
        });

        notifications.Add(new Notification
        {
            UserId = users.Owner1.Id,
            Title = "Có đánh giá mới",
            Message = "Khách hàng vừa đánh giá 5 sao cho sân của bạn.",
            Url = "/Owner/Review",
            Type = "review",
            IsRead = false,
            CreatedAt = now.AddHours(-5)
        });

        notifications.Add(new Notification
        {
            UserId = users.Admin.Id,
            Title = "Có tài khoản chủ sân chờ duyệt",
            Message = "Chủ sân \"Lê Quốc Bảo\" vừa đăng ký và đang chờ bạn phê duyệt.",
            Url = "/Admin/OwnerApproval",
            Type = "system",
            IsRead = false,
            CreatedAt = now.AddDays(-2)
        });

        return notifications;
    }
}
