using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
            var f1 = new FeedbackDto { GuestFeedbackId = Guid.NewGuid(), FeedbackNumber = "FDB-202607-000001", GuestName = "Nguyen Van A", RoomNumber = "101", FeedbackType = "Complaint", Category = "Noise", Priority = "High", Status = "Resolved", Rating = 2, Subject = "On hang xom", Description = "Phong ben canh qua on ao luc 23h. Khong ngu duoc.", Source = "Front Desk", AssignedDepartment = "Front Office", Resolution = "Da chuyen phong cho khach. Da canh cao khach phong 102.", CreatedAt = DateTime.UtcNow.AddHours(-4) }; f1.History.Add("Registered"); f1.History.Add("Assigned → Front Office"); f1.History.Add("Resolved"); f1.History.Add("Closed");
            var f2 = new FeedbackDto { GuestFeedbackId = Guid.NewGuid(), FeedbackNumber = "FDB-202607-000002", GuestName = "Le Van C", RoomNumber = "301", FeedbackType = "Compliment", Category = "Staff Attitude", Priority = "Normal", Status = "New", Rating = 5, Subject = "Nhan vien tuyet voi", Description = "Nhan vien le tan Mai Lan rat nhiet tinh va chu dao.", Source = "In Person", CreatedAt = DateTime.UtcNow.AddHours(-1) }; f2.History.Add("Registered");
            var f3 = new FeedbackDto { GuestFeedbackId = Guid.NewGuid(), FeedbackNumber = "FDB-202607-000003", GuestName = "Tran Thi B", RoomNumber = "201", FeedbackType = "Maintenance", Category = "Room Condition", Priority = "Urgent", Status = "InProgress", Rating = 3, Subject = "Dieu hoa hong", Description = "Dieu hoa khong hoat dong. Phong rat nong.", AssignedDepartment = "Engineering", CreatedAt = DateTime.UtcNow.AddHours(-2) }; f3.History.Add("Registered"); f3.History.Add("Assigned → Engineering"); f3.History.Add("Investigation started");
            Feedbacks.Add(f1); Feedbacks.Add(f2); Feedbacks.Add(f3);
            TotalNew = Feedbacks.Count(f => f.Status is "New"); TotalResolved = Feedbacks.Count(f => f.Status is "Resolved" or "Closed");
            AvgRating = Feedbacks.Any() ? Feedbacks.Average(f => f.Rating) : 0;
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectFeedback(FeedbackDto? f) => SelectedFeedback = f;
    [RelayCommand] private void CreateFeedback() { if (string.IsNullOrWhiteSpace(NewDescription)) { ErrorMessage = "Vui long nhap mo ta."; return; } var nf = new FeedbackDto { GuestFeedbackId = Guid.NewGuid(), FeedbackNumber = $"FDB-202607-{Feedbacks.Count + 1:D6}", FeedbackType = NewType, Category = NewCategory, GuestName = NewGuest, RoomNumber = NewRoom, Description = NewDescription, Rating = NewRating, Priority = NewPriority, Status = "New", CreatedAt = DateTime.UtcNow }; nf.History.Add("Registered"); Feedbacks.Insert(0, nf); TotalNew++; InfoMessage = $"Da ghi nhan {nf.FeedbackNumber}."; }
    [RelayCommand] private void AssignFeedback() { if (SelectedFeedback is null) return; SelectedFeedback.AssignedDepartment = AssignDept; SelectedFeedback.Status = "Assigned"; SelectedFeedback.History.Add($"Assigned → {AssignDept}"); InfoMessage = $"Phan cong {SelectedFeedback.FeedbackNumber}."; }
    [RelayCommand] private void ResolveFeedback() { if (SelectedFeedback is null) return; SelectedFeedback.Resolution = Resolution; SelectedFeedback.Status = "Resolved"; SelectedFeedback.History.Add("Resolved"); TotalResolved++; InfoMessage = $"Da giai quyet {SelectedFeedback.FeedbackNumber}."; }
    [RelayCommand] private void CloseFeedback() { if (SelectedFeedback is null) return; SelectedFeedback.Status = "Closed"; SelectedFeedback.History.Add("Closed"); InfoMessage = $"Da dong {SelectedFeedback.FeedbackNumber}."; }

    partial void OnSelectedFeedbackChanged(FeedbackDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
