using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IEnterpriseConsistencyValidatorService
{
    Task<IReadOnlyList<EnterpriseConsistencyViolationDto>> ValidateAsync(DateTime utcNow, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnterpriseConsistencyViolationDto>> GetViolationsAsync(int take = 200, CancellationToken cancellationToken = default);
    Task<int> AutoRecoverAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}
