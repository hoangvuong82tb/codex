using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class StockReceiptsViewModel : ViewModelBase
{
    public enum ReceiptTabMode
    {
        Create = 0,
        Pending = 1,
        Approved = 2
    }

    private readonly IInventoryAppService _inventoryService;
    private readonly IItemAppService _itemService;
    private readonly ISupplierAppService _supplierService;
    private readonly IPersonAppService _personService;
    private readonly IWarehouseAppService _warehouseService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IWindowManagerService _windowManagerService;
    private readonly Dictionary<int, ItemDto> _itemById = new();
    private CancellationTokenSource? _loadCts;
    private bool _isSyncingDetailAmounts;

    public ObservableCollection<StockReceiptDto> Receipts { get; } = new();
    public ObservableCollection<StockReceiptDto> FilteredReceipts { get; } = new();
    public TablePagerViewModel<StockReceiptDto> ReceiptPager { get; } = new() { ItemName = "phiếu nhập" };
    public ObservableCollection<TransactionDetailRow> Details { get; } = new();
    public ObservableCollection<LookupItemDto> ItemLookup { get; } = new();
    public ObservableCollection<LookupItemDto> FilteredItemLookup { get; } = new();
    public ObservableCollection<LookupItemDto> SupplierLookup { get; } = new();
    public ObservableCollection<LookupItemDto> FilteredSupplierLookup { get; } = new();
    public ObservableCollection<LookupItemDto> EmployeeLookup { get; } = new();
    public ObservableCollection<LookupItemDto> FilteredEmployeeLookup { get; } = new();
    public ObservableCollection<LookupItemDto> WarehouseLookup { get; } = new();
    public ObservableCollection<string> ValidationMessages { get; } = new();
    public ObservableCollection<decimal> VatRateOptions { get; } = new() { 0m, 5m, 8m, 10m };

    [ObservableProperty]
    private StockReceiptDto? selectedReceipt;

    [ObservableProperty]
    private TransactionDetailRow? selectedDetail;

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private DateTime? filterFromDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime? filterToDate = DateTime.Today;

    [ObservableProperty]
    private ReceiptTabMode activeTab = ReceiptTabMode.Create;

    [ObservableProperty]
    private int editingId;

    [ObservableProperty]
    private string receiptNo = string.Empty;

    [ObservableProperty]
    private DateTime receiptDate = DateTime.Today;

    [ObservableProperty]
    private string supplierCode = string.Empty;

    [ObservableProperty]
    private bool isSupplierDropdownOpen;

    [ObservableProperty]
    private string supplierName = string.Empty;

    [ObservableProperty]
    private string receiverName = string.Empty;

    [ObservableProperty]
    private DateTime? expiryDate;

    [ObservableProperty]
    private bool isReceiverDropdownOpen;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string detailProductName = string.Empty;

    [ObservableProperty]
    private int? detailProductItemId;

    [ObservableProperty]
    private bool isDetailProductDropdownOpen;

    [ObservableProperty]
    private decimal detailQuantity;

    [ObservableProperty]
    private string detailQuantityInput = "0";

    [ObservableProperty]
    private decimal detailUnitPrice;

    [ObservableProperty]
    private decimal detailAmount;

    [ObservableProperty]
    private decimal detailVatRate = 10m;

    [ObservableProperty]
    private bool detailPriceIncludesVat = true;

    [ObservableProperty]
    private int? detailWarehouseId;

    [ObservableProperty]
    private int warehouseId;

    [ObservableProperty]
    private string debitAccountCode = "152";

    [ObservableProperty]
    private string creditAccountCode = "331";

    [ObservableProperty]
    private bool isApproved;

    [ObservableProperty]
    private DateTime? postedAt;

    [ObservableProperty]
    private string? postingStatus;

    public decimal TotalQuantity => Details.Sum(x => x.Quantity);
    public decimal TotalAmount => Details.Sum(x => x.Amount);
    public decimal VatAmount => Details.Sum(x => x.VatAmount);
    public decimal InventoryValue => TotalAmount + VatAmount;
    public bool IsReversed => string.Equals(PostingStatus, "Reversed", StringComparison.OrdinalIgnoreCase);
    public bool CanApprove => EditingId > 0 && !IsApproved && !IsReversed;
    public bool CanReverse => EditingId > 0 && IsPosted;
    public bool CanCancelApproval => EditingId > 0 && IsApproved && !IsPosted && !IsReversed;
    public bool HasValidationMessages => ValidationMessages.Count > 0;
    public bool IsPosted => !IsReversed && (PostedAt.HasValue || string.Equals(PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase));
    public string CurrentWorkflowStatusText => EditingId <= 0 ? "Phiếu mới" : IsReversed ? "Đã đảo" : IsPosted ? "Đã ghi sổ" : IsApproved ? "Đã duyệt" : "Chờ duyệt";
    public bool IsCreateTabSelected => ActiveTab == ReceiptTabMode.Create;
    public bool IsPendingTabSelected => ActiveTab == ReceiptTabMode.Pending;
    public bool IsApprovedTabSelected => ActiveTab == ReceiptTabMode.Approved;
    public bool IsListTabSelected => !IsCreateTabSelected;

    public StockReceiptsViewModel(
        IInventoryAppService inventoryService,
        IItemAppService itemService,
        ISupplierAppService supplierService,
        IPersonAppService personService,
        IWarehouseAppService warehouseService,
        INotificationService notificationService,
        IDialogService dialogService,
        IWindowManagerService windowManagerService)
    {
        _inventoryService = inventoryService;
        _itemService = itemService;
        _supplierService = supplierService;
        _personService = personService;
        _warehouseService = warehouseService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _windowManagerService = windowManagerService;

        New();
        _ = LoadAsync();
    }

    public void OpenReceiptDetailModule(StockReceiptDto receipt)
    {
        var moduleKey = $"receipt-detail-{receipt.Id}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            moduleKey,
            $"Chi tiết {receipt.ReceiptNo}",
            "\uE8A7",
            "Chi tiết phiếu nhập",
            "#2F65D9",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\import.ico");

        var vm = new StockReceiptDetailModuleViewModel(receipt, _windowManagerService, moduleKey);
        vm.RequestClose += () =>
        {
            var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
            if (opened is not null)
            {
                _windowManagerService.Close(opened);
            }
        };

        _windowManagerService.OpenOrActivate(module, () => vm);
    }

    partial void OnSelectedReceiptChanged(StockReceiptDto? value)
    {
        if (value is null) return;
        LoadFromSelected(value);
    }

    partial void OnSelectedDetailChanged(TransactionDetailRow? value)
    {
        if (value is null)
        {
            return;
        }

        DetailProductItemId = value.ItemId > 0 ? value.ItemId : null;
        DetailProductName = value.ItemName;
        DetailQuantity = value.Quantity;
        DetailQuantityInput = value.Quantity.ToString("0.####", CultureInfo.InvariantCulture);
        DetailAmount = value.Amount;
        DetailVatRate = value.VatRate;
        DetailPriceIncludesVat = true;
        DetailUnitPrice = GetGrossUnitPrice(value.UnitPrice, value.VatRate);
        DetailAmount = value.TotalWithVat;
        DetailWarehouseId = value.WarehouseId;
        IsDetailProductDropdownOpen = false;
    }

    partial void OnActiveTabChanged(ReceiptTabMode value)
    {
        OnPropertyChanged(nameof(IsCreateTabSelected));
        OnPropertyChanged(nameof(IsPendingTabSelected));
        OnPropertyChanged(nameof(IsApprovedTabSelected));
        OnPropertyChanged(nameof(IsListTabSelected));
        ApplyTabFilter();
    }

    partial void OnFilterFromDateChanged(DateTime? value)
    {
        if (IsBusy)
        {
            return;
        }

        if (FilterToDate.HasValue && value.HasValue && value.Value.Date > FilterToDate.Value.Date)
        {
            FilterToDate = value;
        }

        _ = LoadAsync();
    }

    partial void OnFilterToDateChanged(DateTime? value)
    {
        if (IsBusy)
        {
            return;
        }

        if (FilterFromDate.HasValue && value.HasValue && value.Value.Date < FilterFromDate.Value.Date)
        {
            FilterFromDate = value;
        }

        _ = LoadAsync();
    }

    partial void OnEditingIdChanged(int value)
    {
        ApproveCommand.NotifyCanExecuteChanged();
        ReverseCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsApprovedChanged(bool value)
    {
        ApproveCommand.NotifyCanExecuteChanged();
        ReverseCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CurrentWorkflowStatusText));
    }

    partial void OnPostedAtChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(IsPosted));
        OnPropertyChanged(nameof(IsReversed));
        ReverseCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CurrentWorkflowStatusText));
    }

    partial void OnPostingStatusChanged(string? value)
    {
        OnPropertyChanged(nameof(IsPosted));
        OnPropertyChanged(nameof(IsReversed));
        ApproveCommand.NotifyCanExecuteChanged();
        ReverseCommand.NotifyCanExecuteChanged();
        OnPropertyChanged(nameof(CurrentWorkflowStatusText));
    }

    partial void OnWarehouseIdChanged(int value)
    {
        if (value <= 0)
        {
            return;
        }

        foreach (var row in Details.Where(x => !x.WarehouseId.HasValue || x.WarehouseId <= 0))
        {
            row.WarehouseId = value;
        }
    }

    partial void OnSupplierCodeChanged(string value)
    {
        ApplySupplierFilter(value);
        IsSupplierDropdownOpen = FilteredSupplierLookup.Count > 0;

        var match = SupplierLookup.FirstOrDefault(x =>
            string.Equals(x.Code, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Display, value, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            SupplierName = match.Name;
            IsSupplierDropdownOpen = false;
        }
        else if (string.IsNullOrWhiteSpace(value))
        {
            SupplierName = string.Empty;
        }
    }

    partial void OnReceiverNameChanged(string value)
    {
        ApplyEmployeeFilter(value);
        IsReceiverDropdownOpen = FilteredEmployeeLookup.Count > 0;

        var match = EmployeeLookup.FirstOrDefault(x =>
            string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Display, value, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            IsReceiverDropdownOpen = false;
        }
    }

    partial void OnDetailProductNameChanged(string value)
    {
        ApplyItemFilter(value);
        IsDetailProductDropdownOpen = !string.IsNullOrWhiteSpace(value) && FilteredItemLookup.Count > 0;
    }

    partial void OnDetailProductItemIdChanged(int? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        var selected = ItemLookup.FirstOrDefault(x => x.Id == value.Value);
        if (selected is null)
        {
            return;
        }

        // Chỉ cập nhật khi người dùng chọn item từ dropdown.
        DetailProductName = selected.Name;
        IsDetailProductDropdownOpen = false;
    }

    partial void OnDetailQuantityInputChanged(string value)
    {
        if (TryParseFlexibleDecimal(value, out var parsedQuantity) && parsedQuantity >= 0)
        {
            DetailQuantity = parsedQuantity;
        }
    }

    partial void OnDetailQuantityChanged(decimal value)
    {
        RecalculateDetailAmountFromUnitPrice();
    }

    partial void OnDetailUnitPriceChanged(decimal value)
    {
        RecalculateDetailAmountFromUnitPrice();
    }

    partial void OnDetailVatRateChanged(decimal value)
    {
        RecalculateDetailAmountFromUnitPrice();
    }

    partial void OnDetailPriceIncludesVatChanged(bool value)
    {
        RecalculateDetailAmountFromUnitPrice();
    }

    partial void OnDetailAmountChanged(decimal value)
    {
        if (_isSyncingDetailAmounts)
        {
            return;
        }

        if (DetailQuantity <= 0)
        {
            return;
        }

        _isSyncingDetailAmounts = true;
        DetailUnitPrice = Math.Round(value / DetailQuantity, 4, MidpointRounding.AwayFromZero);
        _isSyncingDetailAmounts = false;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var token = _loadCts.Token;
        IsBusy = true;
        try
        {
            var items = await _itemService.GetLookupAsync();
            ItemLookup.Clear();
            foreach (var item in items) ItemLookup.Add(item);
            ApplyItemFilter(DetailProductName);

            _itemById.Clear();
            var itemDetails = await _itemService.SearchAsync(string.Empty);
            foreach (var item in itemDetails)
            {
                _itemById[item.Id] = item;
            }

            var suppliers = await _supplierService.GetLookupAsync();
            SupplierLookup.Clear();
            foreach (var supplier in suppliers) SupplierLookup.Add(supplier);
            ApplySupplierFilter(SupplierCode);

            var people = await _personService.SearchAsync(null, string.Empty, true);
            var employees = people
                .OrderBy(x => x.FullName)
                .Select(x => new LookupItemDto
                {
                    Id = x.Id,
                    Code = x.Id.ToString(),
                    Name = x.FullName
                })
                .ToList();
            EmployeeLookup.Clear();
            foreach (var employee in employees) EmployeeLookup.Add(employee);
            ApplyEmployeeFilter(ReceiverName);

            var warehouses = await _warehouseService.GetLookupAsync();
            WarehouseLookup.Clear();
            foreach (var warehouse in warehouses) WarehouseLookup.Add(warehouse);

            var rows = await _inventoryService.SearchReceiptsAsync(FilterFromDate, FilterToDate, Keyword, null, token);
            Receipts.Clear();
            foreach (var row in rows) Receipts.Add(row);
            ApplyTabFilter();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void New()
    {
        EditingId = 0;
        ReceiptNo = GenerateNextReceiptNo();
        ReceiptDate = DateTime.Today;
        SupplierCode = string.Empty;
        SupplierName = string.Empty;
        ReceiverName = string.Empty;
        ExpiryDate = null;
        IsReceiverDropdownOpen = false;
        Description = string.Empty;
        DetailProductName = string.Empty;
        DetailProductItemId = null;
        DetailQuantity = 0;
        DetailQuantityInput = "0";
        DetailUnitPrice = 0;
        DetailAmount = 0;
        DetailVatRate = 10m;
        DetailPriceIncludesVat = true;
        DetailWarehouseId = null;
        WarehouseId = WarehouseLookup.FirstOrDefault()?.Id ?? 0;
        DebitAccountCode = "152";
        CreditAccountCode = "331";
        IsApproved = false;
        PostedAt = null;
        PostingStatus = null;
        ClearValidationMessages();
        Details.Clear();
        OnPropertyChanged(nameof(TotalAmount));
        NotifyTotalsChanged();
        OnPropertyChanged(nameof(CurrentWorkflowStatusText));
    }

    private string GenerateNextReceiptNo()
    {
        var nextSequence = GetNextReceiptSequence();
        var randomLetters = GetRandomLetters(2);
        return $"PN-{randomLetters}{nextSequence:00000}";
    }

    private int GetNextReceiptSequence()
    {
        var pattern = new Regex(@"^PN-[A-Z]{2}(?<seq>\d{5,6})$", RegexOptions.Compiled);
        var maxSequence = 0;

        foreach (var receipt in Receipts)
        {
            if (string.IsNullOrWhiteSpace(receipt.ReceiptNo))
            {
                continue;
            }

            var match = pattern.Match(receipt.ReceiptNo.Trim().ToUpperInvariant());
            if (!match.Success)
            {
                continue;
            }

            if (int.TryParse(match.Groups["seq"].Value, out var sequence) && sequence > maxSequence)
            {
                maxSequence = sequence;
            }
        }

        return maxSequence + 1;
    }

    private static string GetRandomLetters(int length)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        Span<char> chars = stackalloc char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = letters[Random.Shared.Next(letters.Length)];
        }

        return new string(chars);
    }

    [RelayCommand]
    private void ShowCreateTab() => ActiveTab = ReceiptTabMode.Create;

    [RelayCommand]
    private void ShowPendingTab() => ActiveTab = ReceiptTabMode.Pending;

    [RelayCommand]
    private void ShowApprovedTab() => ActiveTab = ReceiptTabMode.Approved;

    [RelayCommand]
    private void AddDetail(TransactionDetailRow? anchorRow = null)
    {
        var detail = new TransactionDetailRow
        {
            Quantity = 1,
            UnitPrice = 0,
            VatRate = 10m,
            WarehouseId = WarehouseId
        };
        AttachDetailRow(detail);
        if (anchorRow is null)
        {
            Details.Add(detail);
        }
        else
        {
            var index = Details.IndexOf(anchorRow);
            if (index < 0)
            {
                Details.Add(detail);
            }
            else
            {
                Details.Insert(index + 1, detail);
            }
        }
        SelectedDetail = detail;
        ReindexDetailLines();
    }

    public void PasteExcelRows(string clipboardText)
    {
        var lines = (clipboardText ?? string.Empty)
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            var cells = line.Split('\t');
            if (cells.Length < 3)
            {
                continue;
            }

            var itemText = cells[0].Trim();
            var item = ItemLookup.FirstOrDefault(x =>
                string.Equals(x.Code, itemText, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Name, itemText, StringComparison.OrdinalIgnoreCase));
            if (item is null)
            {
                continue;
            }

            _ = TryParseFlexibleDecimal(cells.ElementAtOrDefault(2), out var quantity);
            _ = TryParseFlexibleDecimal(cells.ElementAtOrDefault(3), out var unitPrice);
            var warehouseText = cells.ElementAtOrDefault(4)?.Trim();
            var warehouse = WarehouseLookup.FirstOrDefault(x =>
                string.Equals(x.Code, warehouseText, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x.Name, warehouseText, StringComparison.OrdinalIgnoreCase));

            var row = new TransactionDetailRow
            {
                ItemId = item.Id,
                ItemCode = item.Code,
                ItemName = item.Name,
                Unit = _itemById.TryGetValue(item.Id, out var itemDetail) ? itemDetail.Unit : string.Empty,
                WarehouseId = warehouse?.Id ?? (WarehouseId > 0 ? WarehouseId : null),
                WarehouseCode = warehouse?.Code ?? WarehouseLookup.FirstOrDefault(x => x.Id == WarehouseId)?.Code ?? string.Empty,
                Quantity = quantity,
                UnitPrice = unitPrice,
                VatRate = 10m
            };
            AttachDetailRow(row);
            Details.Add(row);
        }

        ReindexDetailLines();
        NotifyTotalsChanged();
        ValidateCurrentReceipt(showToast: false);
    }

    [RelayCommand]
    private void RemoveDetail(TransactionDetailRow? row = null)
    {
        var target = row ?? SelectedDetail;
        if (target is null) return;
        Details.Remove(target);
        SelectedDetail = Details.LastOrDefault();
        ReindexDetailLines();
        NotifyTotalsChanged();
    }

    [RelayCommand]
    private void AddDetailFromForm()
    {
        var productName = DetailProductName.Trim();
        if (string.IsNullOrWhiteSpace(productName))
        {
            _notificationService.Show("Nhập tên sản phẩm trước khi thêm dòng.");
            return;
        }

        if (!TryParseFlexibleDecimal(DetailQuantityInput, out var parsedQuantity) || parsedQuantity <= 0)
        {
            _notificationService.Show("Số lượng không hợp lệ. Ví dụ: 2.5 hoặc 2,5");
            return;
        }
        DetailQuantity = parsedQuantity;

        if (DetailUnitPrice < 0)
        {
            _notificationService.Show("Đơn giá không hợp lệ.");
            return;
        }

        var selectedItem = DetailProductItemId.HasValue
            ? ItemLookup.FirstOrDefault(x => x.Id == DetailProductItemId.Value)
            : ItemLookup.FirstOrDefault(x =>
                string.Equals(x.Name, productName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Display, productName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Code, productName, StringComparison.OrdinalIgnoreCase));

        if (selectedItem is null)
        {
            _notificationService.Show("Tên sản phẩm chưa có trong danh mục hàng hóa.");
            return;
        }

        int? targetWarehouseId = DetailWarehouseId ?? (WarehouseId > 0 ? WarehouseId : null);
        if (!targetWarehouseId.HasValue || targetWarehouseId.Value <= 0)
        {
            _notificationService.Show("Chọn kho nhập cho dòng hàng.");
            return;
        }

        var existingRow = SelectedDetail;
        if (existingRow is not null && Details.Contains(existingRow))
        {
            existingRow.ItemId = selectedItem.Id;
            existingRow.ItemCode = selectedItem.Code;
            existingRow.ItemName = selectedItem.Name;
            existingRow.WarehouseId = targetWarehouseId.Value;
            existingRow.Quantity = DetailQuantity;
            existingRow.UnitPrice = GetNetUnitPriceFromDetailInput();
            existingRow.VatRate = DetailVatRate;
            if (_itemById.TryGetValue(existingRow.ItemId, out var existingItemDetail))
            {
                existingRow.Unit = existingItemDetail.Unit;
            }
            var existingWarehouse = WarehouseLookup.FirstOrDefault(x => x.Id == existingRow.WarehouseId);
            existingRow.WarehouseCode = existingWarehouse?.Code ?? string.Empty;
            _notificationService.Show("Đã cập nhật dòng đã chọn.");
        }
        else
        {
            var row = new TransactionDetailRow
            {
                ItemId = selectedItem.Id,
                ItemCode = selectedItem.Code,
                ItemName = selectedItem.Name,
                WarehouseId = targetWarehouseId.Value,
                Quantity = DetailQuantity,
                UnitPrice = GetNetUnitPriceFromDetailInput(),
                VatRate = DetailVatRate
            };

            if (_itemById.TryGetValue(row.ItemId, out var itemDetail))
            {
                row.Unit = itemDetail.Unit;
            }

            var warehouse = WarehouseLookup.FirstOrDefault(x => x.Id == row.WarehouseId);
            row.WarehouseCode = warehouse?.Code ?? string.Empty;

            AttachDetailRow(row);
            Details.Add(row);
            SelectedDetail = row;
        }

        ReindexDetailLines();
        NotifyTotalsChanged();

        SelectedDetail = null;
        DetailProductName = string.Empty;
        DetailProductItemId = null;
        DetailQuantity = 0;
        DetailQuantityInput = "0";
        DetailUnitPrice = 0;
        DetailAmount = 0;
        DetailVatRate = 10m;
        DetailPriceIncludesVat = true;
        DetailWarehouseId = null;
        IsDetailProductDropdownOpen = false;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (!ValidateCurrentReceipt())
            {
                return;
            }

            var dto = new StockReceiptDto
            {
                Id = EditingId,
                ReceiptNo = ReceiptNo,
                ReceiptDate = ReceiptDate,
                SupplierCode = SupplierCode,
                SupplierName = SupplierName,
                ReceiverName = ReceiverName,
                Description = Description,
                WarehouseId = WarehouseId,
                DebitAccountCode = DebitAccountCode,
                CreditAccountCode = CreditAccountCode,
                IsApproved = IsApproved,
                Details = Details.Select(x => x.ToDto()).ToList()
            };

            var saved = await _inventoryService.SaveReceiptAsync(dto);
            _notificationService.Show("Đã lưu phiếu nhập.");
            ActiveTab = ReceiptTabMode.Pending;
            await LoadAsync();
            SelectedReceipt = FilteredReceipts.FirstOrDefault(x => x.Id == saved.Id)
                ?? FilteredReceipts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            AddValidationError(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanApprove))]
    private async Task ApproveAsync()
    {
        if (EditingId <= 0)
        {
            return;
        }

        try
        {
            if (!ValidateCurrentReceipt())
            {
                return;
            }

            await _inventoryService.ApproveReceiptAndPostAsync(EditingId, "inventory-ui");
            _notificationService.Show("Đã duyệt và ghi sổ phiếu nhập.");
            ActiveTab = ReceiptTabMode.Approved;
            await LoadAsync();
            SelectedReceipt = FilteredReceipts.FirstOrDefault(x => x.Id == EditingId)
                ?? FilteredReceipts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            AddValidationError(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanReverse))]
    private async Task ReverseAsync()
    {
        if (EditingId <= 0)
        {
            return;
        }

        if (!_dialogService.Confirm("Đảo phiếu nhập đã ghi sổ? Hệ thống sẽ tạo bút toán đảo và loại khỏi tồn hiện hành.", "Đảo phiếu nhập"))
        {
            return;
        }

        try
        {
            await _inventoryService.ReverseReceiptAsync(EditingId, "Reverse from inventory enterprise workflow", "inventory-ui");
            _notificationService.Show("Đã đảo phiếu nhập.");
            await LoadAsync();
            SelectedReceipt = Receipts.FirstOrDefault(x => x.Id == EditingId);
        }
        catch (Exception ex)
        {
            AddValidationError(ex.Message);
        }
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedReceipt is null)
        {
            return;
        }

        LoadFromSelected(SelectedReceipt);
        ActiveTab = ReceiptTabMode.Create;
    }

    [RelayCommand]
    private async Task RejectSelectedAsync()
    {
        if (SelectedReceipt is null)
        {
            return;
        }

        if (!_dialogService.Confirm("Bạn có chắc muốn không duyệt và hủy phiếu nhập đang chọn?"))
        {
            return;
        }

        try
        {
            await _inventoryService.DeleteReceiptAsync(SelectedReceipt.Id);
            _notificationService.Show("Đã hủy phiếu nhập.");
            await LoadAsync();
            SelectedReceipt = FilteredReceipts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _dialogService.Error(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReapproveAsync()
    {
        if (SelectedReceipt is null)
        {
            return;
        }

        try
        {
            var targetId = SelectedReceipt.Id;
            await _inventoryService.UnapproveReceiptAsync(targetId);
            _notificationService.Show("Đã chuyển phiếu về chờ duyệt.");
            ActiveTab = ReceiptTabMode.Pending;
            await LoadAsync();
            SelectedReceipt = FilteredReceipts.FirstOrDefault(x => x.Id == targetId) ?? FilteredReceipts.FirstOrDefault();
        }
        catch (Exception ex)
        {
            AddValidationError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (EditingId <= 0) return;
        if (!_dialogService.Confirm("Bạn có chắc muốn xóa phiếu nhập này?")) return;

        try
        {
            await _inventoryService.DeleteReceiptAsync(EditingId);
            _notificationService.Show("Đã xóa phiếu nhập.");
            New();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            AddValidationError(ex.Message);
        }
    }

    [RelayCommand]
    private void SelectItem(TransactionDetailRow? row)
    {
        if (row is null) return;
        var item = ItemLookup.FirstOrDefault(x => x.Id == row.ItemId);
        if (item is null) return;
        row.ItemCode = item.Code;
        row.ItemName = item.Name;
        row.Unit = string.Empty;
    }

    [RelayCommand]
    private void SelectSupplier()
    {
        var supplier = SupplierLookup.FirstOrDefault(x => x.Code == SupplierCode || x.Name == SupplierName);
        if (supplier is null) return;
        SupplierCode = supplier.Code;
        SupplierName = supplier.Name;
    }

    private void LoadFromSelected(StockReceiptDto dto)
    {
        EditingId = dto.Id;
        ReceiptNo = dto.ReceiptNo;
        ReceiptDate = dto.ReceiptDate;
        SupplierCode = dto.SupplierCode;
        SupplierName = dto.SupplierName;
        ReceiverName = dto.ReceiverName;
        ExpiryDate = null;
        Description = dto.Description;
        WarehouseId = dto.WarehouseId;
        DebitAccountCode = dto.DebitAccountCode;
        CreditAccountCode = dto.CreditAccountCode;
        IsApproved = dto.IsApproved;
        PostedAt = dto.PostedAt;
        PostingStatus = dto.PostingStatus;
        IsSupplierDropdownOpen = false;
        IsReceiverDropdownOpen = false;
        ClearValidationMessages();

        Details.Clear();
        foreach (var d in dto.Details)
        {
            var row = new TransactionDetailRow
            {
                Id = d.Id,
                LineNo = d.LineNo,
                ItemId = d.ItemId,
                ItemCode = d.ItemCode,
                ItemName = d.ItemName,
                Unit = d.Unit,
                WarehouseId = d.WarehouseId,
                WarehouseCode = d.WarehouseCode,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                VatRate = d.VatRate
            };
            AttachDetailRow(row);
            Details.Add(row);
        }

        ReindexDetailLines();
        NotifyTotalsChanged();
        OnPropertyChanged(nameof(IsPosted));
        OnPropertyChanged(nameof(IsReversed));
        OnPropertyChanged(nameof(CurrentWorkflowStatusText));
    }

    private void ApplyTabFilter()
    {
        FilteredReceipts.Clear();
        if (IsCreateTabSelected)
        {
            ReceiptPager.SetSource(Array.Empty<StockReceiptDto>());
            return;
        }

        var targetApproved = IsApprovedTabSelected;
        foreach (var receipt in Receipts.Where(x => x.IsApproved == targetApproved))
        {
            FilteredReceipts.Add(receipt);
        }

        ReceiptPager.SetSource(FilteredReceipts);
    }

    private void ApplySupplierFilter(string keyword)
    {
        var query = keyword?.Trim() ?? string.Empty;
        var source = SupplierLookup.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            source = source.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Code) && x.Code.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Display) && x.Display.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredSupplierLookup.Clear();
        foreach (var supplier in source)
        {
            FilteredSupplierLookup.Add(supplier);
        }

        if (string.IsNullOrWhiteSpace(query) || FilteredSupplierLookup.Count == 0)
        {
            IsSupplierDropdownOpen = false;
        }
    }

    private void ApplyItemFilter(string keyword)
    {
        var query = keyword?.Trim() ?? string.Empty;
        var source = ItemLookup.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            source = source.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Code) && x.Code.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Display) && x.Display.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredItemLookup.Clear();
        foreach (var item in source.Take(30))
        {
            FilteredItemLookup.Add(item);
        }
    }

    private void ApplyEmployeeFilter(string keyword)
    {
        var query = keyword?.Trim() ?? string.Empty;
        var source = EmployeeLookup.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            source = source.Where(x =>
                (!string.IsNullOrWhiteSpace(x.Code) && x.Code.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Display) && x.Display.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredEmployeeLookup.Clear();
        foreach (var employee in source)
        {
            FilteredEmployeeLookup.Add(employee);
        }

        if (FilteredEmployeeLookup.Count == 0)
        {
            IsReceiverDropdownOpen = false;
        }
    }

    private void ReindexDetailLines()
    {
        for (var i = 0; i < Details.Count; i++)
        {
            Details[i].LineNo = i + 1;
        }
    }

    private static bool TryParseFlexibleDecimal(string? input, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var text = input.Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        var normalized = text.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private void RecalculateDetailAmountFromUnitPrice()
    {
        if (_isSyncingDetailAmounts)
        {
            return;
        }

        _isSyncingDetailAmounts = true;
        DetailAmount = Math.Round(DetailQuantity * DetailUnitPrice, 2, MidpointRounding.AwayFromZero);
        _isSyncingDetailAmounts = false;
    }

    private decimal GetNetUnitPriceFromDetailInput()
    {
        if (!DetailPriceIncludesVat || DetailVatRate <= 0)
        {
            return DetailUnitPrice;
        }

        return Math.Round(DetailUnitPrice / (1m + DetailVatRate / 100m), 4, MidpointRounding.AwayFromZero);
    }

    private static decimal GetGrossUnitPrice(decimal netUnitPrice, decimal vatRate)
    {
        if (vatRate <= 0)
        {
            return netUnitPrice;
        }

        return Math.Round(netUnitPrice * (1m + vatRate / 100m), 4, MidpointRounding.AwayFromZero);
    }

    private void AttachDetailRow(TransactionDetailRow row)
    {
        row.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(TransactionDetailRow.Quantity) or nameof(TransactionDetailRow.UnitPrice) or nameof(TransactionDetailRow.VatRate))
            {
                NotifyTotalsChanged();
                ValidateCurrentReceipt(showToast: false);
                return;
            }

            if (args.PropertyName == nameof(TransactionDetailRow.WarehouseId))
            {
                var selectedWarehouse = WarehouseLookup.FirstOrDefault(x => x.Id == row.WarehouseId);
                row.WarehouseCode = selectedWarehouse?.Code ?? string.Empty;
                return;
            }

            if (args.PropertyName == nameof(TransactionDetailRow.ItemId))
            {
                var selectedItem = ItemLookup.FirstOrDefault(x => x.Id == row.ItemId);
                if (selectedItem is null)
                {
                    row.ItemCode = string.Empty;
                    row.ItemName = string.Empty;
                    row.Unit = string.Empty;
                    return;
                }

                row.ItemCode = selectedItem.Code;
                row.ItemName = selectedItem.Name;
                // ĐV tính tự động theo hàng hóa từ dữ liệu CSDL
                if (_itemById.TryGetValue(row.ItemId, out var itemDetail))
                {
                    row.Unit = itemDetail.Unit;
                }
                else
                {
                    row.Unit = string.Empty;
                }
            }
        };
    }

    private bool ValidateCurrentReceipt(bool showToast = true)
    {
        ValidationMessages.Clear();

        if (Details.Count == 0)
        {
            ValidationMessages.Add("Cần ít nhất 1 dòng chi tiết.");
        }

        if (string.IsNullOrWhiteSpace(DebitAccountCode) || string.IsNullOrWhiteSpace(CreditAccountCode))
        {
            ValidationMessages.Add("Thiếu tài khoản hạch toán nhập kho.");
        }

        var duplicates = Details
            .Where(x => x.ItemId > 0)
            .GroupBy(x => new { x.ItemId, WarehouseId = x.WarehouseId.GetValueOrDefault() })
            .Where(x => x.Count() > 1)
            .Select(x => x.First().ItemName)
            .ToList();
        foreach (var itemName in duplicates)
        {
            ValidationMessages.Add($"Trùng hàng hóa/kho trên nhiều dòng: {itemName}.");
        }

        if (WarehouseId <= 0 && Details.Any(x => !x.WarehouseId.HasValue || x.WarehouseId <= 0))
        {
            ValidationMessages.Add("Chọn kho nhập cho phiếu hoặc từng dòng hàng.");
        }

        foreach (var row in Details)
        {
            if (row.ItemId <= 0)
            {
                ValidationMessages.Add($"Dòng {row.LineNo}: chưa chọn hàng hóa hợp lệ.");
            }

            if (row.Quantity <= 0)
            {
                ValidationMessages.Add($"Dòng {row.LineNo}: số lượng phải lớn hơn 0.");
            }

            if (row.UnitPrice < 0)
            {
                ValidationMessages.Add($"Dòng {row.LineNo}: đơn giá không được âm.");
            }

            if (!row.WarehouseId.HasValue || row.WarehouseId <= 0)
            {
                ValidationMessages.Add($"Dòng {row.LineNo}: chưa chọn kho nhập.");
            }
        }

        OnPropertyChanged(nameof(HasValidationMessages));

        if (ValidationMessages.Count > 0 && showToast)
        {
            _notificationService.Show("Phiếu nhập còn lỗi dữ liệu, kiểm tra khung cảnh báo.");
        }

        return ValidationMessages.Count == 0;
    }

    private void AddValidationError(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            ValidationMessages.Add(message);
        }

        OnPropertyChanged(nameof(HasValidationMessages));
    }

    private void NotifyTotalsChanged()
    {
        OnPropertyChanged(nameof(TotalQuantity));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(VatAmount));
        OnPropertyChanged(nameof(InventoryValue));
    }

    private void ClearValidationMessages()
    {
        ValidationMessages.Clear();
        OnPropertyChanged(nameof(HasValidationMessages));
    }
}

