using System.Collections.ObjectModel;
using System.Windows;
using ASMILE.BO.App.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Core.FrontOffice.LostFound;
using LostFoundAppStatus = ASMILE.BO.Application.FrontOffice.Housekeeping.LostFoundItemStatus;
using LostFoundCoreCategory = ASMILE.BO.Core.FrontOffice.LostFound.LostFoundCategory;
using LostFoundCoreStatus = ASMILE.BO.Core.FrontOffice.LostFound.LostFoundStatus;

namespace ASMILE.BO.App.ViewModels;

// ═══ DTOs ═══

public sealed class LostFoundItemDto
{
    public Guid? PersistentId { get; set; }
    public string ItemCode { get; set; } = "";
    public string ItemName { get; set; } = "";
    public string Category { get; set; } = "Khác";
    public string CategoryIcon => Category switch
    {
        "Tiền" => "💰", "Trang sức" => "💍", "Điện thoại" => "📱", "Laptop" => "💻",
        "Máy tính bảng" => "📟", "Đồng hồ" => "⌚", "Quần áo" => "👔", "Ví" => "👛",
        "Hộ chiếu" => "📘", "CMND" => "🪪", "Thẻ NH" => "💳", "Đồ điện tử" => "🔌", _ => "📦"
    };
    public string Description { get; set; } = "";
    public string? Color { get; set; }
    public string? Brand { get; set; }
    public string? SerialNumber { get; set; }
    public decimal EstimatedValue { get; set; }
    public bool IsHighValue => EstimatedValue >= 5000000;
    public string ValueDisplay => EstimatedValue > 0 ? $"{EstimatedValue:N0} ₫" : "—";

    public string? RoomNumber { get; set; }
    public string? Floor { get; set; }
    public string? Building { get; set; }
    public string? FoundLocation { get; set; }
    public string? FoundBy { get; set; }
    public string? Supervisor { get; set; }
    public DateTime? FoundDate { get; set; }

    public string ItemStatus { get; set; } = "New";
    public string StatusLabel => ItemStatus switch
    {
        "New" => "Mới phát hiện", "Stored" => "Đã lưu kho", "Contacting" => "Đang liên hệ",
        "Returned" => "Đã trả", "Disposed" => "Đã thanh lý", "Overdue" => "Quá hạn", _ => ItemStatus
    };
    public string StatusColor => ItemStatus switch
    {
        "New" => "#D97706", "Stored" => "#2563EB", "Contacting" => "#7C3AED",
        "Returned" => "#16A34A", "Disposed" => "#64748B", "Overdue" => "#DC2626", _ => "#64748B"
    };
    public string StatusBg => ItemStatus switch
    {
        "New" => "#FFF7ED", "Stored" => "#DBEAFE", "Contacting" => "#F3E8FF",
        "Returned" => "#DCFCE7", "Disposed" => "#F1F5F9", "Overdue" => "#FEE2E2", _ => "#F8FAFC"
    };

    public string? GuestName { get; set; }
    public string? GuestPhone { get; set; }
    public string? GuestEmail { get; set; }
    public string? CompanyName { get; set; }
    public string? ReservationNumber { get; set; }
    public string? FolioNumber { get; set; }
    public DateTime? GuestArrival { get; set; }
    public DateTime? GuestDeparture { get; set; }

    public DateTime? ContactDate { get; set; }
    public string? ContactBy { get; set; }
    public string? ContactMethod { get; set; }
    public string? ContactResult { get; set; }
    public string? ContactRemark { get; set; }

    public string? StorageLocation { get; set; }
    public string? ShelfNumber { get; set; }
    public string? Compartment { get; set; }
    public string? SealNumber { get; set; }
    public DateTime? StorageDate { get; set; }
    public string? StoredBy { get; set; }

    public string? ReturnedTo { get; set; }
    public string? RecipientId { get; set; }
    public string? RecipientRelation { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string? ReturnMethod { get; set; }
    public string? SignatureUrl { get; set; }

    public string? DisposalReason { get; set; }
    public DateTime? DisposalDate { get; set; }
    public string? DisposalApprovedBy { get; set; }

    public List<LostFoundPhotoDto> Photos { get; set; } = new();
    public List<LostFoundTimelineEntry> Timeline { get; set; } = new();
    public List<LostFoundAuditEntry> AuditLog { get; set; } = new();
    public string? Remark { get; set; }
}

public sealed class LostFoundPhotoDto
{
    public string Url { get; set; } = "";
    public string Label { get; set; } = "";
    public string Category { get; set; } = "";
}

public sealed class LostFoundTimelineEntry
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = "";
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#64748B";
}

public sealed class LostFoundAuditEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public string? Device { get; set; }
}

public sealed class LostFoundKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#64748B";
    public string BgColor { get; set; } = "#F8FAFC";
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
}

public sealed class LostFoundTreeItem : ObservableObject
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public bool IsSelected { get; set; }
    public bool IsExpanded { get; set; }
    public string? FilterKey { get; set; }
    public string? FilterValue { get; set; }
    public ObservableCollection<LostFoundTreeItem> Children { get; } = new();
}

public sealed class LostFoundPaginationButton
{
    public string Label { get; init; } = "";
    public int? PageNumber { get; init; }
    public bool IsCurrent { get; init; }
    public bool IsEllipsis { get; init; }

    public LostFoundPaginationButton(string label, int? pageNumber, bool isCurrent = false, bool isEllipsis = false)
    {
        Label = label; PageNumber = pageNumber; IsCurrent = isCurrent; IsEllipsis = isEllipsis;
    }
}

// ═══ FORM STATE ═══

public sealed class LostFoundFormField
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string? Placeholder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsReadOnly { get; set; }
    public bool IsMultiline { get; set; }
    public string? FieldType { get; set; }
}

// ═══ VIEW MODEL ═══

public sealed partial class LostFoundViewModel : ViewModelBase
{
    private readonly ILostFoundQueryService? _queryService;
    private readonly ILostFoundCommandService? _commandService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private LostFoundDashboardDto? _realDashboard;
    private int _realTotalCount;
    private bool _isRealDataLoaded;

    public LostFoundViewModel() { }

