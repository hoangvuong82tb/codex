using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IDatabaseConnectionConfigurationService
{
    DatabaseConnectionConfigurationDto GetCurrent();
    Task<DatabaseConnectionTestResult> TestAsync(DatabaseConnectionConfigurationRequest request, CancellationToken cancellationToken = default);
    Task SaveAsync(DatabaseConnectionConfigurationRequest request, CancellationToken cancellationToken = default);
}
