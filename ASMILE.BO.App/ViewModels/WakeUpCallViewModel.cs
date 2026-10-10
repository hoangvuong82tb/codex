using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
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
    public bool IsDueSoon => Status is "Scheduled" or "InProgress" && DateTime.Now >= WakeTime.AddMinutes(-5) && DateTime.Now <= WakeTime;
    public bool IsOverdue => Status is "Scheduled" or "InProgress" && DateTime.Now > WakeTime.AddMinutes(10);
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class WakeUpCallViewModel : ViewModelBase
{
    private readonly IWakeUpCallService wakeUpCallService;
    private readonly IFrontOfficeExecutionContextAccessor contextAccessor;
    private readonly IHotelRoomMasterDataRepository roomRepository;

    public WakeUpCallViewModel(
        IWakeUpCallService wakeUpCallService,
        IFrontOfficeExecutionContextAccessor contextAccessor,
        IHotelRoomMasterDataRepository roomRepository)
    {
        this.wakeUpCallService = wakeUpCallService;
        this.contextAccessor = contextAccessor;
        this.roomRepository = roomRepository;
    }

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
    [ObservableProperty] private string _newRoomNumber = string.Empty;
    [ObservableProperty] private string _newGuestName = string.Empty;
    [ObservableProperty] private bool _newRecurring;
    [ObservableProperty] private bool _newDaily;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedCall is not null;

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        try
        {
            var propertyId = RequirePropertyId();
            var dashboard = await wakeUpCallService.GetDashboardAsync(propertyId, ct);
            var persistedCalls = await wakeUpCallService.GetScheduledAsync(propertyId, DateTime.Today, ct);

            Calls.Clear();
            foreach (var call in persistedCalls)
                Calls.Add(Map(call));

            TotalScheduled = dashboard.Pending;
            TotalCompleted = dashboard.Completed;
            TotalDueSoon = Calls.Count(call => call.IsDueSoon);
            TotalOverdue = Calls.Count(call => call.IsOverdue);
            SelectedCall = Calls.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Calls.Clear();
            SelectedCall = null;
            ErrorMessage = $"Không thể tải danh sách đánh thức từ CSDL: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private void SelectCall(WakeUpCallDto? call) => SelectedCall = call;

    [RelayCommand]
    private async Task CreateCallAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(NewRoomNumber))
        {
            ErrorMessage = "Vui lòng nhập số phòng.";
            return;
        }

        if (NewWakeTime <= DateTime.Now)
        {
            ErrorMessage = "Giờ đánh thức phải lớn hơn thời điểm hiện tại.";
            return;
        }

        try
        {
            var propertyId = RequirePropertyId();
            var room = await roomRepository.GetRoomByNumberAsync(propertyId, NewRoomNumber.Trim(), ct);
            if (room is null)
            {
                ErrorMessage = $"Không tìm thấy phòng {NewRoomNumber.Trim()} trong danh mục phòng của cơ sở hiện tại.";
                return;
            }

            var notes = string.IsNullOrWhiteSpace(NewGuestName)
                ? null
                : $"Khách: {NewGuestName.Trim()}";
            if (NewRecurring || NewDaily)
                notes = string.IsNullOrWhiteSpace(notes) ? "Yêu cầu lặp lại" : $"{notes}; Yêu cầu lặp lại";

            var persisted = await wakeUpCallService.ScheduleAsync(null, room.HotelRoomId, NewWakeTime, notes, ct);
            var created = Map(persisted, room.RoomNumber, NewGuestName);
            Calls.Insert(0, created);
            SelectedCall = created;
            TotalScheduled++;
            RecalculateIndicators();
            InfoMessage = $"Đã tạo đánh thức {created.CallNumber} cho phòng {created.RoomNumber} lúc {created.WakeTime:HH:mm}.";
            NewRoomNumber = string.Empty;
            NewGuestName = string.Empty;
            NewRecurring = false;
            NewDaily = false;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tạo cuộc gọi đánh thức: {ex.Message}";
        }
    }

    [RelayCommand]
    private void StartCall()
    {
        if (SelectedCall is null) return;
        InfoMessage = $"Đang gọi phòng {SelectedCall.RoomNumber ?? "chưa xác định"}. Khi có kết quả, hãy chọn Đã trả lời hoặc Không trả lời để ghi nhận vào CSDL.";
    }

    [RelayCommand]
    private async Task MarkAnsweredAsync(CancellationToken ct)
    {
        if (SelectedCall is null) return;
        await CompleteSelectedAsync(WakeUpCallStatus.Completed, "Đã trả lời", ct);
    }

    [RelayCommand]
    private async Task MarkNoAnswerAsync(CancellationToken ct)
    {
        if (SelectedCall is null) return;

        try
        {
            var result = await wakeUpCallService.CompleteAsync(SelectedCall.WakeUpCallId, WakeUpCallStatus.NoAnswer, ct);
            var roomNumber = SelectedCall.RoomNumber;
            var guestName = SelectedCall.GuestName;
            var retried = await wakeUpCallService.RetryAsync(result.CallId, ct);
            ReplaceSelected(Map(retried, roomNumber, guestName, "Không trả lời; đã xếp lịch gọi lại"));
            InfoMessage = $"Phòng {roomNumber ?? "chưa xác định"} không trả lời. Đã ghi nhận và xếp lịch gọi lại lần {SelectedCall.RetryCount}.";
            await RefreshDashboardAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể ghi nhận cuộc gọi không trả lời: {ex.Message}";
        }
    }

    private async Task CompleteSelectedAsync(WakeUpCallStatus status, string resultText, CancellationToken ct)
    {
        try
        {
            var roomNumber = SelectedCall!.RoomNumber;
            var guestName = SelectedCall.GuestName;
            var persisted = await wakeUpCallService.CompleteAsync(SelectedCall.WakeUpCallId, status, ct);
            ReplaceSelected(Map(persisted, roomNumber, guestName, resultText));
            InfoMessage = $"Đã ghi nhận kết quả cho phòng {roomNumber ?? "chưa xác định"}.";
            await RefreshDashboardAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể cập nhật kết quả cuộc gọi: {ex.Message}";
        }
    }

    private async Task RefreshDashboardAsync(CancellationToken ct)
    {
        var dashboard = await wakeUpCallService.GetDashboardAsync(RequirePropertyId(), ct);
        TotalScheduled = dashboard.Pending;
        TotalCompleted = dashboard.Completed;
        RecalculateIndicators();
    }

    private void ReplaceSelected(WakeUpCallDto replacement)
    {
        var index = SelectedCall is null ? -1 : Calls.IndexOf(SelectedCall);
        if (index >= 0)
            Calls[index] = replacement;
        SelectedCall = replacement;
    }

    private void RecalculateIndicators()
    {
        TotalDueSoon = Calls.Count(call => call.IsDueSoon);
        TotalOverdue = Calls.Count(call => call.IsOverdue);
    }

    private Guid RequirePropertyId()
        => contextAccessor.Current?.Property.PropertyId.Value
            ?? throw new InvalidOperationException("Chưa có cơ sở đang làm việc trong ngữ cảnh Front Office.");

    private static WakeUpCallDto Map(ASMILE.BO.Application.FrontOffice.Concierge.WakeUpCallDto source, string? roomNumber = null, string? guestName = null, string? result = null)
        => new()
        {
            WakeUpCallId = source.CallId,
            CallNumber = source.CallNumber,
            RoomNumber = roomNumber ?? source.RoomNumber,
            GuestName = guestName,
            WakeTime = source.WakeTime,
            Status = source.Status.ToString(),
            Result = result,
            RetryCount = source.RetryCount,
            IsRecurring = source.IsRecurring,
            Notes = source.Notes
        };

    partial void OnSelectedCallChanged(WakeUpCallDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
}