    public LostFoundViewModel(
        ILostFoundQueryService queryService,
        IFrontOfficeExecutionContextAccessor executionContext,
        ILostFoundCommandService? commandService = null)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
        _executionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
        _commandService = commandService;
    }

    private Guid? CurrentPropertyId => _executionContext?.Current?.Property.PropertyId.Value;
    private Guid? CurrentActorId => _executionContext?.Current?.Actor.ActorId?.Value;
    private string CurrentActorName => Supervisors.FirstOrDefault() ?? "Front Office";

    private async Task ReloadRealItemAsync(string itemCode)
    {
        await LoadAsync(CancellationToken.None);
        SelectItem(Items.FirstOrDefault(item => item.ItemCode == itemCode));
    }

    public ObservableCollection<LostFoundItemDto> Items { get; } = new();
    public ObservableCollection<LostFoundItemDto> FilteredItems { get; } = new();
    public ObservableCollection<LostFoundKpi> DashboardKpis { get; } = new();
    public ObservableCollection<LostFoundTimelineEntry> ItemTimeline { get; } = new();
    public ObservableCollection<LostFoundAuditEntry> ItemAuditLog { get; } = new();
    public ObservableCollection<LostFoundTreeItem> TreeItems { get; } = new();
    public ObservableCollection<LostFoundPaginationButton> PageButtons { get; } = new();

    [ObservableProperty] private LostFoundItemDto? _selectedItem;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private LostFoundTreeItem? _selectedTreeItem;
    [ObservableProperty] private string _viewMode = "Grid";

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;

    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterCategory;
    [ObservableProperty] private string? _filterFloor;
    [ObservableProperty] private string? _filterFoundBy;
    [ObservableProperty] private string? _filterValueRange;
    [ObservableProperty] private bool _filterHighValueOnly;

    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 20;
    [ObservableProperty] private int _totalFilteredCount;

    // ═══ FORM STATE ═══
    [ObservableProperty] private bool _isFormOpen;
    [ObservableProperty] private string _formMode = string.Empty;
    [ObservableProperty] private string _formTitle = string.Empty;
    [ObservableProperty] private string _formErrorMessage = string.Empty;

    [ObservableProperty] private string _recordItemName = string.Empty;
    [ObservableProperty] private string _recordCategory = "Khác";
    [ObservableProperty] private string _recordDescription = string.Empty;
    [ObservableProperty] private string _recordColor = string.Empty;
    [ObservableProperty] private string _recordBrand = string.Empty;
    [ObservableProperty] private string _recordSerial = string.Empty;
    [ObservableProperty] private decimal _recordValue;
    [ObservableProperty] private DateTime _recordFoundDate = DateTime.Today;
    [ObservableProperty] private string _recordFoundTime = "08:00";
    [ObservableProperty] private string _recordLocation = string.Empty;
    [ObservableProperty] private string _recordRoom = string.Empty;
    [ObservableProperty] private string _recordFoundBy = string.Empty;
    [ObservableProperty] private string _recordSupervisor = string.Empty;
    [ObservableProperty] private string _recordRemark = string.Empty;
    [ObservableProperty] private string _recordPhotoPath = string.Empty;

    [ObservableProperty] private string _editItemName = string.Empty;
    [ObservableProperty] private string _editCategory = string.Empty;
    [ObservableProperty] private string _editDescription = string.Empty;
    [ObservableProperty] private string _editColor = string.Empty;
    [ObservableProperty] private string _editBrand = string.Empty;
    [ObservableProperty] private string _editSerial = string.Empty;
    [ObservableProperty] private decimal _editValue;
    [ObservableProperty] private string _editRemark = string.Empty;

    [ObservableProperty] private DateTime _contactDate = DateTime.Today;
    [ObservableProperty] private string _contactBy = string.Empty;
    [ObservableProperty] private string _contactPhone = string.Empty;
    [ObservableProperty] private string _contactEmail = string.Empty;
    [ObservableProperty] private string _contactMethod = "Phone";
    [ObservableProperty] private string _contactResult = string.Empty;
    [ObservableProperty] private string _contactRemark = string.Empty;

    [ObservableProperty] private string _returnRecipientName = string.Empty;
    [ObservableProperty] private string _returnRecipientId = string.Empty;
    [ObservableProperty] private string _returnRecipientRelation = "Chính chủ";
    [ObservableProperty] private DateTime _returnDate = DateTime.Today;
    [ObservableProperty] private string _returnTime = "08:00";
    [ObservableProperty] private string _returnMethod = "Trực tiếp";
    [ObservableProperty] private string _returnSignaturePath = string.Empty;
    [ObservableProperty] private string _returnRemark = string.Empty;

    [ObservableProperty] private string _disposalReason = string.Empty;
    [ObservableProperty] private DateTime _disposalDate = DateTime.Today;
    [ObservableProperty] private string _disposalApprovedBy = string.Empty;
    [ObservableProperty] private string _disposalRemark = string.Empty;

    public bool IsRecordForm => FormMode == "Record";
    public bool IsEditForm => FormMode == "Edit";
    public bool IsContactForm => FormMode == "Contact";
    public bool IsReturnForm => FormMode == "Return";
    public bool IsDisposalForm => FormMode == "Dispose";
    public bool IsFormClosed => !IsFormOpen;
    public int TotalPages => Math.Max(1, (TotalFilteredCount + PageSize - 1) / PageSize);
    public string PageDisplay => TotalFilteredCount == 0 ? "0–0 / 0 tài sản" : $"{Math.Min((CurrentPage - 1) * PageSize + 1, TotalFilteredCount)}–{Math.Min(CurrentPage * PageSize, TotalFilteredCount)} / {TotalFilteredCount} tài sản";
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public bool HasSelection => SelectedItem is not null;
    public bool IsDetailClosed => !IsDetailOpen;
    public bool IsGridView => ViewMode == "Grid";
    public bool IsCardView => ViewMode == "Card";

    public int TotalItems => _realTotalCount > 0 ? _realTotalCount : Items.Count;
    public int NewCount => Items.Count(i => i.ItemStatus == "New");
    public int StoredCount => _realDashboard?.Stored ?? Items.Count(i => i.ItemStatus == "Stored");
    public int ContactingCount => _realDashboard?.Claimed ?? Items.Count(i => i.ItemStatus == "Contacting");
    public int ReturnedCount => _realDashboard?.Returned ?? Items.Count(i => i.ItemStatus == "Returned");
    public int DisposedCount => _realDashboard?.Disposed ?? Items.Count(i => i.ItemStatus == "Disposed");
    public int OverdueCount => _realDashboard?.Unclaimed ?? Items.Count(i => i.ItemStatus == "Overdue");
    public int HighValueCount => Items.Count(i => i.IsHighValue);
    public int TodayCount => Items.Count(i => i.FoundDate.HasValue && i.FoundDate.Value.Date == DateTime.Today);
    public int ThisWeekCount => Items.Count(i => i.FoundDate.HasValue && i.FoundDate.Value >= DateTime.Today.AddDays(-7));

    public static string[] StatusFilters { get; } = ["Tất cả", "New", "Stored", "Contacting", "Returned", "Disposed", "Overdue"];
    public static string[] CategoryFilters { get; } = ["Tất cả", "Tiền", "Trang sức", "Điện thoại", "Laptop", "Máy tính bảng", "Đồng hồ", "Quần áo", "Ví", "Hộ chiếu", "CMND", "Thẻ NH", "Đồ điện tử", "Khác"];
    public string[] FloorFilters { get; private set; } = [];
    public string[] FoundByFilters { get; private set; } = [];
    public static string[] ValueRangeFilters { get; } = ["Tất cả", "< 1 triệu", "1-5 triệu", "5-10 triệu", "10-50 triệu", "> 50 triệu"];
    public static int[] PageSizeOptions { get; } = [20, 50, 100];

    public static string[] Housekeepers { get; } = [
        "Nguyễn Thị Lan", "Trần Thị Mai", "Lê Thị Hoa", "Phạm Thị Cúc", "Hoàng Thị Hương",
        "Đỗ Thị Hồng", "Vũ Thị Đào", "Bùi Thị Sen", "Ngô Thị Lài", "Dương Thị Huệ",
        "Lý Thị Thắm", "Đinh Thị Yến"
    ];
    public static string[] Supervisors { get; } = ["Nguyễn Văn Giám Sát", "Trần Thị Kiểm Tra", "Lê Văn Thanh Tra"];
    public static string[] StorageLocations { get; } = ["Kho A - Kệ 1", "Kho A - Kệ 2", "Kho B - Kệ 1", "Kho B - Kệ 2", "Kho VIP"];
    public static string[] ContactMethods { get; } = ["Phone", "Email", "SMS", "Zalo"];

    private static readonly string[] Categories = ["Tiền", "Trang sức", "Điện thoại", "Laptop", "Máy tính bảng", "Đồng hồ", "Quần áo", "Ví", "Hộ chiếu", "CMND", "Thẻ NH", "Đồ điện tử", "Khác"];
    private static readonly string[] CategoryNames = [
        "Tiền mặt", "Nhẫn vàng", "iPhone 15", "MacBook Pro", "iPad Air",
        "Rolex Submariner", "Áo khoác", "Ví da", "Hộ chiếu Mỹ", "CMND",
        "Thẻ Visa", "Sạc dự phòng", "Tai nghe Bluetooth"
    ];
    private static readonly string[] Statuses = ["New", "New", "Stored", "Stored", "Stored", "Contacting", "Returned", "Returned", "Returned", "Returned", "Disposed", "Overdue"];
    private static readonly string[] GuestNames = ["Nguyễn Văn An", "Trần Thị Bình", "John Smith", "Kim Min-Ji", "Tanaka Hiroshi", "Chen Wei", "Sophie Martin", "Hans Mueller", "Anna Rossi", "Park Ji-Sung", "David Chen", "Maria Garcia"];
    private static readonly string[] DisposalReasons = ["Quá 90 ngày không liên lạc được", "Khách từ chối nhận lại", "Hỏng không sử dụng được", "Khách ủy quyền thanh lý", "Chính sách khách sạn"];

    // ═══ INIT ═══

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Items.Clear(); await Task.Delay(120, ct);
            if (_queryService is not null)
            {
                await LoadRealDataAsync(ct);
                return;
            }

            ErrorMessage = "Chưa có Lost & Found query service; không hiển thị dữ liệu mẫu.";
            _realDashboard = null;
            _realTotalCount = 0;
            _isRealDataLoaded = false;
            Items.Clear();
            FilteredItems.Clear();
            BuildDashboard();
            BuildTree();
            ApplyFilters();
            return;

            // Legacy designer fixture retained only for historical reference.
