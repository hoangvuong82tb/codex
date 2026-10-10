using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.FrontOffice.Feedback;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.Interfaces;

namespace ASMILE.BO.App.ViewModels;

public sealed class FeedbackDto
{
    public Guid GuestFeedbackId { get; set; }
    public string FeedbackNumber { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public string? RoomNumber { get; set; }
    public string FeedbackType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "New";
    public string? Source { get; set; }
    public int Rating { get; set; }
    public string? Subject { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? AssignedDepartment { get; set; }
    public string? Resolution { get; set; }
    public DateTime CreatedAt { get; set; }
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class GuestFeedbackViewModel : ViewModelBase
{
    private readonly IGuestFeedbackService? _service;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly ICurrentUserService? _currentUserService;

    public GuestFeedbackViewModel(
        IGuestFeedbackService? service = null,
        IFrontOfficeExecutionContextAccessor? contextAccessor = null,
        ICurrentUserService? currentUserService = null)
    {
        _service = service;
        _contextAccessor = contextAccessor;
        _currentUserService = currentUserService;
    }

    public ObservableCollection<FeedbackDto> Feedbacks { get; } = new();
    [ObservableProperty] private FeedbackDto? _selectedFeedback;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalNew;
    [ObservableProperty] private int _totalResolved;
    [ObservableProperty] private double _avgRating;
    [ObservableProperty] private string _newType = "Complaint";
    [ObservableProperty] private string _newCategory = "Service Quality";
    [ObservableProperty] private string _newGuest = "";
    [ObservableProperty] private string _newRoom = "";
    [ObservableProperty] private string _newDescription = "";
    [ObservableProperty] private int _newRating = 3;
    [ObservableProperty] private string _newPriority = "Normal";
    [ObservableProperty] private string _resolution = "";
    [ObservableProperty] private string _assignDept = "Front Office";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedFeedback is not null;
    public string[] FeedbackTypes { get; } = ["Compliment", "Suggestion", "Complaint", "Maintenance", "Housekeeping", "Food & Beverage", "Front Office", "Billing", "Service Quality", "Other"];
    public string[] Categories { get; } = ["Service Quality", "Cleanliness", "Staff Attitude", "Food Quality", "Room Condition", "Noise", "Billing Error", "Amenities", "Safety", "Other"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Feedbacks.Clear();
            SelectedFeedback = null;
            TotalNew = 0;
            TotalResolved = 0;
            AvgRating = 0;
            ErrorMessage = string.Empty;
            var propertyId = TryGetPropertyId();
            if (_service is null || propertyId is null)
            {
                InfoMessage = "Chưa chọn cơ sở hoặc nguồn dữ liệu phản hồi chưa được cấu hình; không hiển thị dữ liệu mẫu.";
                return;
            }

            var result = await _service.SearchAsync(propertyId.Value, cancellationToken: ct);
            foreach (var item in result.Items) Feedbacks.Add(ToDto(item));
            TotalNew = result.TotalNew;
            TotalResolved = result.TotalResolved;
            AvgRating = result.AverageRating;
            InfoMessage = $"Đã tải {result.TotalCount:N0} phản hồi từ CSDL thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể tải phản hồi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectFeedback(FeedbackDto? f) => SelectedFeedback = f;
    [RelayCommand] private async Task CreateFeedback(CancellationToken ct)
    {
        if (_service is null || TryGetPropertyId() is not { } propertyId)
        {
            ErrorMessage = "Chưa chọn cơ sở hoặc nguồn dữ liệu phản hồi chưa được cấu hình.";
            return;
        }
        try
        {
            ErrorMessage = string.Empty;
            var created = await _service.CreateAsync(new CreateGuestFeedbackCommand(
                propertyId, NewType, NewCategory, NewDescription, NewGuest, NewRoom, NewRating,
                "Front Office", NewPriority, null, ActorId()), ct);
            NewDescription = string.Empty;
            await LoadAsync(ct);
            SelectedFeedback = Feedbacks.FirstOrDefault(x => x.GuestFeedbackId == created.GuestFeedbackId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể ghi nhận phản hồi: {ex.Message}"; }
    }

    [RelayCommand] private async Task AssignFeedback(CancellationToken ct)
    {
        if (SelectedFeedback is null) return;
        await RunTransitionAsync(
            () => _service!.AssignAsync(TryGetPropertyId()!.Value, SelectedFeedback.GuestFeedbackId, AssignDept, ct),
            ct);
    }

    [RelayCommand] private async Task ResolveFeedback(CancellationToken ct)
    {
        if (SelectedFeedback is null) return;
        await RunTransitionAsync(
            () => _service!.ResolveAsync(TryGetPropertyId()!.Value, SelectedFeedback.GuestFeedbackId, Resolution, ct),
            ct);
    }

    [RelayCommand] private async Task CloseFeedback(CancellationToken ct)
    {
        if (SelectedFeedback is null) return;
        await RunTransitionAsync(
            () => _service!.CloseAsync(TryGetPropertyId()!.Value, SelectedFeedback.GuestFeedbackId, ct),
            ct);
    }

    private async Task RunTransitionAsync(Func<Task> operation, CancellationToken ct)
    {
        if (_service is null || TryGetPropertyId() is null)
        {
            ErrorMessage = "Chưa chọn cơ sở hoặc nguồn dữ liệu phản hồi chưa được cấu hình.";
            return;
        }
        try
        {
            ErrorMessage = string.Empty;
            await operation();
            await LoadAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể cập nhật phản hồi: {ex.Message}"; }
    }

    private Guid? TryGetPropertyId() => _contextAccessor?.Current?.Property.PropertyId.Value is { } value && value != Guid.Empty ? value : null;
    private Guid? ActorId() => _contextAccessor?.Current?.Actor.ActorId?.Value ?? _currentUserService?.CurrentUser?.Id;
    private static FeedbackDto ToDto(GuestFeedbackDto item) => new()
    {
        GuestFeedbackId = item.GuestFeedbackId,
        FeedbackNumber = item.FeedbackNumber,
        GuestName = item.GuestName,
        RoomNumber = item.RoomNumber,
        FeedbackType = item.FeedbackType,
        Category = item.Category,
        Priority = item.Priority,
        Status = item.Status.ToString(),
        Source = item.Source,
        Rating = item.Rating,
        Subject = item.Subject,
        Description = item.Description,
        AssignedDepartment = item.AssignedDepartment,
        Resolution = item.Resolution,
        CreatedAt = item.CreatedAtUtc,
        History = new ObservableCollection<string>(item.History.Select(x => $"{x.OccurredAtUtc:dd/MM/yyyy HH:mm} · {x.Action}"))
    };

    partial void OnSelectedFeedbackChanged(FeedbackDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
}
