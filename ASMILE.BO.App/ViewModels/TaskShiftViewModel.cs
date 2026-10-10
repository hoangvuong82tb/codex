using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.TaskShift;
using ASMILE.BO.Core.FrontOffice.TaskShift;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class TaskDto
{
    public Guid TaskAssignmentId { get; set; }
    public string TaskNumber { get; set; } = string.Empty;
    public string TaskType { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Open";
    public string? Department { get; set; }
    public string? AssignedShift { get; set; }
    public string? AssignedStaffName { get; set; }
    public string? Description { get; set; }
    public DateTime? DueTime { get; set; }
    public int? EstimatedMinutes { get; set; }
    public int? ActualMinutes { get; set; }
    public ObservableCollection<string> History { get; set; } = new();
    public string StatusDisplay => Status switch
    {
        "Open" => "Mở",
        "Assigned" => "Đã phân công",
        "Accepted" => "Đã nhận",
        "InProgress" => "Đang thực hiện",
        "OnHold" => "Tạm dừng",
        "Completed" => "Hoàn thành",
        "Cancelled" => "Đã hủy",
        _ => Status
    };
    public string PriorityDisplay => Priority switch
    {
        "Urgent" => "Khẩn cấp",
        "High" => "Cao",
        "Low" => "Thấp",
        _ => "Bình thường"
    };
    public string DueTimeDisplay => DueTime.HasValue ? DueTime.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Chưa đặt hạn";
    public bool IsOverdue => DueTime.HasValue && DueTime.Value < DateTime.UtcNow && Status is not "Completed" and not "Cancelled";
}

public sealed partial class TaskShiftViewModel : ViewModelBase
{
    private readonly ITaskShiftService? _service;

    public TaskShiftViewModel() { }
    public TaskShiftViewModel(ITaskShiftService service) => _service = service;

    public ObservableCollection<TaskDto> Tasks { get; } = new();
    public ObservableCollection<TaskDto> FilteredTasks { get; } = new();
    [ObservableProperty] private TaskDto? _selectedTask;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalOpen;
    [ObservableProperty] private int _totalOverdue;
    [ObservableProperty] private int _totalCompleted;
    [ObservableProperty] private string _newTaskType = string.Empty;
    [ObservableProperty] private string _newModule = string.Empty;
    [ObservableProperty] private string _newDescription = string.Empty;
    [ObservableProperty] private string _newPriority = "Normal";
    [ObservableProperty] private string _assignStaff = string.Empty;
    [ObservableProperty] private string _assignShift = "Morning";
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _filterStatus = "Tất cả";
    [ObservableProperty] private string _filterPriority = "Tất cả";
    [ObservableProperty] private string _filterShift = "Tất cả";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedTask is not null;
    public string[] TaskTypes { get; } = ["Housekeeping", "Service Request", "Concierge", "Wake-up Call", "Guest Feedback", "Lost & Found", "Maintenance", "Front Office", "Custom"];
    public string[] Priorities { get; } = ["Low", "Normal", "High", "Urgent"];
    public string[] Shifts { get; } = ["Morning", "Afternoon", "Evening", "Night"];
    public string[] StatusFilters { get; } = ["Tất cả", "Open", "Assigned", "Accepted", "InProgress", "OnHold", "Completed", "Cancelled"];
    public string[] PriorityFilters { get; } = ["Tất cả", "Urgent", "High", "Normal", "Low"];
    public string[] ShiftFilters { get; } = ["Tất cả", "Morning", "Afternoon", "Evening", "Night"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        if (_service is null)
        {
            ErrorMessage = "Task & Shift chưa được cấu hình service dữ liệu.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var snapshots = await _service.QueryAsync(ct);
            Tasks.Clear();
            foreach (var snapshot in snapshots)
                Tasks.Add(Map(snapshot));
            ApplyFilters();
            RecalculateCounters();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không tải được danh sách công việc: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private void SelectTask(TaskDto? task) => SelectedTask = task;

    [RelayCommand]
    private async Task CreateTaskAsync(CancellationToken ct)
    {
        if (_service is null)
        {
            ErrorMessage = "Task & Shift chưa được cấu hình service dữ liệu.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTaskType))
        {
            ErrorMessage = "Vui lòng chọn loại công việc.";
            return;
        }

        await ExecuteMutationAsync(
            () => _service.CreateAsync(new CreateTaskAssignmentRequest(
                NewTaskType, NewModule, NewPriority, null, null, NewDescription, null, null), ct),
            snapshot =>
            {
                NewTaskType = string.Empty;
                NewModule = string.Empty;
                NewDescription = string.Empty;
                InfoMessage = $"Đã tạo công việc {snapshot.TaskNumber}.";
            });
    }

    [RelayCommand(CanExecute = nameof(CanMutateSelectedTask))]
    private async Task AssignTaskAsync(CancellationToken ct)
    {
        var selected = SelectedTask;
        if (_service is null || selected is null)
            return;

        var taskNumber = selected.TaskNumber;
        await ExecuteMutationAsync(
            () => _service.AssignAsync(selected.TaskAssignmentId, AssignStaff, null, ParseShift(AssignShift), ct),
            snapshot => InfoMessage = $"Đã phân công {taskNumber}.");
    }

    [RelayCommand(CanExecute = nameof(CanMutateSelectedTask))]
    private async Task StartTaskAsync(CancellationToken ct)
    {
        var selected = SelectedTask;
        if (_service is null || selected is null)
            return;

        var taskNumber = selected.TaskNumber;
        await ExecuteMutationAsync(
            () => _service.StartAsync(selected.TaskAssignmentId, ct),
            snapshot => InfoMessage = $"Đã bắt đầu {taskNumber}.");
    }

    [RelayCommand(CanExecute = nameof(CanMutateSelectedTask))]
    private async Task CompleteTaskAsync(CancellationToken ct)
    {
        var selected = SelectedTask;
        if (_service is null || selected is null)
            return;

        var taskNumber = selected.TaskNumber;
        await ExecuteMutationAsync(
            () => _service.CompleteAsync(selected.TaskAssignmentId, ct),
            snapshot => InfoMessage = $"Đã hoàn thành {taskNumber}.");
    }

    private async Task ExecuteMutationAsync(
        Func<Task<TaskAssignmentSnapshot>> mutation,
        Action<TaskAssignmentSnapshot> onSuccess)
    {
        ErrorMessage = string.Empty;
        try
        {
            var snapshot = await mutation();
            var existing = Tasks.FirstOrDefault(x => x.TaskAssignmentId == snapshot.TaskAssignmentId);
            var index = existing is null ? 0 : Tasks.IndexOf(existing);
            if (existing is not null)
                Tasks.Remove(existing);
            Tasks.Insert(index, Map(snapshot));
            SelectedTask = Tasks.FirstOrDefault(x => x.TaskAssignmentId == snapshot.TaskAssignmentId);
            ApplyFilters();
            RecalculateCounters();
            onSuccess(snapshot);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Thao tác không thành công: {ex.Message}";
        }
    }

    private void RecalculateCounters()
    {
        var now = DateTime.UtcNow;
        TotalOpen = Tasks.Count(x => x.Status is "Open" or "Assigned" or "Accepted" or "InProgress" or "OnHold");
        TotalCompleted = Tasks.Count(x => x.Status == "Completed");
        TotalOverdue = Tasks.Count(x => x.DueTime.HasValue && x.DueTime.Value < now && x.Status is not "Completed" and not "Cancelled");
    }

    private bool CanMutateSelectedTask() => SelectedTask is not null && !IsLoading;

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        FilterStatus = "Tất cả";
        FilterPriority = "Tất cả";
        FilterShift = "Tất cả";
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var keyword = SearchText.Trim();
        var rows = Tasks.Where(task =>
            string.IsNullOrWhiteSpace(keyword)
            || task.TaskNumber.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
            || (task.Description?.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ?? false)
            || task.TaskType.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
            || (task.AssignedStaffName?.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ?? false));
        if (FilterStatus != "Tất cả") rows = rows.Where(task => task.Status == FilterStatus);
        if (FilterPriority != "Tất cả") rows = rows.Where(task => task.Priority == FilterPriority);
        if (FilterShift != "Tất cả") rows = rows.Where(task => task.AssignedShift == FilterShift);

        FilteredTasks.Clear();
        foreach (var task in rows) FilteredTasks.Add(task);
    }

    private static TaskDto Map(TaskAssignmentSnapshot snapshot) => new()
    {
        TaskAssignmentId = snapshot.TaskAssignmentId,
        TaskNumber = snapshot.TaskNumber,
        TaskType = snapshot.TaskType,
        SourceModule = snapshot.SourceModule,
        Priority = snapshot.Priority,
        Status = snapshot.Status.ToString(),
        Department = snapshot.Department,
        AssignedShift = snapshot.AssignedShift?.ToString(),
        AssignedStaffName = snapshot.AssignedStaffName,
        Description = snapshot.Description,
        DueTime = snapshot.DueTime,
        EstimatedMinutes = snapshot.EstimatedMinutes,
        ActualMinutes = snapshot.ActualMinutes,
        History = new ObservableCollection<string>(snapshot.History)
    };

    private static ShiftType? ParseShift(string? value) => value switch
    {
        "Morning" => ShiftType.Morning,
        "Afternoon" => ShiftType.Afternoon,
        "Evening" => ShiftType.Evening,
        "Night" => ShiftType.Night,
        _ => null
    };

    partial void OnSelectedTaskChanged(TaskDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        AssignTaskCommand.NotifyCanExecuteChanged();
        StartTaskCommand.NotifyCanExecuteChanged();
        CompleteTaskCommand.NotifyCanExecuteChanged();
    }
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string value) => ApplyFilters();
    partial void OnFilterPriorityChanged(string value) => ApplyFilters();
    partial void OnFilterShiftChanged(string value) => ApplyFilters();
}
