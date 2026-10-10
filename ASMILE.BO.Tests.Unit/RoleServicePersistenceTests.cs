using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Infrastructure.Security.Store;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ASMILE.BO.Tests.Unit;

public sealed class RoleServicePersistenceTests
{
    [Fact]
    public async Task DatabaseRoleCatalogAndPermissionsAreUsedForRuntimeAuthorization()
    {
        await using var db = TestSupport.TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var role = new AppRole { Code = "ReceptionCustom", Name = "Reception custom", IsActive = true };
        var permission = new AppPermission
        {
            Code = "FrontOffice.Stay.View",
            Name = "View stay",
            Group = "FrontOffice",
            Description = "View stay records"
        };
        var user = new AppUser { UserName = "reception", DisplayName = "Reception", PasswordHash = "test" };
        db.AppRoles.Add(role);
        db.AppPermissions.Add(permission);
        db.AppUsers.Add(user);
        db.AppUserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = role.Id });
        db.AppRolePermissions.Add(new AppRolePermission { RoleId = role.Id, PermissionCode = permission.Code });
        await db.SaveChangesAsync();

        var service = new RoleService(new InMemorySecurityStore(), db);

        var roles = await service.GetRolesAsync();
        var permissions = await service.GetPermissionsAsync();
        var effectiveCodes = await service.GetUserPermissionCodesAsync(user.Id);

        Assert.Contains(roles, x => x.Id == role.Id && x.Code == "ReceptionCustom");
        Assert.Contains(permissions, x => x.Code == permission.Code);
        Assert.Contains(permission.Code, effectiveCodes);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task RuntimeAuthorizationIgnoresInactiveOrArchivedRoles(bool isActive, bool isArchived)
    {
        await using var db = TestSupport.TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var role = new AppRole
        {
            Code = "SuspendedRole",
            Name = "Suspended role",
            IsActive = isActive,
            IsArchived = isArchived
        };
        var user = new AppUser { UserName = "operator", DisplayName = "Operator", PasswordHash = "test" };
        const string permissionCode = "FrontOffice.Stay.View";
        db.AppRoles.Add(role);
        db.AppUsers.Add(user);
        db.AppUserRoles.Add(new AppUserRole { UserId = user.Id, RoleId = role.Id });
        db.AppRolePermissions.Add(new AppRolePermission { RoleId = role.Id, PermissionCode = permissionCode });
        await db.SaveChangesAsync();

        var service = new RoleService(new InMemorySecurityStore(), db);
        var effectiveCodes = await service.GetUserPermissionCodesAsync(user.Id);

        Assert.DoesNotContain(permissionCode, effectiveCodes);
    }

    [Fact]
    public async Task SavingRolePermissionsReplacesDatabaseAssignmentsAndIgnoresUnknownCodes()
    {
        await using var db = TestSupport.TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var role = new AppRole { Code = "ReceptionCustom", Name = "Reception custom", IsActive = true };
        db.AppRoles.Add(role);
        db.AppPermissions.AddRange(
            new AppPermission { Code = "FrontOffice.Stay.View", Name = "View stay", Group = "FrontOffice" },
            new AppPermission { Code = "FrontOffice.Stay.Update", Name = "Update stay", Group = "FrontOffice" });
        db.AppRolePermissions.Add(new AppRolePermission { RoleId = role.Id, PermissionCode = "FrontOffice.Stay.View" });
        await db.SaveChangesAsync();

        var service = new RoleService(new InMemorySecurityStore(), db);
        await service.SaveRolePermissionsAsync(role.Id, ["FrontOffice.Stay.Update", "Unknown.Permission"]);

        var savedCodes = await db.AppRolePermissions
            .Where(x => x.RoleId == role.Id)
            .Select(x => x.PermissionCode)
            .ToListAsync();
        Assert.Equal(["FrontOffice.Stay.Update"], savedCodes);
    }

    [Fact]
    public async Task CreatingUserWithDatabaseRoleIdKeepsTheRoleLink()
    {
        await using var db = TestSupport.TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var role = new AppRole { Code = "ReceptionCustom", Name = "Reception custom", IsActive = true };
        db.AppRoles.Add(role);
        await db.SaveChangesAsync();

        var service = new UserAccountService(
            new InMemorySecurityStore(),
            new Pbkdf2PasswordHasher(),
            TestSupport.TestDbContextFactory.AuditLogService(db),
            db);

        var created = await service.CreateUserAsync(new CreateUserRequestDto
        {
            UserName = "reception",
            DisplayName = "Reception",
            InitialPassword = "StrongPassword!123",
            IsActive = true,
            MustChangePassword = false,
            RoleIds = [role.Id]
        });

        var link = await db.AppUserRoles.SingleAsync(x => x.UserId == created.Id);
        Assert.Equal(role.Id, link.RoleId);
    }
}
