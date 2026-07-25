using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class WakeUpCallDto
{
    public Guid WakeUpCallId { get; set; }
    public string CallNumber { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public string? GuestName { get; set; }
    public DateTime WakeTime { get; set; }
    public string Status { get; set; } = "Scheduled";
    public string? Result { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsDaily { get; set; }
    public int RetryCount { get; set; }
    public string? Notes { get; set; }
    public bool IsDueSoon => Status is "Scheduled" or "Pending" && DateTime.Now >= WakeTime.AddMinutes(-5) && DateTime.Now <= WakeTime;
    public bool IsOverdue => Status is "Scheduled" or "Pending" && DateTime.Now > WakeTime.AddMinutes(10);
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class WakeUpCallViewModel : ViewModelBase
{
    public ObservableCollection<WakeUpCallDto> Calls { get; } = new();
    [ObservableProperty] private WakeUpCallDto? _selectedCall;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private int _totalScheduled;
    [ObservableProperty] private int _totalDueSoon;
    [ObservableProperty] private int _totalOverdue;
    [ObservableProperty] private int _totalCompleted;
    [ObservableProperty] private DateTime _newWakeTime = DateTime.Today.AddHours(6).AddMinutes(30);
    [ObservableProperty] private string _newRoomNumber = "";
    [ObservableProperty] private string _newGuestName = "";
    [ObservableProperty] private bool _newRecurring;
    [ObservableProperty] private bool _newDaily;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedCall is not null;

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Calls.Clear();
            var c1 = new WakeUpCallDto { WakeUpCallId = Guid.NewGuid(), CallNumber = "WKP-202607-000001", RoomNumber = "101", GuestName = "Nguyen Van A", WakeTime = DateTime.Today.AddHours(6), Status = "Completed", Result = "Answered", Notes = "Gio bay 8h sang" }; c1.History.Add("Scheduled"); c1.History.Add("Calling"); c1.History.Add("Result: Answered");
            var c2 = new WakeUpCallDto { WakeUpCallId = Guid.NewGuid(), CallNumber = "WKP-202607-000002", RoomNumber = "103", GuestName = "Le Van C", WakeTime = DateTime.Now.AddMinutes(15), Status = "Scheduled", IsRecurring = true, Notes = "VIP - goi moi ngay" }; c2.History.Add("Scheduled");
            var c3 = new WakeUpCallDto { WakeUpCallId = Guid.NewGuid(), CallNumber = "WKP-202607-000003", RoomNumber = "201", GuestName = "Pham Thi D", WakeTime = DateTime.Now.AddMinutes(45), Status = "Pending" }; c3.History.Add("Scheduled"); c3.History.Add("Activated");
            Calls.Add(c1); Calls.Add(c2); Calls.Add(c3);
            TotalScheduled = Calls.Count(c => c.Status is "Scheduled" or "Pending"); TotalCompleted = Calls.Count(c => c.Status == "Completed");
            TotalDueSoon = Calls.Count(c => c.IsDueSoon); TotalOverdue = Calls.Count(c => c.IsOverdue);
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectCall(WakeUpCallDto? c) => SelectedCall = c;

    [RelayCommand]
    private void CreateCall()
    {
        if (string.IsNullOrWhiteSpace(NewRoomNumber)) { ErrorMessage = "Vui long nhap so phong."; return; }
        var nc = new WakeUpCallDto { WakeUpCallId = Guid.NewGuid(), CallNumber = $"WKP-202607-{Calls.Count + 1:D6}", RoomNumber = NewRoomNumber, GuestName = NewGuestName, WakeTime = NewWakeTime, Status = "Scheduled", IsRecurring = NewRecurring, IsDaily = NewDaily };
        nc.History.Add("Scheduled"); Calls.Insert(0, nc); TotalScheduled++;
        InfoMessage = $"Da tao danh thuc {nc.CallNumber} cho phong {nc.RoomNumber} luc {nc.WakeTime:HH:mm}.";
        NewRoomNumber = ""; NewGuestName = "";
    }

    [RelayCommand] private void StartCall() { if (SelectedCall is null) return; SelectedCall.Status = "Calling"; SelectedCall.History.Add("Calling"); InfoMessage = $"Dang goi phong {SelectedCall.RoomNumber}..."; }
    [RelayCommand] private void MarkAnswered() { if (SelectedCall is null) return; SelectedCall.Status = "Completed"; SelectedCall.Result = "Answered"; SelectedCall.History.Add("Result: Answered"); TotalCompleted++; InfoMessage = $"Da danh thuc thanh cong phong {SelectedCall.RoomNumber}."; }
    [RelayCommand] private void MarkNoAnswer() { if (SelectedCall is null) return; SelectedCall.RetryCount++; SelectedCall.Status = "Pending"; SelectedCall.Result = "NoAnswer"; SelectedCall.History.Add($"Retry #{SelectedCall.RetryCount}: No Answer"); InfoMessage = $"Khong tra loi. Thu lai lan {SelectedCall.RetryCount}."; }

    partial void OnSelectedCallChanged(WakeUpCallDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
