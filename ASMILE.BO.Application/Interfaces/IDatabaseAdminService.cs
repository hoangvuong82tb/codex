using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IDatabaseAdminService
{
    Task<SystemConnectionDto> GetPrimaryConnectionAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatabaseObjectMetadataDto>> GetDatabaseObjectsAsync(CancellationToken cancellationToken = default);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task RunMigrationAsync(CancellationToken cancellationToken = default);
    Task RunMaintenanceAsync(CancellationToken cancellationToken = default);
}
