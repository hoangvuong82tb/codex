using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class ServiceRequestDto
{
    public Guid ServiceRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public string? GuestName { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Open";
    public string? AssignedDepartment { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string ChargeType { get; set; } = "Complimentary";
    public string? Description { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class ServiceRequestViewModel : ViewModelBase
{
    public ObservableCollection<ServiceRequestDto> Requests { get; } = new();
    [ObservableProperty] private ServiceRequestDto? _selectedRequest;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterCategory;
    [ObservableProperty] private string? _filterPriority;
    [ObservableProperty] private int _totalOpen;
    [ObservableProperty] private int _totalInProgress;
    [ObservableProperty] private int _totalCompleted;
    [ObservableProperty] private string _newServiceType = "";
    [ObservableProperty] private string _newRoomNumber = "";
    [ObservableProperty] private string _newGuestName = "";
    [ObservableProperty] private string _newCategory = "Housekeeping";
    [ObservableProperty] private string _newPriority = "Normal";
    [ObservableProperty] private string _newChargeType = "Complimentary";
    [ObservableProperty] private decimal? _newCost;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedRequest is not null;
    public string[] StatusFilters { get; } = ["Tat ca", "Open", "Assigned", "InProgress", "Completed", "Cancelled"];
    public string[] CategoryFilters { get; } = ["Tat ca", "Housekeeping", "Engineering", "FrontOffice", "Laundry", "Transportation", "FoodBeverage", "Security"];
    public string[] PriorityFilters { get; } = ["Tat ca", "Low", "Normal", "High", "Urgent", "Emergency"];
    public string[] ServiceTypes { get; } = ["Extra Bed", "Baby Cot", "Extra Pillow", "Wake-up Call", "Airport Pickup", "Laundry", "Room Cleaning", "Late Checkout", "Early Check-in", "Technical Support", "Food Delivery", "Birthday Decoration", "Flowers", "Taxi", "Iron"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Requests.Clear();
            var r1 = new ServiceRequestDto { ServiceRequestId = Guid.NewGuid(), RequestNumber = "SRV-202607-000001", RoomNumber = "101", GuestName = "Nguyen Van A", ServiceType = "Extra Pillow", Category = "Housekeeping", Priority = "Normal", Status = "Completed", AssignedDepartment = "Housekeeping", EstimatedCost = 0, ActualCost = 0, ChargeType = "Complimentary", RequestedAt = DateTime.UtcNow.AddHours(-2), CompletedAt = DateTime.UtcNow.AddHours(-1) }; r1.History.Add("Created"); r1.History.Add("Assigned → Housekeeping"); r1.History.Add("Completed");
            var r2 = new ServiceRequestDto { ServiceRequestId = Guid.NewGuid(), RequestNumber = "SRV-202607-000002", RoomNumber = "103", GuestName = "Le Van C", ServiceType = "Airport Pickup", Category = "Transportation", Priority = "High", Status = "InProgress", AssignedDepartment = "Transportation", EstimatedCost = 300000, ChargeType = "ImmediateCharge", RequestedAt = DateTime.UtcNow.AddHours(-1) }; r2.History.Add("Created"); r2.History.Add("Assigned → Transportation"); r2.History.Add("InProgress");
            var r3 = new ServiceRequestDto { ServiceRequestId = Guid.NewGuid(), RequestNumber = "SRV-202607-000003", RoomNumber = "201", GuestName = "Pham Thi D", ServiceType = "Room Cleaning", Category = "Housekeeping", Priority = "Urgent", Status = "Open", RequestedAt = DateTime.UtcNow }; r3.History.Add("Created");
            Requests.Add(r1); Requests.Add(r2); Requests.Add(r3);
            TotalOpen = Requests.Count(r => r.Status is "Open" or "Assigned");
            TotalInProgress = Requests.Count(r => r.Status == "InProgress");
            TotalCompleted = Requests.Count(r => r.Status == "Completed");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectRequest(ServiceRequestDto? r) => SelectedRequest = r;

    [RelayCommand]
    private void CreateRequest()
    {
        if (string.IsNullOrWhiteSpace(NewServiceType)) { ErrorMessage = "Vui long chon loai dich vu."; return; }
        var nr = new ServiceRequestDto { ServiceRequestId = Guid.NewGuid(), RequestNumber = $"SRV-202607-{Requests.Count + 1:D6}", RoomNumber = NewRoomNumber, GuestName = NewGuestName, ServiceType = NewServiceType, Category = NewCategory, Priority = NewPriority, Status = "Open", ChargeType = NewChargeType, EstimatedCost = NewCost, RequestedAt = DateTime.UtcNow }; nr.History.Add("Created");
        Requests.Insert(0, nr); TotalOpen++;
        InfoMessage = $"Da tao yeu cau: {nr.RequestNumber} - {nr.ServiceType} cho phong {nr.RoomNumber}.";
        NewServiceType = ""; NewRoomNumber = ""; NewGuestName = "";
    }

    [RelayCommand] private void AssignRequest() { if (SelectedRequest is null) return; SelectedRequest.Status = "Assigned"; SelectedRequest.AssignedDepartment = "Housekeeping"; SelectedRequest.History.Add($"Assigned → Housekeeping"); InfoMessage = $"Da phan cong {SelectedRequest.RequestNumber}."; }
    [RelayCommand] private void StartRequest() { if (SelectedRequest is null) return; SelectedRequest.Status = "InProgress"; SelectedRequest.History.Add("InProgress"); TotalInProgress++; InfoMessage = $"Dang thuc hien {SelectedRequest.RequestNumber}."; }
    [RelayCommand] private void CompleteRequest() { if (SelectedRequest is null) return; SelectedRequest.Status = "Completed"; SelectedRequest.CompletedAt = DateTime.UtcNow; SelectedRequest.History.Add("Completed"); TotalCompleted++; InfoMessage = $"Da hoan thanh {SelectedRequest.RequestNumber}."; }
    [RelayCommand] private void CancelRequest() { if (SelectedRequest is null) return; SelectedRequest.Status = "Cancelled"; SelectedRequest.History.Add("Cancelled"); InfoMessage = $"Da huy {SelectedRequest.RequestNumber}."; }

    partial void OnSelectedRequestChanged(ServiceRequestDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
