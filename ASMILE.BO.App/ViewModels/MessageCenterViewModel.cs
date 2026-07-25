using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class MessageDto
{
    public Guid GuestMessageId { get; set; }
    public string MessageNumber { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public string? RoomNumber { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Channel { get; set; } = "FrontDesk";
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Draft";
    public string? Subject { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool RequiresResponse { get; set; }
    public string? ResponseContent { get; set; }
    public string? AssignedStaff { get; set; }
    public DateTime CreatedAt { get; set; }
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class MessageCenterViewModel : ViewModelBase
{
    public ObservableCollection<MessageDto> Messages { get; } = new();
    [ObservableProperty] private MessageDto? _selectedMessage;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalUnread;
    [ObservableProperty] private int _totalPending;
    [ObservableProperty] private string _newType = "Guest Message";
    [ObservableProperty] private string _newGuest = "";
    [ObservableProperty] private string _newRoom = "";
    [ObservableProperty] private string _newSubject = "";
    [ObservableProperty] private string _newContent = "";
    [ObservableProperty] private string _newPriority = "Normal";
    [ObservableProperty] private bool _newRequiresResponse;
    [ObservableProperty] private string _replyContent = "";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedMessage is not null;
    public string[] MessageTypes { get; } = ["Guest Message", "Internal Message", "Reminder", "Wake-up Reminder", "Service Update", "Concierge Update", "Lost & Found Notice", "Payment Reminder", "Checkout Reminder", "VIP Notice"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Messages.Clear();
            var m1 = new MessageDto { GuestMessageId = Guid.NewGuid(), MessageNumber = "MSG-202607-000001", GuestName = "Nguyen Van A", RoomNumber = "101", MessageType = "VIP Notice", Priority = "High", Status = "Read", Subject = "Chao mung khach VIP", Content = "Kinh gui ong Nguyen Van A. Chuc ong co mot ky nghi thu gian tai khach san chung toi.", Channel = "FrontDesk", RequiresResponse = false }; m1.History.Add("Created"); m1.History.Add("Sent"); m1.History.Add("Delivered"); m1.History.Add("Read");
            var m2 = new MessageDto { GuestMessageId = Guid.NewGuid(), MessageNumber = "MSG-202607-000002", GuestName = "Le Van C", RoomNumber = "301", MessageType = "Airport Transfer Confirmation", Priority = "Normal", Status = "Replied", Subject = "Xac nhan xe dua don san bay", Content = "Xe se don ong tai sanh luc 7:30 sang mai.", Channel = "FrontDesk", RequiresResponse = true, ResponseContent = "Cam on. Toi xac nhan.", AssignedStaff = "Mai Lan" }; m2.History.Add("Created"); m2.History.Add("Sent"); m2.History.Add("Delivered"); m2.History.Add("Read"); m2.History.Add("Replied");
            Messages.Add(m1); Messages.Add(m2);
            TotalUnread = Messages.Count(m => m.Status is "Sent" or "Delivered");
            TotalPending = Messages.Count(m => m.RequiresResponse && m.Status != "Replied" && m.Status != "Completed");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectMessage(MessageDto? m) => SelectedMessage = m;

    [RelayCommand] private void CreateMessage() { if (string.IsNullOrWhiteSpace(NewContent)) { ErrorMessage = "Vui long nhap noi dung."; return; } var nm = new MessageDto { GuestMessageId = Guid.NewGuid(), MessageNumber = $"MSG-202607-{Messages.Count + 1:D6}", MessageType = NewType, GuestName = NewGuest, RoomNumber = NewRoom, Subject = NewSubject, Content = NewContent, Priority = NewPriority, RequiresResponse = NewRequiresResponse, Status = "Sent", CreatedAt = DateTime.UtcNow }; nm.History.Add("Created"); nm.History.Add("Sent"); Messages.Insert(0, nm); InfoMessage = $"Da tao tin nhan {nm.MessageNumber}."; }
    [RelayCommand] private void MarkRead() { if (SelectedMessage is null) return; SelectedMessage.Status = "Read"; SelectedMessage.History.Add("Read"); InfoMessage = "Da danh dau da doc."; }
    [RelayCommand] private void SendReply() { if (SelectedMessage is null || string.IsNullOrWhiteSpace(ReplyContent)) return; SelectedMessage.ResponseContent = ReplyContent; SelectedMessage.Status = "Replied"; SelectedMessage.History.Add("Replied"); InfoMessage = $"Da tra loi {SelectedMessage.MessageNumber}."; ReplyContent = ""; }

    partial void OnSelectedMessageChanged(MessageDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
