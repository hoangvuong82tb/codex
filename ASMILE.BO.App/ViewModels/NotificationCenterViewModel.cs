using System.Collections.ObjectModel;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class AlertNotificationDto
{
    public Guid NotificationId { get; set; }
    public string NotificationNumber { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string? SourceModule { get; set; }
    public string? RecipientDepartment { get; set; }
    public bool RequiresAck { get; set; }
    public DateTime CreatedAt { get; set; }
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class NotificationCenterViewModel : ViewModelBase
{
    private readonly INotificationTaskService? _service;

    public ObservableCollection<AlertNotificationDto> Notifications { get; } = new();
    [ObservableProperty] private AlertNotificationDto? _selected;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalUnread;
    [ObservableProperty] private int _totalCritical;
    [ObservableProperty] private string? _filterSeverity;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => Selected is not null;
    public string[] Severities { get; } = ["Tat ca", "Info", "Success", "Warning", "Critical"];

    public NotificationCenterViewModel()
    {
    }

    public NotificationCenterViewModel(INotificationTaskService service)
    {
        _service = service;
    }

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            ErrorMessage = string.Empty;
            Notifications.Clear();
            Selected = null;

            if (_service is null)
            {
                TotalUnread = 0;
                TotalCritical = 0;
                InfoMessage = "Trung tâm thông báo chưa được kết nối dịch vụ dữ liệu; không hiển thị dữ liệu mẫu.";
                return;
            }

            var source = await _service.GetNotificationsAsync(take: 500, cancellationToken: ct);
            TotalUnread = await _service.GetUnreadCountAsync(ct);
            TotalCritical = source.Count(x => string.Equals(x.Severity, "Critical", StringComparison.OrdinalIgnoreCase));

            var alerts = source.Select(ToAlert).ToList();
            if (!string.IsNullOrWhiteSpace(FilterSeverity) && !string.Equals(FilterSeverity, "Tat ca", StringComparison.OrdinalIgnoreCase))
            {
                alerts = alerts
                    .Where(x => string.Equals(x.Severity, FilterSeverity, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            foreach (var alert in alerts)
            {
                Notifications.Add(alert);
            }

            InfoMessage = source.Count == 0
                ? "Chưa có thông báo."
                : string.Empty;
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void Select(AlertNotificationDto? n) => Selected = n;
    [RelayCommand] private async Task MarkRead()
    {
        if (Selected is null || _service is null)
        {
            SetUnavailable();
            return;
        }

        await ExecuteStateChangeAsync(
            () => _service.MarkReadAsync(Selected.NotificationId),
            "Đã đánh dấu đã đọc.");
    }

    [RelayCommand] private async Task Acknowledge()
    {
        if (Selected is null || _service is null)
        {
            SetUnavailable();
            return;
        }

        await ExecuteStateChangeAsync(
            () => _service.AcknowledgeNotificationAsync(Selected.NotificationId),
            "Đã xác nhận thông báo.");
    }

    [RelayCommand] private async Task Dismiss()
    {
        if (Selected is null || _service is null)
        {
            SetUnavailable();
            return;
        }

        await ExecuteStateChangeAsync(
            () => _service.DismissNotificationAsync(Selected.NotificationId),
            "Đã ẩn thông báo.");
    }

    private async Task ExecuteStateChangeAsync(Func<Task> action, string message)
    {
        IsLoading = true;
        try
        {
            ErrorMessage = string.Empty;
            await action();
            await LoadAsync(CancellationToken.None);
            InfoMessage = message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Loi: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SetUnavailable() => ErrorMessage = "Trung tâm thông báo chưa có command/service lưu CSDL; thao tác đã bị chặn.";

    private static AlertNotificationDto ToAlert(ASMILE.BO.Application.DTOs.NotificationDto notification)
    {
        var status = notification.IsDismissed
            ? "Đã ẩn"
            : notification.IsAcknowledged
                ? "Đã xác nhận"
                : notification.IsRead
                    ? "Đã đọc"
                    : "Mới";

        var alert = new AlertNotificationDto
        {
            NotificationId = notification.Id,
            NotificationNumber = notification.Id.ToString("N"),
            Type = notification.NotificationType,
            Severity = notification.Severity,
            Title = notification.Title,
            Message = notification.Message,
            Status = status,
            SourceModule = notification.SourceType,
            RequiresAck = string.Equals(notification.Severity, "Critical", StringComparison.OrdinalIgnoreCase),
            CreatedAt = notification.CreatedAt
        };

        alert.History.Add($"Tạo lúc {notification.CreatedAt:dd/MM/yyyy HH:mm}");
        alert.History.Add(notification.IsRead ? "Đã đọc" : "Chưa đọc");
        if (notification.IsAcknowledged) alert.History.Add("Đã xác nhận");
        if (notification.IsDismissed) alert.History.Add("Đã ẩn");
        return alert;
    }

    partial void OnSelectedChanged(AlertNotificationDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
}
