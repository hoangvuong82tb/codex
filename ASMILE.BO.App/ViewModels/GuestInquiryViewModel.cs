using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Settlement;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Stay;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

// ═══════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════

public sealed class GuestInquiryDto
{
    public string GuestId { get; set; } = "";
    public string GuestCode { get; set; } = "";
    public string Title { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName => $"{Title} {LastName} {FirstName}".Trim();
    public string? Avatar { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string Gender { get; set; } = "";
    public string Nationality { get; set; } = "VN";
    public string NationalityFlag => Nationality switch
    {
        "VN" => "🇻🇳", "KR" => "🇰🇷", "JP" => "🇯🇵", "CN" => "🇨🇳", "US" => "🇺🇸",
        "FR" => "🇫🇷", "DE" => "🇩🇪", "SG" => "🇸🇬", "TH" => "🇹🇭", "AU" => "🇦🇺",
        "GB" => "🇬🇧", "MY" => "🇲🇾", "ID" => "🇮🇩", "PH" => "🇵🇭", "IN" => "🇮🇳", _ => "🏳"
    };
    public string? Language { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PassportNo { get; set; }
    public string? VisaNo { get; set; }
    public string? IdCardNo { get; set; }
    public string? MemberCode { get; set; }
    public string? MemberLevel { get; set; }
    public string MemberLevelColor => MemberLevel switch
    {
        "Platinum" => "#7C3AED", "Gold" => "#D97706", "Silver" => "#64748B", "Bronze" => "#B45309", _ => "#94A3B8"
    };
    public string VipLevel { get; set; } = "None";
    public bool IsVip => VipLevel != "None";
    public string VipColor => VipLevel switch
    {
        "VIP1" => "#D97706", "VIP2" => "#DC2626", "VIP3" => "#7C3AED", _ => "#94A3B8"
    };
    public string VipLabel => VipLevel switch { "VIP1" => "VIP 1", "VIP2" => "VIP 2", "VIP3" => "VIP Platinum", _ => "" };
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public int StayCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSpend => StayCount > 0 ? TotalRevenue / StayCount : 0;
    public DateTime? LastStayDate { get; set; }
    public DateTime? NextStayDate { get; set; }
    public int LoyaltyPoints { get; set; }
    public string? Tags { get; set; }
    public string? CompanyName { get; set; }
    public string? TravelAgentName { get; set; }
    public string? GroupName { get; set; }
    public string? MarketCode { get; set; }
    public string? SourceCode { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public string GuestStatus { get; set; } = "CheckedOut";
    public string GuestStatusLabel => GuestStatus switch
    {
        "InHouse" => "Đang ở", "CheckedOut" => "Đã check-out", "ExpectedArrival" => "Sắp đến",
        "NoShow" => "No Show", "Cancelled" => "Đã hủy", "WalkIn" => "Walk-in", _ => GuestStatus
    };
    public string GuestStatusColor => GuestStatus switch
    {
        "InHouse" => "#16A34A", "CheckedOut" => "#64748B", "ExpectedArrival" => "#2563EB",
        "NoShow" => "#DC2626", "Cancelled" => "#94A3B8", "WalkIn" => "#D97706", _ => "#64748B"
    };
    public string GuestStatusBg => GuestStatus switch
    {
        "InHouse" => "#DCFCE7", "CheckedOut" => "#F1F5F9", "ExpectedArrival" => "#DBEAFE",
        "NoShow" => "#FEE2E2", "Cancelled" => "#F1F5F9", "WalkIn" => "#FFF7ED", _ => "#F8FAFC"
    };
    public string? CurrentRoom { get; set; }
    public string? ReservationNo { get; set; }
    public DateTime? ArrivalDate { get; set; }
    public DateTime? DepartureDate { get; set; }
    public decimal Balance { get; set; }
    public string? GuestType { get; set; }
    public string? RateCode { get; set; }
    public string? RoomType { get; set; }
    public string? PaymentMethod { get; set; }
    public string? CreditCardMask { get; set; }

    // Preferences
    public string? SmokingPref { get; set; }
    public string? BedTypePref { get; set; }
    public string? PillowPref { get; set; }
    public string? FloorPref { get; set; }
    public string? ViewPref { get; set; }
    public string? FoodPref { get; set; }
    public string? Allergy { get; set; }
    public string? WakeUpPref { get; set; }
    public string? TransportationPref { get; set; }
    public string? FavoriteRoom { get; set; }
    public string? FavoriteBeverage { get; set; }
    public string? SpecialRequest { get; set; }
    public DateTime? Birthday { get; set; }
    public DateTime? Anniversary { get; set; }
    public string? VipRemark { get; set; }

    // Sub-lists
    public List<GuestReservationDto> Reservations { get; set; } = new();
    public List<GuestStayDto> Stays { get; set; } = new();
    public List<GuestRoomDto> Rooms { get; set; } = new();
    public List<GuestFolioDto> Folios { get; set; } = new();
    public List<GuestInvoiceDto> Invoices { get; set; } = new();
    public List<GuestPaymentDto> Payments { get; set; } = new();
    public List<GuestTimelineEntry> Timeline { get; set; } = new();
    public List<GuestDocumentDto> Documents { get; set; } = new();
    public List<GuestNoteDto> Notes { get; set; } = new();
    public List<GuestAuditEntry> AuditLog { get; set; } = new();
    public string? AiSummary { get; set; }
}

public sealed class GuestReservationDto
{
    public string ReservationNo { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime Arrival { get; set; }
    public DateTime Departure { get; set; }
    public string RateCode { get; set; } = "";
    public string Package { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string Source { get; set; } = "";
    public string Guarantee { get; set; } = "";
    public string PaymentMethod { get; set; } = "";
    public string? CompanyName { get; set; }
    public string? GroupName { get; set; }
    public string? OtaName { get; set; }
}

public sealed class GuestStayDto
{
    public string StayNo { get; set; } = "";
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public int Nights { get; set; }
    public string RoomNo { get; set; } = "";
    public string Status { get; set; } = "";
    public string Type { get; set; } = "";
    public decimal Revenue { get; set; }
}

public sealed class GuestRoomDto
{
    public string RoomNo { get; set; } = "";
    public string RoomType { get; set; } = "";
    public string Building { get; set; } = "";
    public string Floor { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IsCurrent { get; set; }
}

public sealed class GuestFolioDto
{
    public string FolioNo { get; set; } = "";
    public string FolioType { get; set; } = "";
    public decimal Balance { get; set; }
    public decimal Deposit { get; set; }
    public decimal Refund { get; set; }
    public decimal Outstanding { get; set; }
    public int TransactionCount { get; set; }
    public int InvoiceCount { get; set; }
    public bool IsMaster { get; set; }
}

public sealed class GuestInvoiceDto
{
    public string InvoiceNo { get; set; } = "";
    public DateTime IssueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal VatAmount { get; set; }
    public string Type { get; set; } = "";
    public string PaymentStatus { get; set; } = "";
    public decimal Outstanding { get; set; }
    public string? ReceiptNo { get; set; }
}

public sealed class GuestPaymentDto
{
    public string PaymentNo { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "";
    public string? Reference { get; set; }
    public string? Collector { get; set; }
    public string? Cashier { get; set; }
}

public sealed class GuestTimelineEntry
{
    public string Event { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Detail { get; set; } = "";
    public string Color { get; set; } = "#2563EB";
    public string Icon { get; set; } = "●";
}

public sealed class GuestDocumentDto
{
    public string DocType { get; set; } = "";
    public string DocLabel { get; set; } = "";
    public DateTime? CreatedDate { get; set; }
    public string Icon => DocType switch
    {
        "Passport" => "🛂", "Visa" => "🛃", "IdCard" => "🪪", "RegistrationCard" => "📋",
        "Contract" => "📝", "Invoice" => "📄", "Receipt" => "🧾", "Photo" => "📸", _ => "📎"
    };
}

public sealed class GuestNoteDto
{
    public string Department { get; set; } = "";
    public string Content { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string NoteType { get; set; } = "";
    public string NoteColor => NoteType switch
    {
        "Vip" => "#D97706", "Blacklist" => "#DC2626", "Complaint" => "#EA580C",
        "Internal" => "#64748B", _ => "#2563EB"
    };
}

public sealed class GuestAuditEntry
{
    public string Action { get; set; } = "";
    public string Actor { get; set; } = "";
    public string Detail { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Machine { get; set; } = "";
}

public sealed class GuestInquiryKpi
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public string Color { get; set; } = "#16A34A";
    public string BgColor { get; set; } = "#DCFCE7";
}

// ═══════════════════════════════════════════════════════════════
// VIEWMODEL
// ═══════════════════════════════════════════════════════════════

public sealed partial class GuestInquiryViewModel : ObservableObject
{
    private readonly IGuestProfileRepository _guestRepository;
    private readonly IHotelStayRepository _stayRepository;
    private readonly IHotelFolioRepository _folioRepository;
    private readonly IHotelPaymentRepository _paymentRepository;
    private readonly IHotelInvoiceRepository _invoiceRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly IFrontOfficeExecutionContextAccessor _executionContext;
    private bool _realDataLoaded;
    private bool _realDataLoading;
    private CancellationTokenSource? _detailLoadCts;

    public ObservableCollection<GuestInquiryKpi> DashboardKpis { get; } = new();
    public ObservableCollection<GuestInquiryDto> AllGuests { get; } = new();
    public ObservableCollection<GuestInquiryDto> FilteredGuests { get; } = new();
    public ObservableCollection<string> StatusFilters { get; } = new();
    public ObservableCollection<string> NationalityFilters { get; } = new();
    public ObservableCollection<string> VipFilters { get; } = new();
    public ObservableCollection<string> MemberFilters { get; } = new();
    public ObservableCollection<string> GenderFilters { get; } = new();
    public ObservableCollection<string> GuestTypeFilters { get; } = new();

    [ObservableProperty] private GuestInquiryDto? _selectedGuest;
    [ObservableProperty] private int _selectedDetailTab;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterNationality;
    [ObservableProperty] private string? _filterVip;
    [ObservableProperty] private string? _filterMember;
    [ObservableProperty] private string? _filterGender;
    [ObservableProperty] private string? _filterGuestType;
    [ObservableProperty] private bool _filterVipOnly;
    [ObservableProperty] private bool _filterBlacklist;
    [ObservableProperty] private bool _filterInHouse;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string? _infoMessage;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private int _currentPage = 1;
    [ObservableProperty] private int _pageSize = 50;
    [ObservableProperty] private int _totalItems;
    [ObservableProperty] private int _totalPages;
    [ObservableProperty] private string _pagerSummary = "";

    public bool IsOverviewTab => SelectedDetailTab == 0;
    public bool IsReservationTab => SelectedDetailTab == 1;
    public bool IsStayTab => SelectedDetailTab == 2;
    public bool IsRoomTab => SelectedDetailTab == 3;
    public bool IsFolioTab => SelectedDetailTab == 4;
    public bool IsInvoiceTab => SelectedDetailTab == 5;
    public bool IsPaymentTab => SelectedDetailTab == 6;
    public bool IsHistoryTab => SelectedDetailTab == 7;
    public bool IsPreferencesTab => SelectedDetailTab == 8;
    public bool IsDocumentsTab => SelectedDetailTab == 9;
    public bool IsNotesTab => SelectedDetailTab == 10;
    public bool IsAuditTab => SelectedDetailTab == 11;

    public GuestInquiryViewModel(
        IGuestProfileRepository guestRepository,
        IHotelStayRepository stayRepository,
        IHotelFolioRepository folioRepository,
        IHotelPaymentRepository paymentRepository,
        IHotelInvoiceRepository invoiceRepository,
        IReservationRepository reservationRepository,
        IFrontOfficeExecutionContextAccessor executionContext)
    {
        _guestRepository = guestRepository;
        _stayRepository = stayRepository;
        _folioRepository = folioRepository;
        _paymentRepository = paymentRepository;
        _invoiceRepository = invoiceRepository;
        _reservationRepository = reservationRepository;
        _executionContext = executionContext;
        BuildFilters();
        BuildDashboardKpis();
        InfoMessage = "Chọn cơ sở để tải danh sách khách từ dữ liệu thật.";
        UpdatePagerSummary();
    }

    public bool UsesRealData => true;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_realDataLoaded)
            return;

        var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
        if (!propertyId.HasValue || propertyId.Value == Guid.Empty)
        {
            ErrorMessage = "Chưa chọn cơ sở. Không thể tải danh sách khách thật.";
            InfoMessage = null;
            return;
        }

        await LoadRealDataAsync(propertyId.Value, cancellationToken);
    }

    private async Task LoadRealDataAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        if (_realDataLoading)
            return;

        _realDataLoading = true;
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            var vip = FilterVip switch
            {
                "VIP 1" => ASMILE.BO.Core.FrontOffice.Guest.VipLevel.Bronze,
                "VIP 2" => ASMILE.BO.Core.FrontOffice.Guest.VipLevel.Silver,
                "VIP Platinum" => ASMILE.BO.Core.FrontOffice.Guest.VipLevel.Platinum,
                _ => (ASMILE.BO.Core.FrontOffice.Guest.VipLevel?)null
            };
            var filter = new GuestSearchFilter(
                string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim(),
                vip,
                FilterBlacklist ? true : null,
                null,
                FilterNationality is null || FilterNationality == "Tất cả" ? null : FilterNationality,
                null,
                null,
                null,
                null);
            var page = await _guestRepository.SearchAsync(propertyId, filter, new PageRequest(CurrentPage, PageSize), cancellationToken);

            AllGuests.Clear();
            FilteredGuests.Clear();
            string? operationalInfoMessage = null;
            foreach (var dto in page.Items)
            {
                var item = new GuestInquiryDto
                {
                    GuestId = dto.GuestProfileId.ToString(),
                    GuestCode = dto.GuestNumber,
                    FirstName = dto.FullName,
                    LastName = string.Empty,
                    Title = string.Empty,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    VipLevel = dto.VipLevel switch
                    {
                        ASMILE.BO.Core.FrontOffice.Guest.VipLevel.Bronze => "VIP1",
                        ASMILE.BO.Core.FrontOffice.Guest.VipLevel.Silver => "VIP2",
                        ASMILE.BO.Core.FrontOffice.Guest.VipLevel.Gold => "VIP2",
                        ASMILE.BO.Core.FrontOffice.Guest.VipLevel.Platinum => "VIP3",
                        _ => "None"
                    },
                    IsBlacklisted = dto.IsBlacklisted,
                    StayCount = dto.TotalStays,
                    TotalRevenue = dto.TotalRevenue,
                    Nationality = dto.Nationality ?? "—",
                    Gender = dto.Gender ?? "—",
                    Birthday = dto.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
                    CompanyName = dto.CompanyName,
                    Notes = string.IsNullOrWhiteSpace(dto.Notes)
                        ? []
                        :
                        [
                            new GuestNoteDto
                            {
                                Department = "Guest Profile",
                                Content = dto.Notes,
                                Author = "Guest Profile",
                                CreatedAt = dto.CreatedAtUtc,
                                NoteType = "Internal"
                            }
                        ],
                    GuestStatus = dto.IsBlacklisted ? "Blacklisted" : "Unknown"
                };
                AllGuests.Add(item);
                FilteredGuests.Add(item);
            }

            // The guest search contract contains profile aggregates, while
            // operational status and room assignment live on HotelStay. Enrich
            // the page from the real Stay read path so the list/KPIs are not
            // permanently stuck at "Unknown".
            try
            {
                var businessDate = _executionContext.Current?.BusinessDate.BusinessDate
                    ?? DateOnly.FromDateTime(DateTime.Today);
                var current = await _stayRepository.GetCurrentInHouseAsync(propertyId, cancellationToken);
                var expected = await _stayRepository.GetExpectedArrivalsAsync(propertyId, businessDate, cancellationToken);
                var operationalStays = current.Concat(expected).ToList();
                foreach (var item in AllGuests)
                {
                    if (!Guid.TryParse(item.GuestId, out var guestId))
                        continue;

                    ApplyOperationalStay(item, FindOperationalStay(operationalStays, item, guestId));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                operationalInfoMessage = $"Đã tải hồ sơ khách thật; chưa tải được trạng thái lưu trú: {ex.Message}";
            }

            TotalItems = page.Total;
            TotalPages = page.Total == 0 ? 0 : (int)Math.Ceiling(page.Total / (double)PageSize);
            if (SelectedGuest is not null && !FilteredGuests.Contains(SelectedGuest))
            {
                SelectedGuest = null;
                SelectedDetailTab = 0;
                NotifyTabProperties();
            }
            _realDataLoaded = true;
            InfoMessage = operationalInfoMessage ?? "Đang hiển thị dữ liệu khách từ cơ sở dữ liệu thật.";
            UpdatePagerSummary();
            BuildPagerItems();
            OnPropertyChanged(nameof(HasPrevPage));
            OnPropertyChanged(nameof(HasNextPage));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể tải danh sách khách: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            _realDataLoading = false;
        }
    }

    private void RequestRealReload()
    {
        _ = InitializeAsync();
        if (_realDataLoaded)
        {
            var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
            if (propertyId.HasValue && propertyId.Value != Guid.Empty)
                _ = LoadRealDataAsync(propertyId.Value, CancellationToken.None);
        }
    }

    // Synthetic guest records are intentionally disabled. Production Guest
    // Inquiry must be backed by IGuestProfileRepository.
#if false
    #region Fake Data

    private void BuildFakeData()
    {
        var lastNames = new[] { "Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Vũ", "Đặng", "Bùi", "Đỗ", "Ngô", "Dương", "Lý", "Phan", "Mai", "Đinh", "Hồ", "Trịnh", "Đoàn", "Vương", "Tạ" };
        var firstNames = new[] { "Anh", "Bình", "Chi", "Dũng", "Em", "Phương", "Giang", "Hương", "Inh", "Kim", "Lan", "Minh", "Ngọc", "Oanh", "Phúc", "Quỳnh", "Sơn", "Thảo", "Uyên", "Vũ", "Xuân", "Yến", "Tuấn", "Hải", "Thanh" };
        var titles = new[] { "Ông", "Bà", "Anh", "Chị", "" };
        var genders = new[] { "Nam", "Nữ", "Khác" };
        var nationalities = new[] { "VN", "KR", "JP", "CN", "US", "FR", "DE", "SG", "TH", "AU", "GB", "MY", "ID", "PH", "IN" };
        var languages = new[] { "Tiếng Việt", "English", "한국어", "日本語", "中文", "Français", "Deutsch" };
        var memberLevels = new[] { "None", "Bronze", "Silver", "Gold", "Platinum" };
        var vipLevels = new[] { "None", "None", "None", "None", "VIP1", "VIP1", "VIP2", "VIP3" };
        var statuses = new[] { "InHouse", "InHouse", "InHouse", "CheckedOut", "CheckedOut", "CheckedOut", "CheckedOut", "ExpectedArrival", "ExpectedArrival", "NoShow", "Cancelled", "WalkIn" };
        var guestTypes = new[] { "Individual", "Corporate", "TravelAgent", "OTA", "Group", "VIP", "HouseUse", "Complimentary" };
        var companies = new[] { "Samsung", "LG", "Hyundai", "VinGroup", "FPT", "Vietcombank", "Marriott", "Hilton", "Toyota", null };
        var markets = new[] { "VN", "KR", "JP", "CN", "US", "EU", "SEA" };
        var sources = new[] { "Direct", "Website", "OTA", "GDS", "Phone", "Walk-in", "Corporate" };
        var roomTypes = new[] { "Standard", "Superior", "Deluxe", "Suite", "Presidential" };
        var rateCodes = new[] { "BAR", "CORP", "GOV", "PROMO", "PKG", "VIP" };
        var paymentMethods = new[] { "Tiền mặt", "Visa", "MasterCard", "JCB", "Amex", "Chuyển khoản", "Công nợ" };
        var buildings = new[] { "A", "B", "C" };
        var smokingPrefs = new[] { "Không hút thuốc", "Hút thuốc", "Không yêu cầu" };
        var bedTypes = new[] { "King", "Queen", "Twin", "Double", "Single" };
        var pillowPrefs = new[] { "Cao su", "Lông vũ", "Memory foam", "Không yêu cầu" };
        var floorPrefs = new[] { "Tầng cao", "Tầng thấp", "Tầng trệt", "Không yêu cầu" };
        var viewPrefs = new[] { "Biển", "Thành phố", "Vườn", "Hồ bơi", "Không yêu cầu" };
        var foodPrefs = new[] { "Á", "Âu", "Hàn", "Nhật", "Chay", "Không yêu cầu" };
        var allergies = new[] { "Không", "Hải sản", "Đậu phộng", "Gluten", "Lactose", "Trứng" };
        var wakeUpPrefs = new[] { "Có", "Không" };
        var transportPrefs = new[] { "Taxi", "Limousine", "Xe đưa đón", "Tự túc", "Không yêu cầu" };
        var favRooms = new[] { "Phòng Deluxe", "Phòng Suite", "Phòng Superior", "Phòng Standard", null };
        var favBeverages = new[] { "Cà phê đen", "Trà xanh", "Nước cam", "Rượu vang đỏ", "Bia", null };
        var specialRequests = new[] { "Trang trí hoa", "Bánh sinh nhật", "Giường phụ", "Nôi em bé", "Phòng không ồn", null };

        var guests = new List<GuestInquiryDto>();
        var baseDate = DateTime.Today;

        for (int i = 0; i < 15000; i++)
        {
            var lastName = lastNames[_rng.Next(lastNames.Length)];
            var firstName = firstNames[_rng.Next(firstNames.Length)];
            var title = titles[_rng.Next(titles.Length)];
            var gender = genders[_rng.Next(genders.Length)];
            var nationality = nationalities[_rng.Next(nationalities.Length)];
            var lang = languages[_rng.Next(Math.Min(3, languages.Length))];
            var memberLevel = memberLevels[_rng.Next(memberLevels.Length)];
            var vipLevel = vipLevels[_rng.Next(vipLevels.Length)];
            var status = statuses[_rng.Next(statuses.Length)];
            var isBlacklisted = _rng.Next(100) < 3;
            var guestType = guestTypes[_rng.Next(guestTypes.Length)];
            var company = companies[_rng.Next(companies.Length)];

            var stayCount = status == "InHouse" ? _rng.Next(1, 50) : _rng.Next(0, 50);
            var totalRevenue = stayCount * _rng.Next(500_000, 20_000_000);
            var lastStay = baseDate.AddDays(-_rng.Next(0, 365));
            var nextStay = status == "ExpectedArrival" ? baseDate.AddDays(_rng.Next(1, 30)) : (DateTime?)null;
            var arrival = status switch
            {
                "InHouse" => baseDate.AddDays(-_rng.Next(0, 7)),
                "ExpectedArrival" => baseDate.AddDays(_rng.Next(1, 30)),
                _ => baseDate.AddDays(-_rng.Next(30, 730))
            };
            var departure = status == "InHouse" ? baseDate.AddDays(_rng.Next(1, 14)) : arrival.AddDays(_rng.Next(1, 14));

            var roomNo = status == "InHouse" ? $"{buildings[_rng.Next(buildings.Length)]}{_rng.Next(100, 600)}" : null;
            var resNo = $"RES{8000000 + i}";

            var balance = status == "InHouse" ? _rng.Next(0, 50_000_000) : _rng.Next(-5_000_000, 20_000_000);

            // Reservations
            var reservations = new List<GuestReservationDto>();
            var resCount = _rng.Next(0, 8);
            for (int r = 0; r < resCount; r++)
            {
                var rArrival = baseDate.AddDays(-_rng.Next(0, 730));
                reservations.Add(new GuestReservationDto
                {
                    ReservationNo = $"RES{8000000 + i * 10 + r}",
                    Status = _rng.Next(4) == 0 ? "Cancelled" : _rng.Next(3) == 0 ? "NoShow" : "Confirmed",
                    Arrival = rArrival,
                    Departure = rArrival.AddDays(_rng.Next(1, 14)),
                    RateCode = rateCodes[_rng.Next(rateCodes.Length)],
                    Package = _rng.Next(2) == 0 ? "Bed & Breakfast" : "Room Only",
                    RoomType = roomTypes[_rng.Next(roomTypes.Length)],
                    Source = sources[_rng.Next(sources.Length)],
                    Guarantee = _rng.Next(2) == 0 ? "Credit Card" : "Deposit",
                    PaymentMethod = paymentMethods[_rng.Next(paymentMethods.Length)],
                    CompanyName = company,
                    GroupName = guestType == "Group" ? $"Đoàn {_rng.Next(1, 100)}" : null,
                    OtaName = guestType == "OTA" ? new[] { "Booking.com", "Expedia", "Agoda", "Traveloka" }[_rng.Next(4)] : null
                });
            }

            // Stays
            var stays = new List<GuestStayDto>();
            var stayCountList = _rng.Next(0, 12);
            for (int s = 0; s < stayCountList; s++)
            {
                var sCheckIn = baseDate.AddDays(-_rng.Next(0, 1095));
                stays.Add(new GuestStayDto
                {
                    StayNo = $"STY{6000000 + i * 10 + s}",
                    CheckIn = sCheckIn,
                    CheckOut = sCheckIn.AddDays(_rng.Next(1, 14)),
                    Nights = _rng.Next(1, 14),
                    RoomNo = $"{buildings[_rng.Next(buildings.Length)]}{_rng.Next(100, 600)}",
                    Status = sCheckIn > baseDate ? "Expected" : sCheckIn.AddDays(_rng.Next(1, 14)) < baseDate ? "Completed" : "Active",
                    Type = _rng.Next(5) == 0 ? "WalkIn" : _rng.Next(10) == 0 ? "NoShow" : "Normal",
                    Revenue = _rng.Next(500_000, 30_000_000)
                });
            }

            // Rooms
            var rooms = new List<GuestRoomDto>();
            if (status == "InHouse")
            {
                rooms.Add(new GuestRoomDto
                {
                    RoomNo = roomNo!,
                    RoomType = roomTypes[_rng.Next(roomTypes.Length)],
                    Building = buildings[_rng.Next(buildings.Length)],
                    Floor = $"{_rng.Next(1, 20)}",
                    Status = "Occupied",
                    FromDate = arrival,
                    ToDate = departure,
                    IsCurrent = true
                });
            }
            for (int rm = 0; rm < _rng.Next(0, 5); rm++)
            {
                rooms.Add(new GuestRoomDto
                {
                    RoomNo = $"{buildings[_rng.Next(buildings.Length)]}{_rng.Next(100, 600)}",
                    RoomType = roomTypes[_rng.Next(roomTypes.Length)],
                    Building = buildings[_rng.Next(buildings.Length)],
                    Floor = $"{_rng.Next(1, 20)}",
                    Status = "CheckedOut",
                    FromDate = baseDate.AddDays(-_rng.Next(30, 1095)),
                    ToDate = baseDate.AddDays(-_rng.Next(0, 30)),
                    IsCurrent = false
                });
            }

            // Folios
            var folios = new List<GuestFolioDto>();
            folios.Add(new GuestFolioDto
            {
                FolioNo = $"FOL{7000000 + i}",
                FolioType = "Master",
                Balance = balance,
                Deposit = _rng.Next(0, 10_000_000),
                Refund = balance < 0 ? -balance : 0,
                Outstanding = Math.Max(0, balance),
                TransactionCount = _rng.Next(0, 200),
                InvoiceCount = _rng.Next(0, 10),
                IsMaster = true
            });
            if (_rng.Next(3) == 0)
            {
                folios.Add(new GuestFolioDto
                {
                    FolioNo = $"FOL{7000000 + i}_1",
                    FolioType = "Room",
                    Balance = _rng.Next(0, 10_000_000),
                    Deposit = _rng.Next(0, 5_000_000),
                    TransactionCount = _rng.Next(0, 50),
                    InvoiceCount = _rng.Next(0, 3),
                    IsMaster = false
                });
            }

            // Invoices
            var invoices = new List<GuestInvoiceDto>();
            var invCount = _rng.Next(0, 6);
            for (int inv = 0; inv < invCount; inv++)
            {
                var invAmount = _rng.Next(500_000, 30_000_000);
                invoices.Add(new GuestInvoiceDto
                {
                    InvoiceNo = $"HD{10000000 + i * 10 + inv}",
                    IssueDate = baseDate.AddDays(-_rng.Next(0, 365)),
                    Amount = invAmount,
                    VatAmount = invAmount * 0.1m,
                    Type = _rng.Next(3) == 0 ? "CreditNote" : "Invoice",
                    PaymentStatus = _rng.Next(3) == 0 ? "Unpaid" : _rng.Next(2) == 0 ? "Paid" : "Partial",
                    Outstanding = _rng.Next(3) == 0 ? invAmount : 0,
                    ReceiptNo = _rng.Next(3) != 0 ? $"PT{30000000 + i * 10 + inv}" : null
                });
            }

            // Payments
            var payments = new List<GuestPaymentDto>();
            var payCount = _rng.Next(0, 10);
            for (int p = 0; p < payCount; p++)
            {
                payments.Add(new GuestPaymentDto
                {
                    PaymentNo = $"TT{20000000 + i * 10 + p}",
                    Type = _rng.Next(3) == 0 ? "Deposit" : _rng.Next(4) == 0 ? "Refund" : "Payment",
                    Date = baseDate.AddDays(-_rng.Next(0, 365)),
                    Amount = _rng.Next(100_000, 20_000_000),
                    Method = paymentMethods[_rng.Next(paymentMethods.Length)],
                    Reference = $"REF{_rng.Next(100000, 999999)}",
                    Collector = new[] { "Nguyễn Văn An", "Trần Thị Bình", "Lê Văn Cường" }[_rng.Next(3)],
                    Cashier = new[] { "Phạm Thị Dung", "Hoàng Văn Em" }[_rng.Next(2)]
                });
            }

            // Timeline
            var timeline = new List<GuestTimelineEntry>();
            if (reservations.Any())
            {
                var r = reservations.First();
                timeline.Add(new GuestTimelineEntry { Event = "Booking", Timestamp = r.Arrival.AddDays(-30), Detail = $"Đặt phòng {r.ReservationNo}", Color = "#2563EB", Icon = "📅" });
            }
            if (stays.Any())
            {
                var s = stays.Last();
                timeline.Add(new GuestTimelineEntry { Event = "Check-in", Timestamp = s.CheckIn, Detail = $"Nhận phòng {s.RoomNo}", Color = "#16A34A", Icon = "🏨" });
                if (s.CheckOut.HasValue && s.CheckOut < baseDate)
                    timeline.Add(new GuestTimelineEntry { Event = "Check-out", Timestamp = s.CheckOut.Value, Detail = $"Trả phòng {s.RoomNo}", Color = "#64748B", Icon = "🚪" });
            }
            if (payments.Any())
            {
                var p = payments.Last();
                timeline.Add(new GuestTimelineEntry { Event = "Payment", Timestamp = p.Date, Detail = $"Thanh toán {p.Amount:N0} ₫", Color = "#D97706", Icon = "💳" });
            }

            // Documents
            var documents = new List<GuestDocumentDto>
            {
                new() { DocType = "Passport", DocLabel = $"Hộ chiếu {lastName}", CreatedDate = baseDate.AddDays(-365) },
                new() { DocType = "RegistrationCard", DocLabel = "Registration Card", CreatedDate = arrival },
                new() { DocType = "Photo", DocLabel = "Ảnh chân dung", CreatedDate = baseDate.AddDays(-180) }
            };
            if (invoices.Any()) documents.Add(new GuestDocumentDto { DocType = "Invoice", DocLabel = $"Hóa đơn {invoices.First().InvoiceNo}", CreatedDate = invoices.First().IssueDate });
            if (payments.Any()) documents.Add(new GuestDocumentDto { DocType = "Receipt", DocLabel = $"Biên lai {payments.First().PaymentNo}", CreatedDate = payments.First().Date });

            // Notes
            var notes = new List<GuestNoteDto>
            {
                new() { Department = "Front Office", Content = $"Khách {(isBlacklisted ? "bị blacklist: " + new[]{"No Show nhiều lần", "Hủy phòng phút chót", "Thanh toán chậm", "Phá hoại tài sản"}[_rng.Next(4)] : "thân thiết, đã lưu trú nhiều lần")}", Author = "Lễ tân", CreatedAt = baseDate.AddDays(-_rng.Next(0,365)), NoteType = isBlacklisted ? "Blacklist" : "Internal" }
            };
            if (isVip(vipLevel))
            {
                notes.Add(new GuestNoteDto { Department = "Management", Content = $"Khách VIP {vipLevel}, ưu tiên phòng Suite, tặng hoa quả và rượu vang", Author = "Quản lý", CreatedAt = baseDate.AddDays(-30), NoteType = "Vip" });
            }
            if (_rng.Next(5) == 0)
            {
                notes.Add(new GuestNoteDto { Department = "Housekeeping", Content = "Yêu cầu thêm 2 gối cao su và 1 chăn mỏng", Author = "Housekeeping", CreatedAt = baseDate.AddDays(-_rng.Next(0,60)), NoteType = "Internal" });
            }

            // Audit Log
            var auditLog = new List<GuestAuditEntry>
            {
                new() { Action = "Mở hồ sơ", Actor = "Lễ tân", Detail = "Xem thông tin khách", Timestamp = baseDate.AddDays(-_rng.Next(0,7)), Machine = "WS-01" },
                new() { Action = "Tra cứu", Actor = "Thu ngân", Detail = "Tra cứu folio", Timestamp = baseDate.AddDays(-_rng.Next(0,14)), Machine = "WS-02" },
                new() { Action = "In", Actor = "Kế toán", Detail = "In hóa đơn", Timestamp = baseDate.AddDays(-_rng.Next(0,30)), Machine = "WS-03" },
            };

            // AI Summary
            string aiSummary = "";
            if (isVip(vipLevel) && stayCount > 10)
                aiSummary = $"Khách VIP {vipLabel(vipLevel)}, đã lưu trú {stayCount} lần, doanh thu {totalRevenue:N0} ₫, thích {favRooms[_rng.Next(favRooms.Length)] ?? "phòng cao cấp"}, thường thanh toán bằng {paymentMethods[_rng.Next(3)]}, chưa từng No Show.";
            else if (stayCount > 5)
                aiSummary = $"Khách thân thiết, đã lưu trú {stayCount} lần, doanh thu {totalRevenue:N0} ₫, trung bình {totalRevenue / stayCount:N0} ₫/lần.";
            else if (isBlacklisted)
                aiSummary = $"⚠ Cảnh báo: Khách nằm trong danh sách đen. Lý do: {notes.FirstOrDefault()?.Content ?? "Không rõ"}.";
            else
                aiSummary = $"Khách mới, lần đầu lưu trú {lastStay.ToString("dd/MM/yyyy")}.";

            guests.Add(new GuestInquiryDto
            {
                GuestId = $"{100000 + i}",
                GuestCode = $"KH{100000 + i}",
                Title = title,
                FirstName = firstName,
                LastName = lastName,
                DateOfBirth = new DateTime(_rng.Next(1960, 2005), _rng.Next(1, 13), _rng.Next(1, 28)),
                Gender = gender,
                Nationality = nationality,
                Language = lang,
                Phone = $"0{_rng.Next(100000000, 999999999)}",
                Email = $"{lastName.ToLower()}.{firstName.ToLower()}{i}@email.com",
                PassportNo = _rng.Next(3) == 0 ? $"P{_rng.Next(10000000, 99999999)}" : null,
                VisaNo = nationality != "VN" && _rng.Next(2) == 0 ? $"V{_rng.Next(100000, 999999)}" : null,
                IdCardNo = nationality == "VN" ? $"{_rng.Next(100000000, 999999999):D9}" : null,
                MemberCode = memberLevel != "None" ? $"MB{_rng.Next(10000, 99999)}" : null,
                MemberLevel = memberLevel,
                VipLevel = vipLevel,
                IsBlacklisted = isBlacklisted,
                BlacklistReason = isBlacklisted ? new[] { "No Show nhiều lần", "Hủy phòng phút chót", "Thanh toán chậm", "Phá hoại tài sản" }[_rng.Next(4)] : null,
                StayCount = stayCount,
                TotalRevenue = totalRevenue,
                LastStayDate = lastStay,
                NextStayDate = nextStay,
                LoyaltyPoints = stayCount * _rng.Next(100, 500),
                Tags = string.Join(", ", new[] { isVip(vipLevel) ? vipLevel : null, guestType != "Individual" ? guestType : null, stayCount > 10 ? "Loyal" : null, isBlacklisted ? "Blacklist" : null }.Where(x => x != null)),
                CompanyName = company,
                TravelAgentName = guestType == "TravelAgent" ? $"Đại lý {_rng.Next(1, 50)}" : null,
                GroupName = guestType == "Group" ? $"Đoàn {_rng.Next(1, 100)}" : null,
                MarketCode = markets[_rng.Next(markets.Length)],
                SourceCode = sources[_rng.Next(sources.Length)],
                CountryCode = nationality,
                City = new[] { "Hà Nội", "TP.HCM", "Đà Nẵng", "Nha Trang", "Phú Quốc", "Hội An", "Huế", "Đà Lạt", null }[_rng.Next(9)],
                GuestStatus = status,
                CurrentRoom = roomNo,
                ReservationNo = resNo,
                ArrivalDate = arrival,
                DepartureDate = departure,
                Balance = balance,
                GuestType = guestType,
                RateCode = rateCodes[_rng.Next(rateCodes.Length)],
                RoomType = roomTypes[_rng.Next(roomTypes.Length)],
                PaymentMethod = paymentMethods[_rng.Next(paymentMethods.Length)],
                CreditCardMask = _rng.Next(2) == 0 ? $"****-****-****-{_rng.Next(1000, 9999)}" : null,
                SmokingPref = smokingPrefs[_rng.Next(smokingPrefs.Length)],
                BedTypePref = bedTypes[_rng.Next(bedTypes.Length)],
                PillowPref = pillowPrefs[_rng.Next(pillowPrefs.Length)],
                FloorPref = floorPrefs[_rng.Next(floorPrefs.Length)],
                ViewPref = viewPrefs[_rng.Next(viewPrefs.Length)],
                FoodPref = foodPrefs[_rng.Next(foodPrefs.Length)],
                Allergy = allergies[_rng.Next(allergies.Length)],
                WakeUpPref = wakeUpPrefs[_rng.Next(wakeUpPrefs.Length)],
                TransportationPref = transportPrefs[_rng.Next(transportPrefs.Length)],
                FavoriteRoom = favRooms[_rng.Next(favRooms.Length)],
                FavoriteBeverage = favBeverages[_rng.Next(favBeverages.Length)],
                SpecialRequest = specialRequests[_rng.Next(specialRequests.Length)],
                Birthday = _rng.Next(3) == 0 ? new DateTime(_rng.Next(1960, 2005), _rng.Next(1, 13), _rng.Next(1, 28)) : null,
                Anniversary = _rng.Next(5) == 0 ? baseDate.AddDays(-_rng.Next(0, 365)) : null,
                VipRemark = isVip(vipLevel) ? $"Khách VIP, ưu tiên phòng Suite, tặng welcome drink và hoa quả" : null,
                Reservations = reservations,
                Stays = stays,
                Rooms = rooms,
                Folios = folios,
                Invoices = invoices,
                Payments = payments,
                Timeline = timeline.OrderByDescending(x => x.Timestamp).ToList(),
                Documents = documents,
                Notes = notes,
                AuditLog = auditLog,
                AiSummary = aiSummary
            });
        }

        foreach (var g in guests.OrderBy(g => g.GuestStatus == "InHouse" ? 0 : g.GuestStatus == "ExpectedArrival" ? 1 : 2).ThenBy(g => g.LastName))
            AllGuests.Add(g);
    }

    private static bool isVip(string v) => v != "None";
    private static string vipLabel(string v) => v switch { "VIP1" => "VIP 1", "VIP2" => "VIP 2", "VIP3" => "VIP Platinum", _ => "" };

    #endregion
#endif

    #region Filters

    private void BuildFilters()
    {
        StatusFilters.Clear(); foreach (var s in new[] { "Tất cả", "Đang ở", "Đã check-out", "Sắp đến", "No Show", "Đã hủy", "Walk-in" }) StatusFilters.Add(s);
        NationalityFilters.Clear(); foreach (var s in new[] { "Tất cả" }.Concat(AllGuests.Select(x => x.Nationality).Distinct().OrderBy(x => x))) NationalityFilters.Add(s);
        VipFilters.Clear(); foreach (var s in new[] { "Tất cả", "VIP 1", "VIP 2", "VIP Platinum" }) VipFilters.Add(s);
        MemberFilters.Clear(); foreach (var s in new[] { "Tất cả", "None", "Bronze", "Silver", "Gold", "Platinum" }) MemberFilters.Add(s);
        GenderFilters.Clear(); foreach (var s in new[] { "Tất cả", "Nam", "Nữ", "Khác" }) GenderFilters.Add(s);
        GuestTypeFilters.Clear(); foreach (var s in new[] { "Tất cả", "Individual", "Corporate", "Travel Agent", "OTA", "Group", "VIP", "House Use", "Complimentary" }) GuestTypeFilters.Add(s);
    }

    [RelayCommand]
    private void ApplyFilters()
    {
        if (_guestRepository is not null)
        {
            CurrentPage = 1;
            RequestRealReload();
            return;
        }

        var query = AllGuests.AsEnumerable();
        var kw = SearchText?.Trim().ToLower();

        if (!string.IsNullOrWhiteSpace(kw))
        {
            query = query.Where(x =>
                x.FullName.ToLower().Contains(kw) ||
                (x.Phone?.Contains(kw) ?? false) ||
                (x.Email?.ToLower().Contains(kw) ?? false) ||
                (x.IdCardNo?.Contains(kw) ?? false) ||
                (x.PassportNo?.ToLower().Contains(kw) ?? false) ||
                (x.VisaNo?.ToLower().Contains(kw) ?? false) ||
                (x.MemberCode?.ToLower().Contains(kw) ?? false) ||
                (x.ReservationNo?.ToLower().Contains(kw) ?? false) ||
                (x.CurrentRoom?.ToLower().Contains(kw) ?? false) ||
                (x.CompanyName?.ToLower().Contains(kw) ?? false) ||
                (x.TravelAgentName?.ToLower().Contains(kw) ?? false) ||
                (x.GroupName?.ToLower().Contains(kw) ?? false) ||
                (x.Tags?.ToLower().Contains(kw) ?? false) ||
                (x.VipLevel.ToLower().Contains(kw)) ||
                x.GuestCode.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(FilterStatus) && FilterStatus != "Tất cả")
        {
            var statusMap = new Dictionary<string, string>
            {
                ["Đang ở"] = "InHouse", ["Đã check-out"] = "CheckedOut", ["Sắp đến"] = "ExpectedArrival",
                ["No Show"] = "NoShow", ["Đã hủy"] = "Cancelled", ["Walk-in"] = "WalkIn"
            };
            if (statusMap.TryGetValue(FilterStatus, out var mapped))
                query = query.Where(x => x.GuestStatus == mapped);
        }
        if (!string.IsNullOrWhiteSpace(FilterNationality) && FilterNationality != "Tất cả")
            query = query.Where(x => x.Nationality == FilterNationality);
        if (!string.IsNullOrWhiteSpace(FilterVip) && FilterVip != "Tất cả")
        {
            var vipMap = new Dictionary<string, string> { ["VIP 1"] = "VIP1", ["VIP 2"] = "VIP2", ["VIP Platinum"] = "VIP3" };
            if (vipMap.TryGetValue(FilterVip, out var mv)) query = query.Where(x => x.VipLevel == mv);
        }
        if (!string.IsNullOrWhiteSpace(FilterMember) && FilterMember != "Tất cả")
            query = query.Where(x => x.MemberLevel == FilterMember);
        if (!string.IsNullOrWhiteSpace(FilterGender) && FilterGender != "Tất cả")
            query = query.Where(x => x.Gender == FilterGender);
        if (!string.IsNullOrWhiteSpace(FilterGuestType) && FilterGuestType != "Tất cả")
        {
            var typeMap = new Dictionary<string, string> { ["Travel Agent"] = "TravelAgent", ["House Use"] = "HouseUse" };
            var t = typeMap.TryGetValue(FilterGuestType, out var mt) ? mt : FilterGuestType;
            query = query.Where(x => x.GuestType == t);
        }
        if (FilterVipOnly) query = query.Where(x => x.IsVip);
        if (FilterBlacklist) query = query.Where(x => x.IsBlacklisted);
        if (FilterInHouse) query = query.Where(x => x.GuestStatus == "InHouse");

        var filtered = query.OrderBy(g => g.GuestStatus == "InHouse" ? 0 : g.GuestStatus == "ExpectedArrival" ? 1 : 2).ThenBy(g => g.LastName).ToList();
        TotalItems = filtered.Count;
        TotalPages = (int)Math.Ceiling((double)TotalItems / PageSize);
        if (CurrentPage > TotalPages) CurrentPage = Math.Max(1, TotalPages);

        var paged = filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize).ToList();
        if (SelectedGuest is not null && !paged.Contains(SelectedGuest))
        {
            SelectedGuest = null;
            SelectedDetailTab = 0;
            NotifyTabProperties();
        }
        FilteredGuests.Clear();
        foreach (var item in paged) FilteredGuests.Add(item);

        UpdatePagerSummary();
        BuildDashboardKpis();
        BuildPagerItems();
        OnPropertyChanged(nameof(HasPrevPage));
        OnPropertyChanged(nameof(HasNextPage));
    }

    private void UpdatePagerSummary()
    {
        var start = TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
        var end = Math.Min(CurrentPage * PageSize, TotalItems);
        PagerSummary = $"Hiển thị {start:N0}–{end:N0} / {TotalItems:N0}";
    }

    partial void OnSearchTextChanged(string value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterStatusChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterNationalityChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterVipChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterMemberChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterGenderChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterGuestTypeChanged(string? value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterVipOnlyChanged(bool value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterBlacklistChanged(bool value) => ApplyFiltersCommand.Execute(null);
    partial void OnFilterInHouseChanged(bool value) => ApplyFiltersCommand.Execute(null);
    partial void OnPageSizeChanged(int value) { CurrentPage = 1; ApplyFiltersCommand.Execute(null); }

    [RelayCommand]
    private void ClearFilters()
    {
        SearchText = "";
        FilterStatus = "Tất cả";
        FilterNationality = "Tất cả";
        FilterVip = "Tất cả";
        FilterMember = "Tất cả";
        FilterGender = "Tất cả";
        FilterGuestType = "Tất cả";
        FilterVipOnly = false;
        FilterBlacklist = false;
        FilterInHouse = false;
        CurrentPage = 1;
        ApplyFiltersCommand.Execute(null);
    }

    #endregion

    #region KPI Dashboard

    private void BuildDashboardKpis()
    {
        DashboardKpis.Clear();
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Tổng khách", Value = $"{AllGuests.Count:N0}", Color = "#0F172A", BgColor = "#F8FAFC" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Đang ở", Value = $"{AllGuests.Count(x => x.GuestStatus == "InHouse"):N0}", Color = "#16A34A", BgColor = "#DCFCE7" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "VIP", Value = $"{AllGuests.Count(x => x.IsVip):N0}", Color = "#D97706", BgColor = "#FFF7ED" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Blacklist", Value = $"{AllGuests.Count(x => x.IsBlacklisted):N0}", Color = "#DC2626", BgColor = "#FEE2E2" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Sắp đến", Value = $"{AllGuests.Count(x => x.GuestStatus == "ExpectedArrival"):N0}", Color = "#2563EB", BgColor = "#DBEAFE" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Check-out hôm nay", Value = $"{AllGuests.Count(x => x.DepartureDate?.Date == DateTime.Today):N0}", Color = "#7C3AED", BgColor = "#F3E8FF" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Sinh nhật hôm nay", Value = $"{AllGuests.Count(x => x.Birthday?.Month == DateTime.Today.Month && x.Birthday?.Day == DateTime.Today.Day):N0}", Color = "#E32F70", BgColor = "#FCE7F3" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Khách quen", Value = $"{AllGuests.Count(x => x.StayCount > 5):N0}", Color = "#2563EB", BgColor = "#DBEAFE" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Top Doanh thu", Value = $"{AllGuests.Select(x => x.TotalRevenue).DefaultIfEmpty(0m).Max():N0} ₫", Color = "#16A34A", BgColor = "#DCFCE7" });
        DashboardKpis.Add(new GuestInquiryKpi { Label = "Top Stay Count", Value = $"{AllGuests.Select(x => x.StayCount).DefaultIfEmpty(0).Max():N0}", Color = "#D97706", BgColor = "#FFF7ED" });
    }

    #endregion

    #region Commands

    [RelayCommand] private void Refresh() => ApplyFiltersCommand.Execute(null);

    [RelayCommand]
    private void ExportExcel()
    {
        var rows = FilteredGuests.Select(guest => (IReadOnlyList<string>)[
            guest.GuestCode,
            guest.FullName,
            guest.Phone ?? string.Empty,
            guest.Email ?? string.Empty,
            guest.Nationality,
            guest.GuestStatusLabel,
            guest.StayCount.ToString(),
            CsvExportHelper.F(guest.TotalRevenue),
            CsvExportHelper.F(guest.Balance)
        ]).ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Không có khách thật trong bộ lọc hiện tại để xuất.";
            return;
        }

        var path = CsvExportHelper.Export(
            "guest_inquiry",
            ["Mã khách", "Họ tên", "Điện thoại", "Email", "Quốc tịch", "Trạng thái", "Số lần lưu trú", "Tổng doanh thu", "Còn phải thu"],
            rows);
        InfoMessage = $"Đã xuất danh sách khách thật: {path}";
    }

    [RelayCommand]
    private void ExportPdf()
    {
        var rows = FilteredGuests.Select(guest => (IReadOnlyList<string>)[
            guest.GuestCode,
            guest.FullName,
            guest.Phone ?? string.Empty,
            guest.Email ?? string.Empty,
            guest.Nationality,
            guest.GuestStatusLabel,
            guest.StayCount.ToString(),
            CsvExportHelper.F(guest.TotalRevenue),
            CsvExportHelper.F(guest.Balance)
        ]).ToList();
        if (rows.Count == 0)
        {
            InfoMessage = "Không có khách thật trong bộ lọc hiện tại để xuất PDF.";
            return;
        }

        var path = PdfExportHelper.Export(
            "Tra cứu khách",
            "guest_inquiry",
            ["Mã khách", "Họ tên", "Điện thoại", "Email", "Quốc tịch", "Trạng thái", "Số lần lưu trú", "Tổng doanh thu", "Còn phải thu"],
            rows);
        if (!string.IsNullOrWhiteSpace(path))
            InfoMessage = $"Đã xuất PDF danh sách khách thật: {path}";
    }

    [RelayCommand]
    private void PrintProfile()
        => ShowGuestPrintPreview("Hồ sơ khách", BuildGuestProfileText("HỒ SƠ KHÁCH"));

    [RelayCommand]
    private void PrintRegistrationCard()
    {
        if (!EnsureGuestSelected("in Registration Card")) return;
        var guest = SelectedGuest!;
        var content = new StringBuilder()
            .AppendLine("REGISTRATION CARD")
            .AppendLine(new string('=', 70))
            .AppendLine($"Họ tên: {guest.FullName}")
            .AppendLine($"Mã khách: {guest.GuestCode}")
            .AppendLine($"Điện thoại: {guest.Phone ?? "—"}")
            .AppendLine($"Email: {guest.Email ?? "—"}")
            .AppendLine($"Quốc tịch: {guest.Nationality}")
            .AppendLine($"Số giấy tờ: {guest.IdCardNo ?? guest.PassportNo ?? "—"}")
            .AppendLine($"Ngày đến: {guest.ArrivalDate:dd/MM/yyyy}")
            .AppendLine($"Ngày đi: {guest.DepartureDate:dd/MM/yyyy}")
            .AppendLine($"Phòng: {guest.CurrentRoom ?? "—"}");
        ShowGuestPrintPreview("Registration Card", content.ToString());
    }

    [RelayCommand]
    private void PrintGuestHistory()
    {
        if (!EnsureGuestSelected("in lịch sử khách")) return;
        var guest = SelectedGuest!;
        var content = new StringBuilder()
            .AppendLine("LỊCH SỬ KHÁCH")
            .AppendLine(new string('=', 70))
            .AppendLine($"{guest.FullName} · {guest.GuestCode}")
            .AppendLine();
        if (guest.Stays.Count == 0)
            content.AppendLine("Chưa có lịch sử lưu trú chi tiết trong dữ liệu đã tải.");
        else
            foreach (var stay in guest.Stays)
                content.AppendLine($"{stay.StayNo} | {stay.CheckIn:dd/MM/yyyy} - {stay.CheckOut:dd/MM/yyyy} | Phòng {stay.RoomNo} | {stay.Status} | {stay.Revenue:N0}");
        ShowGuestPrintPreview("Lịch sử khách", content.ToString());
    }

    [RelayCommand]
    private void CopyGuest()
    {
        if (!EnsureGuestSelected("sao chép thông tin khách")) return;
        Clipboard.SetText(BuildGuestProfileText("THÔNG TIN KHÁCH"));
        InfoMessage = $"Đã sao chép thông tin khách {SelectedGuest!.FullName}.";
    }

    [RelayCommand]
    private void EmailGuest()
    {
        if (!EnsureGuestSelected("mở email")) return;
        if (string.IsNullOrWhiteSpace(SelectedGuest!.Email))
        {
            InfoMessage = "Khách đang chọn chưa có email thật.";
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"mailto:{SelectedGuest.Email}",
                UseShellExecute = true
            });
            InfoMessage = $"Đã mở ứng dụng email cho {SelectedGuest.Email}.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không thể mở ứng dụng email: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ShowTimeline()
    {
        if (!EnsureGuestSelected("xem Timeline")) return;
        SelectedDetailTab = 7;
        NotifyTabProperties();
    }

    [RelayCommand]
    private void ShowAudit()
    {
        if (!EnsureGuestSelected("xem Audit")) return;
        SelectedDetailTab = 11;
        NotifyTabProperties();
    }

    private bool EnsureGuestSelected(string action)
    {
        if (SelectedGuest is not null) return true;
        InfoMessage = $"Hãy chọn một khách thật trước khi {action}.";
        return false;
    }

    private string BuildGuestProfileText(string heading)
    {
        if (SelectedGuest is null)
            return $"{heading}\n\nChưa chọn khách thật.";

        var guest = SelectedGuest;
        return new StringBuilder()
            .AppendLine(heading)
            .AppendLine(new string('=', 70))
            .AppendLine($"Họ tên: {guest.FullName}")
            .AppendLine($"Mã khách: {guest.GuestCode}")
            .AppendLine($"Trạng thái: {guest.GuestStatusLabel}")
            .AppendLine($"Điện thoại: {guest.Phone ?? "—"}")
            .AppendLine($"Email: {guest.Email ?? "—"}")
            .AppendLine($"Quốc tịch: {guest.Nationality}")
            .AppendLine($"Công ty: {guest.CompanyName ?? "—"}")
            .AppendLine($"Phòng hiện tại: {guest.CurrentRoom ?? "—"}")
            .AppendLine($"Đặt phòng: {guest.ReservationNo ?? "—"}")
            .AppendLine($"Ngày đến: {guest.ArrivalDate:dd/MM/yyyy}")
            .AppendLine($"Ngày đi: {guest.DepartureDate:dd/MM/yyyy}")
            .AppendLine($"Số lần lưu trú: {guest.StayCount}")
            .AppendLine($"Tổng doanh thu: {guest.TotalRevenue:N0}")
            .AppendLine($"Còn phải thu: {guest.Balance:N0}")
            .ToString();
    }

    private void ShowGuestPrintPreview(string title, string content)
    {
        if (!EnsureGuestSelected("in hồ sơ")) return;
        new PrintPreviewDialog(title, content)
        {
            Owner = System.Windows.Application.Current?.MainWindow
        }.ShowDialog();
        InfoMessage = $"Đã mở xem trước {title.ToLowerInvariant()} từ dữ liệu khách thật.";
    }

    [RelayCommand]
    private void SelectGuest(GuestInquiryDto? guest)
    {
        _detailLoadCts?.Cancel();
        SelectedGuest = guest;
        SelectedDetailTab = 0;
        NotifyTabProperties();

        if (guest is not null && Guid.TryParse(guest.GuestId, out var guestId)
            && _executionContext.Current?.Property.PropertyId.Value is { } propertyId
            && propertyId != Guid.Empty)
        {
            _detailLoadCts = new CancellationTokenSource();
            _ = EnrichOperationalStayAsync(guest, guestId, propertyId, _detailLoadCts.Token);
        }
    }

    private async Task EnrichOperationalStayAsync(
        GuestInquiryDto guest,
        Guid guestId,
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        try
        {
            var businessDate = _executionContext.Current?.BusinessDate.BusinessDate
                ?? DateOnly.FromDateTime(DateTime.Today);
            var current = await _stayRepository.GetCurrentInHouseAsync(propertyId, cancellationToken);
            var expected = await _stayRepository.GetExpectedArrivalsAsync(propertyId, businessDate, cancellationToken);
            var stay = FindOperationalStay(current.Concat(expected), guest, guestId);
            ApplyOperationalStay(guest, stay);
            if (stay is not null)
            {
                await EnrichReservationDetailAsync(guest, propertyId, stay, cancellationToken);
                await EnrichFinancialDetailAsync(guest, propertyId, stay, cancellationToken);
            }
            await EnrichGuestHistoryAsync(guest, guestId, cancellationToken);

            if (ReferenceEquals(SelectedGuest, guest))
            {
                SelectedGuest = guest;
                InfoMessage = stay is null
                    ? "Hồ sơ khách thật không có lưu trú hiện tại hoặc khách đến trong ngày đang chọn."
                    : "Đã tải trạng thái lưu trú hiện tại từ dữ liệu Stay thật.";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            if (ReferenceEquals(SelectedGuest, guest))
                InfoMessage = $"Chưa tải được trạng thái lưu trú thật: {ex.Message}";
        }
    }

    private static HotelStayDto? FindOperationalStay(
        IEnumerable<HotelStayDto> stays,
        GuestInquiryDto guest,
        Guid guestId)
        => stays
            .Where(x => x.GuestId == guestId
                || (string.Equals(x.GuestName, guest.FullName, StringComparison.OrdinalIgnoreCase)
                    && (string.IsNullOrWhiteSpace(guest.Phone)
                        || string.Equals(x.GuestPhone, guest.Phone, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(x => x.Status is HotelStayStatus.InHouse or HotelStayStatus.CheckedIn or HotelStayStatus.RoomMoved or HotelStayStatus.Extended or HotelStayStatus.ExpectedDeparture)
            .ThenByDescending(x => x.UpdatedAtUtc)
            .FirstOrDefault();

    private static void ApplyOperationalStay(GuestInquiryDto guest, HotelStayDto? stay)
    {
        guest.Folios.Clear();
        guest.Invoices.Clear();
        guest.Payments.Clear();
        if (stay is null)
        {
            guest.GuestStatus = guest.IsBlacklisted ? "Blacklisted" : guest.StayCount > 0 ? "CheckedOut" : "Unknown";
            guest.CurrentRoom = null;
            guest.ReservationNo = null;
            guest.ArrivalDate = null;
            guest.DepartureDate = null;
            guest.Stays.Clear();
            return;
        }

        guest.GuestStatus = guest.IsBlacklisted
            ? "Blacklisted"
            : stay.Status is HotelStayStatus.PendingArrival or HotelStayStatus.PreArrival
                ? "ExpectedArrival"
                : "InHouse";
        guest.CurrentRoom = stay.RoomNumber;
        guest.ReservationNo = stay.ReservationNumber;
        guest.ArrivalDate = stay.ScheduledArrivalDate.ToDateTime(TimeOnly.MinValue);
        guest.DepartureDate = stay.ScheduledDepartureDate.ToDateTime(TimeOnly.MinValue);
        guest.Stays =
        [
            new GuestStayDto
            {
                StayNo = stay.StayNumber,
                CheckIn = stay.ActualCheckInAtUtc ?? stay.ScheduledArrivalDate.ToDateTime(TimeOnly.MinValue),
                CheckOut = stay.ActualCheckOutAtUtc,
                Nights = Math.Max(0, stay.ScheduledDepartureDate.DayNumber - stay.ScheduledArrivalDate.DayNumber),
                RoomNo = stay.RoomNumber,
                Status = stay.Status.ToString(),
                Type = stay.Source,
                Revenue = 0m
            }
        ];
    }

    private async Task EnrichFinancialDetailAsync(
        GuestInquiryDto guest,
        Guid propertyId,
        HotelStayDto stay,
        CancellationToken cancellationToken)
    {
        var folio = await _folioRepository.GetByStayAsync(propertyId, stay.HotelStayId, cancellationToken);
        if (folio is null)
            return;

        var ledger = await _folioRepository.GetLedgerAsync(propertyId, folio.HotelFolioId, cancellationToken);
        var entries = ledger?.Entries ?? [];
        guest.Folios.Add(new GuestFolioDto
        {
            FolioNo = folio.FolioNumber,
            FolioType = "Stay",
            Balance = folio.RunningBalance,
            Outstanding = folio.OutstandingBalance,
            Deposit = entries.Where(x => x.EntryType.ToString().Contains("Deposit", StringComparison.OrdinalIgnoreCase)).Sum(x => Math.Abs(x.Amount)),
            Refund = entries.Where(x => x.EntryType.ToString().Contains("Refund", StringComparison.OrdinalIgnoreCase)).Sum(x => Math.Abs(x.Amount)),
            TransactionCount = entries.Count,
            IsMaster = true
        });

        var invoice = await _invoiceRepository.GetByFolioAsync(propertyId, folio.HotelFolioId, cancellationToken);
        if (invoice is not null)
        {
            guest.Invoices.Add(new GuestInvoiceDto
            {
                InvoiceNo = invoice.InvoiceNumber,
                IssueDate = invoice.CreatedAtUtc,
                Amount = invoice.TotalAmount,
                VatAmount = invoice.TaxAmount,
                Type = invoice.Status.ToString(),
                PaymentStatus = invoice.OutstandingAmount <= 0m ? "Paid" : invoice.PaidAmount > 0m ? "Partial" : "Unpaid",
                Outstanding = invoice.OutstandingAmount
            });
        }

        var payments = await _paymentRepository.GetForFolioAsync(propertyId, folio.HotelFolioId, cancellationToken);
        foreach (var payment in payments)
        {
            guest.Payments.Add(new GuestPaymentDto
            {
                PaymentNo = payment.PaymentNumber,
                Type = payment.Status.ToString(),
                Date = payment.CreatedAtUtc,
                Amount = payment.Amount,
                Method = payment.PaymentMethod.ToString(),
                Reference = payment.ExternalReference,
            });
        }
    }

    private async Task EnrichReservationDetailAsync(
        GuestInquiryDto guest,
        Guid propertyId,
        HotelStayDto stay,
        CancellationToken cancellationToken)
    {
        guest.Reservations.Clear();
        if (stay.ReservationId is not { } reservationId || reservationId == Guid.Empty)
            return;

        var reservation = await _reservationRepository.GetDtoAsync(propertyId, reservationId, cancellationToken);
        if (reservation is null)
            return;

        guest.Reservations.Add(new GuestReservationDto
        {
            ReservationNo = reservation.ReservationNumber,
            Status = reservation.Status.ToString(),
            Arrival = reservation.ArrivalDate.ToDateTime(TimeOnly.MinValue),
            Departure = reservation.DepartureDate.ToDateTime(TimeOnly.MinValue),
            RateCode = reservation.RatePlanId?.ToString() ?? "—",
            RoomType = reservation.RoomTypeId?.ToString() ?? "—",
            Source = reservation.Source,
            Guarantee = reservation.GuaranteeType.ToString(),
            PaymentMethod = reservation.DepositStatus.ToString(),
            CompanyName = reservation.CompanyName,
            OtaName = reservation.TravelAgentName
        });
    }

    private async Task EnrichGuestHistoryAsync(
        GuestInquiryDto guest,
        Guid guestId,
        CancellationToken cancellationToken)
    {
        var history = await _guestRepository.GetHistoryAsync(
            new GuestHistoryQuery(guestId, null, null, null, null, 1, 50),
            cancellationToken);

        guest.AuditLog = history
            .OrderByDescending(x => x.OccurredAtUtc)
            .Select(x => new GuestAuditEntry
            {
                Action = x.EventType,
                Actor = x.Module,
                Detail = x.Description,
                Timestamp = x.OccurredAtUtc,
                Machine = x.ReferenceType ?? "Guest history"
            })
            .ToList();
    }

    [RelayCommand]
    private void CloseDetail()
    {
        SelectedGuest = null;
        SelectedDetailTab = 0;
    }

    [RelayCommand]
    private void SelectDetailTab(int tabIndex)
    {
        SelectedDetailTab = tabIndex;
        NotifyTabProperties();
    }

    private void NotifyTabProperties()
    {
        OnPropertyChanged(nameof(IsOverviewTab)); OnPropertyChanged(nameof(IsReservationTab));
        OnPropertyChanged(nameof(IsStayTab)); OnPropertyChanged(nameof(IsRoomTab));
        OnPropertyChanged(nameof(IsFolioTab)); OnPropertyChanged(nameof(IsInvoiceTab));
        OnPropertyChanged(nameof(IsPaymentTab)); OnPropertyChanged(nameof(IsHistoryTab));
        OnPropertyChanged(nameof(IsPreferencesTab)); OnPropertyChanged(nameof(IsDocumentsTab));
        OnPropertyChanged(nameof(IsNotesTab)); OnPropertyChanged(nameof(IsAuditTab));
    }

    [RelayCommand]
    private void GoToPage(int page)
    {
        if (page < 1 || page > TotalPages)
            return;
        CurrentPage = page;
        if (_guestRepository is not null)
        {
            var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
            if (propertyId.HasValue && propertyId.Value != Guid.Empty)
                _ = LoadRealDataAsync(propertyId.Value, CancellationToken.None);
        }
        else
            ApplyFiltersCommand.Execute(null);
    }
    [RelayCommand] private void FirstPage() => GoToPage(1);
    [RelayCommand] private void LastPage() => GoToPage(TotalPages);
    [RelayCommand] private void NextPage() => GoToPage(CurrentPage + 1);
    [RelayCommand] private void PrevPage() => GoToPage(CurrentPage - 1);
    [RelayCommand]
    private void FindInHouse()
    {
        FilterInHouse = true;
        FilterVipOnly = false;
        CurrentPage = 1;
        ApplyFiltersCommand.Execute(null);
    }
    [RelayCommand]
    private void FindVip()
    {
        FilterVipOnly = true;
        FilterInHouse = false;
        CurrentPage = 1;
        ApplyFiltersCommand.Execute(null);
    }
    private string SelectedGuestLabel => SelectedGuest is null
        ? string.Empty
        : $"{SelectedGuest.Title} {SelectedGuest.FullName} ({SelectedGuest.GuestCode})".Trim();

    private void ShowGuestAction(string action)
    {
        InfoMessage = SelectedGuest is null
            ? "Hãy chọn một khách trước khi thực hiện thao tác."
            : $"{action}: {SelectedGuestLabel}";
    }

    [RelayCommand] private void OpenReservation() => ShowGuestAction("Reservation");
    [RelayCommand] private void OpenCheckIn() => ShowGuestAction("Check-in");
    [RelayCommand] private void OpenFolio() => ShowGuestAction("Folio");
    [RelayCommand] private void OpenInvoice() => ShowGuestAction("Invoice");
    [RelayCommand] private void OpenPayment() => ShowGuestAction("Payment");
    [RelayCommand] private void OpenRoom() => ShowGuestAction("Phòng");
    [RelayCommand] private void OpenCompany() => ShowGuestAction("Công ty");
    [RelayCommand] private void OpenGroup() => ShowGuestAction("Đoàn");
    [RelayCommand] private void DismissInfo() => InfoMessage = null;

    #endregion

    #region Pagination

    public static int[] PageSizeOptions { get; } = [20, 50, 100, 200];
    public bool HasPrevPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public ObservableCollection<GuestInquiryPagerItem> PagerItems { get; } = new();

    public void BuildPagerItems()
    {
        PagerItems.Clear();
        if (TotalPages <= 7)
        {
            for (int i = 1; i <= TotalPages; i++)
                PagerItems.Add(new GuestInquiryPagerItem { Label = i.ToString(), Page = i, IsCurrent = i == CurrentPage });
        }
        else
        {
            PagerItems.Add(new GuestInquiryPagerItem { Label = "1", Page = 1, IsCurrent = 1 == CurrentPage });
            if (CurrentPage > 3) PagerItems.Add(new GuestInquiryPagerItem { Label = "...", IsEllipsis = true });
            for (int i = Math.Max(2, CurrentPage - 1); i <= Math.Min(TotalPages - 1, CurrentPage + 1); i++)
                PagerItems.Add(new GuestInquiryPagerItem { Label = i.ToString(), Page = i, IsCurrent = i == CurrentPage });
            if (CurrentPage < TotalPages - 2) PagerItems.Add(new GuestInquiryPagerItem { Label = "...", IsEllipsis = true });
            PagerItems.Add(new GuestInquiryPagerItem { Label = TotalPages.ToString(), Page = TotalPages, IsCurrent = TotalPages == CurrentPage });
        }
    }
    #endregion
}

public sealed class GuestInquiryPagerItem
{
    public string Label { get; set; } = "";
    public int Page { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsEllipsis { get; set; }
}
