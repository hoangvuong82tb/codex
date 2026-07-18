using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Security;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class EnterprisePermissionTests
{
    private static readonly Guid UserId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid CompanyId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    [Fact]
    public async Task RoleInheritance_GrantsParentPermissions()
    {
        var user = new TestCurrentUserService(UserId, roles: []);
        await using var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId), seedAccountingCore: false);
        await GrantCompanyAccessAsync(db);
        var service = new PermissionService(db, user, new TestCompanyContext(CompanyId), TestDbContextFactory.AuditLogService(db));

        var parent = await service.CreateRoleAsync("ParentReports", "Quyền báo cáo", null);
        var child = await service.CreateRoleAsync("ChildReports", "Kế thừa báo cáo", null);
        await service.AssignPermissionsAsync(parent.Id, ["reports.export"]);
        await service.SetRoleInheritanceAsync(child.Id, [parent.Id]);
        await service.AssignUsersToRoleAsync(child.Id, [UserId]);

        var allowed = await service.HasPermissionAsync("reports.export", CompanyId);
        var preview = await service.PreviewEffectivePermissionsAsync(UserId, CompanyId);

        allowed.Should().BeTrue();
        preview.Matrix.Should().Contain(x => x.PermissionCode == "reports.export" && x.IsGranted);
    }

    [Fact]
    public async Task DenyOverride_WinsOverGrantedRole()
    {
        var user = new TestCurrentUserService(UserId, roles: []);
        await using var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId), seedAccountingCore: false);
        await GrantCompanyAccessAsync(db);
        var service = new PermissionService(db, user, new TestCompanyContext(CompanyId), TestDbContextFactory.AuditLogService(db));

        var role = await service.CreateRoleAsync("SecurityEditor", "Sửa phân quyền", null);
        await service.AssignPermissionsAsync(role.Id, ["security.permissions.edit"]);
        await service.AssignUsersToRoleAsync(role.Id, [UserId]);
        db.AppUserPermissions.Add(new() { UserId = UserId, PermissionCode = "security.permissions.edit", IsGranted = false, CompanyId = CompanyId });
        await db.SaveChangesAsync();
        await service.InvalidateCacheAsync(UserId);

        var allowed = await service.HasPermissionAsync("security.permissions.edit", CompanyId);
        var preview = await service.PreviewEffectivePermissionsAsync(UserId, CompanyId);

        allowed.Should().BeFalse();
        preview.Matrix.Should().Contain(x => x.PermissionCode == "security.permissions.edit" && x.IsDenied);
    }

    [Fact]
    public async Task DataPermission_DenyOverridesDataAccess()
    {
        var user = new TestCurrentUserService(UserId, roles: []);
        await using var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId), seedAccountingCore: false);
        await GrantCompanyAccessAsync(db);
        var service = new PermissionService(db, user, new TestCompanyContext(CompanyId), TestDbContextFactory.AuditLogService(db));
        var warehouseId = Guid.NewGuid();

        await service.SetDataPermissionAsync(new DataPermissionDto
        {
            SubjectType = "User",
            SubjectId = UserId,
            ScopeType = "Warehouse",
            ScopeId = warehouseId,
            CompanyId = CompanyId,
            IsGranted = false
        });

        var allowed = await service.HasDataAccessAsync("Warehouse", warehouseId, CompanyId);

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task ReservationPermissionCatalog_IsRegisteredAndAssignedToExistingAdministratorRole()
    {
        var user = new TestCurrentUserService(UserId, roles: []);
        await using var db = TestDbContextFactory.CreateContext(new TestCompanyContext(CompanyId), seedAccountingCore: false);
        var administrator = new AppRole { Code = "Administrator", Name = "Administrator", IsSystem = true };
        db.AppRoles.Add(administrator);
        await db.SaveChangesAsync();

        var service = new PermissionService(db, user, new TestCompanyContext(CompanyId), TestDbContextFactory.AuditLogService(db));
        var permissions = await service.GetPermissionsAsync(administrator.Id);
        var reservationCodes = new[]
        {
            "FrontOffice.Reservation.View",
            "FrontOffice.Reservation.Create",
            "FrontOffice.Reservation.Update",
            "FrontOffice.Reservation.AssignRoom",
            "FrontOffice.Reservation.ChangeStatus",
            "FrontOffice.Reservation.Cancel"
        };

        permissions.Where(x => reservationCodes.Contains(x.Code)).Should().HaveCount(6).And.OnlyContain(x => x.IsAssigned);
    }

    private sealed class TestCompanyContext : ICurrentCompanyContext
    {
        public TestCompanyContext(Guid companyId)
        {
            CompanyId = companyId;
        }

        public Guid? CompanyId { get; private set; }
        public Guid? BranchId { get; private set; }
        public string CompanyName { get; private set; } = "Công ty kiểm thử";
        public string? BranchName { get; private set; }
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
        {
            CompanyId = companyId;
            return Task.CompletedTask;
        }

        public Task SetBranchAsync(Guid? branchId, CancellationToken cancellationToken = default)
        {
            BranchId = branchId;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([new() { Id = CompanyId!.Value, Code = "TEST", Name = CompanyName }]);

        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }

    private static async Task GrantCompanyAccessAsync(ASMILE.BO.Infrastructure.Persistence.AppDbContext db)
    {
        db.UserCompanyAccesses.Add(new() { UserId = UserId, CompanyId = CompanyId, IsActive = true, IsDefault = true });
        await db.SaveChangesAsync();
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly HashSet<string> _permissions;

        public TestCurrentUserService(Guid userId, IReadOnlyList<string> roles)
        {
            _permissions = [];
            CurrentUser = new UserAccountDto
            {
                Id = userId,
                UserName = "phan.quyen",
                DisplayName = "Kiểm thử phân quyền",
                Roles = roles,
                IsActive = true
            };
            CurrentSessionId = Guid.NewGuid();
        }

        public UserAccountDto? CurrentUser { get; private set; }
        public Guid? CurrentSessionId { get; private set; }
        public bool IsAuthenticated => CurrentUser is not null;
        public bool HasPermission(string permissionCode) => _permissions.Contains(permissionCode);
        public void SetCurrentUser(LoginResultDto login) { }
        public void Clear()
        {
            CurrentUser = null;
            CurrentSessionId = null;
        }
    }
}
