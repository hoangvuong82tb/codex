using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class CompanyContextAccessor : ICompanyContextAccessor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly AsyncLocal<ScopeFrame?> _currentScope = new();

    public CompanyContextAccessor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Guid? CurrentCompanyId => _currentScope.Value?.CompanyId;
    public Guid? CurrentBranchId => _currentScope.Value?.BranchId;
    public string? CurrentCompanyName => _currentScope.Value?.CompanyName;
    public string? CurrentBranchName => _currentScope.Value?.BranchName;
    public Guid? CurrentUserId => _currentUserService.CurrentUser?.Id;

    public void SetCurrentScope(Guid? companyId, string? companyName = null, Guid? branchId = null, string? branchName = null)
    {
        _currentScope.Value = new ScopeFrame
        {
            CompanyId = companyId,
            CompanyName = companyName,
            BranchId = branchId,
            BranchName = branchName
        };
    }

    public CompanyScope EnterCompanyScope(Guid companyId, string? companyName = null, Guid? branchId = null, string? branchName = null)
    {
        var previous = _currentScope.Value;
        _currentScope.Value = new ScopeFrame
        {
            Parent = previous,
            CompanyId = companyId,
            CompanyName = companyName ?? previous?.CompanyName,
            BranchId = branchId,
            BranchName = branchId is null ? null : branchName
        };

        return new CompanyScope(() => _currentScope.Value = previous);
    }

    public BranchScope EnterBranchScope(Guid? branchId, string? branchName = null)
    {
        var previous = _currentScope.Value;
        _currentScope.Value = new ScopeFrame
        {
            Parent = previous,
            CompanyId = previous?.CompanyId,
            CompanyName = previous?.CompanyName,
            BranchId = branchId,
            BranchName = branchId is null ? null : branchName ?? previous?.BranchName
        };

        return new BranchScope(() => _currentScope.Value = previous);
    }

    private sealed class ScopeFrame
    {
        public ScopeFrame? Parent { get; init; }
        public Guid? CompanyId { get; init; }
        public string? CompanyName { get; init; }
        public Guid? BranchId { get; init; }
        public string? BranchName { get; init; }
    }
}
