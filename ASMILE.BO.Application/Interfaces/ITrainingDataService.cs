using ASMILE.BO.Application.DTOs;

namespace ASMILE.BO.Application.Interfaces;

public interface ITrainingDataService
{
    Task<TrainingDataStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<TrainingDataPreflightDto> GetPreflightAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrainingDataSeedProfileDto>> GetSeedProfilesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrainingDataDemoProfileDto>> GetDemoProfilesAsync(CancellationToken cancellationToken = default);
    Task<TrainingDataDemoPreviewDto> GetDemoPreviewAsync(TrainingDataSeedProfile profile, CancellationToken cancellationToken = default);
    Task<TrainingDataOperationResult> ExecuteAsync(TrainingDataOperationRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrainingDataOperationResult>> GetRunHistoryAsync(int take = 50, CancellationToken cancellationToken = default);
}

public interface ITrainingDataCleaner
{
    Task<IReadOnlyList<TrainingDataStepResult>> ClearAsync(TrainingDataResetScope scope, bool isDryRun, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TrainingDataStepResult>> ClearTrainingOnlyAsync(TrainingDataSeedProfile? profile, bool isDryRun, CancellationToken cancellationToken = default);
}

public interface ITrainingDataSeeder
{
    Task<IReadOnlyList<TrainingDataStepResult>> SeedAsync(TrainingDataSeedProfile profile, bool isDryRun, Guid? runId = null, CancellationToken cancellationToken = default);
}

public interface ITrainingDataValidator
{
    Task<IReadOnlyList<string>> ValidateAsync(CancellationToken cancellationToken = default);
}
