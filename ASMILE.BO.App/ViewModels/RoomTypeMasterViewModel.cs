using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

/// <summary>
/// Real-data editor for hotel room types. This is deliberately separate from
/// RoomMasterViewModel: room lookup/operations belong to the FO workspace,
/// while room-type setup belongs to the Settings master-data workspace.
/// </summary>
public sealed partial class RoomTypeMasterViewModel : ViewModelBase
{
    private readonly IHotelRoomMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelRoomTypeCommandHandler? _createHandler;
    private readonly UpdateHotelRoomTypeCommandHandler? _updateHandler;
    private readonly ActivateHotelRoomTypeCommandHandler? _activateHandler;
    private readonly DeactivateHotelRoomTypeCommandHandler? _deactivateHandler;
    private bool _initialized;

    public RoomTypeMasterViewModel(
        IHotelRoomMasterDataRepository? repository = null,
        IFrontOfficeExecutionContextAccessor? contextAccessor = null,
        CreateHotelRoomTypeCommandHandler? createHandler = null,
        UpdateHotelRoomTypeCommandHandler? updateHandler = null,
        ActivateHotelRoomTypeCommandHandler? activateHandler = null,
        DeactivateHotelRoomTypeCommandHandler? deactivateHandler = null)
    {
        _repository = repository;
        _contextAccessor = contextAccessor;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _activateHandler = activateHandler;
        _deactivateHandler = deactivateHandler;
    }

    public ObservableCollection<HotelRoomTypeDto> RoomTypes { get; } = [];

