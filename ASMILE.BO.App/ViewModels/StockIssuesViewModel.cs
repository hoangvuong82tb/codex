using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class StockIssuesViewModel : ViewModelBase
{
    public enum IssueTabMode
    {
        Create = 0,
        Pending = 1,
        Approved = 2
    }

    private readonly IInventoryAppService _inventoryService;
    private readonly IItemAppService _itemService;
    private readonly IDepartmentAppService _departmentService;
    private readonly IPersonAppService _personService;
    private readonly IWarehouseAppService _warehouseService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IWindowManagerService _windowManagerService;
    private readonly Dictionary<int, Dictionary<int, StockBalanceDto>> _warehouseStockCache = new();
    private readonly Dictionary<int, string> _itemUnitById = new();
    private readonly Dictionary<string, string> _itemUnitByKey = new();
    private CancellationTokenSource? _loadCts;

    public ObservableCollection<StockIssueDto> Issues { get; } = new();
    public ObservableCollection<StockIssueDto> FilteredIssues { get; } = new();
    public TablePagerViewModel<StockIssueDto> IssuePager { get; } = new() { ItemName = "phiếu xuất" };
    public ObservableCollection<TransactionDetailRow> Details { get; } = new();
    public ObservableCollection<LookupItemDto> ItemLookup { get; } = new();
    public ObservableCollection<LookupItemDto> FilteredItemLookup { get; } = new();
    public ObservableCollection<LookupItemDto> EmployeeLookup { get; } = new();
    public ObservableCollection<LookupItemDto> FilteredEmployeeLookup { get; } = new();
    public ObservableCollection<LookupItemDto> DepartmentLookup { get; } = new();
    public ObservableCollection<LookupItemDto> WarehouseLookup { get; } = new();
    public ObservableCollection<string> ValidationMessages { get; } = new();

    [ObservableProperty]
    private StockIssueDto? selectedIssue;

    [ObservableProperty]
    private TransactionDetailRow? selectedDetail;

    [ObservableProperty]
    private string keyword = string.Empty;

    [ObservableProperty]
    private DateTime? filterFromDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime? filterToDate = DateTime.Today;

    [ObservableProperty]
    private IssueTabMode activeTab = IssueTabMode.Create;

    [ObservableProperty]
    private int editingId;

    [ObservableProperty]
    private string issueNo = string.Empty;

    [ObservableProperty]
    private DateTime issueDate = DateTime.Today;

    [ObservableProperty]
    private string description = string.Empty;

    [ObservableProperty]
    private string detailProductName = string.Empty;

    [ObservableProperty]
    private int? detailProductItemId;

    [ObservableProperty]
    private bool isDetailProductDropdownOpen;

    [ObservableProperty]
    private decimal detailQuantity = 1;

    [ObservableProperty]
    private decimal detailOnHand;

    [ObservableProperty]
    private decimal detailImportPrice;

    [ObservableProperty]
    private decimal detailUnitPrice;

    [ObservableProperty]
    private string receiverName = string.Empty;

    [ObservableProperty]
    private int? selectedReceiverId;

    [ObservableProperty]
    private bool isReceiverDropdownOpen;

    [ObservableProperty]
    private int warehouseId;

    [ObservableProperty]
    private int? toWarehouseId;

    [ObservableProperty]
    private string departmentCode = "HCKT";

    [ObservableProperty]
    private string debitAccountCode = "632";

    [ObservableProperty]
    private string creditAccountCode = "152";

    [ObservableProperty]
    private bool isTransfer;

    [ObservableProperty]
    private bool isApproved;

    [ObservableProperty]
    private DateTime? postedAt;

    [ObservableProperty]
    private string? postingStatus;

    public decimal TotalQuantity => Details.Sum(x => x.Quantity);
    public decimal TotalAmount => Details.Sum(x => x.Amount);
    public decimal DetailEstimatedCost => DetailQuantity * DetailImportPrice;
    public decimal MovingAverageCost => DetailImportPrice;
    public decimal InventoryAvailability => DetailOnHand;
    public decimal InventoryValue => Details.Sum(x => x.Quantity * x.UnitPrice);
    public bool HasAvailabilityWarning => DetailProductItemId.HasValue && DetailQuantity > 0 && DetailQuantity > DetailOnHand;
    public string AvailabilityStatusText => HasAvailabilityWarning ? "Không đủ tồn khả dụng" : "Tồn khả dụng hợp lệ";
    public bool IsReversed => string.Equals(PostingStatus, "Reversed", StringComparison.OrdinalIgnoreCase);
    public bool CanApprove => EditingId > 0 && !IsApproved && !IsReversed;
    public bool CanReverse => EditingId > 0 && IsPosted;
    public bool CanBatchPost => FilteredIssues.Any(x => !x.IsApproved && !x.IsPosted && !x.IsReversed);
    public bool HasValidationMessages => ValidationMessages.Count > 0;
    public bool IsPosted => !IsReversed && (PostedAt.HasValue || string.Equals(PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase));
    public string CurrentWorkflowStatusText => EditingId <= 0 ? "Phiếu mới" : IsReversed ? "Đã đảo" : IsPosted ? "Đã ghi sổ" : IsApproved ? "Đã duyệt" : "Chờ duyệt";
    public bool IsCreateTabSelected => ActiveTab == IssueTabMode.Create;
    public bool IsPendingTabSelected => ActiveTab == IssueTabMode.Pending;
    public bool IsApprovedTabSelected => ActiveTab == IssueTabMode.Approved;
    public bool IsListTabSelected => !IsCreateTabSelected;
    public bool ShowAccountingFields => !IsTransfer;

    public StockIssuesViewModel(
        IInventoryAppService inventoryService,
        IItemAppService itemService,
        IDepartmentAppService departmentService,
        IPersonAppService personService,
        IWarehouseAppService warehouseService,
        INotificationService notificationService,
        IDialogService dialogService,
        IWindowManagerService windowManagerService)
    {
        _inventoryService = inventoryService;
        _itemService = itemService;
        _departmentService = departmentService;
        _personService = personService;
        _warehouseService = warehouseService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _windowManagerService = windowManagerService;

        New();
        _ = LoadAsync();
    }

    public void OpenIssueDetailModule(StockIssueDto issue)
    {
        var normalizedIssue = NormalizeIssueForDisplay(issue);
        var moduleKey = $"issue-detail-{issue.Id}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            moduleKey,
            $"Chi tiết {issue.IssueNo}",
            "\uE8A7",
            "Chi tiết phiếu xuất",
            "#2F65D9",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\export.ico");

        var vm = new StockIssueDetailModuleViewModel(normalizedIssue, _windowManagerService);
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

    private StockIssueDto NormalizeIssueForDisplay(StockIssueDto source)
    {
        var clone = new StockIssueDto
        {
            Id = source.Id,
            IssueNo = source.IssueNo,
            IssueDate = source.IssueDate,
            Description = source.Description,
            WarehouseId = source.WarehouseId,
            WarehouseCode = source.WarehouseCode,
            ToWarehouseId = source.ToWarehouseId,
            ToWarehouseCode = source.ToWarehouseCode,
            DepartmentCode = source.DepartmentCode,
            DebitAccountCode = source.DebitAccountCode,
            CreditAccountCode = source.CreditAccountCode,
            TotalAmount = source.TotalAmount,
            IsApproved = source.IsApproved,
            Details = source.Details.Select(d => new StockTransactionDetailDto
            {
                Id = d.Id,
                LineNo = d.LineNo,
                ItemId = d.ItemId,
                ItemCode = d.ItemCode,
                ItemName = d.ItemName,
                Unit = !string.IsNullOrWhiteSpace(d.Unit)
                    ? d.Unit
                    : ResolveUnit(d.ItemId, d.ItemCode, d.ItemName),
                WarehouseId = d.WarehouseId,
                WarehouseCode = !string.IsNullOrWhiteSpace(d.WarehouseCode)
                    ? d.WarehouseCode
                    : source.WarehouseCode,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Amount = d.Amount
            }).ToList()
        };

        return clone;
    }

    private string ResolveUnit(int itemId, string? itemCode, string? itemName)
    {
        if (itemId > 0 && _itemUnitById.TryGetValue(itemId, out var unitById) && !string.IsNullOrWhiteSpace(unitById))
        {
            return unitById;
        }

        var key = $"{(itemCode ?? string.Empty).Trim().ToUpperInvariant()}|{(itemName ?? string.Empty).Trim().ToUpperInvariant()}";
        if (_itemUnitByKey.TryGetValue(key, out var unitByKey) && !string.IsNullOrWhiteSpace(unitByKey))
        {
            return unitByKey;
        }

        return string.Empty;
    }

    partial void OnSelectedIssueChanged(StockIssueDto? value)
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
        DetailImportPrice = value.UnitPrice;
        DetailUnitPrice = value.UnitPrice;
        IsDetailProductDropdownOpen = false;
        _ = RefreshDetailOnHandAsync();
    }

    partial void OnActiveTabChanged(IssueTabMode value)
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
        var warehouseCode = WarehouseLookup.FirstOrDefault(x => x.Id == value)?.Code ?? string.Empty;
        foreach (var row in Details)
        {
            row.WarehouseId = value > 0 ? value : row.WarehouseId;
            row.WarehouseCode = warehouseCode;
        }
        _ = RefreshDetailOnHandAsync();
        ValidateCurrentIssue(showToast: false);
    }

    partial void OnToWarehouseIdChanged(int? value)
    {
        var toWarehouseCode = value.HasValue ? WarehouseLookup.FirstOrDefault(x => x.Id == value.Value)?.Code ?? string.Empty : string.Empty;
        foreach (var row in Details)
        {
            row.ToWarehouseCode = toWarehouseCode;
        }
    }

    partial void OnDepartmentCodeChanged(string value)
    {
        foreach (var row in Details)
        {
            row.DepartmentCode = value ?? string.Empty;
        }
    }

    partial void OnDetailProductNameChanged(string value)
    {
        ApplyItemFilter(value);
        IsDetailProductDropdownOpen = !string.IsNullOrWhiteSpace(value) && FilteredItemLookup.Count > 0;
        _ = RefreshDetailOnHandAsync();
        OnPropertyChanged(nameof(DetailEstimatedCost));
        OnPropertyChanged(nameof(MovingAverageCost));
        OnPropertyChanged(nameof(InventoryAvailability));
        OnPropertyChanged(nameof(HasAvailabilityWarning));
        OnPropertyChanged(nameof(AvailabilityStatusText));
    }

    partial void OnDetailProductItemIdChanged(int? value)
    {
        if (value.HasValue)
        {
            var selected = ItemLookup.FirstOrDefault(x => x.Id == value.Value);
            if (selected is not null && !string.Equals(DetailProductName, selected.Name, StringComparison.Ordinal))
            {
                DetailProductName = selected.Name;
            }
            IsDetailProductDropdownOpen = false;
        }
        _ = RefreshDetailOnHandAsync();
        OnPropertyChanged(nameof(DetailEstimatedCost));
        OnPropertyChanged(nameof(MovingAverageCost));
        OnPropertyChanged(nameof(InventoryAvailability));
        OnPropertyChanged(nameof(HasAvailabilityWarning));
        OnPropertyChanged(nameof(AvailabilityStatusText));
    }

    partial void OnDetailQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(DetailEstimatedCost));
        OnPropertyChanged(nameof(InventoryAvailability));
        OnPropertyChanged(nameof(HasAvailabilityWarning));
        OnPropertyChanged(nameof(AvailabilityStatusText));
        ValidateCurrentIssue(showToast: false);
    }

    partial void OnDetailImportPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(DetailEstimatedCost));
        OnPropertyChanged(nameof(MovingAverageCost));
    }

    partial void OnReceiverNameChanged(string value)
    {
        ApplyEmployeeFilter(value);
        IsReceiverDropdownOpen = !string.IsNullOrWhiteSpace(value) && FilteredEmployeeLookup.Count > 0;

        var selected = EmployeeLookup.FirstOrDefault(x =>
            string.Equals(x.Name, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Display, value, StringComparison.OrdinalIgnoreCase));
        SelectedReceiverId = selected?.Id;
    }

    partial void OnSelectedReceiverIdChanged(int? value)
    {
        if (!value.HasValue)
        {
            return;
        }

        var selected = EmployeeLookup.FirstOrDefault(x => x.Id == value.Value);
        if (selected is null)
        {
            return;
        }

        if (!string.Equals(ReceiverName, selected.Name, StringComparison.Ordinal))
        {
            ReceiverName = selected.Name;
        }

        IsReceiverDropdownOpen = false;
    }

    partial void OnIsTransferChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowAccountingFields));
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
            _warehouseStockCache.Clear();
            var items = (await _itemService.GetLookupAsync())
                .GroupBy(x => $"{(x.Code ?? string.Empty).Trim().ToUpperInvariant()}|{(x.Name ?? string.Empty).Trim().ToUpperInvariant()}")
                .Select(x => x.First())
                .ToList();
            ItemLookup.Clear();
            foreach (var item in items) ItemLookup.Add(item);
            ApplyItemFilter(DetailProductName);

            var itemRows = await _itemService.SearchAsync(string.Empty);
            _itemUnitById.Clear();
            _itemUnitByKey.Clear();
            foreach (var itemRow in itemRows)
            {
                _itemUnitById[itemRow.Id] = itemRow.Unit;
                var key = $"{(itemRow.ItemCode ?? string.Empty).Trim().ToUpperInvariant()}|{(itemRow.ItemName ?? string.Empty).Trim().ToUpperInvariant()}";
                _itemUnitByKey[key] = itemRow.Unit;
            }

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

            var departments = await _departmentService.GetLookupAsync();
            DepartmentLookup.Clear();
            foreach (var department in departments) DepartmentLookup.Add(department);

            var warehouses = await _warehouseService.GetLookupAsync();
            WarehouseLookup.Clear();
            foreach (var warehouse in warehouses) WarehouseLookup.Add(warehouse);

            var rows = await _inventoryService.SearchIssuesAsync(FilterFromDate, FilterToDate, Keyword, null, token);
            Issues.Clear();
            foreach (var row in rows) Issues.Add(row);
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
        IssueNo = GenerateNextIssueNo();
        IssueDate = DateTime.Today;
        Description = string.Empty;
        WarehouseId = WarehouseLookup.FirstOrDefault()?.Id ?? 0;
        ToWarehouseId = null;
        DepartmentCode = DepartmentLookup.FirstOrDefault()?.Code ?? string.Empty;
        ReceiverName = string.Empty;
        SelectedReceiverId = null;
        DetailProductName = string.Empty;
        DetailProductItemId = null;
        DetailQuantity = 1;
        DetailOnHand = 0;
        DetailImportPrice = 0;
        DetailUnitPrice = 0;
        IsDetailProductDropdownOpen = false;
        DebitAccountCode = "632";
        CreditAccountCode = "152";
        IsTransfer = false;
        IsApproved = false;
        PostedAt = null;
        PostingStatus = null;
        ClearValidationMessages();
        Details.Clear();
        NotifyTotalsChanged();
        OnPropertyChanged(nameof(DetailEstimatedCost));
        OnPropertyChanged(nameof(MovingAverageCost));
        OnPropertyChanged(nameof(InventoryAvailability));
        OnPropertyChanged(nameof(HasAvailabilityWarning));
        OnPropertyChanged(nameof(AvailabilityStatusText));
        OnPropertyChanged(nameof(CurrentWorkflowStatusText));
    }

    private string GenerateNextIssueNo()
    {
        var nextSequence = GetNextIssueSequence();
        var randomLetters = GetRandomLetters(2);
        return $"PX-{randomLetters}{nextSequence:00000}";
    }

    private int GetNextIssueSequence()
    {
        var pattern = new Regex(@"^PX-[A-Z]{2}(?<seq>\d{5,6})$", RegexOptions.Compiled);
        var maxSequence = 0;

        foreach (var issue in Issues)
        {
            if (string.IsNullOrWhiteSpace(issue.IssueNo))
            {
                continue;
            }

            var match = pattern.Match(issue.IssueNo.Trim().ToUpperInvariant());
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
    private void ShowCreateTab() => ActiveTab = IssueTabMode.Create;

    [RelayCommand]
    private void ShowPendingTab() => ActiveTab = IssueTabMode.Pending;

    [RelayCommand]
    private void ShowApprovedTab() => ActiveTab = IssueTabMode.Approved;

    [RelayCommand]
    private void AddDetail(TransactionDetailRow? anchorRow = null)
    {
        var detail = new TransactionDetailRow
        {
            Quantity = 1,
            UnitPrice = 0,
            WarehouseId = WarehouseId > 0 ? WarehouseId : null,
            WarehouseCode = WarehouseLookup.FirstOrDefault(x => x.Id == WarehouseId)?.Code ?? string.Empty,
            ToWarehouseCode = ToWarehouseId.HasValue ? WarehouseLookup.FirstOrDefault(x => x.Id == ToWarehouseId.Value)?.Code ?? string.Empty : string.Empty,
            DepartmentCode = DepartmentCode
        };
        detail.PropertyChanged += (_, _) => NotifyTotalsChanged();
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
        var warehouseCode = WarehouseLookup.FirstOrDefault(x => x.Id == WarehouseId)?.Code ?? string.Empty;
        var toWarehouseCode = ToWarehouseId.HasValue ? WarehouseLookup.FirstOrDefault(x => x.Id == ToWarehouseId.Value)?.Code ?? string.Empty : string.Empty;

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
            var row = new TransactionDetailRow
            {
                ItemId = item.Id,
                ItemCode = item.Code,
                ItemName = item.Name,
                Unit = ResolveUnit(item.Id, item.Code, item.Name),
                WarehouseId = WarehouseId > 0 ? WarehouseId : null,
                WarehouseCode = warehouseCode,
                ToWarehouseCode = toWarehouseCode,
                DepartmentCode = DepartmentCode,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
            row.PropertyChanged += (_, _) => NotifyTotalsChanged();
            Details.Add(row);
        }

        ReindexDetailLines();
        NotifyTotalsChanged();
        ValidateCurrentIssue(showToast: false);
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
        var selectedItem = ItemLookup.FirstOrDefault(x => string.Equals(x.Name, DetailProductName, StringComparison.OrdinalIgnoreCase) || string.Equals(x.Display, DetailProductName, StringComparison.OrdinalIgnoreCase));
        if (DetailProductItemId.HasValue)
        {
            selectedItem = ItemLookup.FirstOrDefault(x => x.Id == DetailProductItemId.Value) ?? selectedItem;
        }

        if (selectedItem is null)
        {
            _notificationService.Show("Chọn tên hàng hóa hợp lệ.");
            return;
        }

        if (DetailQuantity <= 0)
        {
            _notificationService.Show("Số lượng phải lớn hơn 0.");
            return;
        }

        if (DetailUnitPrice < 0)
        {
            _notificationService.Show("Đơn giá không hợp lệ.");
            return;
        }

        var warehouseCode = WarehouseLookup.FirstOrDefault(x => x.Id == WarehouseId)?.Code ?? string.Empty;
        var toWarehouseCode = ToWarehouseId.HasValue ? WarehouseLookup.FirstOrDefault(x => x.Id == ToWarehouseId.Value)?.Code ?? string.Empty : string.Empty;

        var row = SelectedDetail is not null && Details.Contains(SelectedDetail) ? SelectedDetail : new TransactionDetailRow();
        var isNew = row.Id == 0 && !Details.Contains(row);
        row.ItemId = selectedItem.Id;
        row.ItemCode = selectedItem.Code;
        row.ItemName = selectedItem.Name;
        row.Unit = ResolveUnit(selectedItem.Id, selectedItem.Code, selectedItem.Name);
        row.WarehouseId = WarehouseId > 0 ? WarehouseId : null;
        row.WarehouseCode = warehouseCode;
        row.ToWarehouseCode = toWarehouseCode;
        row.DepartmentCode = DepartmentCode;
        row.Quantity = DetailQuantity;
        row.UnitPrice = DetailUnitPrice;

        if (isNew)
        {
                row.PropertyChanged += (_, _) => NotifyTotalsChanged();
            Details.Add(row);
        }

        SelectedDetail = row;
        ReindexDetailLines();
        NotifyTotalsChanged();

        SelectedDetail = null;
        DetailProductName = string.Empty;
        DetailProductItemId = null;
        DetailQuantity = 1;
        DetailOnHand = 0;
        DetailImportPrice = 0;
        DetailUnitPrice = 0;
        IsDetailProductDropdownOpen = false;
    }

    private async Task RefreshDetailOnHandAsync()
    {
        try
        {
            if (WarehouseId <= 0)
            {
                DetailOnHand = 0;
                DetailImportPrice = 0;
                return;
            }

            var itemId = ResolveCurrentDetailItemId();
            if (!itemId.HasValue || itemId.Value <= 0)
            {
                DetailOnHand = 0;
                DetailImportPrice = 0;
                return;
            }

            if (!_warehouseStockCache.TryGetValue(WarehouseId, out var warehouseMap))
            {
                var balances = await _inventoryService.GetWarehouseStockAsync(WarehouseId);
                warehouseMap = balances.ToDictionary(x => x.ItemId, x => x);
                _warehouseStockCache[WarehouseId] = warehouseMap;
            }

            if (warehouseMap.TryGetValue(itemId.Value, out var balance))
            {
                DetailOnHand = balance.ClosingQty;
                DetailImportPrice = balance.AvgPrice;
            }
            else
            {
                DetailOnHand = 0;
                DetailImportPrice = 0;
            }
        }
        catch
        {
            DetailOnHand = 0;
            DetailImportPrice = 0;
        }

        OnPropertyChanged(nameof(DetailEstimatedCost));
        OnPropertyChanged(nameof(MovingAverageCost));
        OnPropertyChanged(nameof(InventoryAvailability));
        OnPropertyChanged(nameof(HasAvailabilityWarning));
        OnPropertyChanged(nameof(AvailabilityStatusText));
    }

    private int? ResolveCurrentDetailItemId()
    {
        if (DetailProductItemId.HasValue && DetailProductItemId.Value > 0)
        {
            return DetailProductItemId.Value;
        }

        if (string.IsNullOrWhiteSpace(DetailProductName))
        {
            return null;
        }

        var selectedItem = ItemLookup.FirstOrDefault(x =>
            string.Equals(x.Name, DetailProductName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Display, DetailProductName, StringComparison.OrdinalIgnoreCase));

        return selectedItem?.Id;
    }

    private static bool TryParseFlexibleDecimal(string? input, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var text = input.Trim();
        if (decimal.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.CurrentCulture, out value))
        {
            return true;
        }

        var normalized = text.Replace(',', '.');
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out value);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            if (!ValidateCurrentIssue())
            {
                return;
            }

            var dto = new StockIssueDto
            {
                Id = EditingId,
                IssueNo = IssueNo,
                IssueDate = IssueDate,
                Description = Description,
                WarehouseId = WarehouseId,
                ToWarehouseId = ToWarehouseId,
                DepartmentCode = DepartmentCode,
                DebitAccountCode = DebitAccountCode,
                CreditAccountCode = CreditAccountCode,
                IsApproved = IsApproved,
                Details = Details.Select(x => x.ToDto()).ToList()
            };

            var saved = await _inventoryService.SaveIssueAsync(dto);
            _notificationService.Show("Đã lưu phiếu xuất.");
            ActiveTab = IssueTabMode.Pending;
            await LoadAsync();
            SelectedIssue = FilteredIssues.FirstOrDefault(x => x.Id == saved.Id)
                ?? FilteredIssues.FirstOrDefault();
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
            if (!ValidateCurrentIssue())
            {
                return;
            }

            await _inventoryService.ApproveIssueAndPostAsync(EditingId, "inventory-ui");
            _notificationService.Show("Đã duyệt, tính giá vốn và ghi sổ phiếu xuất.");
            ActiveTab = IssueTabMode.Approved;
            await LoadAsync();
            SelectedIssue = FilteredIssues.FirstOrDefault(x => x.Id == EditingId)
                ?? FilteredIssues.FirstOrDefault();
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

        if (!_dialogService.Confirm("Đảo phiếu xuất đã ghi sổ? Hệ thống sẽ tạo bút toán đảo và loại khỏi tồn hiện hành.", "Đảo phiếu xuất"))
        {
            return;
        }

        try
        {
            await _inventoryService.ReverseIssueAsync(EditingId, "Reverse from inventory enterprise workflow", "inventory-ui");
            _notificationService.Show("Đã đảo phiếu xuất.");
            await LoadAsync();
            SelectedIssue = Issues.FirstOrDefault(x => x.Id == EditingId);
        }
        catch (Exception ex)
        {
            AddValidationError(ex.Message);
        }
    }

    [RelayCommand(CanExecute = nameof(CanBatchPost))]
    private async Task BatchPostAsync()
    {
        var targets = FilteredIssues.Where(x => !x.IsApproved && !x.IsPosted && !x.IsReversed).ToList();
        if (targets.Count == 0)
        {
            _notificationService.Show("Không có phiếu xuất chờ ghi sổ.");
            return;
        }

        if (!_dialogService.Confirm($"Ghi sổ batch {targets.Count} phiếu xuất đang chờ duyệt?", "Batch posting"))
        {
            return;
        }

        var errors = new List<string>();
        foreach (var issue in targets)
        {
            try
            {
                await _inventoryService.ApproveIssueAndPostAsync(issue.Id, "inventory-ui");
            }
            catch (Exception ex)
            {
                errors.Add($"{issue.IssueNo}: {ex.Message}");
            }
        }

        await LoadAsync();
        if (errors.Count > 0)
        {
            ValidationMessages.Clear();
            foreach (var error in errors)
            {
                ValidationMessages.Add(error);
            }
            OnPropertyChanged(nameof(HasValidationMessages));
            return;
        }

        _notificationService.Show($"Đã ghi sổ {targets.Count} phiếu xuất.");
    }

    [RelayCommand]
    private void EditSelected()
    {
        if (SelectedIssue is null)
        {
            return;
        }

        LoadFromSelected(SelectedIssue);
        ActiveTab = IssueTabMode.Create;
    }

    [RelayCommand]
    private async Task RejectSelectedAsync()
    {
        if (SelectedIssue is null)
        {
            return;
        }

        if (!_dialogService.Confirm("Bạn có chắc muốn không duyệt và hủy phiếu xuất đang chọn?"))
        {
            return;
        }

        try
        {
            await _inventoryService.DeleteIssueAsync(SelectedIssue.Id);
            _notificationService.Show("Đã hủy phiếu xuất.");
            await LoadAsync();
            SelectedIssue = FilteredIssues.FirstOrDefault();
        }
        catch (Exception ex)
        {
            AddValidationError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReapproveAsync()
    {
        if (SelectedIssue is null)
        {
            return;
        }

        try
        {
            var targetId = SelectedIssue.Id;
            await _inventoryService.UnapproveIssueAsync(targetId);
            _notificationService.Show("Đã chuyển phiếu về chờ duyệt.");
            ActiveTab = IssueTabMode.Pending;
            await LoadAsync();
            SelectedIssue = FilteredIssues.FirstOrDefault(x => x.Id == targetId) ?? FilteredIssues.FirstOrDefault();
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
        if (!_dialogService.Confirm("Bạn có chắc muốn xóa phiếu xuất này?")) return;

        try
        {
            await _inventoryService.DeleteIssueAsync(EditingId);
            _notificationService.Show("Đã xóa phiếu xuất.");
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
    }

    private void LoadFromSelected(StockIssueDto dto)
    {
        EditingId = dto.Id;
        IssueNo = dto.IssueNo;
        IssueDate = dto.IssueDate;
        Description = dto.Description;
        WarehouseId = dto.WarehouseId;
        ToWarehouseId = dto.ToWarehouseId;
        DepartmentCode = dto.DepartmentCode;
        DebitAccountCode = dto.DebitAccountCode;
        CreditAccountCode = dto.CreditAccountCode;
        IsApproved = dto.IsApproved;
        PostedAt = dto.PostedAt;
        PostingStatus = dto.PostingStatus;
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
                Unit = !string.IsNullOrWhiteSpace(d.Unit)
                    ? d.Unit
                    : ResolveUnit(d.ItemId, d.ItemCode, d.ItemName),
                WarehouseId = d.WarehouseId,
                WarehouseCode = WarehouseLookup.FirstOrDefault(x => x.Id == d.WarehouseId)?.Code ?? WarehouseLookup.FirstOrDefault(x => x.Id == dto.WarehouseId)?.Code ?? string.Empty,
                ToWarehouseCode = dto.ToWarehouseId.HasValue ? WarehouseLookup.FirstOrDefault(x => x.Id == dto.ToWarehouseId.Value)?.Code ?? string.Empty : string.Empty,
                DepartmentCode = dto.DepartmentCode,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice
            };
            row.PropertyChanged += (_, _) => NotifyTotalsChanged();
            Details.Add(row);
        }

        ReindexDetailLines();
        NotifyTotalsChanged();
        OnPropertyChanged(nameof(IsPosted));
        OnPropertyChanged(nameof(IsReversed));
        OnPropertyChanged(nameof(CurrentWorkflowStatusText));
    }

    private void ReindexDetailLines()
    {
        for (var i = 0; i < Details.Count; i++)
        {
            Details[i].LineNo = i + 1;
        }
    }

    private void ApplyTabFilter()
    {
        FilteredIssues.Clear();
        if (IsCreateTabSelected)
        {
            IssuePager.SetSource(Array.Empty<StockIssueDto>());
            return;
        }

        var targetApproved = IsApprovedTabSelected;
        foreach (var issue in Issues.Where(x => x.IsApproved == targetApproved))
        {
            FilteredIssues.Add(issue);
        }

        IssuePager.SetSource(FilteredIssues);
        OnPropertyChanged(nameof(CanBatchPost));
        BatchPostCommand.NotifyCanExecuteChanged();
    }

    private void ApplyItemFilter(string keyword)
    {
        var query = keyword?.Trim() ?? string.Empty;
        var source = ItemLookup
            .GroupBy(x => $"{(x.Code ?? string.Empty).Trim().ToUpperInvariant()}|{(x.Name ?? string.Empty).Trim().ToUpperInvariant()}")
            .Select(x => x.First())
            .AsEnumerable();
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
                (!string.IsNullOrWhiteSpace(x.Name) && x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(x.Display) && x.Display.Contains(query, StringComparison.OrdinalIgnoreCase)));
        }

        FilteredEmployeeLookup.Clear();
        foreach (var employee in source.Take(30))
        {
            FilteredEmployeeLookup.Add(employee);
        }
    }

    private bool ValidateCurrentIssue(bool showToast = true)
    {
        ValidationMessages.Clear();

        if (Details.Count == 0)
        {
            ValidationMessages.Add("Cần ít nhất 1 dòng chi tiết.");
        }

        if (WarehouseId <= 0)
        {
            ValidationMessages.Add("Chọn kho xuất cho phiếu.");
        }

        if (!IsTransfer && (string.IsNullOrWhiteSpace(DebitAccountCode) || string.IsNullOrWhiteSpace(CreditAccountCode)))
        {
            ValidationMessages.Add("Thiếu tài khoản giá vốn hoặc tài khoản kho.");
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
                ValidationMessages.Add($"Dòng {row.LineNo}: chưa chọn kho xuất.");
            }
        }

        if (HasAvailabilityWarning)
        {
            ValidationMessages.Add("Số lượng dòng đang nhập vượt tồn khả dụng hiện tại.");
        }

        OnPropertyChanged(nameof(HasValidationMessages));

        if (ValidationMessages.Count > 0 && showToast)
        {
            _notificationService.Show("Phiếu xuất còn lỗi dữ liệu, kiểm tra khung cảnh báo.");
        }

        return ValidationMessages.Count == 0;
    }

    private void ClearValidationMessages()
    {
        ValidationMessages.Clear();
        OnPropertyChanged(nameof(HasValidationMessages));
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
        OnPropertyChanged(nameof(InventoryValue));
    }
}