#if false // Legacy designer fixture retained for historical reference only.
#pragma warning disable CS0162
            var rng = new Random(55);
            var itemCode = 1000;

            foreach (var cat in Categories)
            {
                var count = rng.Next(8, 20);
                for (int i = 0; i < count; i++)
                {
                    itemCode++;
                    var code = $"LF{itemCode}";
                    var status = Statuses[rng.Next(Statuses.Length)];
                    var catIdx = Array.IndexOf(Categories, cat);
                    var hk = Housekeepers[rng.Next(Housekeepers.Length)];
                    var sup = Supervisors[rng.Next(Supervisors.Length)];
                    var foundDate = DateTime.Now.AddDays(-rng.Next(0, 120));
                    var floor = rng.Next(1, 11).ToString();
                    var roomNum = $"{floor}{rng.Next(1, 16):D2}";
                    var guest = GuestNames[rng.Next(GuestNames.Length)];
                    var value = cat switch
                    {
                        "Tiền" => rng.Next(100000, 10000000),
                        "Trang sức" => rng.Next(500000, 50000000),
                        "Điện thoại" => rng.Next(1000000, 20000000),
                        "Laptop" => rng.Next(3000000, 40000000),
                        "Đồng hồ" => rng.Next(1000000, 100000000),
                        _ => rng.Next(50000, 5000000)
                    };

                    var item = new LostFoundItemDto
                    {
                        ItemCode = code, ItemName = CategoryNames[catIdx],
                        Category = cat, Description = $"{cat} để quên tại phòng {roomNum}",
                        Color = rng.Next(3) switch { 0 => "Đen", 1 => "Trắng", _ => "Xám" },
                        Brand = rng.Next(3) == 1 ? cat switch { "Điện thoại" => "Apple", "Laptop" => "Dell", "Đồng hồ" => "Rolex", _ => null } : null,
                        SerialNumber = rng.Next(4) == 1 ? $"SN{code}" : null,
                        EstimatedValue = value,
                        RoomNumber = roomNum, Floor = floor, Building = rng.Next(3) switch { 0 => "A", 1 => "B", _ => "C" },
                        FoundLocation = $"Phòng {roomNum}, " + (rng.Next(3) switch { 0 => "trong ngăn kéo", 1 => "trong safe", _ => "dưới gầm giường" }),
                        FoundBy = hk, Supervisor = sup, FoundDate = foundDate,
                        ItemStatus = status,
                        GuestName = status is not ("Disposed" or "Overdue") || rng.Next(3) > 0 ? guest : null,
                        GuestPhone = rng.Next(3) > 0 ? $"0{rng.Next(100000000, 999999999)}" : null,
                        GuestEmail = rng.Next(3) > 0 ? $"{guest?.Split(' ').LastOrDefault()?.ToLower()}@email.com" : null,
                        ReservationNumber = rng.Next(2) == 1 ? $"RES{rng.Next(10000, 99999)}" : null,
                        FolioNumber = rng.Next(2) == 1 ? $"FOL{rng.Next(1000, 9999)}" : null,
                        GuestArrival = foundDate.AddDays(-rng.Next(1, 5)),
                        GuestDeparture = foundDate.AddDays(rng.Next(1, 3)),
                        ContactDate = status is "Contacting" or "Returned" ? foundDate.AddDays(rng.Next(1, 7)) : null,
                        ContactBy = status is "Contacting" or "Returned" ? sup : null,
                        ContactMethod = status is "Contacting" or "Returned" ? ContactMethods[rng.Next(ContactMethods.Length)] : null,
                        ContactResult = status is "Contacting" ? "Đang chờ phản hồi" : status == "Returned" ? "Khách đồng ý nhận lại" : null,
                        StorageLocation = status is not "New" ? StorageLocations[rng.Next(StorageLocations.Length)] : null,
                        ShelfNumber = status is not "New" ? $"Kệ {rng.Next(1, 9)}" : null,
                        Compartment = status is not "New" ? $"Ngăn {rng.Next(1, 6)}" : null,
                        SealNumber = status is not "New" ? $"SEAL{code}" : null,
                        StorageDate = status is not "New" ? foundDate.AddHours(rng.Next(1, 24)) : null,
                        StoredBy = status is not "New" ? sup : null,
                        ReturnedTo = status == "Returned" ? guest : null,
                        RecipientId = status == "Returned" ? $"CMND {rng.Next(100000000, 999999999)}" : null,
                        RecipientRelation = status == "Returned" ? "Chính chủ" : null,
                        ReturnDate = status == "Returned" ? foundDate.AddDays(rng.Next(3, 14)) : null,
                        ReturnMethod = status == "Returned" ? (rng.Next(2) == 1 ? "Trực tiếp" : "Chuyển phát nhanh") : null,
                        DisposalReason = status == "Disposed" ? DisposalReasons[rng.Next(DisposalReasons.Length)] : null,
                        DisposalDate = status == "Disposed" ? foundDate.AddDays(90) : null,
                        DisposalApprovedBy = status == "Disposed" ? Supervisors[0] : null,
                        Remark = status == "Overdue" ? "Quá 90 ngày — cần thanh lý" : value >= 5000000 ? "Tài sản giá trị cao — cần Supervisor duyệt" : null,
                    };

                    if (status is not "New")
                    {
                        item.Photos.Add(new LostFoundPhotoDto { Label = "Found", Category = "Khi phát hiện", Url = $"lf_{code}_found.jpg" });
                        item.Photos.Add(new LostFoundPhotoDto { Label = "Storage", Category = "Nhập kho", Url = $"lf_{code}_storage.jpg" });
                    }
                    if (status == "Returned")
                        item.Photos.Add(new LostFoundPhotoDto { Label = "Return", Category = "Trả khách", Url = $"lf_{code}_return.jpg" });

                    item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = foundDate, Event = "Phát hiện", Detail = $"{hk} tìm thấy tại phòng {roomNum}", Color = "#D97706" });
                    item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = foundDate.AddMinutes(30), Event = "Ghi nhận", Detail = $"Mã {code} — {sup}", Color = "#2563EB" });
                    if (status is not "New")
                        item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = foundDate.AddHours(2), Event = "Nhập kho", Detail = item.StorageLocation ?? "Kho", Color = "#0891B2" });
                    if (status is "Contacting" or "Returned" && item.ContactDate.HasValue)
                        item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = item.ContactDate.Value, Event = "Liên hệ khách", Detail = $"{item.ContactMethod} — {item.ContactResult}", Color = "#7C3AED" });
                    if (status == "Returned" && item.ReturnDate.HasValue)
                        item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = item.ReturnDate.Value, Event = "Đã trả", Detail = $"{item.ReturnedTo} — {item.ReturnMethod}", Color = "#16A34A" });
                    if (status == "Disposed" && item.DisposalDate.HasValue)
                        item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = item.DisposalDate.Value, Event = "Thanh lý", Detail = item.DisposalReason ?? "Hết hạn", Color = "#64748B" });

                    item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = foundDate, Action = "Created", Actor = hk, Detail = $"Phát hiện {item.ItemName} tại phòng {roomNum}", Device = "Housekeeping App" });
                    item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = foundDate.AddMinutes(30), Action = "Recorded", Actor = sup, Detail = $"Ghi nhận tài sản {code}", Device = "Front Office" });
                    if (status is not "New") item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = foundDate.AddHours(2), Action = "Stored", Actor = sup, Detail = $"Nhập kho: {item.StorageLocation}", Device = "Front Office" });
                    if (status is "Contacting" or "Returned" && item.ContactDate.HasValue)
                        item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = item.ContactDate.Value, Action = "Contacted", Actor = item.ContactBy ?? sup, Detail = $"Liên hệ qua {item.ContactMethod}", Device = "Front Office" });
                    if (status == "Returned" && item.ReturnDate.HasValue)
                        item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = item.ReturnDate.Value, Action = "Returned", Actor = sup, Detail = $"Trả cho {item.ReturnedTo}", Device = "Front Office" });
                    if (status == "Disposed")
                        item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = item.DisposalDate ?? foundDate.AddDays(90), Action = "Disposed", Actor = item.DisposalApprovedBy ?? Supervisors[0], Detail = item.DisposalReason ?? "Thanh lý", Device = "Front Office" });

                    Items.Add(item);
                }
            }

            FloorFilters = Enumerable.Range(1, 10).Select(f => f.ToString()).Prepend("Tất cả").ToArray();
            FoundByFilters = Housekeepers.Prepend("Tất cả").ToArray();
            OnPropertyChanged(nameof(FloorFilters)); OnPropertyChanged(nameof(FoundByFilters));
            BuildDashboard(); BuildTree(); ApplyFilters();
            SelectTreeItem(TreeItems.FirstOrDefault());
