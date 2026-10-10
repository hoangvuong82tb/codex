using System.Collections.ObjectModel;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Threading;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class AuditCenterViewModel : ViewModelBase, IDisposable
{
    private readonly IAuditService _auditService;
    private readonly IAuditReplayService _auditReplayService;
    private readonly INavigationService _navigationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly DispatcherTimer _filterTimer;
    private CancellationTokenSource? _loadCancellation;
    private bool _disposed;

    public ObservableCollection<AuditTimelineItemDto> AuditItems { get; } = new();
    public ObservableCollection<AuditCorrelationDto> CorrelationGroups { get; } = new();
    public ObservableCollection<AuditReplayEntryDto> ReplayEntries { get; } = new();
    public ObservableCollection<AuditEntitySnapshotDto> Snapshots { get; } = new();
    public ObservableCollection<AuditExportHistoryDto> ExportHistory { get; } = new();

    [ObservableProperty] private AuditTimelineItemDto? selectedAuditItem;
    [ObservableProperty] private AuditCorrelationDto? selectedCorrelationGroup;
    [ObservableProperty] private AuditReplayEntryDto? selectedReplayEntry;
    [ObservableProperty] private string moduleFilter = string.Empty;
    [ObservableProperty] private string actionFilter = string.Empty;
    [ObservableProperty] private string userFilter = string.Empty;
    [ObservableProperty] private string entityFilter = string.Empty;
    [ObservableProperty] private string correlationFilter = string.Empty;
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddDays(-7);
    [ObservableProperty] private DateTime? toDate = DateTime.Today.AddDays(1);
    [ObservableProperty] private string selectedMetadataJson = string.Empty;
    [ObservableProperty] private string selectedMetadataDisplayJson = string.Empty;
    [ObservableProperty] private string selectedSnapshotJson = string.Empty;
    [ObservableProperty] private string selectedSnapshotDisplayJson = string.Empty;
    [ObservableProperty] private string selectedReplayJson = string.Empty;
    [ObservableProperty] private string selectedSummaryJson = string.Empty;
    [ObservableProperty] private string statusMessage = "Sẵn sàng";

    public bool CanExport => _currentUserService.HasPermission("Logs.Export");

    public AuditCenterViewModel(
        IAuditService auditService,
        IAuditReplayService auditReplayService,
        INavigationService navigationService,
        ICurrentUserService currentUserService)
    {
        _auditService = auditService;
        _auditReplayService = auditReplayService;
        _navigationService = navigationService;
        _currentUserService = currentUserService;
        _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _filterTimer.Tick += (_, _) =>
        {
            _filterTimer.Stop();
            QueueLoad();
        };
        QueueLoad();
    }

    partial void OnModuleFilterChanged(string value) => Debounce();
    partial void OnActionFilterChanged(string value) => Debounce();
    partial void OnUserFilterChanged(string value) => Debounce();
    partial void OnEntityFilterChanged(string value) => Debounce();
    partial void OnCorrelationFilterChanged(string value) => Debounce();
    partial void OnFromDateChanged(DateTime? value) => Debounce();
    partial void OnToDateChanged(DateTime? value) => Debounce();

    partial void OnSelectedAuditItemChanged(AuditTimelineItemDto? value)
    {
        _ = LoadSelectionAsync();
    }

    partial void OnSelectedCorrelationGroupChanged(AuditCorrelationDto? value)
    {
        if (value is null)
        {
            return;
        }

        CorrelationFilter = value.CorrelationId;
    }

    partial void OnSelectedReplayEntryChanged(AuditReplayEntryDto? value)
    {
        SelectedReplayJson = value is null ? string.Empty : AuditJsonDisplayFormatter.FormatForDisplay(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }

    [RelayCommand]
    private Task Refresh()
        => LoadAsync();

    [RelayCommand]
    private async Task OpenSource()
    {
        if (SelectedAuditItem is null || !Guid.TryParse(SelectedAuditItem.EntityId, out var sourceId))
        {
            StatusMessage = "Không tìm thấy mã nguồn hợp lệ để mở chi tiết.";
            return;
        }

        if (SelectedAuditItem.EntityName.Contains("Journal", StringComparison.OrdinalIgnoreCase))
        {
            await _navigationService.OpenJournalAsync(sourceId);
            return;
        }

        await _navigationService.OpenVoucherAsync(SelectedAuditItem.EntityName, sourceId);
    }

    [RelayCommand]
    private async Task ExportJson()
        => await ExportAsync("json");

    [RelayCommand]
    private async Task ExportCsv()
        => await ExportAsync("csv");

    [RelayCommand]
    private async Task ExportZip()
        => await ExportAsync("zip");

    [RelayCommand]
    private async Task LoadReplay()
        => await LoadSelectionAsync();

    private void Debounce()
    {
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    private void QueueLoad()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        _ = LoadAsync(_loadCancellation.Token);
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var logs = await _auditService.QueryAsync(
                string.IsNullOrWhiteSpace(EntityFilter) ? null : EntityFilter,
                null,
                string.IsNullOrWhiteSpace(ActionFilter) ? null : ActionFilter,
                FromDate,
                ToDate,
                cancellationToken);

            var filtered = logs
                .Where(MatchesClientFilters)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Take(1000)
                .Select(ToDto)
                .ToArray();

            SetCollection(AuditItems, filtered);

            var correlations = await _auditReplayService.GetCorrelationsAsync(
                string.IsNullOrWhiteSpace(EntityFilter) ? null : EntityFilter,
                null,
                200,
                cancellationToken);
            SetCollection(CorrelationGroups, correlations);

            var exportHistory = await _auditReplayService.GetExportHistoryAsync(100, cancellationToken);
            SetCollection(ExportHistory, exportHistory);

            StatusMessage = $"Đã tải {AuditItems.Count} sự kiện kiểm tra.";
            await LoadSelectionAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadSelectionAsync(CancellationToken cancellationToken = default)
    {
        var selected = SelectedAuditItem;
        if (selected is null)
        {
            SelectedMetadataJson = string.Empty;
            SelectedMetadataDisplayJson = string.Empty;
            SelectedSnapshotJson = string.Empty;
            SelectedSnapshotDisplayJson = string.Empty;
            SelectedReplayJson = string.Empty;
            SelectedSummaryJson = string.Empty;
            SetCollection(Snapshots, Array.Empty<AuditEntitySnapshotDto>());
            SetCollection(ReplayEntries, Array.Empty<AuditReplayEntryDto>());
            return;
        }

        var snapshots = await _auditReplayService.GetSnapshotsAsync(selected.Id, cancellationToken);
        SetCollection(Snapshots, snapshots);

        var correlationId = selected.CorrelationId;
        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            var replay = await _auditReplayService.GetReplayEntriesAsync(correlationId, take: 1000, cancellationToken: cancellationToken);
            SetCollection(ReplayEntries, replay.OrderBy(x => x.SequenceNo).ThenBy(x => x.TimestampUtc).ToArray());
            var summary = await _auditReplayService.GetReplaySummaryAsync(correlationId, cancellationToken);
            SelectedSummaryJson = AuditJsonDisplayFormatter.SerializePretty(summary);
        }

        // Metadata: build from atomic properties only (avoid double-serializing JSON strings)
        SelectedMetadataJson = JsonSerializer.Serialize(new
        {
            selected.CorrelationId,
            selected.UserName,
            selected.Action,
            selected.EntityName,
            selected.EntityId,
            selected.Detail,
            selected.Module,
            selected.Route
        }, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        SelectedMetadataDisplayJson = AuditJsonDisplayFormatter.FormatForDisplay(SelectedMetadataJson);

        // Snapshots: format with UnsafeRelaxedJsonEscaping for readable display
        SelectedSnapshotJson = snapshots.Count == 0
            ? string.Empty
            : JsonSerializer.Serialize(snapshots, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

        SelectedSnapshotDisplayJson = snapshots.Count == 0
            ? string.Empty
            : BuildSnapshotComparison(snapshots);
    }

    private async Task ExportAsync(string exportType)
    {
        if (!CanExport)
        {
            StatusMessage = "Bạn không có quyền xuất nhật ký kiểm toán (Logs.Export).";
            return;
        }

        var normalizedType = string.IsNullOrWhiteSpace(exportType) ? "json" : exportType.Trim().ToLowerInvariant();
        var extension = normalizedType switch
        {
            "csv" => "csv",
            "zip" => "zip",
            "ndjson" => "ndjson",
            _ => "json"
        };
        var saveDialog = new SaveFileDialog
        {
            Title = "Chọn nơi lưu gói nhật ký kiểm toán",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            FileName = $"asmile-audit-{DateTime.Now:yyyyMMdd-HHmmss}.{extension}",
            DefaultExt = $".{extension}",
            AddExtension = true,
            OverwritePrompt = true,
            Filter = extension switch
            {
                "csv" => "CSV (*.csv)|*.csv",
                "zip" => "ZIP (*.zip)|*.zip",
                "ndjson" => "NDJSON (*.ndjson)|*.ndjson",
                _ => "JSON (*.json)|*.json"
            }
        };
        if (saveDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var result = await _auditReplayService.ExportAsync(new AuditExportRequestDto
            {
                ExportType = normalizedType,
                CorrelationId = string.IsNullOrWhiteSpace(CorrelationFilter) ? null : CorrelationFilter,
                EntityType = string.IsNullOrWhiteSpace(EntityFilter) ? null : EntityFilter,
                ModuleFilter = string.IsNullOrWhiteSpace(ModuleFilter) ? null : ModuleFilter,
                ActionFilter = string.IsNullOrWhiteSpace(ActionFilter) ? null : ActionFilter,
                UserFilter = string.IsNullOrWhiteSpace(UserFilter) ? null : UserFilter,
                FromDate = FromDate,
                ToDate = ToDate,
                FilePath = saveDialog.FileName
            });

            StatusMessage = $"Đã xuất {result.RecordCount} dòng ra {result.FilePath}";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Audit Center export failed for format {ExportType}", normalizedType);
            StatusMessage = "Xuất nhật ký thất bại. Kiểm tra quyền ghi thư mục và nhật ký ứng dụng.";
        }
    }

    private bool MatchesClientFilters(AuditLog log)
    {
        return (string.IsNullOrWhiteSpace(UserFilter) || log.Username.Contains(UserFilter, StringComparison.OrdinalIgnoreCase))
               && (string.IsNullOrWhiteSpace(ModuleFilter) || log.EntityName.Contains(ModuleFilter, StringComparison.OrdinalIgnoreCase) || log.Detail.Contains(ModuleFilter, StringComparison.OrdinalIgnoreCase))
               && (string.IsNullOrWhiteSpace(CorrelationFilter) || string.Equals(log.CorrelationId, CorrelationFilter, StringComparison.OrdinalIgnoreCase));
    }

    private static AuditTimelineItemDto ToDto(AuditLog log)
    {
        return new AuditTimelineItemDto
        {
            Id = log.Id,
            CreatedAt = log.CreatedAt,
            CorrelationId = log.CorrelationId ?? string.Empty,
            UserName = log.Username,
            Action = log.Action,
            EntityName = log.EntityName,
            EntityId = log.EntityId,
            Detail = log.Detail,
            Severity = ResolveSeverity(log.Action),
            Module = ResolveModule(log.EntityName, log.Action),
            Route = ResolveRoute(log.EntityName, log.EntityId),
            MetadataJson = AuditJsonDisplayFormatter.FormatForDisplay(BuildMetadataJson(log)),
            BeforeJson = FormatJsonField(log.OldValuesJson),
            AfterJson = FormatJsonField(log.NewValuesJson),
            DiffJson = string.Empty
        };
    }

    private static string BuildMetadataJson(AuditLog log)
    {
        try
        {
            return JsonSerializer.Serialize(new
            {
                log.IpAddress,
                log.CorrelationId,
                log.EntityName,
                log.EntityId,
                OldValues = TryParseJsonElement(log.OldValuesJson),
                NewValues = TryParseJsonElement(log.NewValuesJson)
            }, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }
        catch
        {
            return "{}";
        }
    }

    private static string FormatJsonField(string? rawJson)
    {
        return AuditJsonDisplayFormatter.FormatForDisplay(rawJson);
    }

    private static object? TryParseJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement;
        }
        catch
        {
            return json;
        }
    }

    private static string BuildSnapshotComparison(IReadOnlyList<AuditEntitySnapshotDto> snapshots)
    {
        if (snapshots.Count == 0) return string.Empty;
        try
        {
            var first = snapshots[0];
            return JsonSerializer.Serialize(new
            {
                first.EntityType,
                first.EntityId,
                first.SnapshotHash,
                Before = AuditJsonDisplayFormatter.FormatForDisplay(first.BeforeJson),
                After = AuditJsonDisplayFormatter.FormatForDisplay(first.AfterJson),
                Diff = AuditJsonDisplayFormatter.FormatForDisplay(first.DiffJson)
            }, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ResolveSeverity(string action)
    {
        if (action.Contains("error", StringComparison.OrdinalIgnoreCase) || action.Contains("fail", StringComparison.OrdinalIgnoreCase))
        {
            return "Error";
        }

        if (action.Contains("delete", StringComparison.OrdinalIgnoreCase)
            || action.Contains("reverse", StringComparison.OrdinalIgnoreCase)
            || action.Contains("lock", StringComparison.OrdinalIgnoreCase))
        {
            return "Warning";
        }

        return "Info";
    }

    private static string ResolveModule(string entityName, string action)
    {
        var text = $"{entityName} {action}".ToLowerInvariant();
        if (text.Contains("journal")) return "general-ledger";
        if (text.Contains("report")) return "report-center";
        if (text.Contains("import")) return "import";
        if (text.Contains("background")) return "operations-center";
        if (text.Contains("task") || text.Contains("notification")) return "notification-center";
        if (text.Contains("vat") || text.Contains("tax")) return "tax-financial";
        if (text.Contains("closing")) return "fiscal-periods";
        if (text.Contains("navigation")) return "navigation";
        return entityName;
    }

    private static string ResolveRoute(string entityName, string entityId)
        => entityName.Contains("Journal", StringComparison.OrdinalIgnoreCase) ? "general-ledger" : entityId;

    private static void SetCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        target.Clear();
        foreach (var item in items)
        {
            target.Add(item);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _filterTimer.Stop();
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
    }
}
