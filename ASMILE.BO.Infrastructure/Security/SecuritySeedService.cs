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

    public SecuritySeedService(InMemorySecurityStore store, IPasswordHasher passwordHasher, IAuditLogService auditLogService, AppDbContext dbContext)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _auditLogService = auditLogService;
        _dbContext = dbContext;
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

        if (_store.Roles.Count == 0)
        {
            _store.Roles.AddRange(DefaultRoles());
        }

        AssignRolePermissions();

        var persistedAdmin = await _dbContext.AppUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserName == "administrator");

        var admin = _store.Users.FirstOrDefault(x => x.NormalizedUserName == "ADMINISTRATOR");
        if (admin is null)
        {
            admin = new UserAccount
            {
                Id = persistedAdmin?.Id ?? Guid.NewGuid(),
                UserName = persistedAdmin?.UserName ?? "administrator",
                NormalizedUserName = "ADMINISTRATOR",
                DisplayName = persistedAdmin?.DisplayName ?? "Administrator",
                Email = persistedAdmin?.Email,
                PasswordHash = _passwordHasher.HashPassword("Maimaiyeuem1987"),
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

            await _auditLogService.LogAsync("SeedAdminExists", "Tài khoản administrator đã tồn tại", "system");
        }

        var adminRole = _store.Roles.First(x => x.Code == "Administrator");
        if (!_store.UserRoles.Any(x => x.UserId == admin.Id && x.RoleId == adminRole.Id))
        {
            _store.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = adminRole.Id });
        }
    }

    private void AssignRolePermissions()
    {
        var map = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Administrator"] = _store.Permissions.Select(x => x.Code).ToArray(),
            ["ITSupport"] = ["System.View","System.Configure","Settings.View","Settings.Edit","VFC.View","Logs.View","Database.View","Database.Backup","Database.Maintenance"],
            ["Accounting"] = _store.Permissions.Where(x => x.Group is "Accounting" or "Logs").Select(x => x.Code).ToArray(),
            ["Warehouse"] = _store.Permissions.Where(x => x.Group == "Warehouse").Select(x => x.Code).ToArray(),
            ["HR"] = _store.Permissions.Where(x => x.Group == "HR").Select(x => x.Code).ToArray(),
            ["Viewer"] = ["System.View","Settings.View","Logs.View","Warehouse.View","Accounting.View","HR.View","VFC.View"]
        };

        _store.RolePermissions.Clear();
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
            new Role { Code = "Viewer", Name = "Viewer", Description = "Chỉ xem", IsSystem = true, IsActive = true }
        };
    }
}
