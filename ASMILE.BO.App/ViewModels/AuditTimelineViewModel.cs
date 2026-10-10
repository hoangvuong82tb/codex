using System.Collections.ObjectModel;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class AuditEntryDto
{
    public string AuditNumber { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string? Description { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Severity { get; set; } = "Info";
}

public sealed partial class AuditTimelineViewModel : ViewModelBase
{
    private readonly IAuditService _auditService;

    public ObservableCollection<AuditEntryDto> Entries { get; } = new();
    [ObservableProperty] private AuditEntryDto? _selected;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _totalToday;
    [ObservableProperty] private int _totalThisWeek;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string? _filterModule;
    [ObservableProperty] private string? _filterAction;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasSelection => Selected is not null;
    public string[] Modules { get; } = ["Tat ca", "Reservation", "Stay", "Folio", "Payment", "Housekeeping", "ServiceRequest", "Concierge", "LostFound", "MessageCenter", "GuestFeedback"];

    public AuditTimelineViewModel(IAuditService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var now = DateTime.Now;
            var from = now.Date.AddDays(-6);
            var to = now.Date.AddDays(1);
            var logs = await _auditService.QueryAsync(fromDate: from, toDate: to, cancellationToken: ct);
            var filtered = logs
                .Where(MatchesFilter)
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Select(ToDto)
                .ToArray();

            Entries.Clear();
            foreach (var item in filtered)
            {
                Entries.Add(item);
            }

            Selected = null;
            TotalToday = logs.Count(x => x.CreatedAt >= now.Date);
            TotalThisWeek = logs.Count(x => x.CreatedAt >= now.Date.AddDays(-6));
            InfoMessage = filtered.Length == 0
                ? "Chưa có sự kiện audit thật trong khoảng thời gian đã chọn."
                : $"Đã tải {filtered.Length:N0} sự kiện audit thật từ cơ sở dữ liệu.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Entries.Clear();
            Selected = null;
            TotalToday = 0;
            TotalThisWeek = 0;
            ErrorMessage = $"Không thể tải nhật ký audit: {ex.Message}";
            InfoMessage = "Không hiển thị dữ liệu mẫu khi nguồn audit thật gặp lỗi.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectEntry(AuditEntryDto? e) => Selected = e;

    partial void OnSelectedChanged(AuditEntryDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));

    private bool MatchesFilter(AuditLog log)
    {
        var module = FilterModule;
        var action = FilterAction;
        return (string.IsNullOrWhiteSpace(module)
                || module.Equals("Tat ca", StringComparison.OrdinalIgnoreCase)
                || log.EntityName.Contains(module, StringComparison.OrdinalIgnoreCase)
                || log.Detail.Contains(module, StringComparison.OrdinalIgnoreCase))
            && (string.IsNullOrWhiteSpace(action)
                || log.Action.Contains(action, StringComparison.OrdinalIgnoreCase));
    }

    private static AuditEntryDto ToDto(AuditLog log)
    {
        var module = ResolveModule(log.EntityName, log.Action);
        return new AuditEntryDto
        {
            AuditNumber = $"AUD-{log.Id:D8}",
            Module = module,
            EntityType = log.EntityName,
            Action = log.Action,
            PerformedBy = string.IsNullOrWhiteSpace(log.Username) ? "system" : log.Username,
            Description = string.IsNullOrWhiteSpace(log.Detail) ? $"{log.Action} {log.EntityName}" : log.Detail,
            OccurredAt = log.CreatedAt,
            Severity = ResolveSeverity(log.Action)
        };
    }

    private static string ResolveModule(string entityName, string action)
    {
        var text = $"{entityName} {action}".ToLowerInvariant();
        if (text.Contains("reservation") || text.Contains("booking")) return "Reservation";
        if (text.Contains("stay") || text.Contains("checkin") || text.Contains("checkout")) return "Stay";
        if (text.Contains("folio") || text.Contains("payment") || text.Contains("invoice")) return "Folio / Payment";
        if (text.Contains("housekeeping") || text.Contains("room")) return "Housekeeping";
        if (text.Contains("message") || text.Contains("notification")) return "MessageCenter";
        if (text.Contains("concierge")) return "Concierge";
        if (text.Contains("lost") || text.Contains("found")) return "LostFound";
        return string.IsNullOrWhiteSpace(entityName) ? "System" : entityName;
    }

    private static string ResolveSeverity(string action)
    {
        if (action.Contains("error", StringComparison.OrdinalIgnoreCase)
            || action.Contains("fail", StringComparison.OrdinalIgnoreCase)) return "Error";
        if (action.Contains("delete", StringComparison.OrdinalIgnoreCase)
            || action.Contains("cancel", StringComparison.OrdinalIgnoreCase)
            || action.Contains("reverse", StringComparison.OrdinalIgnoreCase)) return "Warning";
        return "Info";
    }
}
