using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;

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
    private readonly IServiceRequestService? _service;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;

    public ServiceRequestViewModel(
        IServiceRequestService? service = null,
        IFrontOfficeExecutionContextAccessor? executionContext = null)
    {
        _service = service;
        _executionContext = executionContext;
    }

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
        ErrorMessage = string.Empty;
        try
        {
            Requests.Clear();
            if (_service is null || _executionContext?.Current is null)
            {
                ErrorMessage = "Chưa cấu hình Service Request và property context dữ liệu thật.";
                return;
            }

            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            var persisted = await _service.GetAllAsync(propertyId, ct);
            foreach (var request in persisted)
                Requests.Add(Map(request));

            TotalOpen = Requests.Count(r => r.Status is "Open" or "Assigned");
            TotalInProgress = Requests.Count(r => r.Status == "InProgress");
            TotalCompleted = Requests.Count(r => r.Status == "Completed");
            InfoMessage = $"Đã tải {Requests.Count:N0} yêu cầu từ dữ liệu thật.";
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectRequest(ServiceRequestDto? r) => SelectedRequest = r;

    [RelayCommand]
    private async Task CreateRequest(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(NewServiceType)) { ErrorMessage = "Vui long chon loai dich vu."; return; }
        if (_service is null || _executionContext?.Current is null) { ErrorMessage = "Chưa cấu hình Service Request dữ liệu thật."; return; }

        var result = await _service.CreateAsync(new CreateServiceRequestRequest(
            null,
            null,
            null,
            string.IsNullOrWhiteSpace(NewRoomNumber) ? null : NewRoomNumber.Trim(),
            string.IsNullOrWhiteSpace(NewGuestName) ? null : NewGuestName.Trim(),
            ParseServiceType(NewServiceType),
            ParsePriority(NewPriority),
            $"{NewCategory}: {NewServiceType}",
            null,
            NewCost,
            NewChargeType), ct);
        InfoMessage = $"Đã tạo yêu cầu {result.RequestNumber} từ dữ liệu thật.";
        NewServiceType = ""; NewRoomNumber = ""; NewGuestName = "";
        await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task AssignRequest(CancellationToken ct)
    {
        await TransitionWithActorAsync(ServiceStatus.Assigned, ct);
    }

    [RelayCommand]
    private async Task StartRequest(CancellationToken ct)
    {
        await TransitionAsync(ServiceStatus.InProgress, ct);
    }

    [RelayCommand]
    private async Task CompleteRequest(CancellationToken ct)
    {
        await TransitionAsync(ServiceStatus.Completed, ct);
    }

    [RelayCommand]
    private async Task CancelRequest(CancellationToken ct)
    {
        await TransitionAsync(ServiceStatus.Cancelled, ct);
    }

    private async Task TransitionWithActorAsync(ServiceStatus status, CancellationToken ct)
    {
        if (_executionContext?.Current?.Actor.ActorId is not { } actorId)
        {
            ErrorMessage = "Chưa có người dùng hiện tại để phân công.";
            return;
        }

        if (_service is null || SelectedRequest is null)
        {
            ErrorMessage = "Chọn một yêu cầu từ dữ liệu thật trước.";
            return;
        }

        await _service.AssignAsync(SelectedRequest.ServiceRequestId, actorId.Value, ct);
        InfoMessage = $"Đã phân công {SelectedRequest.RequestNumber} cho người dùng hiện tại.";
        await LoadAsync(ct);
    }

    private async Task TransitionAsync(ServiceStatus status, CancellationToken ct)
    {
        if (_service is null || SelectedRequest is null)
        {
            ErrorMessage = "Chọn một yêu cầu từ dữ liệu thật trước.";
            return;
        }

        await _service.TransitionAsync(SelectedRequest.ServiceRequestId, status, null, ct);
        InfoMessage = $"Đã cập nhật {SelectedRequest.RequestNumber}.";
        await LoadAsync(ct);
    }

    private static ServiceType ParseServiceType(string value)
        => value switch
        {
            "Wake-up Call" => ServiceType.WakeUpCall,
            "Airport Pickup" => ServiceType.AirportTransfer,
            "Taxi" => ServiceType.Taxi,
            "Laundry" => ServiceType.Other,
            "Food Delivery" => ServiceType.RestaurantBooking,
            _ => Enum.TryParse<ServiceType>(value, true, out var parsed) ? parsed : ServiceType.Other
        };

    private static ServicePriority ParsePriority(string value)
        => value switch
        {
            "High" => ServicePriority.High,
            "Urgent" => ServicePriority.Urgent,
            "Emergency" => ServicePriority.Vip,
            _ => ServicePriority.Normal
        };

    private static ServiceRequestDto Map(ASMILE.BO.Application.FrontOffice.Concierge.ServiceRequestDto request)
        => new()
        {
            ServiceRequestId = request.RequestId,
            RequestNumber = request.RequestNumber,
            RoomNumber = request.RoomNumber,
            GuestName = request.GuestName,
            ServiceType = request.Type.ToString(),
            Category = request.Category ?? string.Empty,
            Priority = request.Priority == ServicePriority.Vip ? "Emergency" : request.Priority.ToString(),
            Status = request.Status == ServiceStatus.Requested ? "Open" : request.Status.ToString(),
            AssignedDepartment = request.AssignedDepartment,
            EstimatedCost = request.EstimatedCost,
            ActualCost = request.ActualCost,
            ChargeType = request.ChargeType ?? "ChargeAtCheckout",
            Description = request.Description,
            RequestedAt = request.RequestedAtUtc,
            CompletedAt = request.CompletedAtUtc
        };

    partial void OnSelectedRequestChanged(ServiceRequestDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
}
