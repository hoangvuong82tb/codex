using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface IInfrastructureHealthService
{
    Task<InfrastructureHealthReportDto> CheckAsync(CancellationToken cancellationToken = default);
}

  public interface IDatabaseMigrationService
  {
      Task InitializeAsync(CancellationToken cancellationToken = default);
      Task<MigrationStatusDto> ValidatePendingMigrationsAsync(CancellationToken cancellationToken = default);
      Task<DatabasePreflightDto> GetPreflightAsync(CancellationToken cancellationToken = default);
      Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);
      Task<IReadOnlyList<string>> ValidateSeedConsistencyAsync(CancellationToken cancellationToken = default);
  }

public interface ISqlPerformanceScalingService
{
    Task<IReadOnlyList<JournalLinePerformanceDto>> GetPagedJournalLinesAsync(DateTime fromDate, DateTime toDate, string? accountCode, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PerformanceTimingDto>> MeasureCoreQueriesAsync(CancellationToken cancellationToken = default);
    Task<PerformanceTimingDto> SeedJournalLinesForScaleTestAsync(DataScaleRequestDto request, CancellationToken cancellationToken = default);
}