#pragma warning restore CS0162
#endif
        }
        catch (Exception ex) { ErrorMessage = $"Lỗi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    private async Task LoadRealDataAsync(CancellationToken ct)
    {
        if (_queryService is null || _executionContext is null) return;

        var propertyId = _executionContext.Current?.Property.PropertyId.Value;
        if (propertyId is null)
        {
            ErrorMessage = "Chưa có ngữ cảnh cơ sở để tải Lost & Found.";
            _realDashboard = null;
            _realTotalCount = 0;
            _isRealDataLoaded = true;
            Items.Clear();
            FilteredItems.Clear();
            BuildDashboard();
            BuildTree();
            RebuildPageButtons();
            return;
        }

        var result = await _queryService.SearchAsync(
            new LostFoundSearchFilter(
                propertyId.Value,
                SearchText,
                ParseStatus(FilterStatus),
                null,
                null,
                null,
                ParseCategory(FilterCategory)),
            CurrentPage,
            PageSize,
            ct);

        Items.Clear();
        foreach (var item in result.Items)
            Items.Add(MapItem(item));

        _realDashboard = result.Dashboard;
        _realTotalCount = result.TotalCount;
        _isRealDataLoaded = true;
        BuildDashboard();
        BuildTree();
        ApplyLoadedPage();
        if (SelectedItem is null)
            SelectItem(FilteredItems.FirstOrDefault());
    }

    private void ApplyLoadedPage()
    {
        TotalFilteredCount = _realTotalCount;
        FilteredItems.Clear();
        foreach (var item in Items)
            FilteredItems.Add(item);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay));
        OnPropertyChanged(nameof(HasPrevPage));
        OnPropertyChanged(nameof(HasNextPage));
    }

    private static LostFoundItemDto MapItem(LostFoundItemReadDto item)
    {
        var status = item.Status switch
        {
            LostFoundCoreStatus.Found or LostFoundCoreStatus.Matched => "New",
            LostFoundCoreStatus.Stored => "Stored",
            LostFoundCoreStatus.Claimed or LostFoundCoreStatus.Contacted => "Contacting",
            LostFoundCoreStatus.Returned => "Returned",
            LostFoundCoreStatus.Disposed or LostFoundCoreStatus.Archived => "Disposed",
            _ => "New"
        };

        var timeline = item.History
            .Select(h => new LostFoundTimelineEntry { Timestamp = h.OccurredAtUtc.ToLocalTime(), Event = h.Action, Detail = h.Action, Color = "#2563EB" })
            .ToList();
        var auditLog = item.History
            .Select(h => new LostFoundAuditEntry { Timestamp = h.OccurredAtUtc.ToLocalTime(), Action = h.Action, Actor = "System", Detail = h.Action, Device = "Front Office" })
            .ToList();

        return new LostFoundItemDto
        {
            PersistentId = item.LostFoundItemId,
            ItemCode = item.ItemNumber,
            ItemName = item.ItemName,
            Category = item.Category,
            Description = item.Description ?? string.Empty,
            Color = item.Color,
            Brand = item.Brand,
            SerialNumber = item.SerialNumber,
            EstimatedValue = 0,
            FoundLocation = item.FoundLocation,
            FoundBy = item.FoundByStaff,
            FoundDate = item.FoundDateTime.ToLocalTime(),
            ItemStatus = status,
            GuestName = item.OwnerGuestName,
            ReservationNumber = item.ReservationId?.ToString(),
            StorageLocation = item.StoredLocation,
            ReturnDate = item.ClaimedDate?.ToLocalTime(),
            ReturnedTo = item.ReleasedTo,
            ContactBy = item.ReleasedBy,
            ContactDate = item.ContactDate?.ToLocalTime(),
            ContactMethod = item.ContactMethod,
            ContactResult = item.ContactResult,
            ContactRemark = item.ContactNotes,
            DisposalDate = item.DisposalDate?.ToLocalTime(),
            Remark = item.Notes,
            Timeline = timeline,
            AuditLog = auditLog
        };
    }

    private static LostFoundAppStatus? ParseStatus(string? value) => value switch
    {
        "New" => LostFoundAppStatus.Found,
        "Stored" => LostFoundAppStatus.Stored,
        "Contacting" => LostFoundAppStatus.Contacted,
        "Returned" => LostFoundAppStatus.Returned,
        "Disposed" => LostFoundAppStatus.Disposed,
        "Overdue" => LostFoundAppStatus.Unclaimed,
        _ => null
    };

    private static string? ParseCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Contains("Tất", StringComparison.OrdinalIgnoreCase)) return null;
        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            var v when v.Contains("tiền") => LostFoundCoreCategory.Cash.ToString(),
            var v when v.Contains("trang") => LostFoundCoreCategory.Jewelry.ToString(),
            var v when v.Contains("điện") || v.Contains("laptop") || v.Contains("máy tính") => LostFoundCoreCategory.Electronics.ToString(),
            var v when v.Contains("hộ chiếu") || v.Contains("cmnd") || v.Contains("giấy") => LostFoundCoreCategory.Documents.ToString(),
            var v when v.Contains("quần") || v.Contains("áo") => LostFoundCoreCategory.Clothing.ToString(),
            _ => LostFoundCoreCategory.Others.ToString()
        };
    }

    private static LostFoundCoreCategory ParseCoreCategory(string? value)
    {
        var parsed = ParseCategory(value);
        return Enum.TryParse<LostFoundCoreCategory>(parsed, true, out var category)
            ? category
            : LostFoundCoreCategory.Others;
    }

    private void BuildDashboard()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new LostFoundKpi { Label = "Tổng tài sản", Value = TotalItems.ToString(), Color = "#4B5563", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Mới phát hiện", Value = NewCount.ToString(), Color = "#D97706", BgColor = "#FFF7ED", FilterKey = "Status", FilterValue = "New" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Đã lưu kho", Value = StoredCount.ToString(), Color = "#2563EB", BgColor = "#EFF6FF", FilterKey = "Status", FilterValue = "Stored" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Đang liên hệ", Value = ContactingCount.ToString(), Color = "#7C3AED", BgColor = "#F5F3FF", FilterKey = "Status", FilterValue = "Contacting" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Đã trả", Value = ReturnedCount.ToString(), Color = "#16A34A", BgColor = "#F0FDF4", FilterKey = "Status", FilterValue = "Returned" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Đã thanh lý", Value = DisposedCount.ToString(), Color = "#64748B", BgColor = "#F1F5F9", FilterKey = "Status", FilterValue = "Disposed" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Quá hạn", Value = OverdueCount.ToString(), Color = "#DC2626", BgColor = "#FEF2F2", FilterKey = "Status", FilterValue = "Overdue" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Giá trị cao", Value = HighValueCount.ToString(), Color = "#E32F70", BgColor = "#FFF0F5" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Hôm nay", Value = TodayCount.ToString(), Color = "#0891B2", BgColor = "#ECFEFF" });
        DashboardKpis.Add(new LostFoundKpi { Label = "Tuần này", Value = ThisWeekCount.ToString(), Color = "#4F46E5", BgColor = "#EEF2FF" });
    }

    private void BuildTree()
    {
        TreeItems.Clear();
        TreeItems.Add(new LostFoundTreeItem { Label = "Tất cả", Count = TotalItems, IsSelected = true });
        TreeItems.Add(new LostFoundTreeItem { Label = "Mới phát hiện", Count = NewCount, FilterKey = "Status", FilterValue = "New" });
        TreeItems.Add(new LostFoundTreeItem { Label = "Đã lưu kho", Count = StoredCount, FilterKey = "Status", FilterValue = "Stored" });
        TreeItems.Add(new LostFoundTreeItem { Label = "Đang liên hệ", Count = ContactingCount, FilterKey = "Status", FilterValue = "Contacting" });
        TreeItems.Add(new LostFoundTreeItem { Label = "Đã trả", Count = ReturnedCount, FilterKey = "Status", FilterValue = "Returned" });
        TreeItems.Add(new LostFoundTreeItem { Label = "Đã thanh lý", Count = DisposedCount, FilterKey = "Status", FilterValue = "Disposed" });
        TreeItems.Add(new LostFoundTreeItem { Label = "Quá hạn", Count = OverdueCount, FilterKey = "Status", FilterValue = "Overdue" });
        TreeItems.Add(new LostFoundTreeItem { Label = "Giá trị cao", Count = HighValueCount, FilterKey = "HighValue", FilterValue = "true" });
    }

    private void BuildTimeline(LostFoundItemDto item) { ItemTimeline.Clear(); foreach (var e in item.Timeline.OrderBy(t => t.Timestamp)) ItemTimeline.Add(e); }
    private void BuildAuditLog(LostFoundItemDto item) { ItemAuditLog.Clear(); foreach (var e in item.AuditLog.OrderBy(t => t.Timestamp)) ItemAuditLog.Add(e); }

    // ═══ FILTERS ═══

    [RelayCommand] private void ApplyFilters()
    {
        if (_queryService is not null && _isRealDataLoaded)
        {
            _ = LoadRealDataAsync(CancellationToken.None);
            return;
        }
        var q = Items.AsEnumerable();
        var s = SearchText?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(s))
            q = q.Where(r => r.ItemCode.ToLower().Contains(s) || r.ItemName.ToLower().Contains(s) || (r.GuestName?.ToLower().Contains(s) ?? false) || (r.RoomNumber?.ToLower().Contains(s) ?? false));
        if (!string.IsNullOrEmpty(FilterStatus) && FilterStatus != "Tất cả") q = q.Where(r => r.ItemStatus == FilterStatus);
        if (!string.IsNullOrEmpty(FilterCategory) && FilterCategory != "Tất cả") q = q.Where(r => r.Category == FilterCategory);
        if (!string.IsNullOrEmpty(FilterFloor) && FilterFloor != "Tất cả") q = q.Where(r => r.Floor == FilterFloor);
        if (!string.IsNullOrEmpty(FilterFoundBy) && FilterFoundBy != "Tất cả") q = q.Where(r => r.FoundBy == FilterFoundBy);
        if (FilterHighValueOnly) q = q.Where(r => r.IsHighValue);
        if (!string.IsNullOrEmpty(FilterValueRange) && FilterValueRange != "Tất cả")
            q = FilterValueRange switch
            {
                "< 1 triệu" => q.Where(r => r.EstimatedValue < 1000000),
                "1-5 triệu" => q.Where(r => r.EstimatedValue >= 1000000 && r.EstimatedValue < 5000000),
                "5-10 triệu" => q.Where(r => r.EstimatedValue >= 5000000 && r.EstimatedValue < 10000000),
                "10-50 triệu" => q.Where(r => r.EstimatedValue >= 10000000 && r.EstimatedValue < 50000000),
                "> 50 triệu" => q.Where(r => r.EstimatedValue >= 50000000),
                _ => q
            };
        if (SelectedTreeItem?.FilterKey is not null)
        {
            var k = SelectedTreeItem.FilterKey; var v = SelectedTreeItem.FilterValue;
            q = k switch { "Status" => q.Where(r => r.ItemStatus == v), "HighValue" => q.Where(r => r.IsHighValue), _ => q };
        }
        var list = q.OrderByDescending(r => r.EstimatedValue).ThenBy(r => r.ItemCode).ToList();
        TotalFilteredCount = list.Count; CurrentPage = Math.Min(CurrentPage, TotalPages);
        FilteredItems.Clear();
        foreach (var r in list.Skip((CurrentPage - 1) * PageSize).Take(PageSize)) FilteredItems.Add(r);
        RebuildPageButtons();
        OnPropertyChanged(nameof(PageDisplay)); OnPropertyChanged(nameof(HasPrevPage)); OnPropertyChanged(nameof(HasNextPage));
    }

    [RelayCommand] private void ClearFilters()
    {
        SearchText = string.Empty; FilterStatus = null; FilterCategory = null; FilterFloor = null;
        FilterFoundBy = null; FilterValueRange = null; FilterHighValueOnly = false;
        SelectTreeItem(TreeItems.FirstOrDefault()); ApplyFilters();
    }

    [RelayCommand] private void SelectTreeItem(LostFoundTreeItem? item)
    {
        if (item is null) return;
        foreach (var ti in TreeItems) DeselectTree(ti);
        item.IsSelected = true; SelectedTreeItem = item; CurrentPage = 1; ApplyFilters();
    }
    private static void DeselectTree(LostFoundTreeItem item) { item.IsSelected = false; foreach (var c in item.Children) DeselectTree(c); }

    [RelayCommand] private void ApplyKpiFilter(LostFoundKpi? kpi)
    {
        if (kpi?.FilterKey is null) return; ClearFilters();
        if (kpi.FilterKey == "Status") FilterStatus = kpi.FilterValue; ApplyFilters();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnFilterStatusChanged(string? value) => ApplyFilters();
    partial void OnFilterCategoryChanged(string? value) => ApplyFilters();
    partial void OnFilterFloorChanged(string? value) => ApplyFilters();
    partial void OnFilterFoundByChanged(string? value) => ApplyFilters();
    partial void OnFilterValueRangeChanged(string? value) => ApplyFilters();
    partial void OnFilterHighValueOnlyChanged(bool value) => ApplyFilters();

    // ═══ PAGINATION ═══

    [RelayCommand] private void FirstPage() { CurrentPage = 1; ApplyFilters(); }
    [RelayCommand] private void PrevPage() { if (HasPrevPage) { CurrentPage--; ApplyFilters(); } }
    [RelayCommand] private void NextPage() { if (HasNextPage) { CurrentPage++; ApplyFilters(); } }
    [RelayCommand] private void LastPage() { CurrentPage = TotalPages; ApplyFilters(); }
    [RelayCommand] private void SetPage(int? page) { if (page is null) return; CurrentPage = Math.Clamp(page.Value, 1, TotalPages); ApplyFilters(); }
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFilters(); }
    partial void OnViewModeChanged(string value) { OnPropertyChanged(nameof(IsGridView)); OnPropertyChanged(nameof(IsCardView)); }

    private void RebuildPageButtons()
    {
        PageButtons.Clear();
        var total = TotalPages; var current = Math.Clamp(CurrentPage, 1, total);
        IEnumerable<int?> pages = total <= 5 ? Enumerable.Range(1, total).Cast<int?>()
            : current <= 3 ? new int?[] { 1, 2, 3, 4, 5, null, total }
            : current >= total - 2 ? new int?[] { 1, null, total - 4, total - 3, total - 2, total - 1, total }
            : new int?[] { 1, null, current - 1, current, current + 1, null, total };
        foreach (var p in pages)
            PageButtons.Add(p is null ? new LostFoundPaginationButton("…", null, false, true)
                : new LostFoundPaginationButton(p.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), p.Value, p.Value == current));
    }

    // ═══ SELECTION ═══

    [RelayCommand] private void SelectItem(LostFoundItemDto? item) => SelectedItem = item;
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; SelectedItem = null; ItemTimeline.Clear(); ItemAuditLog.Clear(); }
    partial void OnSelectedItemChanged(LostFoundItemDto? value)
    {
        OnPropertyChanged(nameof(HasSelection));
        if (value is not null)
        {
            IsDetailOpen = true; SelectedDetailTab = 0;
            BuildTimeline(value); BuildAuditLog(value);
        }
    }
    partial void OnIsDetailOpenChanged(bool value) => OnPropertyChanged(nameof(IsDetailClosed));
    partial void OnSelectedDetailTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsGuestTab));
        OnPropertyChanged(nameof(IsStorageTab)); OnPropertyChanged(nameof(IsHistoryTab));
        OnPropertyChanged(nameof(IsPhotoTab)); OnPropertyChanged(nameof(IsRemarkTab));
    }
    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsGuestTab => SelectedDetailTab == 1;
    public bool IsStorageTab => SelectedDetailTab == 2;
    public bool IsHistoryTab => SelectedDetailTab == 3;
    public bool IsPhotoTab => SelectedDetailTab == 4;
    public bool IsRemarkTab => SelectedDetailTab == 5;
    partial void OnFormModeChanged(string value) { OnPropertyChanged(nameof(IsRecordForm)); OnPropertyChanged(nameof(IsEditForm)); OnPropertyChanged(nameof(IsContactForm)); OnPropertyChanged(nameof(IsReturnForm)); OnPropertyChanged(nameof(IsDisposalForm)); }
    partial void OnIsFormOpenChanged(bool value) => OnPropertyChanged(nameof(IsFormClosed));

    // ═══ WORKFLOW ═══

    [RelayCommand] private void OpenRecordForm()
    {
        FormMode = "Record"; FormTitle = "Ghi nhận tài sản thất lạc mới"; FormErrorMessage = string.Empty;
        RecordItemName = string.Empty; RecordCategory = "Khác"; RecordDescription = string.Empty;
        RecordColor = string.Empty; RecordBrand = string.Empty; RecordSerial = string.Empty;
        RecordValue = 0; RecordFoundDate = DateTime.Today; RecordFoundTime = "08:00";
        RecordLocation = string.Empty; RecordRoom = string.Empty;
        RecordFoundBy = Housekeepers[0]; RecordSupervisor = Supervisors[0];
        RecordRemark = string.Empty; RecordPhotoPath = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitRecordForm()
    {
        if (string.IsNullOrWhiteSpace(RecordItemName)) { FormErrorMessage = "Vui lòng nhập tên tài sản."; return; }
        if (string.IsNullOrWhiteSpace(RecordRoom)) { FormErrorMessage = "Vui lòng nhập số phòng."; return; }

        var foundDateTime = RecordFoundDate.Date + (TimeSpan.TryParse(RecordFoundTime, out var t) ? t : TimeSpan.Zero);
        if (_isRealDataLoaded)
        {
            if (_commandService is null || CurrentPropertyId is not Guid propertyId)
            {
                FormErrorMessage = "Dữ liệu thật chưa có đường ghi Lost & Found hoặc thiếu ngữ cảnh cơ sở.";
                return;
            }

            try
            {
                var result = await _commandService.RegisterAsync(new RegisterLostFoundItemCommand(
                    propertyId,
                    $"LF{DateTime.UtcNow:yyyyMMddHHmmssfff}",
                    RecordItemName,
                    ParseCoreCategory(RecordCategory),
                    RecordDescription,
                    RecordLocation,
                    foundDateTime.ToUniversalTime(),
                    RecordFoundBy,
                    null,
                    RecordColor,
                    RecordBrand,
                    RecordSerial,
                    RecordRemark,
                    CurrentActorId));
                IsFormOpen = false;
                InfoMessage = $"Đã ghi nhận tài sản {result.ItemNumber} — {RecordItemName}.";
                await ReloadRealItemAsync(result.ItemNumber);
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ex.Message;
                return;
            }
        }

        FormErrorMessage = "Chưa có dữ liệu Lost & Found thật để ghi nhận.";
        return;

#if false // Legacy designer action fallback retained for historical reference only.
#pragma warning disable CS0162
        var code = $"LF{Items.Count + 1001}";
        var item = new LostFoundItemDto
        {
            ItemCode = code, ItemName = RecordItemName, Category = RecordCategory,
            Description = RecordDescription, Color = string.IsNullOrWhiteSpace(RecordColor) ? null : RecordColor,
            Brand = string.IsNullOrWhiteSpace(RecordBrand) ? null : RecordBrand,
            SerialNumber = string.IsNullOrWhiteSpace(RecordSerial) ? null : RecordSerial,
            EstimatedValue = RecordValue,
            RoomNumber = RecordRoom, Floor = RecordRoom.Length > 0 ? RecordRoom[..1] : "1",
            FoundLocation = RecordLocation, FoundBy = RecordFoundBy, Supervisor = RecordSupervisor,
            FoundDate = foundDateTime, ItemStatus = "New",
            Remark = string.IsNullOrWhiteSpace(RecordRemark) ? null : RecordRemark,
        };
        if (!string.IsNullOrWhiteSpace(RecordPhotoPath))
            item.Photos.Add(new LostFoundPhotoDto { Label = "Found", Category = "Khi phát hiện", Url = RecordPhotoPath });

        item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = foundDateTime, Event = "Phát hiện", Detail = $"{RecordFoundBy} tìm thấy tại phòng {RecordRoom}", Color = "#D97706" });
        item.Timeline.Add(new LostFoundTimelineEntry { Timestamp = foundDateTime.AddMinutes(30), Event = "Ghi nhận", Detail = $"Mã {code} — {RecordSupervisor}", Color = "#2563EB" });
        item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = foundDateTime, Action = "Created", Actor = RecordFoundBy, Detail = $"Phát hiện {RecordItemName} tại phòng {RecordRoom}", Device = "Housekeeping App" });
        item.AuditLog.Add(new LostFoundAuditEntry { Timestamp = foundDateTime.AddMinutes(30), Action = "Recorded", Actor = RecordSupervisor, Detail = $"Ghi nhận tài sản {code}", Device = "Front Office" });

        Items.Add(item); IsFormOpen = false;
        BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = $"Đã ghi nhận tài sản {code} — {RecordItemName}.";
