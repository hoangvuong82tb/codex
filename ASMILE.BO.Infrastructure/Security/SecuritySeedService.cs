using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Security.Store;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class SecuritySeedService : ISecuritySeedService
{
    private readonly InMemorySecurityStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogService _auditLogService;
    private readonly AppDbContext _dbContext;
    private readonly string? _initialAdminPassword;

    public SecuritySeedService(
        InMemorySecurityStore store,
        IPasswordHasher passwordHasher,
        IAuditLogService auditLogService,
        AppDbContext dbContext,
        string? initialAdminPassword = null)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _dbContext = dbContext;
        _initialAdminPassword = string.IsNullOrWhiteSpace(initialAdminPassword)
            ? Environment.GetEnvironmentVariable("ASMILE_BO_INITIAL_ADMIN_PASSWORD")
            : initialAdminPassword;
    }

    public async Task EnsureSeedDataAsync()
    {
        foreach (var permission in DefaultPermissions())
        {
            if (_store.Permissions.All(x => !string.Equals(x.Code, permission.Code, StringComparison.OrdinalIgnoreCase)))
            {
                _store.Permissions.Add(permission);
            }
        }

        // Upsert the built-in roles instead of seeding them only when the store is
        // empty.  Existing installations can already contain custom roles, so a
        // one-time empty-store check would silently miss newly introduced system
        // roles such as FrontOffice after an upgrade.
        foreach (var defaultRole in DefaultRoles())
        {
            if (_store.Roles.All(x => !string.Equals(x.Code, defaultRole.Code, StringComparison.OrdinalIgnoreCase)))
            {
                _store.Roles.Add(defaultRole);
            }
        }

        AssignRolePermissions();
        await EnsurePersistedSecurityCatalogAsync();
        await LoadPersistedUsersAsync();

        var persistedAdmin = await _dbContext.AppUsers
            .FirstOrDefaultAsync(x => x.UserName == "administrator");

        var admin = _store.Users.FirstOrDefault(x => x.NormalizedUserName == "ADMINISTRATOR");
        if (admin is null)
        {
            if (string.IsNullOrWhiteSpace(_initialAdminPassword))
            {
                throw new InvalidOperationException(
                    "Chưa cấu hình mật khẩu administrator khởi tạo. Hãy đặt biến môi trường ASMILE_BO_INITIAL_ADMIN_PASSWORD trước khi chạy ứng dụng lần đầu.");
            }

            admin = new UserAccount
            {
                Id = persistedAdmin?.Id ?? Guid.NewGuid(),
                UserName = persistedAdmin?.UserName ?? "administrator",
                NormalizedUserName = "ADMINISTRATOR",
                DisplayName = persistedAdmin?.DisplayName ?? "Administrator",
                Email = persistedAdmin?.Email,
                PasswordHash = _passwordHasher.HashPassword(_initialAdminPassword),
                IsActive = persistedAdmin?.IsActive ?? true,
                IsLocked = false,
                MustChangePassword = true,
                CreatedBy = "seed"
            };
            _store.Users.Add(admin);
            await _auditLogService.LogAsync("SeedAdmin", "Tạo tài khoản administrator seed", "system");
        }
        else
        {
            if (persistedAdmin is not null && admin.Id != persistedAdmin.Id)
            {
                admin.Id = persistedAdmin.Id;
                admin.UserName = persistedAdmin.UserName;
                admin.DisplayName = persistedAdmin.DisplayName;
                admin.Email = persistedAdmin.Email;
                admin.IsActive = persistedAdmin.IsActive;
            }

            if (string.IsNullOrWhiteSpace(admin.PasswordHash))
            {
                if (string.IsNullOrWhiteSpace(_initialAdminPassword))
                {
                    throw new InvalidOperationException(
                        "Tài khoản administrator chưa có mật khẩu. Hãy đặt biến môi trường ASMILE_BO_INITIAL_ADMIN_PASSWORD để hoàn tất khởi tạo.");
                }

                admin.PasswordHash = _passwordHasher.HashPassword(_initialAdminPassword);
                admin.MustChangePassword = true;
                admin.IsLocked = false;
                admin.FailedLoginCount = 0;
            }

            await _auditLogService.LogAsync("SeedAdminExists", "Tài khoản administrator đã tồn tại", "system");
        }

        await PersistUserAsync(admin);

        var adminRole = _store.Roles.First(x => x.Code == "Administrator");
        if (!_store.UserRoles.Any(x => x.UserId == admin.Id && x.RoleId == adminRole.Id))
        {
            _store.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
        }

        // Keep the persisted authorization boundary in sync with the in-memory
        // security store.  Without this link a fresh installation can authenticate
        // the administrator successfully but every module permission check fails
        // because AppUserRoles is empty.
        var persistedAdminRole = await _dbContext.AppRoles
            .FirstOrDefaultAsync(x => x.Code == "Administrator");
        if (persistedAdminRole is not null
            && !await _dbContext.AppUserRoles.AnyAsync(x => x.UserId == admin.Id && x.RoleId == persistedAdminRole.Id))
        {
            _dbContext.AppUserRoles.Add(new AppUserRole
            {
                UserId = admin.Id,
                RoleId = persistedAdminRole.Id
            });
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task EnsurePersistedSecurityCatalogAsync()
    {
        // The enterprise permission service is DB-backed, while this legacy
        // startup seed historically populated only the in-memory catalog. Keep
        // the database catalog complete as well so a fresh installation can
        // authenticate and authorize before any admin screen is opened.
        var defaultPermissions = DefaultPermissions().ToArray();
        var persistedPermissions = await _dbContext.AppPermissions.ToListAsync();
        foreach (var permission in defaultPermissions)
        {
            if (persistedPermissions.All(x => !string.Equals(x.Code, permission.Code, StringComparison.OrdinalIgnoreCase)))
            {
                var persisted = new AppPermission
                {
                    Code = permission.Code,
                    Name = permission.Name,
                    Group = permission.Group,
                    Description = permission.Description
                };
                _dbContext.AppPermissions.Add(persisted);
                persistedPermissions.Add(persisted);
            }
        }

        var persistedRoles = await _dbContext.AppRoles.ToListAsync();
        foreach (var defaultRole in DefaultRoles())
        {
            var role = persistedRoles.FirstOrDefault(x => string.Equals(x.Code, defaultRole.Code, StringComparison.OrdinalIgnoreCase));
            // Older databases use Accountant for the legacy Accounting role.
            role ??= string.Equals(defaultRole.Code, "Accounting", StringComparison.OrdinalIgnoreCase)
                ? persistedRoles.FirstOrDefault(x => string.Equals(x.Code, "Accountant", StringComparison.OrdinalIgnoreCase))
                : null;
            if (role is null)
            {
                role = new AppRole
                {
                    Code = defaultRole.Code,
                    Name = defaultRole.Name,
                    Description = defaultRole.Description,
                    IsSystem = true,
                    IsActive = true
                };
                _dbContext.AppRoles.Add(role);
                persistedRoles.Add(role);
            }
        }

        await _dbContext.SaveChangesAsync();

        var rolePermissionMap = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Administrator"] = persistedPermissions.Select(x => x.Code).ToArray(),
            ["ITSupport"] = ["System.View", "System.Configure", "Settings.View", "Settings.Edit", "VFC.View", "Logs.View", "Database.View", "Database.Backup", "Database.Maintenance"],
            ["Accounting"] = persistedPermissions
                .Where(x => x.Group is "Accounting" or "Logs" || x.Code.StartsWith("FrontOffice.Folio.", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Code)
                .ToArray(),
            ["Warehouse"] = persistedPermissions.Where(x => x.Group == "Warehouse").Select(x => x.Code).ToArray(),
            ["HR"] = persistedPermissions.Where(x => x.Group == "HR").Select(x => x.Code).ToArray(),
            ["Viewer"] = ["System.View", "Settings.View", "Logs.View", "Warehouse.View", "Accounting.View", "HR.View", "VFC.View", "FrontOffice.Reservation.View", "FrontOffice.Stay.View", "FrontOffice.Folio.View"],
            ["FrontOffice"] = persistedPermissions.Where(x => x.Group == "FrontOffice").Select(x => x.Code).ToArray()
        };
        var existingAssignments = await _dbContext.AppRolePermissions.ToListAsync();
        foreach (var entry in rolePermissionMap)
        {
            var role = persistedRoles.FirstOrDefault(x => string.Equals(x.Code, entry.Key, StringComparison.OrdinalIgnoreCase));
            role ??= string.Equals(entry.Key, "Accounting", StringComparison.OrdinalIgnoreCase)
                ? persistedRoles.FirstOrDefault(x => string.Equals(x.Code, "Accountant", StringComparison.OrdinalIgnoreCase))
                : null;
            if (role is null)
            {
                continue;
            }

            foreach (var permissionCode in entry.Value)
            {
                if (existingAssignments.Any(x => x.RoleId == role.Id && string.Equals(x.PermissionCode, permissionCode, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var assignment = new AppRolePermission { RoleId = role.Id, PermissionCode = permissionCode };
                _dbContext.AppRolePermissions.Add(assignment);
                existingAssignments.Add(assignment);
            }
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task LoadPersistedUsersAsync()
    {
        var users = await _dbContext.AppUsers
            .AsNoTracking()
            .ToListAsync();

        foreach (var persisted in users)
        {
            var user = _store.Users.FirstOrDefault(x => x.Id == persisted.Id)
                ?? _store.Users.FirstOrDefault(x => string.Equals(x.NormalizedUserName, persisted.UserName, StringComparison.OrdinalIgnoreCase));
            if (user is null)
            {
                user = new UserAccount { Id = persisted.Id };
                _store.Users.Add(user);
            }

            user.Id = persisted.Id;
            user.UserName = persisted.UserName;
            user.NormalizedUserName = persisted.UserName.ToUpperInvariant();
            user.DisplayName = persisted.DisplayName;
            user.Email = persisted.Email;
            user.PasswordHash = persisted.PasswordHash;
            user.IsActive = persisted.IsActive;
            user.IsLocked = persisted.IsLocked;
            user.MustChangePassword = persisted.MustChangePassword;
            user.FailedLoginCount = persisted.FailedLoginCount;
            user.LastLoginAt = persisted.LastLoginAt;
            user.LastPasswordChangedAt = persisted.LastPasswordChangedAt;
            user.Notes = persisted.Notes;

            var roleCodes = await _dbContext.AppUserRoles
                .Where(x => x.UserId == persisted.Id)
                .Select(x => x.Role!.Code)
                .ToListAsync();
            foreach (var roleCode in roleCodes)
            {
                var role = _store.Roles.FirstOrDefault(x => string.Equals(x.Code, roleCode, StringComparison.OrdinalIgnoreCase));
                if (role is not null && !_store.UserRoles.Any(x => x.UserId == user.Id && x.RoleId == role.Id))
                {
                    _store.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
            }
        }
    }

    private async Task PersistUserAsync(UserAccount user)
    {
        var persisted = await _dbContext.AppUsers
            .FirstOrDefaultAsync(x => x.Id == user.Id || x.UserName == user.UserName);
        if (persisted is null)
        {
            persisted = new AppUser { Id = user.Id };
            _dbContext.AppUsers.Add(persisted);
        }

        persisted.UserName = user.UserName;
        persisted.DisplayName = user.DisplayName;
        persisted.Email = user.Email;
        persisted.PasswordHash = user.PasswordHash;
        persisted.IsActive = user.IsActive;
        persisted.IsLocked = user.IsLocked;
        persisted.MustChangePassword = user.MustChangePassword;
        persisted.FailedLoginCount = user.FailedLoginCount;
        persisted.LastLoginAt = user.LastLoginAt;
        persisted.LastPasswordChangedAt = user.LastPasswordChangedAt;
        persisted.Notes = user.Notes;
        persisted.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }

    private void AssignRolePermissions()
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Administrator"] = _store.Permissions.Select(x => x.Code).ToArray(),
            ["ITSupport"] = ["System.View","System.Configure","Settings.View","Settings.Edit","VFC.View","Logs.View","Database.View","Database.Backup","Database.Maintenance"],
            ["Accounting"] = _store.Permissions
                .Where(x => x.Group is "Accounting" or "Logs"
                    || x.Code.StartsWith("FrontOffice.Folio.", StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Code)
                .ToArray(),
            ["Warehouse"] = _store.Permissions.Where(x => x.Group == "Warehouse").Select(x => x.Code).ToArray(),
            ["HR"] = _store.Permissions.Where(x => x.Group == "HR").Select(x => x.Code).ToArray(),
            ["Viewer"] = ["System.View","Settings.View","Logs.View","Warehouse.View","Accounting.View","HR.View","VFC.View","FrontOffice.Reservation.View","FrontOffice.Stay.View","FrontOffice.Folio.View"],
            ["FrontOffice"] = _store.Permissions
                .Where(x => x.Group == "FrontOffice")
                .Select(x => x.Code)
                .ToArray()
        };

        // Rebuild only the assignments owned by built-in roles.  Custom roles
        // are customer data and must survive every startup/upgrade seed pass.
        var seededRoleIds = _store.Roles
            .Where(role => map.ContainsKey(role.Code))
            .Select(role => role.Id)
            .ToHashSet();
        _store.RolePermissions.RemoveAll(x => seededRoleIds.Contains(x.RoleId));
        foreach (var role in _store.Roles)
        {
            if (!map.TryGetValue(role.Code, out var codes))
            {
                continue;
            }

            foreach (var code in codes)
            {
                var permission = _store.Permissions.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
                if (permission is not null)
                {
                    _store.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
                }
            }
        }
    }

    private static IEnumerable<AppPermission> DefaultPermissions()
    {
        var data = new[]
        {
            ("System.View","Xem hệ thống","System"),("System.Configure","Cấu hình hệ thống","System"),("System.Maintenance","Bảo trì hệ thống","System"),
            ("Settings.View","Xem cài đặt","Settings"),("Settings.Edit","Sửa cài đặt","Settings"),
            ("Security.Users.View","Xem người dùng","Security"),("Security.Users.Create","Tạo người dùng","Security"),("Security.Users.Edit","Sửa người dùng","Security"),("Security.Users.Delete","Xóa người dùng","Security"),
            ("Security.Roles.View","Xem vai trò","Security"),("Security.Roles.Edit","Sửa vai trò","Security"),("Security.Permissions.Edit","Sửa quyền","Security"),
            ("Database.View","Xem database","Database"),("Database.EditConnection","Sửa kết nối","Database"),("Database.RunMigration","Chạy migration","Database"),("Database.Backup","Backup","Database"),("Database.Restore","Restore","Database"),("Database.Maintenance","Bảo trì DB","Database"),
            ("VFC.View","Xem VFC","VirtualFileCenter"),("VFC.OpenDocument","Mở chứng từ","VirtualFileCenter"),("VFC.OpenPhysicalLocation","Mở vị trí vật lý","VirtualFileCenter"),("VFC.Print","In","VirtualFileCenter"),("VFC.Export","Xuất","VirtualFileCenter"),("VFC.RebuildIndex","Rebuild index","VirtualFileCenter"),
            ("Accounting.View","Xem kế toán","Accounting"),("Accounting.Create","Tạo kế toán","Accounting"),("Accounting.Edit","Sửa kế toán","Accounting"),("Accounting.Delete","Xóa kế toán","Accounting"),("Accounting.Print","In kế toán","Accounting"),("Accounting.Export","Xuất kế toán","Accounting"),("Accounting.Approve","Duyệt kế toán","Accounting"),
            ("Warehouse.View","Xem kho","Warehouse"),("Warehouse.Create","Tạo kho","Warehouse"),("Warehouse.Edit","Sửa kho","Warehouse"),("Warehouse.Delete","Xóa kho","Warehouse"),("Warehouse.Print","In kho","Warehouse"),("Warehouse.Export","Xuất kho","Warehouse"),("Warehouse.Approve","Duyệt kho","Warehouse"),
            ("Sales.View","Xem sales","Sales"),("Sales.Create","Tạo sales","Sales"),("Sales.Edit","Sửa sales","Sales"),("Sales.Delete","Xóa sales","Sales"),("Sales.Sync","Đồng bộ sales","Sales"),("Sales.ConfigureClient","Cấu hình client sales","Sales"),
            ("HR.View","Xem HR","HR"),("HR.Create","Tạo HR","HR"),("HR.Edit","Sửa HR","HR"),("HR.Delete","Xóa HR","HR"),("HR.PayrollView","Xem lương","HR"),("HR.PayrollEdit","Sửa lương","HR"),("HR.Export","Xuất HR","HR"),
            ("Logs.View","Xem log","Logs"),("Logs.Export","Xuất log","Logs"),("Logs.Delete","Xóa log","Logs"),
            ("FrontOffice.Reservation.View","Xem đặt phòng","FrontOffice"),("FrontOffice.Reservation.Create","Tạo đặt phòng","FrontOffice"),("FrontOffice.Reservation.Update","Cập nhật đặt phòng","FrontOffice"),
            ("FrontOffice.Reservation.AssignRoom","Phân phòng đặt phòng","FrontOffice"),("FrontOffice.Reservation.ChangeStatus","Đổi trạng thái đặt phòng","FrontOffice"),("FrontOffice.Reservation.Cancel","Hủy đặt phòng","FrontOffice"),
            ("FrontOffice.Stay.View","Xem lưu trú","FrontOffice"),("FrontOffice.Stay.Prepare","Chuẩn bị lưu trú","FrontOffice"),("FrontOffice.Stay.CheckIn","Nhận phòng","FrontOffice"),
            ("FrontOffice.Stay.CheckOut","Trả phòng","FrontOffice"),("FrontOffice.Stay.Cancel","Hủy lưu trú","FrontOffice"),("FrontOffice.Stay.Update","Cập nhật lưu trú","FrontOffice"),
            ("FrontOffice.Folio.View","Xem Folio","FrontOffice"),("FrontOffice.Folio.Create","Tạo Folio","FrontOffice"),
            ("FrontOffice.Folio.AddCharge","Ghi nhận phát sinh Folio","FrontOffice"),("FrontOffice.Folio.AddPayment","Ghi nhận thanh toán Folio","FrontOffice"),
            ("FrontOffice.Folio.Manage","Quản lý Folio","FrontOffice"),("FrontOffice.Folio.Reverse","Đảo giao dịch Folio","FrontOffice"),("FrontOffice.Folio.CorrectionApprove","Duyệt điều chỉnh Folio","FrontOffice"),
            ("FrontOffice.Reporting.View","Xem báo cáo Front Office","FrontOffice"),
            ("reports.view","Xem báo cáo","Reports"),("reports.export","Xuất báo cáo","Reports"),
            ("reports.export.sensitive","Xuất trường nhạy cảm của báo cáo","Reports"),
            ("reports.backfill","Backfill ngày hạch toán Folio","Reports"),
            ("restaurant.alacarte.view","Xem nhà hàng À La Carte","Sales"),("restaurant.alacarte.manage","Quản lý nhà hàng À La Carte","Sales"),
            ("restaurant.banquet.view","Xem nhà hàng tiệc","Sales"),("restaurant.banquet.manage","Quản lý nhà hàng tiệc","Sales")
        };

        return data.Select(x => new AppPermission { Code = x.Item1, Name = x.Item2, Group = x.Item3, Description = x.Item2 });
    }

    private static IEnumerable<Role> DefaultRoles()
    {
        return new[]
        {
            new Role { Code = "Administrator", Name = "Administrator", Description = "Toàn quyền", IsSystem = true, IsActive = true },
            new Role { Code = "ITSupport", Name = "IT Support", Description = "Hỗ trợ hệ thống", IsSystem = true, IsActive = true },
            new Role { Code = "Accounting", Name = "Accounting", Description = "Kế toán", IsSystem = true, IsActive = true },
            new Role { Code = "Warehouse", Name = "Warehouse", Description = "Kho", IsSystem = true, IsActive = true },
            new Role { Code = "HR", Name = "HR", Description = "Nhân sự", IsSystem = true, IsActive = true },
            new Role { Code = "Viewer", Name = "Viewer", Description = "Chỉ xem", IsSystem = true, IsActive = true },
            new Role { Code = "FrontOffice", Name = "Front Office", Description = "Vận hành lễ tân và lưu trú", IsSystem = true, IsActive = true }
        };
    }
}
