using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Security.Store;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

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
        var seed = new SecuritySeedService(store, new Pbkdf2PasswordHasher(), TestDbContextFactory.AuditLogService(db), db);
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
}
