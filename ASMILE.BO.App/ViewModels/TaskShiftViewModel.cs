using System.Collections.ObjectModel;
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
}

public sealed partial class TaskShiftViewModel : ViewModelBase
{
    public ObservableCollection<TaskDto> Tasks { get; } = new();
    [ObservableProperty] private TaskDto? _selectedTask;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalOpen;
    [ObservableProperty] private int _totalOverdue;
    [ObservableProperty] private int _totalCompleted;
    [ObservableProperty] private string _newTaskType = "";
    [ObservableProperty] private string _newModule = "";
    [ObservableProperty] private string _newDescription = "";
    [ObservableProperty] private string _newPriority = "Normal";
    [ObservableProperty] private string _assignStaff = "";
    [ObservableProperty] private string _assignShift = "Morning";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedTask is not null;
    public string[] TaskTypes { get; } = ["Housekeeping", "Service Request", "Concierge", "Wake-up Call", "Guest Feedback", "Lost & Found", "Maintenance", "Front Office", "Custom"];
    public string[] Shifts { get; } = ["Morning", "Afternoon", "Evening", "Night"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Tasks.Clear();
            var t1 = new TaskDto { TaskAssignmentId = Guid.NewGuid(), TaskNumber = "TSK-202607-000001", TaskType = "Housekeeping", SourceModule = "Housekeeping", Priority = "High", Status = "InProgress", Department = "Housekeeping", AssignedShift = "Morning", AssignedStaffName = "Mai Lan", Description = "Don phong 101 - khach VIP", DueTime = DateTime.UtcNow.AddHours(1), EstimatedMinutes = 45 }; t1.History.Add("Created"); t1.History.Add("Assigned → Mai Lan (Morning)"); t1.History.Add("Started");
            var t2 = new TaskDto { TaskAssignmentId = Guid.NewGuid(), TaskNumber = "TSK-202607-000002", TaskType = "Service Request", SourceModule = "ServiceRequest", Priority = "Urgent", Status = "Open", Department = "Engineering", Description = "Sua dieu hoa phong 201", DueTime = DateTime.UtcNow.AddMinutes(30), EstimatedMinutes = 60 }; t2.History.Add("Created");
            var t3 = new TaskDto { TaskAssignmentId = Guid.NewGuid(), TaskNumber = "TSK-202607-000003", TaskType = "Concierge", SourceModule = "Concierge", Priority = "Normal", Status = "Completed", Department = "Concierge", AssignedShift = "Afternoon", AssignedStaffName = "Tran Van B", Description = "Dat nha hang cho khach 301", ActualMinutes = 20 }; t3.History.Add("Created"); t3.History.Add("Assigned"); t3.History.Add("Completed");
            Tasks.Add(t1); Tasks.Add(t2); Tasks.Add(t3);
            TotalOpen = Tasks.Count(t => t.Status is "Open" or "Assigned"); TotalCompleted = Tasks.Count(t => t.Status == "Completed");
            foreach (var t in Tasks) if (t.DueTime.HasValue && DateTime.UtcNow > t.DueTime && t.Status is not "Completed" and not "Cancelled") { t.Status = "Overdue"; TotalOverdue++; }
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectTask(TaskDto? t) => SelectedTask = t;
    [RelayCommand] private void CreateTask() { if (string.IsNullOrWhiteSpace(NewTaskType)) { ErrorMessage = "Vui long chon loai cong viec."; return; } var nt = new TaskDto { TaskAssignmentId = Guid.NewGuid(), TaskNumber = $"TSK-202607-{Tasks.Count + 1:D6}", TaskType = NewTaskType, SourceModule = NewModule, Description = NewDescription, Priority = NewPriority, Status = "Open" }; nt.History.Add("Created"); Tasks.Insert(0, nt); TotalOpen++; InfoMessage = $"Tao cong viec {nt.TaskNumber}."; }
    [RelayCommand] private void AssignTask() { if (SelectedTask is null) return; SelectedTask.AssignedStaffName = AssignStaff; SelectedTask.AssignedShift = AssignShift; SelectedTask.Status = "Assigned"; SelectedTask.History.Add($"Assigned → {AssignStaff} ({AssignShift})"); InfoMessage = $"Phan cong {SelectedTask.TaskNumber}."; }
    [RelayCommand] private void StartTask() { if (SelectedTask is null) return; SelectedTask.Status = "InProgress"; SelectedTask.History.Add("Started"); InfoMessage = $"Bat đau {SelectedTask.TaskNumber}."; }
    [RelayCommand] private void CompleteTask() { if (SelectedTask is null) return; SelectedTask.Status = "Completed"; SelectedTask.History.Add("Completed"); TotalCompleted++; InfoMessage = $"Hoan thanh {SelectedTask.TaskNumber}."; }

    partial void OnSelectedTaskChanged(TaskDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