    [ObservableProperty] private HotelRoomTypeDto? selectedRoomType;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private int standardAdultCapacity = 2;
    [ObservableProperty] private int standardChildCapacity;
    [ObservableProperty] private int maximumAdultCapacity = 2;
    [ObservableProperty] private int maximumChildCapacity;
    [ObservableProperty] private int defaultOccupancy = 1;
    [ObservableProperty] private int displayOrder;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasInfo => !string.IsNullOrWhiteSpace(InfoMessage);
    public bool HasSelection => SelectedRoomType is not null;
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedStatus => SelectedRoomType?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";
    public string SelectedSummary => SelectedRoomType is null
        ? "Chọn một hạng phòng để xem và chỉnh sửa."
        : $"{SelectedRoomType.Code} · {SelectedRoomType.Name}";

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (_initialized) return;
        _initialized = true;
        await LoadAsync(ct);
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty)
            {
                RoomTypes.Clear();
                ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context để tải hạng phòng.";
                return;
            }

            var result = await _repository.SearchRoomTypesAsync(
                propertyId,
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                null,
                new PageRequest(1, 200),
                "displayOrder",
                ct);
            RoomTypes.Clear();
            foreach (var item in result.Items)
                RoomTypes.Add(item);

            if (SelectedRoomType is null || RoomTypes.All(x => x.HotelRoomTypeId != SelectedRoomType.HotelRoomTypeId))
                SelectedRoomType = RoomTypes.FirstOrDefault();
            InfoMessage = $"Đã tải {RoomTypes.Count} hạng phòng từ dữ liệu thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không tải được hạng phòng: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            RaiseState();
        }
    }

    [RelayCommand]
    private async Task SearchAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private void NewRoomType()
    {
        SelectedRoomType = null;
        Code = string.Empty;
        Name = string.Empty;
        Description = string.Empty;
        StandardAdultCapacity = 2;
        StandardChildCapacity = 0;
        MaximumAdultCapacity = 2;
        MaximumChildCapacity = 0;
        DefaultOccupancy = 1;
        DisplayOrder = RoomTypes.Count + 1;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        IsEditing = true;
        RaiseState();
    }

    [RelayCommand]
    private void EditRoomType()
    {
        if (SelectedRoomType is null) return;
        Code = SelectedRoomType.Code;
        Name = SelectedRoomType.Name;
        Description = SelectedRoomType.Description ?? string.Empty;
        StandardAdultCapacity = SelectedRoomType.StandardAdultCapacity;
        StandardChildCapacity = SelectedRoomType.StandardChildCapacity;
        MaximumAdultCapacity = SelectedRoomType.MaximumAdultCapacity;
        MaximumChildCapacity = SelectedRoomType.MaximumChildCapacity;
        DefaultOccupancy = SelectedRoomType.DefaultOccupancy;
        DisplayOrder = SelectedRoomType.DisplayOrder;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        IsEditing = true;
        RaiseState();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        RaiseState();
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Mã và tên hạng phòng là bắt buộc.";
            RaiseState();
            return;
        }

        var validation = ValidateCapacity();
        if (validation is not null)
        {
            ErrorMessage = validation;
            RaiseState();
            return;
        }

        if (_createHandler is null || _updateHandler is null)
        {
            ErrorMessage = "Chưa đăng ký command hạng phòng thật.";
            RaiseState();
            return;
        }

        try
        {
            if (SelectedRoomType is null)
            {
                var result = await _createHandler.HandleAsync(
                    new CreateHotelRoomTypeCommand(Code.Trim(), Name.Trim(), NullIfEmpty(Description),
                        StandardAdultCapacity, StandardChildCapacity, MaximumAdultCapacity,
                        MaximumChildCapacity, DefaultOccupancy, DisplayOrder), ct);
                if (result.IsFailure)
                {
                    ErrorMessage = result.Error?.Message ?? "Không tạo được hạng phòng.";
                    return;
                }
                InfoMessage = $"Đã thêm hạng phòng {result.Value!.Code}.";
            }
            else
            {
                var result = await _updateHandler.HandleAsync(
                    new UpdateHotelRoomTypeCommand(SelectedRoomType.HotelRoomTypeId, Code.Trim(), Name.Trim(), NullIfEmpty(Description),
                        StandardAdultCapacity, StandardChildCapacity, MaximumAdultCapacity,
                        MaximumChildCapacity, DefaultOccupancy, DisplayOrder, SelectedRoomType.RowVersion ?? []), ct);
                if (result.IsFailure)
                {
                    ErrorMessage = result.Error?.Message ?? "Không cập nhật được hạng phòng.";
                    return;
                }
                InfoMessage = $"Đã cập nhật hạng phòng {result.Value!.Code}.";
            }

            IsEditing = false;
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không lưu được hạng phòng: {ex.Message}";
        }
        finally
        {
            RaiseState();
        }
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedRoomType is null) return;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            var result = SelectedRoomType.IsActive
                ? await (_deactivateHandler?.HandleAsync(new DeactivateHotelRoomTypeCommand(SelectedRoomType.HotelRoomTypeId, SelectedRoomType.RowVersion ?? []), ct)
                    ?? Task.FromResult<Result<HotelRoomTypeDto>>(Result<HotelRoomTypeDto>.Failure(Error.System("HOTEL_ROOM_TYPE_COMMAND_MISSING", "Command is unavailable."))))
                : await (_activateHandler?.HandleAsync(new ActivateHotelRoomTypeCommand(SelectedRoomType.HotelRoomTypeId, SelectedRoomType.RowVersion ?? []), ct)
                    ?? Task.FromResult<Result<HotelRoomTypeDto>>(Result<HotelRoomTypeDto>.Failure(Error.System("HOTEL_ROOM_TYPE_COMMAND_MISSING", "Command is unavailable."))));
            if (result.IsFailure)
            {
                ErrorMessage = result.Error?.Message ?? "Không đổi được trạng thái hạng phòng.";
                return;
            }
            InfoMessage = result.Value!.IsActive ? "Đã bật hạng phòng." : "Đã tạm ngưng hạng phòng.";
            await LoadAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không đổi được trạng thái: {ex.Message}";
        }
        finally
        {
            RaiseState();
        }
    }

    partial void OnSelectedRoomTypeChanged(HotelRoomTypeDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(IsReadOnlyPanelVisible));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(SelectedSummary));
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            _ = LoadAsync(CancellationToken.None);
    }

    private string? ValidateCapacity()
    {
        if (StandardAdultCapacity < 0 || StandardChildCapacity < 0 || MaximumAdultCapacity < 0 || MaximumChildCapacity < 0 || DefaultOccupancy < 0)
            return "Sức chứa không được âm.";
        if (MaximumAdultCapacity < StandardAdultCapacity || MaximumChildCapacity < StandardChildCapacity)
            return "Sức chứa tối đa phải lớn hơn hoặc bằng sức chứa tiêu chuẩn.";
        if (DefaultOccupancy > MaximumAdultCapacity + MaximumChildCapacity)
            return "Số khách mặc định vượt quá sức chứa tối đa.";
        return null;
    }

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void RaiseState()
    {
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasInfo));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(SelectedStatus));
        OnPropertyChanged(nameof(SelectedSummary));
    }
}
