namespace ASMILE.BO.Application.DTOs;

public enum TrainingDataOperationType
{
    CheckStatus = 0,
    ClearBusinessData = 1,
    SeedFoundation = 2,
    SeedFullTraining = 3,
    DeleteTrainingData = 4,
    ResetToBlank = 5,
    PreflightCheck = 6,
    ResetTrainingOnly = 7
}

public enum TrainingDataSeedProfile
{
    Blank = 0,
    FoundationOnly = 1,
    FullTraining = 2,
    AccountingDemo = 3,
    InventoryDemo = 4,
    NewCompanyBlank = 5,
    NewCompanyFoundation = 6,
    NewCompanyFullTraining = 7,
    RestaurantDemo = 8,
    HotelDemo = 9,
    RestaurantHotelFullDemo = 10
}

public enum TrainingDataResetScope
{
    BusinessOnly = 0,
    BusinessAndOperationalLogs = 1
}

public sealed class TrainingDataStatusDto
{
    public bool HasBusinessData { get; set; }
    public bool HasTrainingDataSignature { get; set; }
    public int JournalCount { get; set; }
    public int InventoryDocumentCount { get; set; }
    public int CashTransactionCount { get; set; }
    public int BankTransactionCount { get; set; }
    public int CustomerCount { get; set; }
    public int SupplierCount { get; set; }
    public int ItemCount { get; set; }
    public int WarehouseCount { get; set; }
    public DateTime CheckedAtUtc { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public sealed class TrainingDataOperationRequest
{
    public TrainingDataOperationType OperationType { get; set; }
    public TrainingDataSeedProfile? SeedProfile { get; set; }
    public TrainingDataResetScope ResetScope { get; set; } = TrainingDataResetScope.BusinessAndOperationalLogs;
    public bool IsDryRun { get; set; }
    public string ConfirmationText { get; set; } = string.Empty;
}

public sealed class TrainingDataStepResult
{
    public int StepOrder { get; set; }
    public string StepCode { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public bool Success { get; set; }
    public int BeforeCount { get; set; }
    public int DeletedCount { get; set; }
    public int AfterCount { get; set; }
    public int CreatedCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }
}

public sealed class TrainingDataOperationResult
{
    public Guid RunId { get; set; }
    public TrainingDataOperationType OperationType { get; set; }
    public TrainingDataSeedProfile? SeedProfile { get; set; }
    public TrainingDataResetScope ResetScope { get; set; }
    public bool IsDryRun { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int TotalDeletedCount { get; set; }
    public int TotalCreatedCount { get; set; }
    public IReadOnlyList<TrainingDataStepResult> Steps { get; set; } = [];
    public IReadOnlyList<string> ValidationWarnings { get; set; } = [];
    public DateTime StartedAtUtc { get; set; }
    public DateTime FinishedAtUtc { get; set; }
}

public sealed class TrainingDataSeedProfileDto
{
    public TrainingDataSeedProfile Profile { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class TrainingDataPreflightDto
{
    public DateTime CheckedAtUtc { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string EnvironmentName { get; set; } = string.Empty;
    public bool AllowDestructiveReset { get; set; }
    public bool AllowInProduction { get; set; }
    public bool AllowResetWithRealDataRisk { get; set; }
    public int CompanyCount { get; set; }
    public int BranchCount { get; set; }
    public int JournalCount { get; set; }
    public int BusinessDocumentCount { get; set; }
    public int SystemUserCount { get; set; }
    public int RoleCount { get; set; }
    public int PermissionCount { get; set; }
    public int AccountingStandardCount { get; set; }
    public int SettingsCount { get; set; }
    public int TrainingMarkerCount { get; set; }
    public bool HasRealDataRisk { get; set; }
    public string RiskLevel { get; set; } = "Low";
    public string Summary { get; set; } = string.Empty;
    public IReadOnlyList<TrainingDataSafetyWarningDto> Warnings { get; set; } = [];
}

public sealed class TrainingDataSafetyWarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Message { get; set; } = string.Empty;
    public bool IsBlocking { get; set; }
}

public sealed class TrainingDataDemoProfileDto
{
    public TrainingDataSeedProfile Profile { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IndustryType { get; set; } = string.Empty;
}

public sealed class TrainingDataDemoPreviewDto
{
    public TrainingDataSeedProfile Profile { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IndustryType { get; set; } = string.Empty;
    public IReadOnlyList<TrainingDataDemoPreviewItemDto> EstimatedCounts { get; set; } = [];
    public IReadOnlyList<string> KeyModules { get; set; } = [];
    public IReadOnlyList<string> SafetyNotes { get; set; } = [];
}

public sealed class TrainingDataDemoPreviewItemDto
{
    public string Name { get; set; } = string.Empty;
    public int EstimatedCount { get; set; }
}
