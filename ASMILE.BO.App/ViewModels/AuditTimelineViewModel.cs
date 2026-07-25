using System.Collections.ObjectModel;
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
    public ObservableCollection<AuditEntryDto> Entries { get; } = new();
    [ObservableProperty] private AuditEntryDto? _selected;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private int _totalToday;
    [ObservableProperty] private int _totalThisWeek;
    [ObservableProperty] private string? _filterModule;
    [ObservableProperty] private string? _filterAction;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => Selected is not null;
    public string[] Modules { get; } = ["Tat ca", "Reservation", "Stay", "Folio", "Payment", "Housekeeping", "ServiceRequest", "Concierge", "LostFound", "MessageCenter", "GuestFeedback"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Entries.Clear();
            var now = DateTime.UtcNow;
            Entries.Add(new AuditEntryDto { AuditNumber = "ADT-000001", Module = "Check-in", EntityType = "HotelStay", Action = "CheckIn", PerformedBy = "Mai Lan", Description = "Check-in guest Nguyen Van A to Phong 101", OccurredAt = now.AddHours(-3), Severity = "Success" });
            Entries.Add(new AuditEntryDto { AuditNumber = "ADT-000002", Module = "Folio", EntityType = "HotelFolio", Action = "Create", PerformedBy = "System", Description = "Created folio for stay STY-202607-000001", OccurredAt = now.AddHours(-3), Severity = "Info" });
            Entries.Add(new AuditEntryDto { AuditNumber = "ADT-000003", Module = "Housekeeping", EntityType = "Task", Action = "Complete", PerformedBy = "Mai Lan", Description = "Completed cleaning Phong 102", OccurredAt = now.AddHours(-2), Severity = "Success" });
            Entries.Add(new AuditEntryDto { AuditNumber = "ADT-000004", Module = "Payment", EntityType = "HotelPayment", Action = "Receive", PerformedBy = "Tran Van B", Description = "Received payment 5,000,000 VND from Le Van C", OccurredAt = now.AddHours(-1), Severity = "Success" });
            Entries.Add(new AuditEntryDto { AuditNumber = "ADT-000005", Module = "Reservation", EntityType = "DatPhong", Action = "Cancel", PerformedBy = "Mai Lan", Description = "Cancelled reservation RSV-202607-000009 due to guest request", OccurredAt = now.AddMinutes(-30), Severity = "Warning" });
            TotalToday = Entries.Count(e => e.OccurredAt.Date == now.Date); TotalThisWeek = Entries.Count(e => e.OccurredAt >= now.AddDays(-7));
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectEntry(AuditEntryDto? e) => Selected = e;

    partial void OnSelectedChanged(AuditEntryDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
