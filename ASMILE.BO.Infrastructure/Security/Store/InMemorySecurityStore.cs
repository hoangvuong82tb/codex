using ASMILE.BO.Core.Entities;

namespace ASMILE.BO.Infrastructure.Security.Store;

public sealed class InMemorySecurityStore
{
    public List<UserAccount> Users { get; } = [];
    public List<Role> Roles { get; } = [];
    public List<AppPermission> Permissions { get; } = [];
    public List<UserRole> UserRoles { get; } = [];
    public List<RolePermission> RolePermissions { get; } = [];
    public List<LoginSession> Sessions { get; } = [];
    public List<LoginAudit> Audits { get; } = [];
}