#pragma warning restore CS0162
#endif
    }

    [RelayCommand] private void OpenEditForm()
    {
        if (SelectedItem is null) return;
        FormMode = "Edit"; FormTitle = $"Chỉnh sửa tài sản {SelectedItem.ItemCode}"; FormErrorMessage = string.Empty;
        EditItemName = SelectedItem.ItemName; EditCategory = SelectedItem.Category;
        EditDescription = SelectedItem.Description; EditColor = SelectedItem.Color ?? "";
        EditBrand = SelectedItem.Brand ?? ""; EditSerial = SelectedItem.SerialNumber ?? "";
        EditValue = SelectedItem.EstimatedValue; EditRemark = SelectedItem.Remark ?? "";
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitEditForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(EditItemName)) { FormErrorMessage = "Vui lòng nhập tên tài sản."; return; }

        if (_isRealDataLoaded)
        {
            if (_commandService is null || SelectedItem.PersistentId is not Guid itemId || CurrentPropertyId is not Guid propertyId)
            {
                FormErrorMessage = "Dữ liệu thật chưa có đường sửa Lost & Found.";
                return;
            }

            try
            {
                var itemCode = SelectedItem.ItemCode;
                await _commandService.UpdateAsync(new UpdateLostFoundItemCommand(
                    itemId,
                    propertyId,
                    EditItemName,
                    ParseCoreCategory(EditCategory),
                    EditDescription,
                    EditColor,
                    EditBrand,
                    EditSerial,
                    EditRemark,
                    CurrentActorId));
                IsFormOpen = false;
                InfoMessage = $"Đã cập nhật tài sản {itemCode}.";
                await ReloadRealItemAsync(itemCode);
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ex.Message;
                return;
            }
        }

        FormErrorMessage = "Chưa có dữ liệu Lost & Found thật để cập nhật.";
        return;

