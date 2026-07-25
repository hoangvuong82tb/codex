using System.Collections.ObjectModel;
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

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Notifications.Clear();
            var n1 = new AlertNotificationDto { NotificationNumber = "NTF-000001", Type = "VIP", Severity = "Success", Title = "VIP Arrival", Message = "Khach VIP Le Van C se den luc 14:00. Chuan bi phong 301 va don trai cay.", SourceModule = "VIP", CreatedAt = DateTime.UtcNow.AddHours(-1) }; n1.History.Add("Created");
            var n2 = new AlertNotificationDto { NotificationNumber = "NTF-000002", Type = "Housekeeping", Severity = "Critical", Title = "Phong 105 OOO", Message = "Phong 105 da duoc danh dau Out Of Order. Can bao tri ngay.", SourceModule = "Housekeeping", RecipientDepartment = "Engineering", RequiresAck = true }; n2.History.Add("Created"); n2.History.Add("Read");
            var n3 = new AlertNotificationDto { NotificationNumber = "NTF-000003", Type = "Payment", Severity = "Warning", Title = "Outstanding Balance", Message = "Khach Pham Thi D (P.102) con no 2,500,000 VND. Sap den ngay tra phong.", SourceModule = "Folio" }; n3.History.Add("Created");
            Notifications.Add(n1); Notifications.Add(n2); Notifications.Add(n3);
            TotalUnread = Notifications.Count(n => n.Status is "New" or "Unread"); TotalCritical = Notifications.Count(n => n.Severity == "Critical");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void Select(AlertNotificationDto? n) => Selected = n;
    [RelayCommand] private void MarkRead() { if (Selected is null) return; Selected.Status = "Read"; Selected.History.Add("Read"); TotalUnread = Notifications.Count(no => no.Status is "New" or "Unread"); }
    [RelayCommand] private void Acknowledge() { if (Selected is null) return; Selected.Status = "Acknowledged"; Selected.History.Add("Acknowledged"); InfoMessage = $"Da xac nhan {Selected.NotificationNumber}."; }
    [RelayCommand] private void Dismiss() { if (Selected is null) return; Selected.Status = "Dismissed"; Selected.History.Add("Dismissed"); }

    partial void OnSelectedChanged(AlertNotificationDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
