using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;

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
    public string SlaStatus { get; set; } = "OnTrack";
    public string SlaStatusLabel => SlaStatus switch
    {
        "Breached" => "Quá SLA",
        "AtRisk" => "Sắp quá hạn",
        _ => "Đúng hạn"
    };
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
    private readonly IServiceRequestService? _service;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;

    public ConciergeViewModel(
        IServiceRequestService? service = null,
        IFrontOfficeExecutionContextAccessor? executionContext = null)
    {
        _service = service;
        _executionContext = executionContext;
    }

    public ObservableCollection<ConciergeDto> Requests { get; } = new();
    [ObservableProperty] private ConciergeDto? _selectedRequest;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalOpen;
    [ObservableProperty] private int _totalInProgress;
    [ObservableProperty] private int _totalCompleted;
    [ObservableProperty] private int _totalSlaBreached;
    [ObservableProperty] private decimal _slaOnTimePercent;
    [ObservableProperty] private string _newRequestType = "";
    [ObservableProperty] private string _newGuestName = "";
    [ObservableProperty] private string _newRoomNumber = "";
    [ObservableProperty] private string _newPriority = "Normal";
    [ObservableProperty] private decimal? _newCost;
    [ObservableProperty] private string _newChargeType = "";
    [ObservableProperty] private string _assignDepartment = "Concierge";
    [ObservableProperty] private DateTime _assignExpected = DateTime.Now.AddHours(4);

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasSelection => SelectedRequest is not null;
    public string[] RequestTypes { get; } = ["Restaurant Reservation", "Transportation", "Airport Transfer", "City Tour", "Ticket Booking", "Courier", "Parcel Handling", "Luggage Storage", "Luggage Delivery", "VIP Assistance", "Business Center", "Flower Arrangement", "Medical Assistance", "Translation", "Custom Request"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            Requests.Clear();
            if (_service is null || _executionContext?.Current is null)
            {
                ErrorMessage = "Chưa cấu hình Concierge và property context dữ liệu thật.";
                return;
            }

            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            var persisted = await _service.GetAllAsync(propertyId, ct);
            foreach (var request in persisted)
                Requests.Add(Map(request));

            RecalculateTotals();
            var sla = await _service.GetSlaReportAsync(propertyId, ct);
            TotalSlaBreached = sla.Sum(x => x.Breached);
            var completed = sla.Sum(x => x.Completed);
            SlaOnTimePercent = completed == 0 ? 0 : Math.Round(sla.Sum(x => x.OnTime) * 100m / completed, 2);
            InfoMessage = $"Đã tải {Requests.Count:N0} yêu cầu Concierge từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            InfoMessage = "Đã hủy tải dữ liệu Concierge.";
        }
        catch (Exception ex)
        {
            Requests.Clear();
            RecalculateTotals();
            TotalSlaBreached = 0;
            SlaOnTimePercent = 0;
            ErrorMessage = $"Không thể tải yêu cầu Concierge: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectRequest(ConciergeDto? request) => SelectedRequest = request;

    [RelayCommand]
    private async Task CreateRequest(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(NewRequestType))
        {
            ErrorMessage = "Vui lòng chọn loại yêu cầu.";
            return;
        }

        if (_service is null || _executionContext?.Current is null)
        {
            ErrorMessage = "Chưa cấu hình Concierge dữ liệu thật.";
            return;
        }

        try
        {
            var result = await _service.CreateAsync(new CreateServiceRequestRequest(
                null,
                null,
                null,
                string.IsNullOrWhiteSpace(NewRoomNumber) ? null : NewRoomNumber.Trim(),
                string.IsNullOrWhiteSpace(NewGuestName) ? null : NewGuestName.Trim(),
                ParseServiceType(NewRequestType),
                ParsePriority(NewPriority),
                NewRequestType.Trim(),
                AssignExpectedUtc(),
                NewCost,
                string.IsNullOrWhiteSpace(NewChargeType) ? null : NewChargeType.Trim()), ct);

            InfoMessage = $"Đã tạo yêu cầu {result.RequestNumber} từ dữ liệu thật.";
            NewRequestType = "";
            NewGuestName = "";
            NewRoomNumber = "";
            NewCost = null;
            await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể tạo yêu cầu: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task AssignRequest(CancellationToken ct)
    {
        if (!TryGetSelected(out var selected) || !TryGetActor(out var actorId)) return;
        if (_service is null) { ErrorMessage = "Chưa cấu hình Concierge dữ liệu thật."; return; }

        try
        {
            await _service.AssignAsync(selected.ConciergeRequestId, actorId.Value, ct);
            InfoMessage = $"Đã phân công {selected.RequestNumber} cho người dùng hiện tại.";
            await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể phân công yêu cầu: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task StartRequest(CancellationToken ct) => await TransitionAsync(ServiceStatus.InProgress, ct);

    [RelayCommand]
    private async Task CompleteRequest(CancellationToken ct) => await TransitionAsync(ServiceStatus.Completed, ct);

    private async Task TransitionAsync(ServiceStatus status, CancellationToken ct)
    {
        if (!TryGetSelected(out var selected) || _service is null)
        {
            if (_service is null) ErrorMessage = "Chưa cấu hình Concierge dữ liệu thật.";
            return;
        }

        try
        {
            await _service.TransitionAsync(selected.ConciergeRequestId, status, null, ct);
            InfoMessage = $"Đã cập nhật {selected.RequestNumber}.";
            await LoadAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể cập nhật yêu cầu: {ex.Message}"; }
    }

    private bool TryGetSelected(out ConciergeDto selected)
    {
        if (SelectedRequest is not null)
        {
            selected = SelectedRequest;
            return true;
        }

        selected = null!;
        ErrorMessage = "Chọn một yêu cầu từ dữ liệu thật trước.";
        return false;
    }

    private bool TryGetActor(out ASMILE.BO.Core.FrontOffice.Foundation.Identifiers.ActorId actorId)
    {
        if (_executionContext?.Current?.Actor.ActorId is { } currentActor)
        {
            actorId = currentActor;
            return true;
        }

        actorId = default;
        ErrorMessage = "Chưa có người dùng hiện tại để phân công.";
        return false;
    }

    private void RecalculateTotals()
    {
        TotalOpen = Requests.Count(request => request.Status is "Open" or "Assigned");
        TotalInProgress = Requests.Count(request => request.Status == "InProgress");
        TotalCompleted = Requests.Count(request => request.Status == "Completed");
    }

    private DateTime? AssignExpectedUtc()
        => AssignExpected == default ? null : AssignExpected.ToUniversalTime();

    private static ServiceType ParseServiceType(string value)
        => value switch
        {
            "Transportation" => ServiceType.Taxi,
            "Airport Transfer" => ServiceType.AirportTransfer,
            "Luggage Storage" or "Luggage Delivery" => ServiceType.Luggage,
            "Parcel Handling" => ServiceType.PackageHandling,
            "Restaurant Reservation" => ServiceType.RestaurantBooking,
            "City Tour" => ServiceType.TourBooking,
            "Ticket Booking" => ServiceType.TicketBooking,
            "VIP Assistance" => ServiceType.VipService,
            _ => Enum.TryParse<ServiceType>(value, true, out var parsed) ? parsed : ServiceType.SpecialRequest
        };

    private static ServicePriority ParsePriority(string value)
        => value switch
        {
            "High" => ServicePriority.High,
            "Urgent" => ServicePriority.Urgent,
            "VIP" => ServicePriority.Vip,
            _ => ServicePriority.Normal
        };

    private static ConciergeDto Map(ASMILE.BO.Application.FrontOffice.Concierge.ServiceRequestDto request)
    {
        var status = request.Status == ServiceStatus.Requested ? "Open" : request.Status.ToString();
        var dto = new ConciergeDto
        {
            ConciergeRequestId = request.RequestId,
            RequestNumber = request.RequestNumber,
            GuestName = request.GuestName,
            RoomNumber = request.RoomNumber,
            RequestType = request.Type.ToString(),
            Priority = request.Priority.ToString(),
            Status = status,
            SlaStatus = request.SlaStatus.ToString(),
            AssignedDepartment = request.AssignedDepartment,
            ExpectedCompletion = request.SlaDeadlineAtUtc?.ToLocalTime(),
            EstimatedCost = request.EstimatedCost,
            ActualCost = request.ActualCost,
            ChargeType = request.ChargeType,
            Notes = request.Description,
            RequestedAt = request.RequestedAtUtc.ToLocalTime()
        };

        dto.History.Add($"{dto.RequestedAt:g} · {status}");
        if (!string.IsNullOrWhiteSpace(request.AssignedName))
            dto.History.Add($"Đã phân công · {request.AssignedName}");
        if (request.CompletedAtUtc is { } completedAt)
            dto.History.Add($"Hoàn thành · {completedAt.ToLocalTime():g}");
        return dto;
    }

    partial void OnSelectedRequestChanged(ConciergeDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));
}
