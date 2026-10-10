using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Concierge;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public enum TelephoneWorkspaceMode { Incoming, Outgoing, Log, Contacts }

public sealed partial class TelephoneCenterViewModel : ViewModelBase
{
    private readonly ITelephoneService service;
    private readonly IFrontOfficeExecutionContextAccessor contextAccessor;

    public TelephoneCenterViewModel(ITelephoneService service, IFrontOfficeExecutionContextAccessor contextAccessor)
    {
        this.service = service;
        this.contextAccessor = contextAccessor;
    }

    public ObservableCollection<TelephoneCallDto> Calls { get; } = new();
    public ObservableCollection<TelephoneContactDto> Contacts { get; } = new();
    public string[] Categories { get; } = ["Guest", "Company", "Supplier", "Internal", "Other"];
    public bool IsContactsMode => Mode == TelephoneWorkspaceMode.Contacts;
    public bool IsCallsMode => !IsContactsMode;
    public string WorkspaceTitle => Mode switch
    {
        TelephoneWorkspaceMode.Incoming => "Cuộc gọi đến",
        TelephoneWorkspaceMode.Outgoing => "Cuộc gọi đi",
        TelephoneWorkspaceMode.Contacts => "Danh bạ",
        _ => "Nhật ký cuộc gọi"
    };

    [ObservableProperty] private TelephoneWorkspaceMode mode = TelephoneWorkspaceMode.Log;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private TelephoneCallDto? selectedCall;
    [ObservableProperty] private TelephoneContactDto? selectedContact;
    [ObservableProperty] private string phoneNumber = string.Empty;
    [ObservableProperty] private string guestName = string.Empty;
    [ObservableProperty] private string roomNumber = string.Empty;
    [ObservableProperty] private string subject = string.Empty;
    [ObservableProperty] private string notes = string.Empty;
    [ObservableProperty] private int durationSeconds;
    [ObservableProperty] private string contactName = string.Empty;
    [ObservableProperty] private string contactPhone = string.Empty;
    [ObservableProperty] private string contactRoom = string.Empty;
    [ObservableProperty] private string contactCategory = "Guest";
    [ObservableProperty] private string contactNotes = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasContactSelection => SelectedContact is not null;

    partial void OnModeChanged(TelephoneWorkspaceMode value)
    {
        OnPropertyChanged(nameof(IsContactsMode));
        OnPropertyChanged(nameof(IsCallsMode));
        OnPropertyChanged(nameof(WorkspaceTitle));
    }
    partial void OnSelectedContactChanged(TelephoneContactDto? value)
    {
        OnPropertyChanged(nameof(HasContactSelection));
        if (value is null) return;
        ContactName = value.DisplayName; ContactPhone = value.PhoneNumber; ContactRoom = value.RoomNumber ?? string.Empty; ContactCategory = value.Category; ContactNotes = value.Notes ?? string.Empty;
    }
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnInfoMessageChanged(string value) => OnPropertyChanged(nameof(HasInfo));

    public async Task InitializeAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    [RelayCommand]
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        ErrorMessage = string.Empty;
        if (contextAccessor.Current is not { } context) { ErrorMessage = "Chưa chọn Property cho Front Office."; return; }
        IsLoading = true;
        try
        {
            if (IsContactsMode)
            {
                Contacts.Clear();
                foreach (var item in await service.GetContactsAsync(context.Property.PropertyId.Value, NullIfEmpty(Keyword), ct)) Contacts.Add(item);
            }
            else
            {
                Calls.Clear();
                TelephoneCallDirection? direction = Mode == TelephoneWorkspaceMode.Incoming
                    ? TelephoneCallDirection.Incoming
                    : Mode == TelephoneWorkspaceMode.Outgoing
                        ? TelephoneCallDirection.Outgoing
                        : null;
                foreach (var item in await service.GetCallsAsync(context.Property.PropertyId.Value, direction, NullIfEmpty(Keyword), ct)) Calls.Add(item);
            }
        }
        catch (Exception ex) { ErrorMessage = $"Không thể tải {WorkspaceTitle.ToLowerInvariant()}: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task LogCallAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(PhoneNumber)) { ErrorMessage = "Vui lòng nhập số điện thoại."; return; }
        var direction = Mode == TelephoneWorkspaceMode.Outgoing ? TelephoneCallDirection.Outgoing : TelephoneCallDirection.Incoming;
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            await service.LogCallAsync(direction, PhoneNumber, NullIfEmpty(GuestName), NullIfEmpty(RoomNumber), NullIfEmpty(Subject), NullIfEmpty(Notes), DateTime.UtcNow, Math.Max(0, DurationSeconds), ct);
            PhoneNumber = string.Empty; GuestName = string.Empty; RoomNumber = string.Empty; Subject = string.Empty; Notes = string.Empty; DurationSeconds = 0;
            InfoMessage = "Đã ghi nhật ký cuộc gọi vào dữ liệu thật.";
            await RefreshAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể ghi cuộc gọi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task SaveContactAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(ContactName) || string.IsNullOrWhiteSpace(ContactPhone)) { ErrorMessage = "Tên và số điện thoại là bắt buộc."; return; }
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            await service.SaveContactAsync(SelectedContact?.ContactId, ContactName, ContactPhone, NullIfEmpty(ContactRoom), ContactCategory, NullIfEmpty(ContactNotes), ct);
            SelectedContact = null; ContactName = string.Empty; ContactPhone = string.Empty; ContactRoom = string.Empty; ContactNotes = string.Empty;
            InfoMessage = "Đã lưu danh bạ vào dữ liệu thật.";
            await RefreshAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể lưu danh bạ: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    public async Task DeactivateContactAsync(CancellationToken ct = default)
    {
        if (SelectedContact is null) return;
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            await service.DeactivateContactAsync(SelectedContact.ContactId, ct);
            SelectedContact = null; InfoMessage = "Đã ẩn liên hệ khỏi danh bạ."; await RefreshAsync(ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không thể ẩn liên hệ: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