#if false // Legacy designer action fallback retained for historical reference only.
#pragma warning disable CS0162
        SelectedItem.ItemName = EditItemName; SelectedItem.Category = EditCategory;
        SelectedItem.Description = EditDescription; SelectedItem.Color = string.IsNullOrWhiteSpace(EditColor) ? null : EditColor;
        SelectedItem.Brand = string.IsNullOrWhiteSpace(EditBrand) ? null : EditBrand;
        SelectedItem.SerialNumber = string.IsNullOrWhiteSpace(EditSerial) ? null : EditSerial;
        SelectedItem.EstimatedValue = EditValue; SelectedItem.Remark = string.IsNullOrWhiteSpace(EditRemark) ? null : EditRemark;
        IsFormOpen = false; BuildDashboard(); BuildTree(); ApplyFilters();
        InfoMessage = $"Đã cập nhật tài sản {SelectedItem.ItemCode}.";
#pragma warning restore CS0162
#endif
    }

    [RelayCommand] private void OpenContactForm()
    {
        if (SelectedItem is null || SelectedItem.ItemStatus is not ("Stored" or "New")) return;
        FormMode = "Contact"; FormTitle = $"Liên hệ khách — {SelectedItem.GuestName}"; FormErrorMessage = string.Empty;
        ContactDate = DateTime.Today; ContactBy = Supervisors[0];
        ContactPhone = SelectedItem.GuestPhone ?? ""; ContactEmail = SelectedItem.GuestEmail ?? "";
        ContactMethod = "Phone"; ContactResult = string.Empty; ContactRemark = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitContactForm()
    {
        if (SelectedItem is null) return;
        if (_isRealDataLoaded)
        {
            if (_commandService is null || SelectedItem.PersistentId is not Guid itemId || CurrentPropertyId is not Guid propertyId)
            {
                FormErrorMessage = "Dữ liệu thật chưa có đường ghi liên hệ Lost & Found.";
                return;
            }

            var result = string.IsNullOrWhiteSpace(ContactResult) ? "Đang chờ phản hồi" : ContactResult.Trim();
            try
            {
                var itemCode = SelectedItem.ItemCode;
                await _commandService.ContactAsync(new ContactLostFoundItemCommand(
                    itemId,
                    propertyId,
                    ContactBy,
                    ContactDate.ToUniversalTime(),
                    ContactMethod,
                    result,
                    ContactRemark,
                    CurrentActorId));
                IsFormOpen = false;
                InfoMessage = $"Đã ghi nhận liên hệ khách cho {itemCode}.";
                await ReloadRealItemAsync(itemCode);
            }
            catch (Exception ex)
            {
                FormErrorMessage = ex.Message;
            }
            return;
        }

        FormErrorMessage = "Chưa có dữ liệu Lost & Found thật để ghi nhận liên hệ.";
        return;

#if false // Legacy designer action fallback retained for historical reference only.
#pragma warning disable CS0162
        SelectedItem.ItemStatus = "Contacting"; SelectedItem.ContactDate = ContactDate;
        SelectedItem.ContactBy = ContactBy; SelectedItem.ContactMethod = ContactMethod;
        SelectedItem.ContactResult = string.IsNullOrWhiteSpace(ContactResult) ? "Đang chờ phản hồi" : ContactResult;
        SelectedItem.Timeline.Add(new LostFoundTimelineEntry { Timestamp = ContactDate, Event = "Liên hệ khách", Detail = $"{ContactMethod} — {ContactResult}", Color = "#7C3AED" });
        SelectedItem.AuditLog.Add(new LostFoundAuditEntry { Timestamp = ContactDate, Action = "Contacted", Actor = ContactBy, Detail = $"Liên hệ qua {ContactMethod}", Device = "Front Office" });
        IsFormOpen = false; BuildDashboard(); BuildTree(); BuildTimeline(SelectedItem); BuildAuditLog(SelectedItem); ApplyFilters();
        InfoMessage = $"Đã liên hệ khách cho {SelectedItem.ItemCode}.";
#pragma warning restore CS0162
#endif
    }

    [RelayCommand] private void OpenReturnForm()
    {
        if (SelectedItem is null || SelectedItem.ItemStatus is not ("Contacting" or "Stored")) return;
        FormMode = "Return"; FormTitle = $"Trả tài sản {SelectedItem.ItemCode}"; FormErrorMessage = string.Empty;
        ReturnRecipientName = SelectedItem.GuestName ?? ""; ReturnRecipientId = "";
        ReturnRecipientRelation = "Chính chủ"; ReturnDate = DateTime.Today; ReturnTime = "08:00";
        ReturnMethod = "Trực tiếp"; ReturnSignaturePath = string.Empty; ReturnRemark = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitReturnForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(ReturnRecipientName)) { FormErrorMessage = "Vui lòng nhập tên người nhận."; return; }
        if (SelectedItem.IsHighValue && string.IsNullOrWhiteSpace(ReturnSignaturePath))
        { FormErrorMessage = "Tài sản giá trị cao — yêu cầu chữ ký người nhận."; return; }

        var returnDateTime = ReturnDate.Date + (TimeSpan.TryParse(ReturnTime, out var t2) ? t2 : TimeSpan.Zero);
        if (_isRealDataLoaded)
        {
            if (_commandService is null || SelectedItem.PersistentId is not Guid itemId || CurrentPropertyId is not Guid propertyId)
            {
                FormErrorMessage = "Dữ liệu thực chưa có đường ghi Lost & Found.";
                return;
            }

            try
            {
                var itemCode = SelectedItem.ItemCode;
                await _commandService.ReturnAsync(new ReturnLostFoundItemCommand(
                    itemId, propertyId, CurrentActorName, ReturnRecipientName,
                    returnDateTime.ToUniversalTime(), CurrentActorId));
                IsFormOpen = false;
                InfoMessage = $"Đã trả tài sản {itemCode} cho {ReturnRecipientName}.";
                await ReloadRealItemAsync(itemCode);
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ex.Message;
                return;
            }
        }

        SelectedItem.ItemStatus = "Returned"; SelectedItem.ReturnDate = returnDateTime;
        SelectedItem.ReturnedTo = ReturnRecipientName; SelectedItem.RecipientId = ReturnRecipientId;
        SelectedItem.RecipientRelation = ReturnRecipientRelation; SelectedItem.ReturnMethod = ReturnMethod;
        SelectedItem.SignatureUrl = string.IsNullOrWhiteSpace(ReturnSignaturePath) ? null : ReturnSignaturePath;
        SelectedItem.Timeline.Add(new LostFoundTimelineEntry { Timestamp = returnDateTime, Event = "Đã trả", Detail = $"{ReturnRecipientName} — {ReturnMethod}", Color = "#16A34A" });
        SelectedItem.AuditLog.Add(new LostFoundAuditEntry { Timestamp = returnDateTime, Action = "Returned", Actor = Supervisors[0], Detail = $"Trả cho {ReturnRecipientName}", Device = "Front Office" });
        IsFormOpen = false; BuildDashboard(); BuildTree(); BuildTimeline(SelectedItem); BuildAuditLog(SelectedItem); ApplyFilters();
        InfoMessage = $"Đã trả tài sản {SelectedItem.ItemCode} cho {ReturnRecipientName}.";
    }

    [RelayCommand] private void OpenDisposalForm()
    {
        if (SelectedItem is null || SelectedItem.ItemStatus is "Returned") return;
        FormMode = "Dispose"; FormTitle = $"Thanh lý tài sản {SelectedItem.ItemCode}"; FormErrorMessage = string.Empty;
        DisposalReason = DisposalReasons[0]; DisposalDate = DateTime.Today;
        DisposalApprovedBy = Supervisors[0]; DisposalRemark = string.Empty;
        IsFormOpen = true;
    }

    [RelayCommand] private async Task SubmitDisposalForm()
    {
        if (SelectedItem is null) return;
        if (string.IsNullOrWhiteSpace(DisposalReason)) { FormErrorMessage = "Vui lòng nhập lý do thanh lý."; return; }
        if (_isRealDataLoaded)
        {
            if (_commandService is null || SelectedItem.PersistentId is not Guid itemId || CurrentPropertyId is not Guid propertyId)
            {
                FormErrorMessage = "Dữ liệu thực chưa có đường ghi Lost & Found.";
                return;
            }

            try
            {
                var itemCode = SelectedItem.ItemCode;
                await _commandService.DisposeAsync(new DisposeLostFoundItemCommand(
                    itemId, propertyId, DisposalReason, DisposalDate.ToUniversalTime(), CurrentActorId));
                IsFormOpen = false;
                InfoMessage = $"Đã thanh lý tài sản {itemCode}.";
                await ReloadRealItemAsync(itemCode);
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ex.Message;
                return;
            }
        }

        SelectedItem.ItemStatus = "Disposed"; SelectedItem.DisposalDate = DisposalDate;
        SelectedItem.DisposalReason = DisposalReason; SelectedItem.DisposalApprovedBy = DisposalApprovedBy;
        SelectedItem.Timeline.Add(new LostFoundTimelineEntry { Timestamp = DisposalDate, Event = "Thanh lý", Detail = DisposalReason, Color = "#64748B" });
        SelectedItem.AuditLog.Add(new LostFoundAuditEntry { Timestamp = DisposalDate, Action = "Disposed", Actor = DisposalApprovedBy, Detail = DisposalReason, Device = "Front Office" });
        IsFormOpen = false; BuildDashboard(); BuildTree(); BuildTimeline(SelectedItem); BuildAuditLog(SelectedItem); ApplyFilters();
        InfoMessage = $"Đã thanh lý tài sản {SelectedItem.ItemCode}.";
    }

    [RelayCommand] private void CloseForm()
    {
        IsFormOpen = false; FormErrorMessage = string.Empty;
    }

    [RelayCommand] private async Task DeleteItem()
    {
        if (SelectedItem is null) return;
        var r = MessageBox.Show($"Xóa tài sản {SelectedItem.ItemCode} — {SelectedItem.ItemName}?\n\nHành động này không thể hoàn tác.", "Xác nhận xóa", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (r != MessageBoxResult.OK) return;

        if (_isRealDataLoaded)
        {
            if (_commandService is null || SelectedItem.PersistentId is not Guid itemId || CurrentPropertyId is not Guid propertyId)
            {
                ErrorMessage = "Dữ liệu thật chưa có đường xóa Lost & Found.";
                return;
            }

            try
            {
                var itemCode = SelectedItem.ItemCode;
                await _commandService.DeleteAsync(new DeleteLostFoundItemCommand(itemId, propertyId, CurrentActorId));
                SelectedItem = null;
                IsDetailOpen = false;
                InfoMessage = $"Đã xóa tài sản {itemCode}.";
                await LoadAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return;
        }

        ErrorMessage = "Chưa có dữ liệu Lost & Found thật để xóa.";
    }

    [RelayCommand] private async Task StoreItem()
    {
        if (SelectedItem is null || SelectedItem.ItemStatus is not "New") return;
        if (_isRealDataLoaded)
        {
            if (_commandService is null || SelectedItem.PersistentId is not Guid itemId || CurrentPropertyId is not Guid propertyId)
            {
                FormErrorMessage = "Dữ liệu thực chưa có đường ghi Lost & Found.";
                return;
            }

            try
            {
                var itemCode = SelectedItem.ItemCode;
                var location = StorageLocations.FirstOrDefault() ?? "Kho A - Kệ 1";
                await _commandService.StoreAsync(new StoreLostFoundItemCommand(
                    itemId, propertyId, location, DateTime.UtcNow, CurrentActorId));
                InfoMessage = $"Đã nhập kho tài sản {itemCode}.";
                await ReloadRealItemAsync(itemCode);
                return;
            }
            catch (Exception ex)
            {
                FormErrorMessage = ex.Message;
                return;
            }
        }

        SelectedItem.ItemStatus = "Stored"; SelectedItem.StorageDate = DateTime.Now;
        SelectedItem.StorageLocation = StorageLocations[0]; SelectedItem.ShelfNumber = "Kệ 1"; SelectedItem.Compartment = "Ngăn 1";
        SelectedItem.SealNumber = $"SEAL{SelectedItem.ItemCode}"; SelectedItem.StoredBy = Supervisors[0];
        SelectedItem.Photos.Add(new LostFoundPhotoDto { Label = "Storage", Category = "Nhập kho", Url = $"lf_{SelectedItem.ItemCode}_storage.jpg" });
        SelectedItem.Timeline.Add(new LostFoundTimelineEntry { Timestamp = DateTime.Now, Event = "Nhập kho", Detail = SelectedItem.StorageLocation ?? "Kho", Color = "#0891B2" });
        SelectedItem.AuditLog.Add(new LostFoundAuditEntry { Timestamp = DateTime.Now, Action = "Stored", Actor = Supervisors[0], Detail = $"Nhập kho: {SelectedItem.StorageLocation}", Device = "Front Office" });
        BuildDashboard(); BuildTree(); BuildTimeline(SelectedItem); BuildAuditLog(SelectedItem); ApplyFilters();
        InfoMessage = $"Đã nhập kho tài sản {SelectedItem.ItemCode}.";
    }

    [RelayCommand] private void PrintReport()
    {
        if (SelectedItem is null) return;
        MessageBox.Show($"BIÊN BẢN TÀI SẢN THẤT LẠC\n\nMã: {SelectedItem.ItemCode}\nTên: {SelectedItem.ItemName}\nLoại: {SelectedItem.Category}\nPhòng: {SelectedItem.RoomNumber}\nKhách: {SelectedItem.GuestName}\nTrạng thái: {SelectedItem.StatusLabel}\n\nIn lúc: {DateTime.Now:dd/MM/yyyy HH:mm}", "In biên bản", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand] private void OpenGuestProfile()
    {
        if (SelectedItem?.GuestName is null) return;
        MessageBox.Show($"HỒ SƠ KHÁCH\n\nTên: {SelectedItem.GuestName}\nReservation: {SelectedItem.ReservationNumber}\nFolio: {SelectedItem.FolioNumber}\nPhone: {SelectedItem.GuestPhone}\nEmail: {SelectedItem.GuestEmail}\nCompany: {SelectedItem.CompanyName ?? "—"}\nArrival: {SelectedItem.GuestArrival:dd/MM/yyyy}\nDeparture: {SelectedItem.GuestDeparture:dd/MM/yyyy}", "Hồ sơ khách", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand] private void SetGridView() => ViewMode = "Grid";
    [RelayCommand] private void SetCardView() => ViewMode = "Card";
    [RelayCommand]
    private void ExportExcel()
    {
        if (!_isRealDataLoaded)
        {
            InfoMessage = "Chưa có Lost & Found dữ liệu thật; không xuất dữ liệu mẫu.";
            return;
        }
        var rows = FilteredItems.Count > 0 ? FilteredItems.ToList() : Items.ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Không có tài sản thật trong bộ lọc hiện tại để xuất.";
            return;
        }
        try
        {
            var path = CsvExportHelper.Export(
                "lost_found",
                ["Mã tài sản", "Tên", "Loại", "Phòng", "Ngày phát hiện", "Trạng thái", "Giá trị ước tính", "Người phát hiện"],
                rows.Select(row => (IReadOnlyList<string>)[
                    row.ItemCode,
                    row.ItemName,
                    row.Category,
                    row.RoomNumber ?? string.Empty,
                    row.FoundDate?.ToString("dd/MM/yyyy") ?? string.Empty,
                    row.StatusLabel,
                    CsvExportHelper.F(row.EstimatedValue),
                    row.FoundBy ?? string.Empty
                ]).ToList());
            InfoMessage = $"Đã xuất {rows.Count} tài sản thật ra file CSV tương thích Excel: {path}";
            MessageBox.Show(InfoMessage, "Xuất Lost & Found", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể xuất Lost & Found thật: {ex.Message}";
        }
    }
    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
}
