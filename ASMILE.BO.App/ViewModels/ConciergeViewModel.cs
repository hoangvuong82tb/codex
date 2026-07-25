using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class ConciergeDto
{
    public Guid ConciergeRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public string? RoomNumber { get; set; }
    public string RequestType { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Open";
    public string? AssignedDepartment { get; set; }
    public DateTime? ExpectedCompletion { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? ChargeType { get; set; }
    public string? Notes { get; set; }
    public DateTime RequestedAt { get; set; }
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class ConciergeViewModel : ViewModelBase
{
    public ObservableCollection<ConciergeDto> Requests { get; } = new();
    [ObservableProperty] private ConciergeDto? _selectedRequest;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalOpen;
    [ObservableProperty] private int _totalInProgress;
    [ObservableProperty] private int _totalCompleted;
    [ObservableProperty] private string _newRequestType = "";
    [ObservableProperty] private string _newGuestName = "";
    [ObservableProperty] private string _newRoomNumber = "";
    [ObservableProperty] private string _newPriority = "Normal";
    [ObservableProperty] private decimal? _newCost;
    [ObservableProperty] private string _newChargeType = "";
    [ObservableProperty] private string _assignDepartment = "Concierge";
    [ObservableProperty] private DateTime _assignExpected = DateTime.Now.AddHours(4);

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedRequest is not null;
    public string[] RequestTypes { get; } = ["Restaurant Reservation", "Transportation", "Airport Transfer", "City Tour", "Ticket Booking", "Courier", "Parcel Handling", "Luggage Storage", "Luggage Delivery", "VIP Assistance", "Business Center", "Flower Arrangement", "Medical Assistance", "Translation", "Custom Request"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Requests.Clear();
            var r1 = new ConciergeDto { ConciergeRequestId = Guid.NewGuid(), RequestNumber = "CON-202607-000001", GuestName = "Le Van C", RoomNumber = "301", RequestType = "Airport Transfer", Priority = "High", Status = "Completed", ActualCost = 500000, ChargeType = "ImmediateCharge", Notes = "Noi bai airport 8h sang" }; r1.History.Add("Registered"); r1.History.Add("Assigned"); r1.History.Add("Completed");
            var r2 = new ConciergeDto { ConciergeRequestId = Guid.NewGuid(), RequestNumber = "CON-202607-000002", GuestName = "Nguyen Van A", RoomNumber = "101", RequestType = "Restaurant Reservation", Priority = "Normal", Status = "InProgress", AssignedDepartment = "Concierge", Notes = "Nha hang Le Jardin, 19h, 4 nguoi" }; r2.History.Add("Registered"); r2.History.Add("Assigned → Concierge");
            var r3 = new ConciergeDto { ConciergeRequestId = Guid.NewGuid(), RequestNumber = "CON-202607-000003", GuestName = "Tran Thi B", RoomNumber = "201", RequestType = "City Tour", Priority = "Normal", Status = "Open", Notes = "Tour Ha Noi 1 ngay" }; r3.History.Add("Registered");
            Requests.Add(r1); Requests.Add(r2); Requests.Add(r3);
            TotalOpen = Requests.Count(r => r.Status is "Open"); TotalInProgress = Requests.Count(r => r.Status is "InProgress" or "Assigned"); TotalCompleted = Requests.Count(r => r.Status == "Completed");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectRequest(ConciergeDto? r) => SelectedRequest = r;

    [RelayCommand] private void CreateRequest() { if (string.IsNullOrWhiteSpace(NewRequestType)) { ErrorMessage = "Chon loai yeu cau."; return; } var nr = new ConciergeDto { ConciergeRequestId = Guid.NewGuid(), RequestNumber = $"CON-202607-{Requests.Count + 1:D6}", RequestType = NewRequestType, GuestName = NewGuestName, RoomNumber = NewRoomNumber, Priority = NewPriority, Status = "Open", EstimatedCost = NewCost, ChargeType = NewChargeType, RequestedAt = DateTime.UtcNow }; nr.History.Add("Registered"); Requests.Insert(0, nr); TotalOpen++; InfoMessage = $"Tao yeu cau: {nr.RequestNumber}."; }
    [RelayCommand] private void AssignRequest() { if (SelectedRequest is null) return; SelectedRequest.AssignedDepartment = AssignDepartment; SelectedRequest.ExpectedCompletion = AssignExpected; SelectedRequest.Status = "Assigned"; SelectedRequest.History.Add($"Assigned → {AssignDepartment}"); InfoMessage = $"Phan cong {SelectedRequest.RequestNumber}."; }
    [RelayCommand] private void StartRequest() { if (SelectedRequest is null) return; SelectedRequest.Status = "InProgress"; SelectedRequest.History.Add("In progress"); TotalInProgress++; InfoMessage = $"Dang xu ly {SelectedRequest.RequestNumber}."; }
    [RelayCommand] private void CompleteRequest() { if (SelectedRequest is null) return; SelectedRequest.Status = "Completed"; SelectedRequest.ActualCost = NewCost; SelectedRequest.History.Add("Completed"); TotalCompleted++; InfoMessage = $"Hoan thanh {SelectedRequest.RequestNumber}."; }

    partial void OnSelectedRequestChanged(ConciergeDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
