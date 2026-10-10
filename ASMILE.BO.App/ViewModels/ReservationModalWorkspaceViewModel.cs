using ASMILE.BO.App.Services;
using ASMILE.BO.Application.FrontOffice.Reservation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

/// <summary>
/// Presentation-only adapter around the existing reservation workspace. It changes modal content, never reservation rules.
/// </summary>
public sealed partial class ReservationModalWorkspaceViewModel : ViewModelBase
{
    private readonly Action close;
    private bool isSavingNew;

    public ReservationModalWorkspaceViewModel(
        ReservationWorkspaceViewModel workspace,
        ReservationModalKind kind,
        Action close)
    {
        Workspace = workspace;
        Kind = kind;
        this.close = close;
    }

    public ReservationWorkspaceViewModel Workspace { get; }
    public ReservationModalKind Kind { get; private set; }
    public bool IsNew => Kind == ReservationModalKind.New;
    public bool IsSearch => Kind == ReservationModalKind.Search;
    public bool IsDetail => Kind == ReservationModalKind.Edit;
    public bool IsAssignRoom => Kind == ReservationModalKind.AssignRoom;
    public bool IsConfirm => Kind == ReservationModalKind.Confirm;
    public bool IsCancel => Kind == ReservationModalKind.Cancel;
    public bool IsStatus => Kind == ReservationModalKind.Status;
    public bool IsOverlay => !IsNew;
    public string IconGlyph => Kind switch
    {
        ReservationModalKind.New => "\uE710",
        ReservationModalKind.Search => "\uE721",
        ReservationModalKind.Edit => "\uE70F",
        ReservationModalKind.AssignRoom => "\uE8B7",
        ReservationModalKind.Confirm => "\uE8FB",
        ReservationModalKind.Cancel => "\uE711",
        ReservationModalKind.Status => "\uE9D2",
        _ => "\uE721"
    };
    public string Title => Kind switch
    {
        ReservationModalKind.New => "Đặt phòng mới",
        ReservationModalKind.Search => "Tìm đặt phòng",
        ReservationModalKind.Edit => "Sửa đặt phòng",
        ReservationModalKind.AssignRoom => "Phân phòng",
        ReservationModalKind.Confirm => "Xác nhận đặt phòng",
        ReservationModalKind.Cancel => "Hủy đặt phòng",
        ReservationModalKind.Status => "Tình trạng đặt phòng",
        _ => "Tìm đặt phòng"
    };
    public string Subtitle => Kind switch
    {
        ReservationModalKind.New => "Tạo hồ sơ đặt phòng và thông tin lưu trú của khách",
        ReservationModalKind.Search => "Tra cứu theo số đặt phòng, tên khách hoặc ngày lưu trú",
        ReservationModalKind.Edit => "Xem và cập nhật thông tin đặt phòng",
        ReservationModalKind.AssignRoom => "Phân hoặc đổi phòng cho đặt phòng đã chọn",
        ReservationModalKind.Confirm => "Xác nhận các đặt phòng nháp hoặc tạm giữ",
        ReservationModalKind.Cancel => "Hủy đặt phòng và ghi nhận lý do",
        ReservationModalKind.Status => "Tổng quan tình trạng đặt phòng theo trạng thái",
        _ => "Tra cứu đặt phòng"
    };
    public string? ReservationNumber => Workspace.SelectedReservation?.ReservationNumber;
    public bool IsSavingNew => isSavingNew;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        // Edit is opened with a reservation that has already been loaded by
        // ReservationWorkspaceViewModel.OpenForEditAsync. Running the normal
        // search initialization here clears/replaces that selection and turns
        // the edit form into an empty search workspace.
        if (Kind == ReservationModalKind.Edit)
        {
            if (Workspace.SelectedReservation is null)
                Workspace.ErrorMessage = "Không có đặt phòng được chọn để chỉnh sửa.";
            return;
        }

        Workspace.Mode = Kind switch
        {
            ReservationModalKind.Search => WorkspaceMode.Search,
            ReservationModalKind.Status => WorkspaceMode.Status,
            ReservationModalKind.Confirm => WorkspaceMode.Confirmation,
            ReservationModalKind.Cancel => WorkspaceMode.Cancellation,
            ReservationModalKind.AssignRoom => WorkspaceMode.RoomAssignment,
            _ => WorkspaceMode.Search
        };
        switch (Kind)
        {
            case ReservationModalKind.New:
                await Workspace.NewAsync();
                break;
            case ReservationModalKind.Confirm:
                Workspace.SelectedStatus = ASMILE.BO.Core.FrontOffice.Reservation.ReservationStatus.Draft;
                await Workspace.LoadAsync();
                break;
            case ReservationModalKind.Cancel:
                Workspace.SelectedStatus = ASMILE.BO.Core.FrontOffice.Reservation.ReservationStatus.Confirmed;
                await Workspace.LoadAsync();
                break;
            default:
                await Workspace.LoadAsync();
                break;
        }
    }

    [RelayCommand]
    private void Close() => close();

    [RelayCommand]
    private async Task SaveNewAsync()
    {
        await CreateNewAsync();
    }

    public async Task<bool> CreateNewAsync()
    {
        if (!IsNew || isSavingNew || Workspace.IsSaving) return false;
        isSavingNew = true;
        OnPropertyChanged(nameof(IsSavingNew));
        try
        {
            await Workspace.SaveAsync();
            if (Workspace.HasError || Workspace.SelectedReservation is null) return false;
            if (Workspace.SelectedReservation.Status != ASMILE.BO.Core.FrontOffice.Reservation.ReservationStatus.Confirmed)
            {
                Workspace.ErrorMessage = "Đặt phòng chưa được xác nhận. Vui lòng thử lại trước khi kết thúc.";
                return false;
            }

            Workspace.InfoMessage = $"Tạo đặt phòng {Workspace.SelectedReservation.ReservationNumber} hoàn tất.";
            SetKind(ReservationModalKind.Edit);
            return true;
        }
        catch (Exception ex)
        {
            Workspace.ErrorMessage = $"Không thể tạo đặt phòng: {ex.Message}";
            return false;
        }
        finally
        {
            isSavingNew = false;
            OnPropertyChanged(nameof(IsSavingNew));
        }
    }

    [RelayCommand]
    private async Task OpenDetailAsync()
    {
        if (Workspace.SelectedReservation is null) return;
        await Workspace.LoadAsync();
        if (!Workspace.HasError) SetKind(ReservationModalKind.Edit);
    }

    [RelayCommand]
    private async Task OpenAssignAsync()
    {
        if (Workspace.SelectedReservation is null) return;
        await Workspace.LoadAsync();
        if (Workspace.HasError) return;
        await Workspace.OpenRoomPanelAsync();
        if (!Workspace.HasError) SetKind(ReservationModalKind.AssignRoom);
    }

    [RelayCommand]
    private async Task BackToSearchAsync()
    {
        SetKind(ReservationModalKind.Search);
        await Workspace.LoadAsync();
    }

    private void SetKind(ReservationModalKind kind)
    {
        Kind = kind;
        OnPropertyChanged(nameof(Kind));
        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(IsSearch));
        OnPropertyChanged(nameof(IsDetail));
        OnPropertyChanged(nameof(IsAssignRoom));
        OnPropertyChanged(nameof(IsConfirm));
        OnPropertyChanged(nameof(IsCancel));
        OnPropertyChanged(nameof(IsStatus));
        OnPropertyChanged(nameof(IsOverlay));
        OnPropertyChanged(nameof(IconGlyph));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Subtitle));
    }
}
