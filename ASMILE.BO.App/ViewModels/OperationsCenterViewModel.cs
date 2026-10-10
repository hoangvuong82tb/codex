using ASMILE.BO.App.Infrastructure;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class OperationsCenterViewModel : ViewModelBase
{
    private readonly IBackgroundSchedulerService _backgroundSchedulerService;
    private readonly IArchiveService _archiveService;
    private readonly IRecoveryService _recoveryService;
    private readonly IRuntimeTelemetryService _runtimeTelemetryService;
    private readonly IFailureRecoveryService _failureRecoveryService;
    private readonly IWorkflowResilienceService _workflowResilienceService;
    private readonly ITT99ComplianceService _tt99ComplianceService;
    private readonly ITT200ComplianceService _tt200ComplianceService;
    private readonly IApprovalWorkflowService _approvalWorkflowService;
    private readonly IInfrastructureHealthService _healthService;
    private readonly IDatabaseMigrationService _databaseMigrationService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IDialogService _dialogService;
    private readonly INavigationService _navigationService;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private bool _suppressSelectionRefresh;

    public ObservableCollection<BackgroundSchedulerDefinitionDto> JobDefinitions { get; } = new();
    public ObservableCollection<BackgroundSchedulerExecutionDto> JobExecutions { get; } = new();
    public ObservableCollection<BackgroundSchedulerLogDto> JobLogs { get; } = new();
    public ObservableCollection<BackgroundSchedulerDurationPointDto> DurationMetadata { get; } = new();
    public ObservableCollection<BackgroundSchedulerFailureSummaryDto> FailureSummary { get; } = new();
    public ObservableCollection<InfrastructureHealthCheckDto> HealthChecks { get; } = new();
    public ObservableCollection<RetentionPreviewDto> RetentionPreview { get; } = new();
    public ObservableCollection<ArchiveBatchDto> ArchiveHistory { get; } = new();
    public ObservableCollection<RecoveryTimelineEntryDto> RecoveryTimeline { get; } = new();
    public ObservableCollection<ArchiveIntegrityResultDto> ArchiveIntegrityResults { get; } = new();
    public ObservableCollection<RuntimeTelemetryTimelineDto> TelemetryTimeline { get; } = new();
    public ObservableCollection<TelemetryDisplayItem> TelemetryDisplayItems { get; } = new();
    public ObservableCollection<SlowOperationLogDto> SlowOperations { get; } = new();
    public ObservableCollection<TelemetryDisplayItem> SlowOperationDisplayItems { get; } = new();
    public ObservableCollection<RuntimeExceptionLogDto> RuntimeExceptions { get; } = new();
    public ObservableCollection<TelemetryDisplayItem> RuntimeErrorDisplayItems { get; } = new();
    public ObservableCollection<SystemResourceSnapshotDto> SystemResources { get; } = new();
    public ObservableCollection<TelemetryDisplayItem> PerformanceDisplayItems { get; } = new();
    public ObservableCollection<UiResponsivenessSnapshotDto> UiResponsiveness { get; } = new();
    public ObservableCollection<RecoveryRecommendationDto> RecoveryRecommendations { get; } = new();
    public ObservableCollection<TelemetryDisplayItem> RecommendationDisplayItems { get; } = new();
    public ObservableCollection<WorkflowExecutionDto> WorkflowExecutions { get; } = new();
    public ObservableCollection<WorkflowExecutionStepDto> WorkflowSteps { get; } = new();
    public ObservableCollection<WorkflowConsistencyViolationDto> WorkflowViolations { get; } = new();
    public ObservableCollection<WorkflowLeaseDto> WorkflowLeases { get; } = new();
    public ObservableCollection<WorkflowDistributedLockDto> DistributedLocks { get; } = new();
    public ObservableCollection<WorkflowDeadLetterDto> DeadLetters { get; } = new();
    public ObservableCollection<WorkflowRecoveryCheckpointDto> RecoveryCheckpoints { get; } = new();
    public ObservableCollection<WorkflowExecutionMetricDto> WorkflowMetrics { get; } = new();
    public ObservableCollection<TT99ComplianceIssueDto> TT99Issues { get; } = new();
    public ObservableCollection<TT99ComplianceSnapshotDto> TT99Snapshots { get; } = new();
    public ObservableCollection<TT99ExportManifestDto> TT99ExportManifests { get; } = new();
    public ObservableCollection<TT200ComplianceIssueDto> TT200Issues { get; } = new();
    public ObservableCollection<TT200ComplianceSnapshotDto> TT200Snapshots { get; } = new();
    public ObservableCollection<TT200ExportManifestDto> TT200ExportManifests { get; } = new();
    public ObservableCollection<ApprovalWorkflowDto> ApprovalWorkflows { get; } = new();
    public ObservableCollection<ApprovalStepDto> ApprovalSteps { get; } = new();
    public ObservableCollection<ApprovalHistoryDto> ApprovalHistory { get; } = new();
    public ObservableCollection<ApprovalSnapshotDto> ApprovalSnapshots { get; } = new();

    [ObservableProperty]
    private SchedulerDashboardDto? dashboard;
    [ObservableProperty]
    private RuntimeTelemetrySummaryDto? telemetrySummary;
    [ObservableProperty]
    private TT99ValidationSummaryDto? tT99ValidationSummary;
    [ObservableProperty]
    private TT200ValidationSummaryDto? tT200ValidationSummary;

    [ObservableProperty]
    private BackgroundSchedulerDefinitionDto? selectedJobDefinition;

    [ObservableProperty]
    private BackgroundSchedulerExecutionDto? selectedJobExecution;
    [ObservableProperty]
    private WorkflowExecutionDto? selectedWorkflowExecution;
    [ObservableProperty]
    private WorkflowReplaySessionDto? selectedWorkflowReplaySession;
    [ObservableProperty]
    private WorkflowRecoveryCheckpointDto? selectedRecoveryCheckpoint;
    [ObservableProperty]
    private WorkflowDeadLetterDto? selectedDeadLetter;
    [ObservableProperty]
    private ApprovalWorkflowDto? selectedApprovalWorkflow;

    [ObservableProperty]
    private string healthStatus = "Unknown";

    [ObservableProperty]
    private string statusMessage = "Sẵn sàng";

    [ObservableProperty]
    private int telemetrySubTabIndex;

    public bool IsTelemetryTabAll => TelemetrySubTabIndex == 0;
    public bool IsTelemetryTabSlow => TelemetrySubTabIndex == 1;
    public bool IsTelemetryTabErrors => TelemetrySubTabIndex == 2;
    public bool IsTelemetryTabPerformance => TelemetrySubTabIndex == 3;
    public bool IsTelemetryTabRecommendations => TelemetrySubTabIndex == 4;

    partial void OnTelemetrySubTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsTelemetryTabAll));
        OnPropertyChanged(nameof(IsTelemetryTabSlow));
        OnPropertyChanged(nameof(IsTelemetryTabErrors));
        OnPropertyChanged(nameof(IsTelemetryTabPerformance));
        OnPropertyChanged(nameof(IsTelemetryTabRecommendations));
    }

    public OperationsCenterViewModel(
        IBackgroundSchedulerService backgroundSchedulerService,
        IArchiveService archiveService,
        IRecoveryService recoveryService,
        IRuntimeTelemetryService runtimeTelemetryService,
        IFailureRecoveryService failureRecoveryService,
        IWorkflowResilienceService workflowResilienceService,
        ITT99ComplianceService tt99ComplianceService,
        ITT200ComplianceService tt200ComplianceService,
        IApprovalWorkflowService approvalWorkflowService,
        IInfrastructureHealthService healthService,
        IDatabaseMigrationService databaseMigrationService,
        IWorkspaceService workspaceService,
        IDialogService dialogService,
        INavigationService navigationService)
    {
        _backgroundSchedulerService = backgroundSchedulerService;
        _archiveService = archiveService;
        _recoveryService = recoveryService;
        _runtimeTelemetryService = runtimeTelemetryService;
        _failureRecoveryService = failureRecoveryService;
        _workflowResilienceService = workflowResilienceService;
        _tt99ComplianceService = tt99ComplianceService;
        _tt200ComplianceService = tt200ComplianceService;
        _approvalWorkflowService = approvalWorkflowService;
        _healthService = healthService;
        _databaseMigrationService = databaseMigrationService;
        _workspaceService = workspaceService;
        _dialogService = dialogService;
        _navigationService = navigationService;
        SafeFireAndForget.Run(RefreshAllAsync);
    }

    partial void OnSelectedJobExecutionChanged(BackgroundSchedulerExecutionDto? value)
    {
        if (_suppressSelectionRefresh)
        {
            return;
        }

        SafeFireAndForget.Run(() => RefreshSelectedExecutionLogsAsync(value?.Id));
    }

    partial void OnSelectedWorkflowExecutionChanged(WorkflowExecutionDto? value)
    {
        if (_suppressSelectionRefresh)
        {
            return;
        }

        SafeFireAndForget.Run(() => RefreshSelectedWorkflowStepsAsync(value?.Id));
    }

    partial void OnSelectedApprovalWorkflowChanged(ApprovalWorkflowDto? value)
    {
        if (_suppressSelectionRefresh)
        {
            return;
        }

        SafeFireAndForget.Run(() => RefreshSelectedApprovalWorkflowAsync(value?.Id));
    }

    [RelayCommand]
    private Task RefreshAll()
    {
        return RefreshAllAsync();
    }

    [RelayCommand]
    private async Task RetryJob()
    {
        if (SelectedJobExecution is null)
        {
            return;
        }

        await _backgroundSchedulerService.RetryAsync(SelectedJobExecution.Id);
        StatusMessage = "Đã yêu cầu chạy lại job.";
        await RefreshJobsAsync();
    }

    [RelayCommand]
    private async Task CancelJob()
    {
        if (SelectedJobExecution is null)
        {
            return;
        }

        await _backgroundSchedulerService.CancelAsync(SelectedJobExecution.Id);
        StatusMessage = "Đã gửi yêu cầu hủy job.";
        await RefreshJobsAsync();
    }

    [RelayCommand]
    private async Task RunNow()
    {
        if (SelectedJobDefinition is null)
        {
            return;
        }

        await _backgroundSchedulerService.RunNowAsync(SelectedJobDefinition.Code, "operations-center");
        StatusMessage = "Đã đưa job vào lịch chạy ngay.";
        await RefreshJobsAsync();
    }

    [RelayCommand]
    private async Task PauseSchedule()
    {
        if (SelectedJobDefinition is null)
        {
            return;
        }

        await _backgroundSchedulerService.PauseScheduleAsync(SelectedJobDefinition.Id);
        StatusMessage = "Đã tạm dừng lịch chạy.";
        await RefreshJobsAsync();
    }

    [RelayCommand]
    private async Task ResumeSchedule()
    {
        if (SelectedJobDefinition is null)
        {
            return;
        }

        await _backgroundSchedulerService.ResumeScheduleAsync(SelectedJobDefinition.Id);
        StatusMessage = "Đã tiếp tục lịch chạy.";
        await RefreshJobsAsync();
    }

    [RelayCommand]
    private async Task RunMigration()
    {
        if (!_dialogService.Confirm("Chạy các migration database đang chờ?", "Xác nhận vận hành"))
        {
            return;
        }

        await _databaseMigrationService.ApplyMigrationsAsync();
        await RefreshHealthAsync();
        StatusMessage = "Đã chạy migration database.";
    }

    [RelayCommand]
    private async Task ValidateSeed()
    {
        var warnings = await _databaseMigrationService.ValidateSeedConsistencyAsync();
        StatusMessage = warnings.Count == 0 ? "Seed dữ liệu nhất quán." : $"Có {warnings.Count} cảnh báo seed dữ liệu.";
    }

    [RelayCommand]
    private async Task ClearWorkspaceCache()
    {
        await _workspaceService.ClearWorkspaceCacheAsync();
        StatusMessage = "Đã xóa bộ nhớ đệm workspace.";
    }

    [RelayCommand]
    private void ResetNavigationHistory()
    {
        StatusMessage = "Lịch sử điều hướng sẽ đặt lại khi mở phiên shell mới.";
    }

    [RelayCommand]
    private async Task ExportDiagnostics()
    {
        var diagnostics = new
        {
            ExportedAt = DateTime.UtcNow,
            HealthStatus,
            Dashboard,
            HealthChecks,
            JobExecutions,
            FailureSummary,
            DurationMetadata
        };
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"asmile-diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(diagnostics, new JsonSerializerOptions { WriteIndented = true }));
        StatusMessage = $"Đã xuất chẩn đoán: {path}";
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        var target = ApplicationDataPaths.UserLogsDirectory;
        Directory.CreateDirectory(target);
        Process.Start(new ProcessStartInfo { FileName = target, UseShellExecute = true });
    }

    [RelayCommand]
    private void NavigateTelemetrySubTab(string? idxStr)
    {
        if (int.TryParse(idxStr, out int idx) && idx >= 0 && idx <= 4)
            TelemetrySubTabIndex = idx;
    }

    [RelayCommand]
    private Task OpenAuditCenter()
    {
        return ObserveUiAsync("open-audit-center", () => _navigationService.OpenModuleAsync("audit-center"));
    }

    [RelayCommand]
    private async Task RetryWorkflow()
    {
        if (SelectedWorkflowExecution is null)
        {
            return;
        }

        await _workflowResilienceService.RetryWorkflowAsync(SelectedWorkflowExecution.Id, "Operations Center retry");
        StatusMessage = "Đã yêu cầu chạy lại workflow.";
        await RefreshWorkflowAsync();
    }

    [RelayCommand]
    private async Task CancelWorkflow()
    {
        if (SelectedWorkflowExecution is null)
        {
            return;
        }

        await _workflowResilienceService.CancelWorkflowAsync(SelectedWorkflowExecution.Id, "Operations Center cancel");
        StatusMessage = "Đã hủy workflow.";
        await RefreshWorkflowAsync();
    }

    [RelayCommand]
    private async Task ReplayWorkflow()
    {
        if (SelectedWorkflowExecution is null)
        {
            return;
        }

        SelectedWorkflowReplaySession = await _workflowResilienceService.ReplayWorkflowAsync(SelectedWorkflowExecution.Id);
        StatusMessage = "Đã tải phiên replay workflow.";
    }

    [RelayCommand]
    private async Task RestoreCheckpoint()
    {
        if (SelectedRecoveryCheckpoint is null)
        {
            return;
        }

        await _workflowResilienceService.ResumeFromCheckpointAsync(SelectedRecoveryCheckpoint.Id);
        StatusMessage = "Đã khôi phục checkpoint.";
        await RefreshWorkflowAsync();
    }

    [RelayCommand]
    private async Task ReleaseStaleLocks()
    {
        await _workflowResilienceService.ReleaseStaleLocksAsync(DateTime.UtcNow);
        StatusMessage = "Đã giải phóng lock quá hạn.";
        await RefreshWorkflowAsync();
    }

    [RelayCommand]
    private async Task RunTt99Validation()
    {
        await _tt99ComplianceService.ValidateComplianceAsync();
        await RefreshTt99ComplianceAsync();
        StatusMessage = "Đã chạy kiểm tra tuân thủ TT99.";
    }

    [RelayCommand]
    private async Task CreateTt99Snapshot()
    {
        var periodCode = DateTime.UtcNow.ToString("yyyy-MM");
        await _tt99ComplianceService.CreateSnapshotAsync(periodCode);
        await RefreshTt99ComplianceAsync();
        StatusMessage = $"Đã tạo snapshot TT99 kỳ {periodCode}.";
    }

    [RelayCommand]
    private async Task RunTt200Validation()
    {
        await _tt200ComplianceService.ValidateComplianceAsync();
        await RefreshTt200ComplianceAsync();
        StatusMessage = "Đã chạy kiểm tra tuân thủ TT200.";
    }

    [RelayCommand]
    private async Task CreateTt200Snapshot()
    {
        var periodCode = DateTime.UtcNow.ToString("yyyy-MM");
        await _tt200ComplianceService.CreateSnapshotAsync(periodCode);
        await RefreshTt200ComplianceAsync();
        StatusMessage = $"Đã tạo snapshot TT200 kỳ {periodCode}.";
    }

    [RelayCommand]
    private async Task EscalateApprovalTimeout()
    {
        var escalated = await _approvalWorkflowService.EscalateTimeoutAsync(DateTime.UtcNow);
        StatusMessage = $"Đã nâng cấp {escalated} phê duyệt quá hạn.";
        await RefreshApprovalAsync();
    }

    private Task RefreshAllAsync()
        => ObserveUiAsync("refresh-all", async () =>
        {
            await _refreshGate.WaitAsync();
            IsBusy = true;
            _suppressSelectionRefresh = true;
            try
            {
                await _backgroundSchedulerService.ValidateStartupAsync();
                await RefreshDashboardAsync();
                await RefreshJobsAsync();
                await RefreshFailureSummaryAsync();
                await RefreshDurationMetadataAsync();
                await RefreshRetentionAsync();
                await RefreshTelemetryAsync();
                await RefreshWorkflowAsync();
                await RefreshTt99ComplianceAsync();
                await RefreshTt200ComplianceAsync();
                await RefreshApprovalAsync();
                await RefreshHealthAsync();
            }
            finally
            {
                _suppressSelectionRefresh = false;
                IsBusy = false;
                _refreshGate.Release();
            }
        });

    private async Task RefreshDashboardAsync()
    {
        Dashboard = await _backgroundSchedulerService.GetDashboardAsync();
    }

    private async Task RefreshJobsAsync()
    {
        var selectedDefinitionId = SelectedJobDefinition?.Id;
        var selectedExecutionId = SelectedJobExecution?.Id;

        JobDefinitions.Clear();
        foreach (var definition in await _backgroundSchedulerService.GetDefinitionsAsync())
        {
            JobDefinitions.Add(definition);
        }

        var previousSuppressSelection = _suppressSelectionRefresh;
        _suppressSelectionRefresh = true;
        try
        {
            SelectedJobDefinition = selectedDefinitionId is null
                ? null
                : JobDefinitions.FirstOrDefault(x => x.Id == selectedDefinitionId);

            JobExecutions.Clear();
            foreach (var execution in await _backgroundSchedulerService.GetExecutionsAsync())
            {
                JobExecutions.Add(execution);
            }

            SelectedJobExecution = selectedExecutionId is null
                ? null
                : JobExecutions.FirstOrDefault(x => x.Id == selectedExecutionId);
        }
        finally
        {
            _suppressSelectionRefresh = previousSuppressSelection;
        }
    }

    private async Task RefreshFailureSummaryAsync()
    {
        FailureSummary.Clear();
        foreach (var item in await _backgroundSchedulerService.GetFailureSummaryAsync())
        {
            FailureSummary.Add(item);
        }
    }

    private async Task RefreshDurationMetadataAsync()
    {
        DurationMetadata.Clear();
        foreach (var item in await _backgroundSchedulerService.GetDurationChartMetadataAsync())
        {
            DurationMetadata.Add(item);
        }
    }

    private async Task RefreshSelectedExecutionLogsAsync(Guid? executionId)
    {
        await _refreshGate.WaitAsync();
        try
        {
        JobLogs.Clear();
        if (executionId is null || executionId == Guid.Empty)
        {
            return;
        }

        foreach (var log in await _backgroundSchedulerService.GetLogsAsync(executionId.Value))
        {
            JobLogs.Add(log);
        }
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    private async Task RefreshHealthAsync()
    {
        var health = await _healthService.CheckAsync();
        HealthStatus = health.Status;
        HealthChecks.Clear();
        foreach (var check in health.Checks)
        {
            HealthChecks.Add(check);
        }
    }

    private async Task RefreshRetentionAsync()
    {
        RetentionPreview.Clear();
        foreach (var item in await _archiveService.PreviewRetentionAsync())
        {
            RetentionPreview.Add(item);
        }

        ArchiveHistory.Clear();
        foreach (var item in await _archiveService.GetArchiveHistoryAsync())
        {
            ArchiveHistory.Add(item);
        }

        RecoveryTimeline.Clear();
        foreach (var item in await _recoveryService.GetRecoveryTimelineAsync())
        {
            RecoveryTimeline.Add(item);
        }

        ArchiveIntegrityResults.Clear();
        foreach (var item in await _archiveService.ValidateArchiveIntegrityAsync())
        {
            ArchiveIntegrityResults.Add(item);
        }
    }

    private async Task RefreshTelemetryAsync()
    {
        TelemetrySummary = await _runtimeTelemetryService.GetRuntimeSummaryAsync();

        var timeline = await _runtimeTelemetryService.GetTelemetryTimelineAsync(take: 100);
        TelemetryTimeline.Clear();
        TelemetryDisplayItems.Clear();
        foreach (var item in timeline)
        {
            TelemetryTimeline.Add(item);
            TelemetryDisplayItems.Add(ToDisplayItem(item));
        }

        var slowOps = await _runtimeTelemetryService.GetSlowOperationsAsync(take: 100);
        SlowOperations.Clear();
        SlowOperationDisplayItems.Clear();
        foreach (var item in slowOps)
        {
            SlowOperations.Add(item);
            SlowOperationDisplayItems.Add(ToDisplayItem(item));
        }

        var errors = await _runtimeTelemetryService.GetRuntimeExceptionsAsync(take: 100);
        RuntimeExceptions.Clear();
        RuntimeErrorDisplayItems.Clear();
        foreach (var item in errors)
        {
            RuntimeExceptions.Add(item);
            RuntimeErrorDisplayItems.Add(ToDisplayItem(item));
        }

        var resources = await _runtimeTelemetryService.GetSystemResourceSnapshotsAsync(take: 100);
        SystemResources.Clear();
        PerformanceDisplayItems.Clear();
        foreach (var item in resources)
        {
            SystemResources.Add(item);
            PerformanceDisplayItems.Add(ToDisplayItem(item));
        }

        var uiResp = await _runtimeTelemetryService.GetUiResponsivenessSnapshotsAsync(take: 100);
        UiResponsiveness.Clear();
        foreach (var item in uiResp)
        {
            UiResponsiveness.Add(item);
        }

        var recs = await _failureRecoveryService.GenerateRecoverySuggestionsAsync();
        RecoveryRecommendations.Clear();
        RecommendationDisplayItems.Clear();
        foreach (var item in recs)
        {
            RecoveryRecommendations.Add(item);
            RecommendationDisplayItems.Add(ToDisplayItem(item));
        }

        var summary = TelemetrySummary;
        StatusMessage = summary is null
            ? "Telemetry: đang tải..."
            : $"Telemetry: {summary.TotalEvents} sự kiện | {summary.SlowOperationCount} tác vụ chậm | {summary.ExceptionCount} lỗi | TB {summary.AverageDurationMs:F1} ms";
    }

    private static TelemetryDisplayItem ToDisplayItem(RuntimeTelemetryTimelineDto x) => new()
    {
        TimeText = x.RecordedAtUtc.ToLocalTime().ToString("dd/MM HH:mm:ss"),
        ModuleDisplay = OperationsDisplayTextTranslator.TranslateModule(x.Module),
        EventTypeDisplay = OperationsDisplayTextTranslator.TranslateEventType(x.SourceType),
        OperationName = x.EventName,
        DurationMs = x.DurationMs,
        DurationDisplay = $"{x.DurationMs} ms",
        SeverityDisplay = OperationsDisplayTextTranslator.TranslateSeverity(x.Severity),
        MessageDisplay = OperationsDisplayTextTranslator.TranslateMessage(
            string.IsNullOrWhiteSpace(x.Message) ? x.EventName : x.Message),
        CorrelationId = x.CorrelationId ?? string.Empty,
        StatusDisplay = OperationsDisplayTextTranslator.TranslateStatus(x.Category)
    };

    private static TelemetryDisplayItem ToDisplayItem(SlowOperationLogDto x) => new()
    {
        TimeText = x.RecordedAtUtc.ToLocalTime().ToString("dd/MM HH:mm:ss"),
        ModuleDisplay = OperationsDisplayTextTranslator.TranslateModule(x.Module),
        EventTypeDisplay = "Tác vụ chậm",
        OperationName = x.OperationName,
        DurationMs = x.DurationMs,
        DurationDisplay = $"{x.DurationMs} ms",
        SeverityDisplay = OperationsDisplayTextTranslator.TranslateSeverity(x.Severity),
        MessageDisplay = OperationsDisplayTextTranslator.TranslateMessage(x.Message ?? x.OperationName),
        CorrelationId = x.CorrelationId ?? string.Empty,
        StatusDisplay = OperationsDisplayTextTranslator.TranslateStatus(x.Category)
    };

    private static TelemetryDisplayItem ToDisplayItem(RuntimeExceptionLogDto x) => new()
    {
        TimeText = x.RecordedAtUtc.ToLocalTime().ToString("dd/MM HH:mm:ss"),
        ModuleDisplay = OperationsDisplayTextTranslator.TranslateModule(x.Module),
        EventTypeDisplay = OperationsDisplayTextTranslator.TranslateEventType(x.ExceptionType),
        OperationName = x.ExceptionType,
        DurationMs = 0,
        DurationDisplay = "—",
        SeverityDisplay = OperationsDisplayTextTranslator.TranslateSeverity(x.Severity),
        MessageDisplay = x.Message ?? x.ExceptionType,
        CorrelationId = x.CorrelationId ?? x.StackHash ?? string.Empty,
        StatusDisplay = OperationsDisplayTextTranslator.TranslateStatus(x.Category)
    };

    private static TelemetryDisplayItem ToDisplayItem(SystemResourceSnapshotDto x) => new()
    {
        TimeText = x.RecordedAtUtc.ToLocalTime().ToString("dd/MM HH:mm:ss"),
        ModuleDisplay = OperationsDisplayTextTranslator.TranslateModule(x.Module),
        EventTypeDisplay = "Hiệu năng",
        OperationName = $"CPU: {x.CpuUsagePercent:F1}%",
        DurationMs = 0,
        DurationDisplay = $"WS: {x.WorkingSetBytes / 1024 / 1024}MB",
        SeverityDisplay = x.CpuUsagePercent > 80 ? "Cảnh báo" : "Thông tin",
        MessageDisplay = $"CPU {x.CpuUsagePercent:F1}% | RAM {x.MemoryUsageBytes / 1024 / 1024}MB | {x.ThreadCount} luồng",
        CorrelationId = x.CorrelationId ?? string.Empty,
        StatusDisplay = "Đã ghi nhận"
    };

    private static TelemetryDisplayItem ToDisplayItem(RecoveryRecommendationDto x) => new()
    {
        TimeText = string.Empty,
        ModuleDisplay = string.Empty,
        EventTypeDisplay = "Khuyến nghị",
        OperationName = x.RecommendationCode,
        DurationMs = 0,
        DurationDisplay = string.Empty,
        SeverityDisplay = OperationsDisplayTextTranslator.TranslateSeverity(x.Severity),
        MessageDisplay = x.SuggestedAction,
        CorrelationId = string.Empty,
        StatusDisplay = OperationsDisplayTextTranslator.TranslateStatus(x.Severity)
    };

    private async Task RefreshWorkflowAsync()
    {
        var selectedExecutionId = SelectedWorkflowExecution?.Id;
        WorkflowExecutions.Clear();
        foreach (var item in await _workflowResilienceService.GetWorkflowHistoryAsync(take: 150))
        {
            WorkflowExecutions.Add(item);
        }

        WorkflowViolations.Clear();
        foreach (var item in await _workflowResilienceService.GetConsistencyViolationsAsync(100))
        {
            WorkflowViolations.Add(item);
        }

        WorkflowLeases.Clear();
        foreach (var item in await _workflowResilienceService.GetLeasesAsync(100))
        {
            WorkflowLeases.Add(item);
        }

        DistributedLocks.Clear();
        foreach (var item in await _workflowResilienceService.GetDistributedLocksAsync(100))
        {
            DistributedLocks.Add(item);
        }

        DeadLetters.Clear();
        foreach (var item in await _workflowResilienceService.GetDeadLettersAsync(100))
        {
            DeadLetters.Add(item);
        }

        RecoveryCheckpoints.Clear();
        foreach (var item in await _workflowResilienceService.GetRecoveryCheckpointsAsync(100))
        {
            RecoveryCheckpoints.Add(item);
        }

        WorkflowMetrics.Clear();
        foreach (var item in await _workflowResilienceService.GetWorkflowExecutionMetricsAsync(100))
        {
            WorkflowMetrics.Add(item);
        }

        var previousSuppressSelection = _suppressSelectionRefresh;
        _suppressSelectionRefresh = true;
        try
        {
            SelectedWorkflowExecution = selectedExecutionId is null
                ? WorkflowExecutions.FirstOrDefault()
                : WorkflowExecutions.FirstOrDefault(x => x.Id == selectedExecutionId);

            await RefreshSelectedWorkflowStepsAsync(SelectedWorkflowExecution?.Id);
        }
        finally
        {
            _suppressSelectionRefresh = previousSuppressSelection;
        }
    }

    private async Task RefreshTt99ComplianceAsync()
    {
        TT99ValidationSummary = await _tt99ComplianceService.GetValidationSummaryAsync();

        TT99Issues.Clear();
        foreach (var issue in await _tt99ComplianceService.ValidateComplianceAsync())
        {
            TT99Issues.Add(issue);
        }

        TT99Snapshots.Clear();
        foreach (var snapshot in await _tt99ComplianceService.GetSnapshotsAsync())
        {
            TT99Snapshots.Add(snapshot);
        }

        TT99ExportManifests.Clear();
        foreach (var manifest in await _tt99ComplianceService.GetExportManifestsAsync())
        {
            TT99ExportManifests.Add(manifest);
        }
    }

    private async Task RefreshTt200ComplianceAsync()
    {
        TT200ValidationSummary = await _tt200ComplianceService.GetValidationSummaryAsync();

        TT200Issues.Clear();
        foreach (var issue in await _tt200ComplianceService.ValidateComplianceAsync())
        {
            TT200Issues.Add(issue);
        }

        TT200Snapshots.Clear();
        foreach (var snapshot in await _tt200ComplianceService.GetSnapshotsAsync())
        {
            TT200Snapshots.Add(snapshot);
        }

        TT200ExportManifests.Clear();
        foreach (var manifest in await _tt200ComplianceService.GetExportManifestsAsync())
        {
            TT200ExportManifests.Add(manifest);
        }
    }

    private async Task RefreshSelectedWorkflowStepsAsync(Guid? workflowExecutionId)
    {
        if (!_suppressSelectionRefresh)
        {
            await _refreshGate.WaitAsync();
        }
        try
        {
        WorkflowSteps.Clear();
        if (workflowExecutionId is null || workflowExecutionId == Guid.Empty)
        {
            return;
        }

        foreach (var step in await _workflowResilienceService.GetWorkflowStepsAsync(workflowExecutionId.Value))
        {
            WorkflowSteps.Add(step);
        }
        }
        finally
        {
            if (!_suppressSelectionRefresh)
            {
                _refreshGate.Release();
            }
        }
    }

    private async Task RefreshApprovalAsync()
    {
        var selectedId = SelectedApprovalWorkflow?.Id;
        ApprovalWorkflows.Clear();
        foreach (var workflow in await _approvalWorkflowService.GetWorkflowsAsync(150))
        {
            ApprovalWorkflows.Add(workflow);
        }

        var previousSuppressSelection = _suppressSelectionRefresh;
        _suppressSelectionRefresh = true;
        try
        {
            SelectedApprovalWorkflow = selectedId is null
                ? ApprovalWorkflows.FirstOrDefault()
                : ApprovalWorkflows.FirstOrDefault(x => x.Id == selectedId);
            await RefreshSelectedApprovalWorkflowAsync(SelectedApprovalWorkflow?.Id);
        }
        finally
        {
            _suppressSelectionRefresh = previousSuppressSelection;
        }
    }

    private async Task RefreshSelectedApprovalWorkflowAsync(Guid? workflowId)
    {
        if (!_suppressSelectionRefresh)
        {
            await _refreshGate.WaitAsync();
        }
        try
        {
        ApprovalSteps.Clear();
        ApprovalHistory.Clear();
        ApprovalSnapshots.Clear();
        if (workflowId is null || workflowId == Guid.Empty)
        {
            return;
        }

        foreach (var step in await _approvalWorkflowService.GetStepsAsync(workflowId.Value))
        {
            ApprovalSteps.Add(step);
        }

        foreach (var history in await _approvalWorkflowService.ReplayTrailAsync(workflowId.Value))
        {
            ApprovalHistory.Add(history);
        }

        foreach (var snapshot in await _approvalWorkflowService.GetSnapshotsAsync(workflowId.Value))
        {
            ApprovalSnapshots.Add(snapshot);
        }
        }
        finally
        {
            if (!_suppressSelectionRefresh)
            {
                _refreshGate.Release();
            }
        }
    }

    private async Task ObserveUiAsync(string operation, Func<Task> action, object? metadata = null)
    {
        var sw = Stopwatch.StartNew();
        Exception? failure = null;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            sw.Stop();
            await _runtimeTelemetryService.TrackUiLatencyAsync("operations-center", operation, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds >= 1000, failure?.Message, null, null, metadata);
        }
    }
}

public sealed class TelemetryDisplayItem
{
    public string TimeText { get; set; } = string.Empty;
    public string ModuleDisplay { get; set; } = string.Empty;
    public string EventTypeDisplay { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public string DurationDisplay { get; set; } = string.Empty;
    public string SeverityDisplay { get; set; } = string.Empty;
    public string MessageDisplay { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string StatusDisplay { get; set; } = string.Empty;
}
