using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Security.Store;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class ReservationPermissionSeedTests
{
    [Fact]
    public async Task RES_PERM_U_001_security_seed_upserts_reservation_permissions_and_grants_administrator_without_duplicates()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var store = new InMemorySecurityStore();
        store.Permissions.Add(new AppPermission { Code = "System.View", Name = "Xem hệ thống", Group = "System", Description = "Xem hệ thống" });
        store.Roles.Add(new Role { Code = "Administrator", Name = "Administrator", IsSystem = true, IsActive = true });
        var seed = new SecuritySeedService(store, new Pbkdf2PasswordHasher(), TestDbContextFactory.AuditLogService(db), db, initialAdminPassword: "UnitTest-Admin-Password-2026!");
        var reservationCodes = new[]
        {
            "FrontOffice.Reservation.View",
            "FrontOffice.Reservation.Create",
            "FrontOffice.Reservation.Update",
            "FrontOffice.Reservation.AssignRoom",
            "FrontOffice.Reservation.ChangeStatus",
            "FrontOffice.Reservation.Cancel"
        };

        await seed.EnsureSeedDataAsync();
        await seed.EnsureSeedDataAsync();

        store.Permissions.Where(x => reservationCodes.Contains(x.Code)).Should().HaveCount(6);
        store.Permissions.Where(x => reservationCodes.Contains(x.Code)).Select(x => x.Code).Should().OnlyHaveUniqueItems();

        var administratorRole = store.Roles.Single(x => x.Code == "Administrator");
        var assignedCodes = store.RolePermissions
            .Where(x => x.RoleId == administratorRole.Id)
            .Join(store.Permissions, rolePermission => rolePermission.PermissionId, permission => permission.Id, (_, permission) => permission.Code)
            .ToArray();
        assignedCodes.Should().Contain(reservationCodes);
    }

    [Fact]
    public async Task RES_PERM_U_002_accounting_role_can_open_and_manage_folio_workflows()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var store = new InMemorySecurityStore();
        var seed = new SecuritySeedService(store, new Pbkdf2PasswordHasher(), TestDbContextFactory.AuditLogService(db), db, initialAdminPassword: "UnitTest-Admin-Password-2026!");

        await seed.EnsureSeedDataAsync();

        var accountingRole = store.Roles.Single(x => x.Code == "Accounting");
        var assignedCodes = store.RolePermissions
            .Where(x => x.RoleId == accountingRole.Id)
            .Join(store.Permissions, rolePermission => rolePermission.PermissionId, permission => permission.Id, (_, permission) => permission.Code)
            .ToArray();

        assignedCodes.Should().Contain(new[]
        {
            "FrontOffice.Folio.View",
            "FrontOffice.Folio.AddCharge",
            "FrontOffice.Folio.AddPayment",
            "FrontOffice.Folio.Manage",
            "FrontOffice.Folio.Reverse"
        });
    }

    [Fact]
    public async Task RES_PERM_U_003_existing_role_store_receives_front_office_role_and_permissions()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var store = new InMemorySecurityStore();
        // Simulate an upgraded installation where legacy roles already exist.
        store.Roles.Add(new Role { Code = "Administrator", Name = "Administrator", IsSystem = true, IsActive = true });
        var seed = new SecuritySeedService(store, new Pbkdf2PasswordHasher(), TestDbContextFactory.AuditLogService(db), db, initialAdminPassword: "UnitTest-Admin-Password-2026!");

        await seed.EnsureSeedDataAsync();
        await seed.EnsureSeedDataAsync();

        store.Roles.Count(x => x.Code == "FrontOffice").Should().Be(1);
        var frontOfficeRole = store.Roles.Single(x => x.Code == "FrontOffice");
        var assignedCodes = store.RolePermissions
            .Where(x => x.RoleId == frontOfficeRole.Id)
            .Join(store.Permissions, rolePermission => rolePermission.PermissionId, permission => permission.Id, (_, permission) => permission.Code)
            .ToArray();

        assignedCodes.Should().Contain(new[]
        {
            "FrontOffice.Reservation.View",
            "FrontOffice.Stay.View",
            "FrontOffice.Stay.CheckIn",
            "FrontOffice.Stay.CheckOut",
            "FrontOffice.Folio.View"
        });
    }

    [Fact]
    public async Task RES_PERM_U_004_seed_does_not_remove_custom_role_permissions()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var store = new InMemorySecurityStore();
        var seed = new SecuritySeedService(store, new Pbkdf2PasswordHasher(), TestDbContextFactory.AuditLogService(db), db, initialAdminPassword: "UnitTest-Admin-Password-2026!");

        await seed.EnsureSeedDataAsync();

        var customRole = new Role { Code = "NightManager", Name = "Night Manager", IsSystem = false, IsActive = true };
        store.Roles.Add(customRole);
        var customPermission = store.Permissions.Single(x => x.Code == "Security.Users.View");
        store.RolePermissions.Add(new RolePermission { RoleId = customRole.Id, PermissionId = customPermission.Id });

        await seed.EnsureSeedDataAsync();

        store.RolePermissions.Should().Contain(x => x.RoleId == customRole.Id && x.PermissionId == customPermission.Id);
    }

    [Fact]
    public async Task RES_PERM_U_005_seed_persists_administrator_role_link_for_authorization()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var store = new InMemorySecurityStore();
        var seed = new SecuritySeedService(
            store,
            new Pbkdf2PasswordHasher(),
            TestDbContextFactory.AuditLogService(db),
            db,
            initialAdminPassword: "UnitTest-Admin-Password-2026!");

        await seed.EnsureSeedDataAsync();

        var persistedAdmin = await db.AppUsers.SingleAsync(x => x.UserName == "administrator");
        var persistedRole = await db.AppRoles.SingleAsync(x => x.Code == "Administrator");

        (await db.AppUserRoles.AnyAsync(x => x.UserId == persistedAdmin.Id && x.RoleId == persistedRole.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task RES_PERM_U_006_persisted_audit_export_permission_is_not_granted_to_read_only_roles()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var store = new InMemorySecurityStore();
        var seed = new SecuritySeedService(
            store,
            new Pbkdf2PasswordHasher(),
            TestDbContextFactory.AuditLogService(db),
            db,
            initialAdminPassword: "UnitTest-Admin-Password-2026!");

        await seed.EnsureSeedDataAsync();

        (await db.AppPermissions.AnyAsync(x => x.Code == "Logs.Export")).Should().BeTrue();
        var assignedRoleCodes = await db.AppRolePermissions
            .Where(x => x.PermissionCode == "Logs.Export")
            .Join(db.AppRoles, assignment => assignment.RoleId, role => role.Id, (_, role) => role.Code)
            .ToArrayAsync();

        assignedRoleCodes.Should().Contain(new[] { "Administrator", "Accounting" });
        assignedRoleCodes.Should().NotContain(new[] { "Viewer", "ITSupport" });
    }
}
