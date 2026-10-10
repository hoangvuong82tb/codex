using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class HousekeepingLinenInventoryRow
{
    public Guid RoomId { get; init; }
    public string? RoomNumber { get; init; }
    public string RoomDisplay => !string.IsNullOrWhiteSpace(RoomNumber) ? RoomNumber! : RoomId == Guid.Empty ? "—" : RoomId.ToString("N")[..8].ToUpperInvariant();
    public string LinenCode { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public int WashCycleCount { get; init; }
    public int DamagedCount { get; init; }
    public DateTime? LastRestockedUtc { get; init; }
    public string LastRestockedDisplay => LastRestockedUtc.HasValue ? LastRestockedUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Chưa ghi nhận";
}

public sealed class HousekeepingAmenityInventoryRow
{
    public Guid RoomId { get; init; }
    public string? RoomNumber { get; init; }
    public string RoomDisplay => !string.IsNullOrWhiteSpace(RoomNumber) ? RoomNumber! : RoomId == Guid.Empty ? "—" : RoomId.ToString("N")[..8].ToUpperInvariant();
    public string AmenityCode { get; init; } = string.Empty;
    public int CurrentStock { get; init; }
    public int ParLevel { get; init; }
    public int ConsumptionLast30Days { get; init; }
    public int Shortage => Math.Max(ParLevel - CurrentStock, 0);
    public bool IsLowStock { get; init; }
    public string StockStatus => IsLowStock ? "Dưới ngưỡng" : "Đủ tồn";
    public DateTime? LastRefillUtc { get; init; }
    public string LastRefillDisplay => LastRefillUtc.HasValue ? LastRefillUtc.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm") : "Chưa ghi nhận";
}

public sealed partial class HousekeepingSupplyViewModel : ViewModelBase
{
    private readonly ILinenService _linenService;
    private readonly IAmenityService _amenityService;
    private readonly IFrontOfficeExecutionContextAccessor _executionContext;

    public HousekeepingSupplyViewModel(ILinenService linenService, IAmenityService amenityService, IFrontOfficeExecutionContextAccessor executionContext)
    {
        _linenService = linenService;
        _amenityService = amenityService;
        _executionContext = executionContext;
    }

    public ObservableCollection<HousekeepingLinenInventoryRow> LinenInventory { get; } = new();
    public ObservableCollection<HousekeepingAmenityInventoryRow> AmenityInventory { get; } = new();
    public ObservableCollection<LinenType> LinenTypes { get; } = new();
    public ObservableCollection<AmenityType> AmenityTypes { get; } = new();
    public ObservableCollection<HousekeepingLinenInventoryRow> FilteredLinenInventory { get; } = new();
    public ObservableCollection<HousekeepingAmenityInventoryRow> FilteredAmenityInventory { get; } = new();

    [ObservableProperty] private HousekeepingAmenityInventoryRow? _selectedAmenity;
    [ObservableProperty] private HousekeepingLinenInventoryRow? _selectedLinen;
    [ObservableProperty] private LinenAction _selectedLinenAction = LinenAction.Restocked;
    [ObservableProperty] private int _linenQuantity = 1;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string _stockFilter = "Tất cả";

    public bool HasSelectedAmenity => SelectedAmenity is not null;
    public bool HasSelectedLinen => SelectedLinen is not null;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public static IReadOnlyList<LinenAction> LinenActionOptions { get; } = Enum.GetValues<LinenAction>();
    public static IReadOnlyList<string> StockFilters { get; } = ["Tất cả", "Dưới ngưỡng", "Đủ tồn"];
    public bool HasData => LinenInventory.Count > 0 || AmenityInventory.Count > 0;
    public int LinenLineCount => LinenInventory.Count;
    public int LinenUnitCount => LinenInventory.Sum(x => x.Quantity);
    public int AmenityLineCount => AmenityInventory.Count;
    public int LowStockCount => AmenityInventory.Count(x => x.IsLowStock);
    public int RefillRoomCount => AmenityInventory.Where(x => x.IsLowStock).Select(x => x.RoomId).Distinct().Count();
    public string DataMessage => HasData
        ? $"Đồ vải: {LinenInventory.Count} dòng tồn kho · Tiện nghi: {AmenityInventory.Count} dòng tồn kho"
        : "Chưa có dữ liệu tồn kho đồ vải/tiện nghi thật cho cơ sở hiện tại.";

    public async Task InitializeAsync(CancellationToken ct = default) => await RefreshAsync(ct);

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        LinenInventory.Clear();
        AmenityInventory.Clear();
        FilteredLinenInventory.Clear();
        FilteredAmenityInventory.Clear();
        LinenTypes.Clear();
        AmenityTypes.Clear();
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(DataMessage));
        try
        {
            var propertyId = _executionContext.Current?.Property.PropertyId.Value;
            if (propertyId is not Guid id || id == Guid.Empty)
            {
                ErrorMessage = "Chưa chọn cơ sở Front Office; không thể đọc tồn kho.";
                return;
            }

            // Linen and amenity services share the scoped EF DbContext. Keep these
            // reads sequential so refresh cannot start concurrent operations on it.
            var linenTypes = await _linenService.GetLinenTypesAsync(ct);
            var amenityTypes = await _amenityService.GetAmenityTypesAsync(ct);
            var linenInventory = await _linenService.GetInventoryAsync(id, null, ct);
            var amenityInventory = await _amenityService.GetInventoryAsync(id, null, ct);

            foreach (var item in linenTypes) LinenTypes.Add(item);
            foreach (var item in amenityTypes) AmenityTypes.Add(item);
            foreach (var item in linenInventory)
                    LinenInventory.Add(new HousekeepingLinenInventoryRow { RoomId = item.RoomId, RoomNumber = item.RoomNumber, LinenCode = item.LinenCode, Quantity = item.Quantity, WashCycleCount = item.WashCycleCount, DamagedCount = item.DamagedCount, LastRestockedUtc = item.LastRestockedUtc });

            var thresholds = AmenityTypes.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);
            foreach (var item in amenityInventory)
            {
                var threshold = thresholds.TryGetValue(item.AmenityCode, out var type) ? type.MinAlertThreshold : 0;
                AmenityInventory.Add(new HousekeepingAmenityInventoryRow { RoomId = item.RoomId, RoomNumber = item.RoomNumber, AmenityCode = item.AmenityCode, CurrentStock = item.CurrentStock, ParLevel = item.ParLevel, ConsumptionLast30Days = item.ConsumptionLast30Days, IsLowStock = item.CurrentStock <= threshold, LastRefillUtc = item.LastRefillUtc });
            }
            InfoMessage = HasData ? "Đã tải dữ liệu tồn kho từ cơ sở dữ liệu." : "Không có dòng tồn kho thật; màn hình không tự tạo dữ liệu mẫu.";
            ApplyFilters();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            InfoMessage = "Đã hủy tải dữ liệu tồn kho.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải tồn kho: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(HasData));
            OnPropertyChanged(nameof(DataMessage));
            OnPropertyChanged(nameof(LinenLineCount));
            OnPropertyChanged(nameof(LinenUnitCount));
            OnPropertyChanged(nameof(AmenityLineCount));
            OnPropertyChanged(nameof(LowStockCount));
            OnPropertyChanged(nameof(RefillRoomCount));
        }
    }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = string.Empty;
        StockFilter = "Tất cả";
        ApplyFilters();
    }

    [RelayCommand(CanExecute = nameof(HasSelectedAmenity))]
    private async Task RequestRefillAsync(CancellationToken ct)
    {
        if (SelectedAmenity is null) return;
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            var request = await _amenityService.CreateRefillRequestAsync(SelectedAmenity.RoomId, ct);
            var resultMessage = request.Items.Count == 0 ? "Phòng đã đủ tồn hoặc chưa có tiện nghi dưới ngưỡng; không tạo yêu cầu mới." : $"Đã tạo yêu cầu cấp bù thật cho {request.Items.Count} loại tiện nghi.";
            await RefreshAsync(ct);
            InfoMessage = resultMessage;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            InfoMessage = "Đã hủy tạo yêu cầu cấp bù.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tạo yêu cầu cấp bù: {ex.Message}";
        }
        finally { IsLoading = false; }
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLinen))]
    private async Task RecordLinenTransactionAsync(CancellationToken ct)
    {
        if (SelectedLinen is null) return;
        if (LinenQuantity <= 0)
        {
            ErrorMessage = "Số lượng đồ vải phải lớn hơn 0.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            var actorId = _executionContext.Current?.Actor.ActorId?.Value;
            await _linenService.RecordLinenTransactionAsync(
                new LinenTransaction(
                    Guid.NewGuid(),
                    SelectedLinen.LinenCode,
                    SelectedLinen.RoomId,
                    SelectedLinenAction,
                    LinenQuantity,
                    DateTime.UtcNow,
                    actorId),
                ct);
            var roomNumber = SelectedLinen.RoomDisplay;
            var action = SelectedLinenAction;
            await RefreshAsync(ct);
            InfoMessage = $"Đã ghi nhận {action} {LinenQuantity} đơn vị cho phòng {roomNumber}.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            InfoMessage = "Đã hủy ghi nhận biến động đồ vải.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể ghi nhận biến động đồ vải: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedAmenityChanged(HousekeepingAmenityInventoryRow? value)
    {
        OnPropertyChanged(nameof(HasSelectedAmenity));
        RequestRefillCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedLinenChanged(HousekeepingLinenInventoryRow? value)
    {
        OnPropertyChanged(nameof(HasSelectedLinen));
        RecordLinenTransactionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();

    partial void OnStockFilterChanged(string value) => ApplyFilters();

    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));

    private void ApplyFilters()
    {
        var keyword = SearchText.Trim();
        var linenRows = LinenInventory.Where(row =>
            string.IsNullOrWhiteSpace(keyword)
            || row.RoomDisplay.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
            || row.LinenCode.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));
        var amenityRows = AmenityInventory.Where(row =>
            string.IsNullOrWhiteSpace(keyword)
            || row.RoomDisplay.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
            || row.AmenityCode.Contains(keyword, StringComparison.CurrentCultureIgnoreCase));

        if (StockFilter == "Dưới ngưỡng")
            amenityRows = amenityRows.Where(row => row.IsLowStock);
        else if (StockFilter == "Đủ tồn")
            amenityRows = amenityRows.Where(row => !row.IsLowStock);

        FilteredLinenInventory.Clear();
        foreach (var row in linenRows) FilteredLinenInventory.Add(row);
        FilteredAmenityInventory.Clear();
        foreach (var row in amenityRows) FilteredAmenityInventory.Add(row);
    }
}
