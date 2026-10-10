using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Message;
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
    private readonly IGuestMessageService _service;
    private readonly IFrontOfficeExecutionContextAccessor _contextAccessor;

    public MessageCenterViewModel(IGuestMessageService service, IFrontOfficeExecutionContextAccessor contextAccessor)
    {
        _service = service;
        _contextAccessor = contextAccessor;
    }

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
            var context = _contextAccessor.Current ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
            var rows = await _service.GetAllAsync(context.Property.PropertyId.Value, ct);
            foreach (var row in rows)
                Messages.Add(Map(row));
            TotalUnread = Messages.Count(m => m.Status is "Sent" or "Delivered");
            TotalPending = Messages.Count(m => m.RequiresResponse && m.Status != "Replied" && m.Status != "Completed");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectMessage(MessageDto? m) => SelectedMessage = m;

    [RelayCommand]
    private async Task CreateMessageAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(NewContent)) { ErrorMessage = "Vui long nhap noi dung."; return; }
        try
        {
            await _service.CreateAndSendAsync(new CreateGuestMessageRequest(
                null, null, null, null, NullIfEmpty(NewGuest), NullIfEmpty(NewRoom),
                NewType, "FrontDesk", NewPriority, NullIfEmpty(NewSubject), NewContent.Trim(),
                NewRequiresResponse, "FrontOffice"), ct);
            NewContent = string.Empty;
            await LoadAsync(ct);
            InfoMessage = "Da tao tin nhan trong du lieu that.";
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task MarkReadAsync(CancellationToken ct)
    {
        if (SelectedMessage is null) return;
        try { SelectedMessage = Map(await _service.MarkReadAsync(SelectedMessage.GuestMessageId, ct)); await LoadAsync(ct); InfoMessage = "Da danh dau da doc."; }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task SendReplyAsync(CancellationToken ct)
    {
        if (SelectedMessage is null || string.IsNullOrWhiteSpace(ReplyContent)) return;
        try { await _service.ReplyAsync(SelectedMessage.GuestMessageId, ReplyContent, ct); ReplyContent = string.Empty; await LoadAsync(ct); InfoMessage = $"Da tra loi {SelectedMessage.MessageNumber}."; }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
    }

    private static MessageDto Map(GuestMessageDto row) => new()
    {
        GuestMessageId = row.GuestMessageId,
        MessageNumber = row.MessageNumber,
        GuestName = row.GuestName,
        RoomNumber = row.RoomNumber,
        MessageType = row.MessageType,
        Channel = row.Channel,
        Priority = row.Priority,
        Status = row.Status,
        Subject = row.Subject,
        Content = row.Content,
        RequiresResponse = row.RequiresResponse,
        ResponseContent = row.ResponseContent,
        AssignedStaff = row.AssignedStaff,
        CreatedAt = row.CreatedAtUtc,
        History = new ObservableCollection<string>(row.History)
    };

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    partial void OnSelectedMessageChanged(MessageDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
}
