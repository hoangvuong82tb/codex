using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class MenuRecipeViewModel : ViewModelBase
{
    private readonly IMenuCatalogService _menuCatalogService;
    private readonly IMenuRecipeService _menuRecipeService;
    private readonly IAlacarteRestaurantService _alacarteRestaurantService;

    // ===================================================================
    // SECTION CONSTANTS
    // ===================================================================
    public const int SEC_DASHBOARD = 0;
    public const int SEC_MENU_ITEMS = 1;
    public const int SEC_CATEGORIES = 2;
    public const int SEC_INGREDIENTS = 3;
    public const int SEC_PREPARATION_AREAS = 4;
    public const int SEC_RECIPE = 9;
    public const int SEC_COST = 5;
    public const int SEC_WARNINGS = 6;
    public const int SEC_REPORTS = 7;
    public const int SEC_SETTINGS = 8;

    public static readonly string[] SectionLabels = [
        "Tổng quan", "Món ăn", "Nhóm món", "Nguyên liệu", "Khu chế biến",
        "Tính giá vốn", "Cảnh báo", "Cài đặt"
    ];
    public static readonly string[] SectionIcons = [
        "", "", "", "", "",
        "", "", ""
    ];

    // ===================================================================
    // COLLECTIONS
    // ===================================================================
    public ObservableCollection<MenuCategoryDto> Categories { get; } = new();
    public ObservableCollection<MenuCategoryCardState> CategoryCards { get; } = new();
    public ObservableCollection<MenuItemCardState> MenuCards { get; } = new();
    public ObservableCollection<MenuItemCardState> FilteredMenuCards { get; } = new();
    public ObservableCollection<RecipeLineEditor> RecipeLines { get; } = new();
    public ObservableCollection<IngredientLookupDto> IngredientLookup { get; } = new();
    public ObservableCollection<WarehouseLookupDto> WarehouseLookup { get; } = new();
    public ObservableCollection<SubstituteIngredientDto> SubstituteIngredients { get; } = new();
    public ObservableCollection<IngredientLookupDto> AvailableSubstituteItems { get; } = new();
    public ObservableCollection<string> Warnings { get; } = new();
    public ObservableCollection<string> PreparationAreas { get; } = new();
    public ObservableCollection<PreparationAreaSettingRow> PreparationAreaSettings { get; } = new();
    public ObservableCollection<string> PreparationStationGroups { get; } = new() { "Bếp", "Bar", "Dịch vụ", "Khác" };
    public ObservableCollection<MenuCategoryCardState> FilteredCategoryCards { get; } = new();
    public ObservableCollection<MenuHistoryEntry> MenuHistoryEntries { get; } = new();
    private readonly Dictionary<Guid, List<MenuHistoryEntry>> _menuHistoryByItem = new();
    private int _menuCardLoadVersion;

    // Dashboard data
    public ObservableCollection<IngredientLookupDto> FilteredIngredients { get; } = new();
    public ObservableCollection<MenuItemCardState> DashboardCriticalItems { get; } = new();
    public ObservableCollection<MenuCategoryCardState> ManageableCategories { get; } = new();
    public ObservableCollection<MenuCategoryParentOption> NewCategoryParentOptions { get; } = new();
    public ObservableCollection<MenuCategoryParentOption> EditCategoryParentOptions { get; } = new();
    public ObservableCollection<MenuCategoryDto> VisibleMenuCategories { get; } = new();
    public ObservableCollection<ServiceTypeOption> ServiceTypeOptions { get; } = new();
    public ObservableCollection<CostDetailRow> CostDetailRows { get; } = new();
    public ObservableCollection<CostIngredientRow> CostIngredientRows { get; } = new();

    // ===================================================================
    // OBSERVABLE PROPERTIES
    // ===================================================================
    [ObservableProperty] private int selectedSection = SEC_MENU_ITEMS;
    [ObservableProperty] private MenuCategoryCardState? selectedCategoryCard;
    [ObservableProperty] private MenuCategoryDto? selectedCategory;
    [ObservableProperty] private MenuItemCardState? selectedMenuCard;
    [ObservableProperty] private IngredientLookupDto? selectedIngredient;
    [ObservableProperty] private RecipeLineEditor? selectedRecipeLine;
    [ObservableProperty] private SubstituteIngredientDto? selectedSubstituteIngredient;
    [ObservableProperty] private IngredientLookupDto? selectedSubstituteItem;
    [ObservableProperty] private decimal substituteConversionRate = 1m;
    [ObservableProperty] private int substitutePriority = 1;
    [ObservableProperty] private bool substituteAutoReplace = true;
    [ObservableProperty] private string substituteNotes = string.Empty;
    [ObservableProperty] private string substituteSearchText = string.Empty;
    [ObservableProperty] private WarehouseLookupDto? selectedWarehouse;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string categorySearchText = string.Empty;
    [ObservableProperty] private string ingredientSearchText = string.Empty;
    [ObservableProperty] private Guid? filterCategoryId;
    [ObservableProperty] private string activeFilterChip = "Tất cả";
    [ObservableProperty] private string statusMessage = string.Empty;
    [ObservableProperty] private bool statusIsSuccess = true;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private decimal previewCost;
    [ObservableProperty] private decimal previewMarginAmount;
    [ObservableProperty] private decimal previewMarginRate;
    [ObservableProperty] private decimal ingredientQuantityInput = 1m;
    [ObservableProperty] private decimal ingredientLossRateInput;
    [ObservableProperty] private string selectedPreparationArea = PreparationAreaSettingRow.DefaultBreakfastKitchenCode;
    [ObservableProperty] private bool isCreatingNewMenu;
    [ObservableProperty] private CreateMenuItemRequest editingItem = new();
    [ObservableProperty] private SaveRestaurantExtraServiceRequest editingService = new();
    [ObservableProperty] private Guid? editingRecipeId;
    [ObservableProperty] private string editingRecipeName = "Default";
    [ObservableProperty] private bool editingRecipeIsDefault = true;
    [ObservableProperty] private int selectedWorkspaceTab;
    [ObservableProperty] private bool isSidebarExpanded = true;
    [ObservableProperty] private bool isEditingStatusActive = true;
    [ObservableProperty] private bool canEdit = true;
    [ObservableProperty] private bool isSchemaMissing;
    [ObservableProperty] private int selectedMenuCatalogTab;
    [ObservableProperty] private string? selectedServiceTypeFilter = "All";
    [ObservableProperty] private bool isCreatingCategory;
    [ObservableProperty] private string newCategoryName = string.Empty;
    [ObservableProperty] private string newCategoryCode = string.Empty;
    [ObservableProperty] private Guid? newCategoryParentId;
    [ObservableProperty] private PreparationAreaSettingRow? selectedPreparationAreaSetting;
    [ObservableProperty] private string editPreparationAreaCode = string.Empty;
    [ObservableProperty] private string editPreparationAreaName = string.Empty;
    [ObservableProperty] private string editPreparationAreaStationGroup = "Bếp";
    [ObservableProperty] private bool editPreparationAreaIsActive = true;

    // Category management
    [ObservableProperty] private MenuCategoryCardState? editingCategory;
    [ObservableProperty] private string editCategoryName = string.Empty;
    [ObservableProperty] private string editCategoryCode = string.Empty;
    [ObservableProperty] private bool editCategoryIsActive = true;
    [ObservableProperty] private Guid? editCategoryParentId;
    public ObservableCollection<MenuItemCardState> CategoryDetailItems { get; } = new();
    [ObservableProperty] private MenuItemCardState? selectedCategoryMenuItem;
    public ObservableCollection<RecipeLineEditor> SelectedCategoryRecipeLines { get; } = new();
    public bool HasCategoryDetailItems => CategoryDetailItems.Count > 0;
    public bool HasSelectedCategoryRecipeLines => SelectedCategoryRecipeLines.Count > 0;
    public int CategoryDetailActiveCount => CategoryDetailItems.Count(m => !m.IsInactive);
    public int CategoryDetailRecipeCount => CategoryDetailItems.Count(m => m.HasRecipe);
    public int CategoryDetailNoRecipeCount => CategoryDetailItems.Count(m => !m.HasRecipe);
    public decimal CategoryDetailAvgPrice
    {
        get
        {
            var active = CategoryDetailItems.Where(m => !m.IsInactive).ToList();
            if (active.Count == 0) return 0;
            // Parse price from display string
            return active.Sum(m =>
            {
                var p = m.UnitPriceDisplay.Replace(" đ", "").Replace(",", "");
                return decimal.TryParse(p, System.Globalization.NumberStyles.Any, null, out var v) ? v : 0;
            }) / active.Count;
        }
    }
    public string CategoryDetailAvgPriceDisplay => CategoryDetailAvgPrice > 0 ? $"{CategoryDetailAvgPrice:N0} đ" : "---";

    // Cost section
    [ObservableProperty] private int costScopeIndex; // 0 = single item, 1 = current group, 2 = all items
    [ObservableProperty] private string costProgressText = string.Empty;
    [ObservableProperty] private decimal costTotalEstimated;
    [ObservableProperty] private decimal costTotalSelling;
    [ObservableProperty] private int costProcessedCount;
    [ObservableProperty] private int costTotalCount;
    [ObservableProperty] private MenuItemCardState? selectedCostMenuCard;
    [ObservableProperty] private Guid? selectedCostCategoryId;
    [ObservableProperty] private CostDetailRow? selectedCostDetailRow;

    // ===================================================================
    // PRIVATE STATE
    // ===================================================================
    private readonly List<MenuItemDto> _allMenuItems = [];
    private readonly List<RestaurantExtraServiceDto> _allExtraServices = [];
    private readonly HashSet<Guid> _itemsWithRecipe = [];
    private readonly SemaphoreSlim _dbLock = new(1, 1);
    private bool _dashboardLoaded;
    private bool _isLoadingCategorySelection;

    public ObservableCollection<string> FilterChips { get; } = new();
    public static readonly string[] FoodFilterChips = ["Tất cả", "Có định mức", "Chưa định mức", "Ngưng bán"];
    public static readonly string[] ServiceFilterChips = ["Tất cả", "Đang bán", "Ngưng bán"];

    // Sort options
    public static readonly List<string> SortOptions = ["Tên A-Z", "Tên Z-A", "Giá thấp-cao", "Giá cao-thấp", "Chưa định mức trước"];
    [ObservableProperty] private string selectedSortOption = "Tên A-Z";
    [ObservableProperty] private Guid? selectedMenuCategoryFilter;
    public static readonly string[] CostScopeLabels = ["Món hiện tại", "Theo nhóm", "Tất cả món"];

    // ===================================================================
    // CONSTRUCTOR
    // ===================================================================
    public MenuRecipeViewModel(IMenuCatalogService menuCatalogService, IMenuRecipeService menuRecipeService, IAlacarteRestaurantService alacarteRestaurantService)
    {
        _menuCatalogService = menuCatalogService;
        _menuRecipeService = menuRecipeService;
        _alacarteRestaurantService = alacarteRestaurantService;
        RecipeLines.CollectionChanged += RecipeLines_CollectionChanged;
        ResetCollection(FilterChips, FoodFilterChips);
        BuildServiceTypeOptions();
        LoadPreparationAreaSettings();
        _ = InitializeAsync();
    }

    // ===================================================================
    // COMPUTED PROPERTIES (shared)
    // ===================================================================
    public int TotalMenuCount => _allMenuItems.Count;
    public int HasRecipeCount => MenuCards.Count(m => m.HasRecipe);
    public int NoRecipeCount => TotalMenuCount - HasRecipeCount;
    public int CostWarningCount => Warnings.Count;
    public bool HasMenuItems => FilteredMenuCards.Count > 0;
    public bool NoMenuItems => !HasMenuItems;
    public bool NoCategories => Categories.Count == 0;
    public bool HasCategories => Categories.Count > 0;
    public bool HasSelectedMenu => SelectedMenuCard is not null || IsCreatingNewMenu;
    public bool NoSelectedMenu => !HasSelectedMenu;
    public bool ShowEmptyState => !HasSelectedMenu && SelectedSection == SEC_MENU_ITEMS && (IsServiceCatalogSelected || HasCategories);
    public bool HasRecipeLines => RecipeLines.Count > 0;
    public bool NoRecipeLines => !HasRecipeLines;
    public bool HasSelectedRecipeLine => SelectedRecipeLine is not null;
    public bool NoSelectedRecipeLine => !HasSelectedRecipeLine;
    public bool HasSubstituteIngredients => SubstituteIngredients.Count > 0;
    public bool NoSubstituteIngredients => !HasSubstituteIngredients;
    public bool CanAddSubstitute => CanEdit && !IsBusy && SelectedRecipeLine is not null && SelectedRecipeLine.Id != Guid.Empty;
    public bool HasMenuHistory => MenuHistoryEntries.Count > 0;
    public bool NoMenuHistory => !HasMenuHistory;
    public bool HasWarnings => Warnings.Count > 0;
    public bool NoCostWarnings => !HasWarnings;
    public bool HasImage => !string.IsNullOrWhiteSpace(IsServiceCatalogSelected ? EditingService.ImagePath : EditingItem.ImagePath);
    public bool NoImage => !HasImage;
    public bool IsFirstRun => NoCategories || (HasCategories && !NoMenuItems && !HasSelectedMenu);
    public bool CanCreateMenu => (IsServiceCatalogSelected || HasCategories) && CanEdit && !IsBusy && !IsSchemaMissing;
    public bool ShowAddMenuButton => CanCreateMenu;
    public bool ShowBootstrapCategoriesButton => NoCategories && CanEdit && !IsBusy;
    public bool CanCreateCategory => CanEdit && !IsBusy && !IsSchemaMissing;
    public bool CanSaveCategory => CanCreateCategory && !string.IsNullOrWhiteSpace(NewCategoryName);
    public bool CanSavePreparationArea => CanEdit && !IsBusy && !string.IsNullOrWhiteSpace(EditPreparationAreaName);
    public bool CanDeleteMenu => (IsServiceCatalogSelected ? EditingService.Id.HasValue : EditingItem.Id.HasValue) && !IsBusy;
    public bool CanDeleteCategory => EditingCategory is not null && !EditingCategory.IsVirtual && !IsBusy;
    public bool IsRootCategorySelected => EditingCategory is not null && !EditingCategory.ParentCategoryId.HasValue;
    public bool IsChildCategorySelected => EditingCategory?.ParentCategoryId.HasValue == true;
    public bool IsServiceCategorySelected => EditingCategory?.IsServiceCategory == true;
    public bool CanManageCategoryMenuItems => IsChildCategorySelected && !IsServiceCategorySelected;
    public bool IsFoodCatalogSelected => SelectedMenuCatalogTab == 0;
    public bool IsServiceCatalogSelected => SelectedMenuCatalogTab == 1;
    public string CurrentMenuCatalogTitle => IsServiceCatalogSelected ? "Danh sách dịch vụ" : "Danh sách món";
    public string CurrentCategoryCatalogTitle => IsServiceCatalogSelected ? "Danh sách nhóm dịch vụ" : "Danh sách nhóm món";
    public string CreateCategoryButtonText => IsServiceCatalogSelected ? "Thêm nhóm dịch vụ" : "Thêm nhóm";
    public string CreateMenuButtonText => IsServiceCatalogSelected ? "Thêm dịch vụ" : "Thêm món";
    public string GeneralInfoSectionTitle => IsServiceCatalogSelected ? "Thông tin dịch vụ" : "Thông tin món";
    public string CategoryLabelText => IsServiceCatalogSelected ? "Nhóm dịch vụ *" : "Nhóm món *";
    public bool ShowFoodCatalogFilters => IsFoodCatalogSelected;
    public bool ShowServiceTypeFilter => IsServiceCatalogSelected;
    public bool ShowFoodWorkspace => IsFoodCatalogSelected;
    public bool ShowServiceWorkspace => IsServiceCatalogSelected;
    public bool ShowFoodSidebarState => IsFoodCatalogSelected;
    public bool ShowServiceSidebarState => IsServiceCatalogSelected;
    public bool ShowNoFoodCategoryState => IsFoodCatalogSelected && NoCategories;
    public string EmptyStateTitle => IsServiceCatalogSelected ? "Chọn dịch vụ từ danh sách" : "Chọn món từ danh sách";
    public string EmptyStateDescription => IsServiceCatalogSelected
        ? "Chọn một dịch vụ bên trái để xem chi tiết hoặc tạo mới."
        : "Chọn một món bên trái để xem chi tiết.";
    public string CategoryEmptySelectionTitle => IsServiceCatalogSelected ? "Chọn nhóm dịch vụ" : "Chọn nhóm món";
    public string CategoryEmptySelectionDescription => IsServiceCatalogSelected
        ? "Chọn một nhóm dịch vụ bên trái để xem chi tiết và cấu hình phân loại."
        : "Chọn một nhóm bên trái để xem chi tiết và danh sách món.";
    public string EmptyStateCreateButtonText => IsServiceCatalogSelected ? "Thêm dịch vụ mới" : "Thêm món mới";
    public string NoItemsFoundText => IsServiceCatalogSelected ? "Không tìm thấy dịch vụ phù hợp." : "Không tìm thấy món phù hợp.";
    public string SelectedCatalogTitle => IsServiceCatalogSelected
        ? (IsCreatingNewMenu ? "Dịch vụ mới" : (!string.IsNullOrWhiteSpace(EditingService.Name) ? EditingService.Name : "Dịch vụ mới"))
        : (IsCreatingNewMenu ? "Món mới" : (EditingItem.Name?.Trim().Length > 0 ? EditingItem.Name : (NoCategories ? "Chưa có món nào" : "")));
    public string CurrentActionButtonText => IsServiceCatalogSelected ? "Lưu dịch vụ" : "Lưu & GV";
    public string CurrentDeleteButtonText => IsServiceCatalogSelected ? "Ngưng DV" : "Xóa menu";
    public string CurrentDuplicateButtonText => IsServiceCatalogSelected ? "Sao chép DV" : "Sao chép";
    public string CurrentActiveStatusButtonText => IsServiceCatalogSelected
        ? (EditingService.IsActive ? "Ngưng bán" : "Mở bán")
        : ActiveStatusButtonText;
    public string CurrentStatusPillText => IsServiceCatalogSelected
        ? (EditingService.IsActive ? "Đang bán" : "Ngưng bán")
        : StatusPillText;
    public string SelectedServiceTypeDisplay => GetServiceTypeDisplay(EditingService.ServiceType);
    public string SelectedServicePriceDisplay => EditingService.DefaultPrice > 0 ? $"{EditingService.DefaultPrice:N0} đ" : "Chưa khai báo";

    // Section visibility
    public bool IsSectionDashboard => SelectedSection == SEC_DASHBOARD;
    public bool IsSectionMenuItems => SelectedSection == SEC_MENU_ITEMS;
    public bool IsSectionCategories => SelectedSection == SEC_CATEGORIES;
    public bool IsSectionIngredients => SelectedSection == SEC_INGREDIENTS;
    public bool IsSectionPreparationAreas => SelectedSection == SEC_PREPARATION_AREAS;
    public bool IsSectionRecipe => SelectedSection == SEC_RECIPE;
    public bool IsSectionCost => SelectedSection == SEC_COST;
    public bool IsSectionWarnings => SelectedSection == SEC_WARNINGS;
    public bool IsSectionReports => SelectedSection == SEC_REPORTS;
    public bool IsSectionSettings => SelectedSection == SEC_SETTINGS;

    public string SelectedMenuTitle => SelectedCatalogTitle;
    public string HeaderMetaText => IsServiceCatalogSelected
        ? $"Mã DV: {EditingService.Code}  •  Loại: {SelectedServiceTypeDisplay}  •  Đơn giá: {SelectedServicePriceDisplay}  •  VAT: {EditingService.VatRate * 100m:N0}%"
        : $"Mã món: {EditingItem.Code}  •  Nhóm: {SelectedCategoryCard?.Name ?? string.Empty}  •  Đơn vị bán: {EditingItem.UnitName}  •  VAT: {FormattedVatRate}";
    public string SaveButtonText => IsCreatingNewMenu ? "Lưu món" : "Lưu thay đổi";
    public string StatusPillText => EditingItem.IsActive ? "Đang bán" : "Ngưng bán";
    public string FormattedPrice => $"{EditingItem.UnitPrice:N0} đ";
    public string FormattedVatRate => $"{EditingItem.VatRate * 100m:N0}%";

    public string UnitPriceText
    {
        get => EditingItem.UnitPrice == 0 ? string.Empty : $"{EditingItem.UnitPrice:N0}";
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                EditingItem.UnitPrice = 0;
                RefreshInlineRecipeCostPreview();
                NotifyComputedState();
                return;
            }
            var cleaned = value.Replace(".", "").Replace(" ", "").Replace("đ", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleaned, out var d))
            {
                EditingItem.UnitPrice = d;
                RefreshInlineRecipeCostPreview();
                NotifyComputedState();
            }
        }
    }
    public string VatRateText
    {
        get => EditingItem.VatRate == 0 ? string.Empty : $"{EditingItem.VatRate * 100m:N0}";
        set
        {
            if (string.IsNullOrWhiteSpace(value)) { EditingItem.VatRate = 0; return; }
            var cleaned = value.Replace("%", "").Replace(" ", "").Trim();
            if (decimal.TryParse(cleaned, out var d)) EditingItem.VatRate = d / 100m;
        }
    }
    public string ServicePriceText
    {
        get => EditingService.DefaultPrice == 0 ? string.Empty : $"{EditingService.DefaultPrice:N0}";
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                EditingService.DefaultPrice = 0;
                NotifyComputedState();
                return;
            }

            var cleaned = value.Replace(".", "").Replace(" ", "").Replace("đ", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleaned, out var d))
            {
                EditingService.DefaultPrice = d;
                NotifyComputedState();
            }
        }
    }
    public string ServiceVatRateText
    {
        get => EditingService.VatRate == 0 ? string.Empty : $"{EditingService.VatRate * 100m:N0}";
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                EditingService.VatRate = 0;
                NotifyComputedState();
                return;
            }

            var cleaned = value.Replace("%", "").Replace(" ", "").Trim();
            if (decimal.TryParse(cleaned, out var d))
            {
                EditingService.VatRate = d / 100m;
                NotifyComputedState();
            }
        }
    }
    public string ActiveStatusText => EditingItem.IsActive ? "Đang bán" : "Ngưng bán";
    public string ActiveStatusButtonText => EditingItem.IsActive ? "Ngưng bán" : "Mở bán";
    public string RecipeStatusText => HasRecipeLines ? "Có định mức" : "Chưa định mức";
    public string IngredientLineTotalDisplay => $"{RecipeLines.Sum(x => x.EstimatedLineCost):N0} đ";
    public string LossCostDisplay => $"{Math.Max(0, PreviewCost - RecipeLines.Sum(x => x.EstimatedLineCost)):N0} đ";
    public string CostDisplay => $"{PreviewCost:N0} đ";
    public string SellingPriceDisplay => $"{EditingItem.UnitPrice:N0} đ";
    public string MarginAmountDisplay => $"{PreviewMarginAmount:N0} đ";
    public string MarginRateDisplay => $"{PreviewMarginRate * 100m:N1}%";
    public string SelectedImagePath => IsServiceCatalogSelected ? (EditingService.ImagePath ?? string.Empty) : (EditingItem.ImagePath ?? string.Empty);
    public GridLength SidebarWidth => IsSidebarExpanded ? new GridLength(172) : new GridLength(64);
    public Visibility SidebarTextVisibility => IsSidebarExpanded ? Visibility.Visible : Visibility.Collapsed;
    public string SidebarToggleGlyph => IsSidebarExpanded ? "\uE76B" : "\uE76C";
    public string SidebarToggleToolTip => IsSidebarExpanded ? "Thu gọn sidebar" : "Mở rộng sidebar";
    public string LastUpdatedDisplay
    {
        get
        {
            var typeLabel = IsServiceCatalogSelected ? "dịch vụ" : "món";
            var totalOfType = MenuCards.Count;
            var recipeOfType = IsServiceCatalogSelected ? 0 : MenuCards.Count(m => m.HasRecipe);
            var display = $"{totalOfType} {typeLabel}";
            if (recipeOfType > 0) display += $" | {recipeOfType} có định mức";
            if (FilteredMenuCards.Count < totalOfType) display += $" | Đang hiển thị {FilteredMenuCards.Count} {typeLabel}";
            return display;
        }
    }

    public string MarginStatus => PreviewMarginRate switch { >= 0.3m => "Tốt", >= 0.1m => "Thấp", _ => "Bán lỗ" };
    public Brush MarginBrush => PreviewMarginRate switch { >= 0.3m => new SolidColorBrush(Color.FromRgb(34, 197, 94)), >= 0.1m => new SolidColorBrush(Color.FromRgb(234, 179, 8)), _ => new SolidColorBrush(Color.FromRgb(239, 68, 68)) };
    public Brush StatusBannerBrush
        => (IsServiceCatalogSelected ? EditingService.IsActive : EditingItem.IsActive)
            ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
            : new SolidColorBrush(Color.FromRgb(148, 163, 184));
    public bool ShowCategoryKpis => !IsServiceCatalogSelected && IsRootCategorySelected;
    public bool ShowCategoryItemsPanel => !IsServiceCatalogSelected && IsChildCategorySelected;

    // Recipe footer
    public string TotalIngredientsText => $"Tổng: {RecipeLines.Count} nguyên liệu";
    public string TotalEffectiveQuantityDisplay
    {
        get
        {
            var total = RecipeLines.Sum(l => l.EffectiveQuantity);
            return total > 0 ? $"Tổng SL sau hao hụt: {total:N2}" : string.Empty;
        }
    }
    public bool HasRecipeFooter => RecipeLines.Count > 0;

    // Dashboard computed
    public int DashboardTotalItems => TotalMenuCount;
    public int DashboardHasRecipeCount => HasRecipeCount;
    public int DashboardNoRecipeCount => NoRecipeCount;
    public int DashboardInactiveCount => MenuCards.Count(m => m.IsInactive);
    public int DashboardCategoryCount => Categories.Count;
    public bool HasDashboardCriticalItems => DashboardCriticalItems.Count > 0;
    public int DashboardWarningCount => AllItemWarnings.Count;
    public string DashboardTotalCostEstimate
    {
        get
        {
            if (CostTotalEstimated > 0) return $"{CostTotalEstimated:N0} ₫";
            // Estimate from recipe lines if available
            var est = RecipeLines.Sum(x => x.EstimatedLineCost);
            return est > 0 ? $"{est:N0} ₫" : "Chưa tính";
        }
    }

    // Ingredients
    public bool HasIngredients => FilteredIngredients.Count > 0;
    public bool NoIngredients => !HasIngredients;

    // ===================================================================
    // SECTION NAVIGATION
    // ===================================================================
    [RelayCommand]
    private void NavigateSection(string sectionStr)
    {
        if (!int.TryParse(sectionStr, out int section)) return;
        if (section == SEC_REPORTS) section = SEC_WARNINGS;
        if (section < 0 || section > SEC_SETTINGS) return;
        SelectedSection = section;
    }

    private void OnSectionChanged()
    {
        NotifySectionVisibility();
        switch (SelectedSection)
        {
            case SEC_DASHBOARD: _ = LoadDashboardAsync(); break;
            case SEC_CATEGORIES: LoadManageableCategories(); break;
            case SEC_INGREDIENTS: ApplyIngredientFilter(); break;
            case SEC_MENU_ITEMS:
                _ = InitializeAsync();
                break;
            case SEC_WARNINGS: _ = LoadAllWarningsAsync(); break;
        }
    }

    private void ResetMenuFilters()
    {
        SearchText = string.Empty;
        FilterCategoryId = null;
        ActiveFilterChip = "Tất cả";
        SelectedSortOption = "Tên A-Z";
    }

    partial void OnSelectedSectionChanged(int value)
    {
        NotifySectionVisibility();
        if (value == SEC_MENU_ITEMS)
            ResetMenuFilters();
        OnSectionChanged();
    }

    private void NotifySectionVisibility() { OnPropertyChanged(nameof(IsSectionDashboard)); OnPropertyChanged(nameof(IsSectionMenuItems)); OnPropertyChanged(nameof(IsSectionCategories)); OnPropertyChanged(nameof(IsSectionIngredients)); OnPropertyChanged(nameof(IsSectionPreparationAreas)); OnPropertyChanged(nameof(IsSectionRecipe)); OnPropertyChanged(nameof(IsSectionCost)); OnPropertyChanged(nameof(IsSectionWarnings)); OnPropertyChanged(nameof(IsSectionReports)); OnPropertyChanged(nameof(IsSectionSettings)); }

    // ===================================================================
    // IMAGE UPLOAD
    // ===================================================================
    [RelayCommand]
    private void SelectMenuImage()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = IsServiceCatalogSelected ? "Chọn ảnh dịch vụ" : "Chọn ảnh món ăn",
            Filter = "Hình ảnh (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp",
            Multiselect = false
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var srcPath = dialog.FileName;
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASMILE-BO", "MenuImages");
            Directory.CreateDirectory(appData);
            var ext = Path.GetExtension(srcPath);
            var code = IsServiceCatalogSelected ? EditingService.Code : EditingItem.Code;
            var safeCode = string.IsNullOrWhiteSpace(code) ? "new" : CreateSlug(code);
            var prefix = IsServiceCatalogSelected ? "service" : "menu";
            var fileName = $"{prefix}_{safeCode}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
            var destPath = Path.Combine(appData, fileName);
            File.Copy(srcPath, destPath, overwrite: true);
            if (IsServiceCatalogSelected)
            {
                EditingService.ImagePath = destPath;
            }
            else
            {
                EditingItem.ImagePath = destPath;
            }

            if (SelectedMenuCard is not null) SelectedMenuCard.ImagePath = destPath;
            OnPropertyChanged(nameof(SelectedImageFullPath));
            OnPropertyChanged(nameof(HasImage));
            OnPropertyChanged(nameof(NoImage));
            OnPropertyChanged(nameof(SelectedImagePath));
            StatusMessage = IsServiceCatalogSelected ? "Đã chọn ảnh dịch vụ." : "Đã chọn ảnh món."; StatusIsSuccess = true;
        }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; StatusIsSuccess = false; }
    }

    [RelayCommand]
    private void RemoveMenuImage()
    {
        if (IsServiceCatalogSelected)
        {
            EditingService.ImagePath = null;
        }
        else
        {
            EditingItem.ImagePath = null;
        }

        if (SelectedMenuCard is not null) SelectedMenuCard.ImagePath = null;
        OnPropertyChanged(nameof(SelectedImageFullPath));
        OnPropertyChanged(nameof(HasImage));
        OnPropertyChanged(nameof(NoImage));
        OnPropertyChanged(nameof(SelectedImagePath));
        StatusMessage = "Đã xóa ảnh."; StatusIsSuccess = true;
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarExpanded = !IsSidebarExpanded;
    }

    public string? SelectedImageFullPath
    {
        get
        {
            var path = IsServiceCatalogSelected ? EditingService.ImagePath : EditingItem.ImagePath;
            if (string.IsNullOrEmpty(path)) return null;
            if (Path.IsPathRooted(path)) return path;
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASMILE-BO", "MenuImages");
            var full = Path.Combine(appData, path);
            return File.Exists(full) ? full : null;
        }
    }

    partial void OnIsSidebarExpandedChanged(bool value)
    {
        OnPropertyChanged(nameof(SidebarWidth));
        OnPropertyChanged(nameof(SidebarTextVisibility));
        OnPropertyChanged(nameof(SidebarToggleGlyph));
        OnPropertyChanged(nameof(SidebarToggleToolTip));
    }

    // ===================================================================
    // SECTION: DASHBOARD
    // ===================================================================
    private async Task LoadDashboardAsync()
    {
        if (_dashboardLoaded || IsBusy) return;
        await _dbLock.WaitAsync();
        try
        {
            IsBusy = true;
            DashboardCriticalItems.Clear();
            await RefreshLookupsAsync();
            await LoadAllMenuItemsAsync();

            // Find items with cost issues (recent inactive, no recipe)
            var critical = MenuCards
                .Where(m => m.IsInactive || !m.HasRecipe)
                .Take(20)
                .ToList();
            ResetCollection(DashboardCriticalItems, critical);

            NotifyDashboardState();
            StatusMessage = $"Dashboard: {TotalMenuCount} món, {HasRecipeCount} có định lượng, {Categories.Count} nhóm";
            StatusIsSuccess = true;
            _dashboardLoaded = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tải dashboard: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally { IsBusy = false; _dbLock.Release(); NotifyComputedState(); }
    }

    private void NotifyDashboardState()
    {
        OnPropertyChanged(nameof(DashboardTotalItems));
        OnPropertyChanged(nameof(DashboardHasRecipeCount));
        OnPropertyChanged(nameof(DashboardNoRecipeCount));
        OnPropertyChanged(nameof(DashboardInactiveCount));
        OnPropertyChanged(nameof(DashboardCategoryCount));
        OnPropertyChanged(nameof(HasDashboardCriticalItems));
        OnPropertyChanged(nameof(DashboardWarningCount));
        OnPropertyChanged(nameof(DashboardTotalCostEstimate));
    }

    [RelayCommand]
    private void NavigateFromDashboard(string targetStr)
    {
        if (int.TryParse(targetStr, out int target))
            SelectedSection = target;
    }

    // ===================================================================
    // SECTION: CATEGORY MANAGEMENT
    // ===================================================================
    private void LoadManageableCategories()
    {
        ResetCollection(ManageableCategories, CategoryCards);
        OnPropertyChanged(nameof(ManageableCategories));
    }

    private void PromoteCategoryInView(Guid categoryId)
    {
        MoveCategoryToFront(CategoryCards, categoryId);
        MoveCategoryToFront(FilteredCategoryCards, categoryId);
        MoveCategoryToFront(ManageableCategories, categoryId);
    }

    private static void MoveCategoryToFront(ObservableCollection<MenuCategoryCardState> categories, Guid categoryId)
    {
        var index = categories
            .Select((item, idx) => new { item, idx })
            .FirstOrDefault(x => x.item.Id == categoryId)?
            .idx;

        if (!index.HasValue || index.Value <= 0)
        {
            return;
        }

        var card = categories[index.Value];
        categories.RemoveAt(index.Value);
        categories.Insert(0, card);
    }

    [RelayCommand]
    private async Task SelectEditCategoryAsync(MenuCategoryCardState? cat)
    {
        if (cat is null) return;
        EditingCategory = cat;
        EditCategoryName = cat.Name;
        EditCategoryCode = cat.Code;
        EditCategoryParentId = cat.ParentCategoryId;
        EditCategoryIsActive = true;
        await LoadAllMenuItemsAsync();
        LoadCategoryDetailItems(cat.Id);
    }

    private void LoadCategoryDetailItems(Guid categoryId)
    {
        var catName = Categories.FirstOrDefault(c => c.Id == categoryId)?.Name ?? string.Empty;
        // Try MenuCards first (from current ViewModel state)
        var items = MenuCards.Where(m => m.CategoryId == categoryId).OrderBy(m => m.Name).ToList();
        // If MenuCards is empty, try from FilteredMenuCards or build from _allMenuItems
        if (items.Count == 0)
        {
            items = _allMenuItems
                .Where(m => m.MenuCategoryId == categoryId)
                .Select(m => new MenuItemCardState
                {
                    Id = m.Id, Name = m.Name, Code = m.Code, CategoryName = m.CategoryName,
                    UnitPriceDisplay = $"{m.UnitPrice:N0} đ",
                    CategoryId = m.MenuCategoryId,
                    HasRecipe = m.HasRecipe,
                    IsInactive = !m.IsActive,
                    RecipeStatusBadge = m.HasRecipe ? "Có định lượng" : "Chưa định mức",
                    ActiveStatusBadge = m.IsActive ? "Đang bán" : "Ngưng bán"
                }).ToList();
        }
        ResetCollection(CategoryDetailItems, items);
        SelectedCategoryMenuItem = items.FirstOrDefault();
        // Update the category card's item count
        var card = CategoryCards.FirstOrDefault(c => c.Id == categoryId);
        if (card is not null && card.ItemCount == 0 && items.Count > 0)
        {
            card.ItemCount = items.Count;
            var idx = CategoryCards.IndexOf(card);
            if (idx >= 0) { CategoryCards.RemoveAt(idx); CategoryCards.Insert(idx, card); }
        }
        OnPropertyChanged(nameof(HasCategoryDetailItems));
        OnPropertyChanged(nameof(CategoryDetailActiveCount));
        OnPropertyChanged(nameof(CategoryDetailRecipeCount));
        OnPropertyChanged(nameof(CategoryDetailNoRecipeCount));
        OnPropertyChanged(nameof(CategoryDetailAvgPrice));
        OnPropertyChanged(nameof(CategoryDetailAvgPriceDisplay));
    }

    private async Task LoadEditingCategoryAsync(MenuCategoryCardState? cat)
    {
        if (cat is null)
        {
            CategoryDetailItems.Clear();
            SelectedCategoryRecipeLines.Clear();
            SelectedCategoryMenuItem = null;
            return;
        }

        _isLoadingCategorySelection = true;
        try
        {
            EditCategoryName = cat.Name;
            EditCategoryCode = cat.Code;
            EditCategoryParentId = cat.ParentCategoryId;
            EditCategoryIsActive = true;
            if (cat.IsVirtual)
            {
                CategoryDetailItems.Clear();
                SelectedCategoryRecipeLines.Clear();
                SelectedCategoryMenuItem = null;
                OnPropertyChanged(nameof(HasCategoryDetailItems));
                return;
            }
            await LoadAllMenuItemsAsync();
            LoadCategoryDetailItems(cat.Id);
        }
        finally
        {
            _isLoadingCategorySelection = false;
        }
    }

    [RelayCommand]
    private async Task SaveCategoryEditAsync()
    {
        if (EditingCategory is null || string.IsNullOrWhiteSpace(EditCategoryName)) return;
        if (EditingCategory.IsVirtual)
        {
            StatusMessage = "Nhóm dịch vụ hệ thống đang lấy theo loại dịch vụ thực tế, chưa chỉnh sửa trực tiếp tại màn này.";
            StatusIsSuccess = false;
            return;
        }
        IsBusy = true;
        try
        {
            await _menuCatalogService.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
            {
                Id = EditingCategory.Id,
                ParentCategoryId = EditCategoryParentId,
                Code = string.IsNullOrWhiteSpace(EditCategoryCode) ? CreateSlug(EditCategoryName) : EditCategoryCode,
                Name = EditCategoryName,
                SortOrder = Categories.Count + 1,
                IsActive = EditCategoryIsActive
            });
            await RefreshLookupsAsync();
            LoadManageableCategories();
            StatusMessage = $"Đã cập nhật nhóm {EditCategoryName}.";
            StatusIsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    [RelayCommand]
    private async Task ToggleCategoryActiveAsync(MenuCategoryCardState? cat)
    {
        if (cat is null) return;
        if (cat.IsVirtual)
        {
            StatusMessage = "Nhóm dịch vụ hệ thống đang lấy theo loại dịch vụ thực tế, chưa ngưng trực tiếp tại màn này.";
            StatusIsSuccess = false;
            return;
        }
        IsBusy = true;
        try
        {
            await _menuCatalogService.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
            {
                Id = cat.Id, Code = cat.Code, Name = cat.Name,
                SortOrder = 0, IsActive = false
            });
            await RefreshLookupsAsync();
            LoadManageableCategories();
            StatusMessage = $"Đã ngưng nhóm {cat.Name}.";
            StatusIsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(MenuCategoryCardState? cat)
    {
        if (cat is null || IsBusy) return;
        if (cat.IsVirtual)
        {
            StatusMessage = "Nhóm dịch vụ hệ thống đang lấy theo loại dịch vụ thực tế, chưa xóa trực tiếp tại màn này.";
            StatusIsSuccess = false;
            return;
        }

        var result = MessageBox.Show(
            $"Xóa nhóm {cat.Name}?\n\nNếu nhóm đang có món hoặc có nhóm con, hệ thống sẽ chuyển sang ngưng sử dụng thay vì xóa hẳn.",
            "Xác nhận xóa nhóm",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            await _menuCatalogService.DeleteOrDeactivateCategoryAsync(cat.Id, softDeleteOnly: false);
            var deletedName = cat.Name;
            await RefreshLookupsAsync();
            LoadManageableCategories();
            EditingCategory = FilteredCategoryCards.FirstOrDefault();
            StatusMessage = $"Đã xử lý xóa nhóm {deletedName}.";
            StatusIsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanDeleteCategory));
            NotifyComputedState();
        }
    }

    // ===================================================================
    // SECTION: INGREDIENTS
    // ===================================================================
    partial void OnIngredientSearchTextChanged(string value) => ApplyIngredientFilter();

    private void ApplyIngredientFilter()
    {
        var filtered = IngredientLookup.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(IngredientSearchText))
        {
            var term = IngredientSearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(x =>
                x.ItemName.ToLowerInvariant().Contains(term) ||
                x.ItemCode.ToLowerInvariant().Contains(term));
        }
        ResetCollection(FilteredIngredients, filtered.ToList());
        OnPropertyChanged(nameof(HasIngredients));
        OnPropertyChanged(nameof(NoIngredients));
    }

    // ===================================================================
    // SECTION: COST CALCULATION
    // ===================================================================
    public string CostScopeLabel => CostScopeIndex >= 0 && CostScopeIndex < CostScopeLabels.Length
        ? CostScopeLabels[CostScopeIndex] : "Món hiện tại";

    public bool HasCostDetailRows => CostDetailRows.Count > 0;
    public bool HasCostIngredientRows => CostIngredientRows.Count > 0;
    public string CostTargetText => CostScopeIndex switch
    {
        0 => SelectedCostMenuCard?.Name ?? SelectedMenuCard?.Name ?? EditingItem.Name ?? string.Empty,
        1 => Categories.FirstOrDefault(x => x.Id == SelectedCostCategoryId)?.Name
             ?? Categories.FirstOrDefault(x => x.Id == FilterCategoryId)?.Name
             ?? SelectedCategoryCard?.Name
             ?? string.Empty,
        _ => "Tất cả món"
    };

    partial void OnCostScopeIndexChanged(int value)
    {
        OnPropertyChanged(nameof(CostScopeLabel));
        OnPropertyChanged(nameof(CostTargetText));
    }

    partial void OnSelectedCostMenuCardChanged(MenuItemCardState? value)
    {
        OnPropertyChanged(nameof(CostTargetText));
    }

    partial void OnSelectedCostCategoryIdChanged(Guid? value)
    {
        OnPropertyChanged(nameof(CostTargetText));
    }

    partial void OnSelectedCostDetailRowChanged(CostDetailRow? value)
    {
        CostIngredientRows.Clear();
        if (value is not null)
        {
            foreach (var line in value.Lines)
            {
                CostIngredientRows.Add(line);
            }
        }
        OnPropertyChanged(nameof(HasCostIngredientRows));
    }

    [RelayCommand]
    private async Task RunCostCalculationAsync()
    {
        IsBusy = true;
        CostProgressText = "Đang tính...";
        CostProcessedCount = 0;
        CostTotalEstimated = 0;
        CostTotalSelling = 0;
        CostDetailRows.Clear();
        CostIngredientRows.Clear();
        SelectedCostDetailRow = null;

        try
        {
            if (CostScopeIndex == 0)
            {
                var selectedItemId = SelectedCostMenuCard?.Id ?? SelectedMenuCard?.Id ?? EditingItem.Id;
                if (selectedItemId is null)
                {
                    StatusMessage = "Vui lòng chọn món.";
                    StatusIsSuccess = false;
                    return;
                }

                CostTotalCount = 1;
                var detail = await _menuCatalogService.GetMenuItemDetailAsync(selectedItemId.Value);
                var preview = await _menuRecipeService.PreviewCostAsync(selectedItemId.Value);
                AddCostDetailRow(detail, preview);
                CostProcessedCount = 1;
                CostTotalEstimated = preview.EstimatedCost;
                CostTotalSelling = preview.SellingPrice;
                SelectedCostDetailRow = CostDetailRows.FirstOrDefault();
                CostProgressText = "Đã tính 1 món.";
            }
            else
            {
                var currentCategoryId = SelectedCostCategoryId ?? FilterCategoryId ?? (EditingItem.MenuCategoryId == Guid.Empty ? null : EditingItem.MenuCategoryId);
                var items = CostScopeIndex == 1 && currentCategoryId.HasValue
                    ? MenuCards.Where(m => !m.IsInactive && m.CategoryId == currentCategoryId.Value).ToList()
                    : MenuCards.Where(m => CostScopeIndex == 2 || !m.IsInactive).ToList();
                CostTotalCount = items.Count;
                CostProcessedCount = 0;
                decimal totalCost = 0, totalSell = 0;

                foreach (var item in items)
                {
                    try
                    {
                        var detail = await _menuCatalogService.GetMenuItemDetailAsync(item.Id);
                        if (detail is not null)
                        {
                            var preview = await _menuRecipeService.PreviewCostAsync(item.Id);
                            AddCostDetailRow(detail, preview);
                            totalCost += preview.EstimatedCost;
                            totalSell += detail.UnitPrice;
                        }
                    }
                    catch { }

                    CostProcessedCount++;
                    CostProgressText = $"Đã tính {CostProcessedCount}/{CostTotalCount} món...";
                }

                CostTotalEstimated = totalCost;
                CostTotalSelling = totalSell;
                SelectedCostDetailRow = CostDetailRows.FirstOrDefault();
                CostProgressText = $"Hoàn tất: {CostProcessedCount} món, tổng giá vốn {totalCost:N0} đ";
            }

            OnPropertyChanged(nameof(HasCostDetailRows));
            OnPropertyChanged(nameof(HasCostIngredientRows));
            StatusMessage = CostProgressText;
            StatusIsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CostTotalEstimated));
            OnPropertyChanged(nameof(CostTotalSelling));
            OnPropertyChanged(nameof(CostProgressText));
            OnPropertyChanged(nameof(CostProcessedCount));
            OnPropertyChanged(nameof(CostTotalCount));
            OnPropertyChanged(nameof(CostTargetText));
            NotifyComputedState();
        }
    }

    private void AddCostDetailRow(MenuItemDto? detail, MenuCostPreviewDto preview)
    {
        var sellingPrice = detail?.UnitPrice ?? preview.SellingPrice;
        CostDetailRows.Add(new CostDetailRow
        {
            MenuItemId = preview.MenuItemId,
            Code = preview.MenuItemCode,
            Name = preview.MenuItemName,
            CategoryName = detail?.CategoryName ?? string.Empty,
            SellingPrice = sellingPrice,
            EstimatedCost = preview.EstimatedCost,
            MarginAmount = sellingPrice - preview.EstimatedCost,
            MarginRate = sellingPrice > 0 ? (sellingPrice - preview.EstimatedCost) / sellingPrice : 0m,
            LineCount = preview.Lines.Count,
            WarningCount = preview.Warnings.Count,
            Lines = preview.Lines.Select(x => new CostIngredientRow
            {
                LineNo = x.LineNo,
                ItemCode = x.ItemCode,
                ItemName = x.ItemName,
                Quantity = x.Quantity,
                LossRatePercent = x.LossRate * 100m,
                EffectiveQuantity = x.EffectiveQuantity,
                UnitCost = x.UnitCost,
                LineCost = x.LineCost,
                AvailableQuantity = x.AvailableQuantity,
                Warning = x.Warning ?? (x.MissingCost ? "Thiếu giá vốn" : x.InsufficientStock ? "Thiếu tồn kho" : string.Empty)
            }).ToList()
        });
    }

    // ===================================================================
    // SECTION: ALL WARNINGS
    // ===================================================================
    public ObservableCollection<ItemWarningDto> AllItemWarnings { get; } = new();
    public bool HasAllWarnings => AllItemWarnings.Count > 0;
    public bool NoAllWarnings => !HasAllWarnings;
    public int CriticalAllWarningCount => AllItemWarnings.Count(x => x.Severity == "error");
    public int StockAllWarningCount => AllItemWarnings.Count(x => x.WarningType == "stock");
    public int CostAllWarningCount => AllItemWarnings.Count(x => x.WarningType == "cost");
    public int RecipeAllWarningCount => AllItemWarnings.Count(x => x.WarningType == "recipe");

    private async Task LoadAllWarningsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        AllItemWarnings.Clear();
        try
        {
            var items = await _menuCatalogService.GetMenuItemsAsync(includeInactive: true);
            foreach (var item in items)
            {
                if (!item.HasRecipe)
                {
                    AllItemWarnings.Add(new ItemWarningDto
                    {
                        ItemId = item.Id, ItemCode = item.Code, ItemName = item.Name,
                        Severity = "warning",
                        WarningType = "recipe",
                        WarningTypeText = "Định lượng",
                        Message = "Chưa có định lượng"
                    });
                }
                try
                {
                    var preview = await _menuRecipeService.PreviewCostAsync(item.Id);
                    foreach (var line in preview.Lines.Where(x => x.InsufficientStock || x.MissingCost))
                    {
                        AllItemWarnings.Add(new ItemWarningDto
                        {
                            ItemId = item.Id, ItemCode = item.Code, ItemName = item.Name,
                            IngredientName = line.ItemName,
                            Severity = line.InsufficientStock ? "error" : "warning",
                            WarningType = line.InsufficientStock ? "stock" : "cost",
                            WarningTypeText = line.InsufficientStock ? ((line.AvailableQuantity <= 0) ? "Hết kho" : "Thiếu tồn") : "Giá vốn",
                            RequiredQuantity = line.EffectiveQuantity,
                            AvailableQuantity = line.AvailableQuantity,
                            MissingQuantity = Math.Max(0, line.EffectiveQuantity - line.AvailableQuantity),
                            Message = line.InsufficientStock
                                ? $"{line.ItemName}: cần {line.EffectiveQuantity:N2}, tồn {line.AvailableQuantity:N2}, thiếu {Math.Max(0, line.EffectiveQuantity - line.AvailableQuantity):N2}."
                                : $"{line.ItemName}: chưa có giá vốn."
                        });
                    }
                    if (preview.EstimatedCost > 0 && preview.SellingPrice < preview.EstimatedCost)
                    {
                        AllItemWarnings.Add(new ItemWarningDto
                        {
                            ItemId = item.Id, ItemCode = item.Code, ItemName = item.Name,
                            Severity = "error",
                            WarningType = "margin",
                            WarningTypeText = "Biên lợi nhuận",
                            Message = $"Giá bán {preview.SellingPrice:N0} đ thấp hơn giá vốn {preview.EstimatedCost:N0} đ."
                        });
                    }
                }
                catch { /* skip */ }
            }
            OnPropertyChanged(nameof(HasAllWarnings));
            OnPropertyChanged(nameof(NoAllWarnings));
            OnPropertyChanged(nameof(CriticalAllWarningCount));
            OnPropertyChanged(nameof(StockAllWarningCount));
            OnPropertyChanged(nameof(CostAllWarningCount));
            OnPropertyChanged(nameof(RecipeAllWarningCount));
            StatusMessage = $"Có {AllItemWarnings.Count} cảnh báo.";
            StatusIsSuccess = AllItemWarnings.Count == 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    // ===================================================================
    // EXISTING COMMANDS & METHODS (preserved)
    // ===================================================================
    [RelayCommand]
    private void CreateNewMenu()
    {
        if (IsServiceCatalogSelected)
        {
            CreateNewService();
            return;
        }

        IsCreatingNewMenu = true;
        EditingItem = new CreateMenuItemRequest
        {
            UnitName = "Phần",
            HasRecipeManagement = true,
            IsActive = true,
            VatRate = 0.08m,
            IsVatIncluded = true,
            IsHotelService = IsServiceCatalogSelected
        };
        AssignDefaultCategoryForCurrentCatalog();
        SelectedMenuCard = null; SelectedPreparationArea = GetDefaultPreparationAreaCode(); IsEditingStatusActive = true;
        SelectedWorkspaceTab = 0; ClearRecipeState(); MenuHistoryEntries.Clear(); NotifyComputedState();
    }

    [RelayCommand]
    private void SetSelectedMenuCatalogTab(string? tabValue)
    {
        if (!int.TryParse(tabValue, out var tabIndex))
        {
            tabIndex = 0;
        }

        SelectedMenuCatalogTab = tabIndex;
    }

    [RelayCommand]
    private void ShowCreateCategory()
    {
        if (!CanCreateCategory) return;
        IsCreatingCategory = true;
        NewCategoryName = string.Empty; NewCategoryCode = string.Empty;
        NewCategoryParentId = IsServiceCatalogSelected ? GetDefaultServiceParentCategoryId() : null;
        StatusMessage = "Nhập tên nhóm món mới."; StatusIsSuccess = true;
        NotifyComputedState();
    }

    [RelayCommand]
    private void CancelCreateCategory()
    {
        IsCreatingCategory = false; NewCategoryName = string.Empty; NewCategoryCode = string.Empty; NewCategoryParentId = null;
        NotifyComputedState();
    }

    [RelayCommand]
    private async Task CreateCategoryAsync()
    {
        if (!CanSaveCategory) return;
        IsBusy = true;
        try
        {
            var parentCategoryId = NewCategoryParentId;
            if (IsServiceCatalogSelected && !parentCategoryId.HasValue)
            {
                parentCategoryId = GetDefaultServiceParentCategoryId();
            }

            var category = await _menuCatalogService.CreateOrUpdateCategoryAsync(new CreateMenuCategoryRequest
            {
                Name = NewCategoryName,
                ParentCategoryId = parentCategoryId,
                Code = string.IsNullOrWhiteSpace(NewCategoryCode) ? CreateSlug(NewCategoryName) : NewCategoryCode,
                SortOrder = Categories.Count + 1, IsActive = true
            });
            CategorySearchText = string.Empty;
            await RefreshLookupsAsync();
            FilterCategoryId = category.Id;
            PromoteCategoryInView(category.Id);
            EditingCategory = CategoryCards.FirstOrDefault(x => x.Id == category.Id);
            if (IsCreatingNewMenu) EditingItem.MenuCategoryId = category.Id;
            IsCreatingCategory = false; NewCategoryName = string.Empty; NewCategoryCode = string.Empty; NewCategoryParentId = null;
            StatusMessage = $"Đã tạo nhóm món {category.Name}."; StatusIsSuccess = true;
        }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; StatusIsSuccess = false; }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    [RelayCommand]
    private async Task BootstrapCategoriesAsync()
    {
        if (IsBusy) return; IsBusy = true;
        try { await _menuCatalogService.EnsureDefaultsAsync(); await RefreshLookupsAsync(); StatusMessage = "Đã tạo nhóm món mặc định."; StatusIsSuccess = true; }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; StatusIsSuccess = false; }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    [RelayCommand]
    private void RefreshAll()
    {
        ClearRecipeState(); IsCreatingNewMenu = false; SelectedMenuCard = null;
        EditingItem = new CreateMenuItemRequest { IsVatIncluded = true, VatRate = 0.08m };
        EditingService = new SaveRestaurantExtraServiceRequest { IsVatIncluded = true };
        SelectedPreparationArea = GetDefaultPreparationAreaCode();
        SelectedWorkspaceTab = 0; _ = InitializeAsync();
    }

    partial void OnSelectedMenuCardChanged(MenuItemCardState? value)
    {
        if (value is null || IsCreatingNewMenu) return;
        var loadVersion = Interlocked.Increment(ref _menuCardLoadVersion);
        OnPropertyChanged(nameof(HasSelectedMenu)); OnPropertyChanged(nameof(NoSelectedMenu));
        OnPropertyChanged(nameof(ShowEmptyState));
        _ = LoadSelectedMenuCardAsync(value, loadVersion);
    }

    partial void OnSelectedRecipeLineChanged(RecipeLineEditor? value)
    {
        ResetSubstituteForm();
        RefreshAvailableSubstituteItems();
        OnPropertyChanged(nameof(HasSelectedRecipeLine));
        OnPropertyChanged(nameof(NoSelectedRecipeLine));
        OnPropertyChanged(nameof(CanAddSubstitute));

        if (value is null)
        {
            SubstituteIngredients.Clear();
            NotifySubstituteState();
            return;
        }

        _ = LoadSubstitutesAsync();
    }

    partial void OnSelectedSubstituteIngredientChanged(SubstituteIngredientDto? value)
    {
        if (value is null)
        {
            return;
        }

        SelectedSubstituteItem = IngredientLookup.FirstOrDefault(x => x.ItemId == value.SubstituteInventoryItemId);
        SubstituteConversionRate = value.ConversionRate;
        SubstitutePriority = value.Priority;
        SubstituteAutoReplace = value.AutoReplace;
        SubstituteNotes = value.Notes ?? string.Empty;
        RefreshAvailableSubstituteItems();
        OnPropertyChanged(nameof(CanAddSubstitute));
    }

    partial void OnSubstituteSearchTextChanged(string value)
    {
        RefreshAvailableSubstituteItems();
    }

    private async Task LoadSelectedMenuCardAsync(MenuItemCardState value, int loadVersion)
    {
        try { await LoadMenuCardAsync(value, loadVersion); }
        catch (Exception ex)
        {
            if (loadVersion == Volatile.Read(ref _menuCardLoadVersion))
            {
                StatusMessage = $"Không thể tải chi tiết món: {ex.Message}";
                StatusIsSuccess = false;
            }
        }
    }

    [RelayCommand]
    private async Task SelectMenuAsync(MenuItemCardState card)
    {
        if (card is null) return;
        var loadVersion = Interlocked.Increment(ref _menuCardLoadVersion);
        await LoadMenuCardAsync(card, loadVersion);
    }

    private async Task LoadMenuCardAsync(MenuItemCardState card, int loadVersion)
    {
        if (card is null || loadVersion != Volatile.Read(ref _menuCardLoadVersion)) return;
        IsBusy = true;
        try
        {
            if (loadVersion != Volatile.Read(ref _menuCardLoadVersion)) return;
            if (IsServiceCatalogSelected)
            {
                var service = _allExtraServices.FirstOrDefault(x => x.Id == card.Id);
                if (service is not null && loadVersion == Volatile.Read(ref _menuCardLoadVersion))
                {
                    ApplySelectedService(service);
                }
                return;
            }

            IsCreatingNewMenu = false;
            var detail = await _menuCatalogService.GetMenuItemDetailAsync(card.Id);
            if (loadVersion != Volatile.Read(ref _menuCardLoadVersion)) return;
            if (detail is not null)
            {
                EditingItem = new CreateMenuItemRequest
                {
                    Id = detail.Id, MenuCategoryId = detail.MenuCategoryId, Code = detail.Code,
                    Name = detail.Name, UnitName = detail.UnitName, UnitPrice = detail.UnitPrice,
                    VatRate = detail.VatRate, IsVatIncluded = detail.IsVatIncluded, HasRecipeManagement = detail.HasRecipeManagement,
                    IsCombo = detail.IsCombo, IsHotelService = detail.IsHotelService,
                    IsActive = detail.IsActive, Note = detail.Note, PreparationArea = detail.PreparationArea,
                    ImagePath = detail.ImagePath
                };
                SelectedPreparationArea = ResolvePreparationAreaSelection(detail.PreparationArea);
                IsEditingStatusActive = detail.IsActive;
                SelectedWorkspaceTab = 0;
            }
            await LoadRecipeAsync(card.Id, loadVersion);
            if (loadVersion == Volatile.Read(ref _menuCardLoadVersion))
            {
                LoadMenuHistory(card.Id);
            }
        }
        finally
        {
            if (loadVersion == Volatile.Read(ref _menuCardLoadVersion))
            {
                IsBusy = false;
                NotifyComputedState();
            }
        }
    }

    [RelayCommand]
    private async Task SaveMenuItemAsync()
    {
        if (IsBusy) return;
        if (!ValidateEditingItemForSave()) return;
        IsBusy = true;
        try
        {
            await SaveMenuItemCoreAsync("Đã lưu món thành công.");
        }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; StatusIsSuccess = false; }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    private async Task<MenuItemDto> SaveMenuItemCoreAsync(string successMessage, bool recordHistory = true)
    {
        var isNew = EditingItem.Id is null;
        EditingItem.PreparationArea = SelectedPreparationArea;
        EditingItem.IsActive = IsEditingStatusActive;
        var saved = await _menuCatalogService.CreateOrUpdateMenuItemAsync(EditingItem);

            ApplySavedMenuItem(saved);
            await RefreshLookupsAsync();
            await LoadAllMenuItemsAsync();
            ApplyFilters();
            SelectedMenuCard = FilteredMenuCards.FirstOrDefault(x => x.Id == saved.Id)
                ?? MenuCards.FirstOrDefault(x => x.Id == saved.Id);
            LoadCategoryDetailItems(saved.MenuCategoryId);
            await AutoRefreshSelectedCostAsync(saved.Id, updateRecipeLinesFromPreview: true);

            StatusMessage = successMessage;
        StatusIsSuccess = true;
        if (recordHistory)
        {
            AddMenuHistory(saved.Id, isNew ? "Tạo món" : "Lưu thông tin món", $"{saved.Code} - {saved.Name}");
        }
        NotifyComputedState();
        return saved;
    }

    private void ApplySavedMenuItem(MenuItemDto saved)
    {
        EditingItem = new CreateMenuItemRequest
        {
            Id = saved.Id,
            MenuCategoryId = saved.MenuCategoryId,
            Code = saved.Code,
            Name = saved.Name,
            UnitName = saved.UnitName,
            UnitPrice = saved.UnitPrice,
            VatRate = saved.VatRate,
            IsVatIncluded = saved.IsVatIncluded,
            HasRecipeManagement = saved.HasRecipeManagement,
            IsCombo = saved.IsCombo,
            IsHotelService = saved.IsHotelService,
            IsActive = saved.IsActive,
            Note = saved.Note,
            PreparationArea = saved.PreparationArea,
            ImagePath = saved.ImagePath
        };
        SelectedPreparationArea = ResolvePreparationAreaSelection(saved.PreparationArea);
        IsEditingStatusActive = saved.IsActive;
        IsCreatingNewMenu = false;
    }

    private bool ValidateEditingItemForSave()
    {
        if (EditingItem.MenuCategoryId == Guid.Empty) { StatusMessage = "Vui lòng chọn nhóm món trước khi lưu."; StatusIsSuccess = false; return false; }
        if (string.IsNullOrWhiteSpace(EditingItem.Code)) { StatusMessage = "Vui lòng nhập mã món."; StatusIsSuccess = false; return false; }
        if (string.IsNullOrWhiteSpace(EditingItem.Name)) { StatusMessage = "Vui lòng nhập tên món."; StatusIsSuccess = false; return false; }
        return true;
    }

    [RelayCommand]
    private async Task ToggleActiveAsync()
    {
        if (EditingItem.Id is null) return;
        IsEditingStatusActive = !IsEditingStatusActive;
        var dto = new CreateMenuItemRequest
        {
            Id = EditingItem.Id, MenuCategoryId = EditingItem.MenuCategoryId,
            Code = EditingItem.Code, Name = EditingItem.Name, UnitName = EditingItem.UnitName,
            UnitPrice = EditingItem.UnitPrice, VatRate = EditingItem.VatRate, IsVatIncluded = EditingItem.IsVatIncluded,
            HasRecipeManagement = EditingItem.HasRecipeManagement, IsCombo = EditingItem.IsCombo,
            IsHotelService = EditingItem.IsHotelService, IsActive = IsEditingStatusActive,
            Note = EditingItem.Note, PreparationArea = SelectedPreparationArea,
            ImagePath = EditingItem.ImagePath
        };
        await _menuCatalogService.CreateOrUpdateMenuItemAsync(dto);
        EditingItem.IsActive = IsEditingStatusActive;
        await LoadAllMenuItemsAsync(); ApplyFilters();
        SelectMenuCardById(EditingItem.Id.Value);
        StatusMessage = IsEditingStatusActive ? "Đã kích hoạt món." : "Đã ngưng bán món."; StatusIsSuccess = true;
        AddMenuHistory(EditingItem.Id.Value, IsEditingStatusActive ? "Kích hoạt món" : "Ngưng bán món", $"{EditingItem.Code} - {EditingItem.Name}");
        NotifyComputedState();
    }

    [RelayCommand]
    private async Task DuplicateMenuItemAsync()
    {
        if (EditingItem.Id is null) return;
        IsCreatingNewMenu = true;
        var original = EditingItem;
        EditingItem = new CreateMenuItemRequest
        {
            MenuCategoryId = original.MenuCategoryId,
            Code = original.Code + "_COPY",
            Name = original.Name + " (sao chép)",
            UnitName = original.UnitName, UnitPrice = original.UnitPrice,
            VatRate = original.VatRate, IsVatIncluded = original.IsVatIncluded, HasRecipeManagement = original.HasRecipeManagement,
            IsCombo = original.IsCombo, IsActive = true,
            PreparationArea = SelectedPreparationArea
        };
        ClearRecipeState(); SelectedMenuCard = null; MenuHistoryEntries.Clear();
        StatusMessage = "Đã sao chép món, hãy lưu để hoàn tất."; StatusIsSuccess = true;
        NotifyComputedState();
    }

    [RelayCommand]
    private async Task DeleteMenuAsync()
    {
        if (EditingItem.Id is null || IsBusy) return;

        var result = MessageBox.Show(
            $"Xóa menu {EditingItem.Name} ({EditingItem.Code})?\n\nNếu món đã có định lượng hoặc dữ liệu liên quan, hệ thống sẽ ngưng bán thay vì xóa hẳn.",
            "Xác nhận xóa menu",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            var deletedId = EditingItem.Id.Value;
            var deletedName = EditingItem.Name;
            var deletedCode = EditingItem.Code;

            await _menuCatalogService.DeleteOrDeactivateMenuItemAsync(deletedId, softDeleteOnly: false);
            await LoadAllMenuItemsAsync();
            await RefreshLookupsAsync();
            ApplyFilters();

            var replacement = FilteredMenuCards.FirstOrDefault() ?? MenuCards.FirstOrDefault();
            SelectedMenuCard = replacement;
            if (replacement is null)
            {
                IsCreatingNewMenu = false;
                EditingItem = new CreateMenuItemRequest { IsVatIncluded = true, VatRate = 0.08m };
                SelectedPreparationArea = GetDefaultPreparationAreaCode();
                ClearRecipeState();
                MenuHistoryEntries.Clear();
            }

            StatusMessage = $"Đã xử lý xóa menu {deletedName}.";
            StatusIsSuccess = true;
            AddMenuHistory(deletedId, "Xóa menu", $"{deletedCode} - {deletedName}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanDeleteMenu));
            NotifyComputedState();
        }
    }

    [RelayCommand]
    private void AddRecipeLine()
    {
        if (SelectedIngredient is null)
        {
            StatusMessage = "Vui lòng chọn nguyên liệu.";
            StatusIsSuccess = false;
            return;
        }
        if (SelectedWarehouse is null)
        {
            StatusMessage = "Vui lòng chọn kho tiêu hao trước khi thêm nguyên liệu.";
            StatusIsSuccess = false;
            return;
        }
        if (IngredientQuantityInput <= 0)
        {
            StatusMessage = "Số lượng định mức phải lớn hơn 0.";
            StatusIsSuccess = false;
            return;
        }
        if (IngredientLossRateInput < 0 || IngredientLossRateInput > 100)
        {
            StatusMessage = "Tỷ lệ hao hụt phải trong khoảng 0..100%.";
            StatusIsSuccess = false;
            return;
        }
        var lineNo = RecipeLines.Count + 1;
        var line = new RecipeLineEditor
        {
            Id = Guid.Empty, LineNo = lineNo,
            ItemId = SelectedIngredient.ItemId, ItemCode = SelectedIngredient.ItemCode,
            ItemName = SelectedIngredient.ItemName, UnitName = SelectedIngredient.UnitName,
            Quantity = IngredientQuantityInput, LossRatePercent = IngredientLossRateInput,
            DefaultWarehouseId = SelectedWarehouse?.WarehouseId,
            WarehouseDisplay = SelectedWarehouse?.WarehouseName ?? string.Empty,
            IsRequired = true,
            EstimatedUnitCost = SelectedIngredient.ReferenceUnitCost,
            EstimatedLineCost = SelectedIngredient.ReferenceUnitCost * IngredientQuantityInput * (1 + IngredientLossRateInput / 100m)
        };
        RecipeLines.Add(line);
        SelectedRecipeLine = line;
        RefreshInlineRecipeCostPreview();
        NotifyComputedState();
    }

    [RelayCommand]
    private void RemoveRecipeLine(RecipeLineEditor? line)
    {
        if (line is null) return;
        RecipeLines.Remove(line);
        if (SelectedRecipeLine == line)
        {
            SelectedRecipeLine = RecipeLines.FirstOrDefault();
        }
        ReIndexRecipeLines();
        RefreshInlineRecipeCostPreview();
        NotifyComputedState();
    }

    [RelayCommand]
    private async Task LoadSubstitutesAsync()
    {
        SubstituteIngredients.Clear();
        if (SelectedRecipeLine is null)
        {
            NotifySubstituteState();
            return;
        }

        if (SelectedRecipeLine.Id == Guid.Empty)
        {
            StatusMessage = "Hãy lưu định lượng trước khi thêm nguyên liệu thay thế.";
            StatusIsSuccess = false;
            NotifySubstituteState();
            return;
        }

        try
        {
            var rows = await _menuRecipeService.GetSubstitutesAsync(SelectedRecipeLine.Id);
            var rowNo = 1;
            foreach (var row in rows)
            {
                row.RowNo = rowNo++;
                SubstituteIngredients.Add(row);
            }

            RefreshAvailableSubstituteItems();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi tải nguyên liệu thay thế: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally
        {
            NotifySubstituteState();
        }
    }

    [RelayCommand]
    private async Task AddSubstituteAsync()
    {
        if (SelectedRecipeLine is null)
        {
            StatusMessage = "Vui lòng chọn dòng nguyên liệu trong bảng định lượng.";
            StatusIsSuccess = false;
            return;
        }

        if (SelectedRecipeLine.Id == Guid.Empty)
        {
            StatusMessage = "Hãy lưu định lượng trước khi thêm nguyên liệu thay thế.";
            StatusIsSuccess = false;
            return;
        }

        if (SelectedSubstituteItem is null)
        {
            StatusMessage = "Vui lòng chọn nguyên liệu thay thế.";
            StatusIsSuccess = false;
            return;
        }

        if (SubstituteConversionRate <= 0)
        {
            StatusMessage = "Tỷ lệ quy đổi phải lớn hơn 0.";
            StatusIsSuccess = false;
            return;
        }

        if (SubstitutePriority < 1)
        {
            StatusMessage = "Ưu tiên phải lớn hơn hoặc bằng 1.";
            StatusIsSuccess = false;
            return;
        }

        IsBusy = true;
        try
        {
            var request = new CreateSubstituteIngredientRequest
            {
                RecipeLineId = SelectedRecipeLine.Id,
                SubstituteInventoryItemId = SelectedSubstituteItem.ItemId,
                ConversionRate = SubstituteConversionRate,
                Priority = SubstitutePriority,
                AutoReplace = SubstituteAutoReplace,
                Notes = SubstituteNotes,
                UserName = "system"
            };

            await _menuRecipeService.CreateSubstituteAsync(request);
            await LoadSubstitutesAsync();
            ResetSubstituteForm();
            StatusMessage = "Đã thêm nguyên liệu thay thế.";
            StatusIsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally
        {
            IsBusy = false;
            NotifyComputedState();
            NotifySubstituteState();
        }
    }

    [RelayCommand]
    private async Task SaveSubstituteAsync()
    {
        if (SelectedSubstituteIngredient is null)
        {
            await AddSubstituteAsync();
            return;
        }

        IsBusy = true;
        try
        {
            await _menuRecipeService.UpdateSubstituteAsync(new UpdateSubstituteIngredientRequest
            {
                Id = SelectedSubstituteIngredient.Id,
                ConversionRate = SubstituteConversionRate,
                Priority = SubstitutePriority,
                AutoReplace = SubstituteAutoReplace,
                Notes = SubstituteNotes,
                UserName = "system"
            });
            await LoadSubstitutesAsync();
            ResetSubstituteForm();
            StatusMessage = "Đã lưu nguyên liệu thay thế.";
            StatusIsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally
        {
            IsBusy = false;
            NotifyComputedState();
            NotifySubstituteState();
        }
    }

    [RelayCommand]
    private async Task DeleteSubstituteAsync(SubstituteIngredientDto? row)
    {
        if (row is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _menuRecipeService.DeleteSubstituteAsync(row.Id, "system");
            await LoadSubstitutesAsync();
            ResetSubstituteForm();
            StatusMessage = "Đã xóa nguyên liệu thay thế.";
            StatusIsSuccess = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Lỗi: {ex.Message}";
            StatusIsSuccess = false;
        }
        finally
        {
            IsBusy = false;
            NotifyComputedState();
            NotifySubstituteState();
        }
    }

    [RelayCommand]
    private async Task SaveRecipeAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            if (EditingItem.Id is null)
            {
                if (!ValidateEditingItemForSave()) return;

                EditingItem.PreparationArea = SelectedPreparationArea;
                EditingItem.IsActive = IsEditingStatusActive;
                var savedMenu = await _menuCatalogService.CreateOrUpdateMenuItemAsync(EditingItem);
                EditingItem.Id = savedMenu.Id;
                IsCreatingNewMenu = false;
                await RefreshLookupsAsync();
                await LoadAllMenuItemsAsync();
                ApplyFilters();
            }

            var menuItemId = EditingItem.Id.Value;
            var lines = RecipeLines.Select(ToDtoLine).ToList();
            var saved = await _menuRecipeService.SaveRecipeAsync(new SaveMenuRecipeRequest
            {
                RecipeId = EditingRecipeId, MenuItemId = menuItemId,
                Name = EditingRecipeName, IsDefault = EditingRecipeIsDefault, Lines = lines
            });
            EditingRecipeId = saved.Id;
            EditingRecipeName = saved.Name;
            EditingRecipeIsDefault = saved.IsDefault;
            ResetCollection(RecipeLines, saved.Lines.Select(ToEditor));
            await AutoRefreshSelectedCostAsync(menuItemId, updateRecipeLinesFromPreview: true);
            UpdateMenuCardRecipeState(menuItemId, saved.Lines.Count > 0);
            ApplyFilters();
            LoadCategoryDetailItems(EditingItem.MenuCategoryId);
            StatusMessage = $"Đã lưu định lượng và tự tính giá vốn {PreviewCost:N0} đ ({saved.Lines.Count:N0} nguyên liệu)."; StatusIsSuccess = true;
            AddMenuHistory(menuItemId, "Lưu định lượng", $"{saved.Lines.Count:N0} nguyên liệu, giá vốn {PreviewCost:N0} đ");
        }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; StatusIsSuccess = false; }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    [RelayCommand]
    private async Task CalculateCostAsync()
    {
        if (EditingItem.Id is null) return;
        IsBusy = true;
        try
        {
            var preview = await _menuRecipeService.PreviewCostAsync(EditingItem.Id.Value);
            PreviewCost = preview.EstimatedCost; PreviewMarginAmount = preview.MarginAmount;
            PreviewMarginRate = preview.MarginRate; ResetCollection(Warnings, preview.Warnings);
            AddMenuHistory(EditingItem.Id.Value, "Tính giá vốn", $"Giá vốn {preview.EstimatedCost:N0} đ");
        }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; StatusIsSuccess = false; }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    [RelayCommand]
    private async Task SaveAndCalculateCostAsync()
    {
        if (IsBusy) return;
        if (!ValidateEditingItemForSave()) return;
        IsBusy = true;
        try
        {
            var saved = await SaveMenuItemCoreAsync("Đã lưu món, đang tính giá vốn.", recordHistory: false);
            var preview = await _menuRecipeService.PreviewCostAsync(saved.Id);
            PreviewCost = preview.EstimatedCost;
            PreviewMarginAmount = preview.MarginAmount;
            PreviewMarginRate = preview.MarginRate;
            ResetCollection(Warnings, preview.Warnings);
            StatusMessage = "Đã lưu món và tính giá vốn.";
            StatusIsSuccess = true;
            AddMenuHistory(saved.Id, "Lưu & tính giá vốn", $"Giá vốn {preview.EstimatedCost:N0} đ");
        }
        catch (Exception ex) { StatusMessage = $"Lỗi: {ex.Message}"; StatusIsSuccess = false; }
        finally { IsBusy = false; NotifyComputedState(); }
    }

    [RelayCommand]
    private void MoveRecipeLineUp(RecipeLineEditor? line)
    {
        if (line is null) return;
        var idx = RecipeLines.IndexOf(line);
        if (idx <= 0) return;
        RecipeLines.Move(idx, idx - 1);
        ReIndexRecipeLines(); NotifyComputedState();
    }

    [RelayCommand]
    private void MoveRecipeLineDown(RecipeLineEditor? line)
    {
        if (line is null) return;
        var idx = RecipeLines.IndexOf(line);
        if (idx < 0 || idx >= RecipeLines.Count - 1) return;
        RecipeLines.Move(idx, idx + 1);
        ReIndexRecipeLines(); NotifyComputedState();
    }

    [RelayCommand]
    private void SelectFilterChip(string chip) { ActiveFilterChip = chip; ApplyFilters(); }

    // ===================================================================
    // CATEGORY → MENU ITEM NAVIGATION
    // ===================================================================
    [RelayCommand]
    private async Task SelectCategoryMenuItemAsync(MenuItemCardState? item)
    {
        if (item is null) return;
        SelectedCategoryMenuItem = item;
        SelectedCategoryRecipeLines.Clear();
        try
        {
            var recipe = await _menuRecipeService.GetRecipeAsync(item.Id);
            if (recipe is not null && recipe.Lines.Count > 0)
            {
                foreach (var line in recipe.Lines)
                {
                    SelectedCategoryRecipeLines.Add(new RecipeLineEditor
                    {
                        Id = line.Id, LineNo = line.LineNo, ItemId = line.ItemId,
                        ItemCode = line.ItemCode, ItemName = line.ItemName, UnitName = line.UnitName,
                        Quantity = line.Quantity, LossRatePercent = line.LossRate * 100m,
                        DefaultWarehouseId = line.DefaultWarehouseId,
                        WarehouseDisplay = line.DefaultWarehouseId?.ToString() ?? string.Empty,
                        IsRequired = line.IsRequired, Note = line.Note,
                        EstimatedUnitCost = line.EstimatedUnitCost, EstimatedLineCost = line.EstimatedLineCost
                    });
                }
            }
        }
        catch { }
        OnPropertyChanged(nameof(HasSelectedCategoryRecipeLines));
    }

    [RelayCommand]
    private void GoToMenuItem(MenuItemCardState? item)
    {
        if (item is null) return;
        SelectedSection = SEC_MENU_ITEMS;
        // Set the selected menu card to trigger loading
        SelectMenuCardById(item.Id);
    }

    [RelayCommand]
    private void GoToMenuItemRecipe(MenuItemCardState? item)
    {
        if (item is null) return;
        SelectedSection = SEC_MENU_ITEMS;
        SelectedWorkspaceTab = 1; // Tab index 1 = Định lượng
        SelectMenuCardById(item.Id);
    }

    // ===================================================================
    // EXPAND/COLLAPSE MENU ITEM TO SHOW RECIPE LINES
    // ===================================================================
    [RelayCommand]
    private async Task ToggleItemExpandAsync(MenuItemCardState? item)
    {
        if (item is null) return;
        item.IsExpanded = !item.IsExpanded;
        if (item.IsExpanded && item.RecipeLines.Count == 0)
        {
            try
            {
                var recipe = await _menuRecipeService.GetRecipeAsync(item.Id);
                if (recipe is not null)
                {
                    foreach (var line in recipe.Lines)
                    {
                        item.RecipeLines.Add(new RecipeLineEditor
                        {
                            Id = line.Id, LineNo = line.LineNo, ItemId = line.ItemId,
                            ItemCode = line.ItemCode, ItemName = line.ItemName, UnitName = line.UnitName,
                            Quantity = line.Quantity, LossRatePercent = line.LossRate * 100m,
                            DefaultWarehouseId = line.DefaultWarehouseId,
                            WarehouseDisplay = line.DefaultWarehouseId?.ToString() ?? string.Empty,
                            IsRequired = line.IsRequired, Note = line.Note,
                            EstimatedUnitCost = line.EstimatedUnitCost, EstimatedLineCost = line.EstimatedLineCost
                        });
                    }
                }
                else
                {
                    item.RecipeLines.Add(new RecipeLineEditor { ItemName = "Ch&#x1B0;a c&#xF3; &#x111;&#x1ECB;nh l&#x1B0;&#x1EE3;ng", LineNo = 0 });
                }
            }
            catch { item.RecipeLines.Add(new RecipeLineEditor { ItemName = "L&#x1ED7;i t&#x1EA3;i d&#x1EEF; li&#x1EC7;u", LineNo = 0 }); }
        }
        // Refresh the list so the expanded state shows
        var idx = FilteredMenuCards.IndexOf(item);
        if (idx >= 0)
        {
            FilteredMenuCards.RemoveAt(idx);
            FilteredMenuCards.Insert(idx, item);
        }
    }

    // ===================================================================
    // PARTIAL CHANGE HANDLERS
    // ===================================================================
    partial void OnSearchTextChanged(string value) => ApplyFilters();
    partial void OnCategorySearchTextChanged(string value) => ApplyCategoryFilter();
    partial void OnFilterCategoryIdChanged(Guid? value) => ApplyFilters();
    partial void OnEditingCategoryChanged(MenuCategoryCardState? value)
    {
        RefreshEditCategoryParentOptions();
        OnPropertyChanged(nameof(CanDeleteCategory));
        OnPropertyChanged(nameof(IsRootCategorySelected));
        OnPropertyChanged(nameof(IsChildCategorySelected));
        OnPropertyChanged(nameof(CanManageCategoryMenuItems));
        if (_isLoadingCategorySelection) return;
        _ = LoadEditingCategoryAsync(value);
    }

    partial void OnSelectedCategoryMenuItemChanged(MenuItemCardState? value)
    {
        if (value is null)
        {
            SelectedCategoryRecipeLines.Clear();
            OnPropertyChanged(nameof(HasSelectedCategoryRecipeLines));
            return;
        }
        _ = SelectCategoryMenuItemAsync(value);
    }

    partial void OnSelectedCategoryCardChanged(MenuCategoryCardState? value)
    {
        FilterCategoryId = value?.Id;
        if (value is null) ActiveFilterChip = "Tất cả";
        OnPropertyChanged(nameof(SelectedCategoryCard)); OnPropertyChanged(nameof(HeaderMetaText));
    }

    partial void OnSelectedMenuCatalogTabChanged(int value)
    {
        if (value < 0 || value > 1)
        {
            SelectedMenuCatalogTab = 0;
            return;
        }

        FilterCategoryId = null;
        ActiveFilterChip = "Tất cả";
        SelectedMenuCard = null;
        IsCreatingNewMenu = false;
        EditingItem = new CreateMenuItemRequest { IsVatIncluded = true, VatRate = 0.08m };
        EditingService = new SaveRestaurantExtraServiceRequest { IsVatIncluded = true };
        SelectedServiceTypeFilter = "All";
        SelectedWorkspaceTab = 0;
        ClearRecipeState();
        ResetCollection(FilterChips, IsServiceCatalogSelected ? ServiceFilterChips : FoodFilterChips);
        BuildServiceTypeOptions();
        ApplyCategoryFilter();
        RefreshCategoryParentOptions();
        RefreshEditCategoryParentOptions();
        if (EditingCategory is not null && FilteredCategoryCards.All(x => x.Id != EditingCategory.Id))
        {
            EditingCategory = FilteredCategoryCards.FirstOrDefault();
        }
        OnPropertyChanged(nameof(IsFoodCatalogSelected));
        OnPropertyChanged(nameof(IsServiceCatalogSelected));
        OnPropertyChanged(nameof(CurrentMenuCatalogTitle));
        OnPropertyChanged(nameof(CurrentCategoryCatalogTitle));
        OnPropertyChanged(nameof(CreateCategoryButtonText));
        OnPropertyChanged(nameof(CreateMenuButtonText));
        OnPropertyChanged(nameof(GeneralInfoSectionTitle));
        OnPropertyChanged(nameof(CategoryLabelText));
        OnPropertyChanged(nameof(ShowFoodCatalogFilters));
        OnPropertyChanged(nameof(ShowServiceTypeFilter));
        OnPropertyChanged(nameof(ShowFoodWorkspace));
        OnPropertyChanged(nameof(ShowServiceWorkspace));
        OnPropertyChanged(nameof(ShowFoodSidebarState));
        OnPropertyChanged(nameof(ShowServiceSidebarState));
        OnPropertyChanged(nameof(ShowNoFoodCategoryState));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
        OnPropertyChanged(nameof(EmptyStateCreateButtonText));
        OnPropertyChanged(nameof(NoItemsFoundText));
        OnPropertyChanged(nameof(CurrentActionButtonText));
        OnPropertyChanged(nameof(CurrentDeleteButtonText));
        OnPropertyChanged(nameof(CurrentDuplicateButtonText));
        OnPropertyChanged(nameof(CurrentActiveStatusButtonText));
        OnPropertyChanged(nameof(CurrentStatusPillText));
        OnPropertyChanged(nameof(SelectedServiceTypeDisplay));
        OnPropertyChanged(nameof(SelectedServicePriceDisplay));
        OnPropertyChanged(nameof(LastUpdatedDisplay));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(CategoryEmptySelectionTitle));
        OnPropertyChanged(nameof(CategoryEmptySelectionDescription));
        OnPropertyChanged(nameof(IsServiceCategorySelected));
        OnPropertyChanged(nameof(CanManageCategoryMenuItems));
        OnPropertyChanged(nameof(ShowCategoryKpis));
        OnPropertyChanged(nameof(ShowCategoryItemsPanel));
        _ = InitializeAsync();
    }

    partial void OnIsBusyChanged(bool value) { NotifyComputedState(); OnPropertyChanged(nameof(CanSavePreparationArea)); OnPropertyChanged(nameof(CanDeleteMenu)); OnPropertyChanged(nameof(CanDeleteCategory)); OnPropertyChanged(nameof(CanManageCategoryMenuItems)); }
    partial void OnCanEditChanged(bool value) { NotifyComputedState(); OnPropertyChanged(nameof(CanSavePreparationArea)); }
    partial void OnIsSchemaMissingChanged(bool value) => NotifyComputedState();
    partial void OnNewCategoryNameChanged(string value) { if (string.IsNullOrWhiteSpace(NewCategoryCode)) OnPropertyChanged(nameof(CanSaveCategory)); }
    partial void OnNewCategoryCodeChanged(string value) => OnPropertyChanged(nameof(CanSaveCategory));
    partial void OnSelectedServiceTypeFilterChanged(string? value) => ApplyFilters();

    // ===================================================================
    // INITIALIZATION & HELPERS
    // ===================================================================
    private async Task InitializeAsync()
    {
        await _dbLock.WaitAsync();
        try
        {
            IsBusy = true;
            if (IsServiceCatalogSelected)
            {
                await LoadAllExtraServicesAsync();
            }
            else
            {
                await LoadAllMenuItemsAsync();
            }
            await RefreshLookupsAsync();
            ApplyFilters();
        }
        finally { IsBusy = false; _dbLock.Release(); NotifyComputedState(); }
    }

    private async Task RefreshLookupsAsync()
    {
        var currentEditingCategoryId = EditingCategory?.Id;
        var categories = await _menuCatalogService.GetCategoriesAsync();
        ResetCollection(Categories, categories);
        var levelLookup = BuildCategoryLevelLookup(categories);
        var serviceLookup = BuildServiceCategoryLookup(categories);
        var cards = categories.Select(c => new MenuCategoryCardState
        {
            Id = c.Id,
            ParentCategoryId = c.ParentCategoryId,
            ParentCategoryName = c.ParentCategoryName,
            Level = levelLookup.TryGetValue(c.Id, out var level) ? level : 0,
            Code = c.Code,
            Name = c.Name,
            ItemCount = _allMenuItems.Count(x => x.MenuCategoryId == c.Id),
            IsServiceCategory = serviceLookup.TryGetValue(c.Id, out var isServiceCategory) && isServiceCategory
        }).ToList();

        if (_allExtraServices.Count > 0)
        {
            var serviceGroups = _allExtraServices
                .GroupBy(x => string.IsNullOrWhiteSpace(x.ServiceType) ? "Other" : x.ServiceType)
                .Select(g => new MenuCategoryCardState
                {
                    Id = BuildStableServiceGroupId(g.Key),
                    Code = g.Key,
                    Name = GetServiceTypeDisplay(g.Key),
                    ItemCount = g.Count(),
                    IsServiceCategory = true,
                    IsVirtual = true,
                    ServiceTypeCode = g.Key
                });

            cards = cards
                .Where(x => !(x.IsServiceCategory && x.IsVirtual))
                .Concat(serviceGroups)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();
        }

        ResetCollection(CategoryCards, cards);
        ApplyCategoryFilter();
        RefreshCategoryParentOptions();
        RefreshEditCategoryParentOptions();
        if (!IsServiceCatalogSelected && _allMenuItems.Count > 0)
        {
            BuildMenuCards();
        }
        RefreshVisibleMenuCategories();
        if (currentEditingCategoryId.HasValue)
        {
            EditingCategory = cards.FirstOrDefault(x => x.Id == currentEditingCategoryId.Value);
        }
        if (EditingCategory is not null && FilteredCategoryCards.All(x => x.Id != EditingCategory.Id))
        {
            EditingCategory = FilteredCategoryCards.FirstOrDefault();
        }
        try
        {
            var lookup = await _menuCatalogService.GetIngredientsLookupAsync();
            ResetCollection(IngredientLookup, lookup.Items);
            ResetCollection(WarehouseLookup, lookup.Warehouses);
            SelectedWarehouse = WarehouseLookup.FirstOrDefault();
            ApplyIngredientFilter();
            RefreshAvailableSubstituteItems();
            OnPropertyChanged(nameof(HasIngredients)); OnPropertyChanged(nameof(NoIngredients));
            if (WarehouseLookup.Count == 0)
            {
                StatusMessage = "Chưa có kho tiêu hao đang hoạt động. Vui lòng cấu hình kho tiêu hao trước khi lập định lượng.";
                StatusIsSuccess = false;
            }
        }
        catch { }
    }

    private async Task LoadAllMenuItemsAsync()
    {
        var items = await _menuCatalogService.GetMenuItemsAsync(includeInactive: true);
        _allMenuItems.Clear(); _allMenuItems.AddRange(items);
        BuildMenuCards();
    }

    private async Task LoadRecipeAsync(Guid menuItemId, int? selectionLoadVersion = null)
    {
        try
        {
            var recipe = await _menuRecipeService.GetRecipeAsync(menuItemId);
            if (selectionLoadVersion.HasValue
                && selectionLoadVersion.Value != Volatile.Read(ref _menuCardLoadVersion))
            {
                return;
            }

            if (recipe is not null)
            {
                EditingRecipeId = recipe.Id; EditingRecipeName = recipe.Name;
                EditingRecipeIsDefault = recipe.IsDefault;
                ResetCollection(RecipeLines, recipe.Lines.Select(ToEditor));
                SelectedRecipeLine = RecipeLines.FirstOrDefault();
                UpdateMenuCardRecipeState(menuItemId, recipe.Lines.Count > 0);
                await AutoRefreshSelectedCostAsync(
                    menuItemId,
                    updateRecipeLinesFromPreview: true,
                    selectionLoadVersion);
            }
            else ClearRecipeState();
        }
        catch
        {
            if (!selectionLoadVersion.HasValue
                || selectionLoadVersion.Value == Volatile.Read(ref _menuCardLoadVersion))
            {
                ClearRecipeState();
            }
        }
    }

    private async Task AutoRefreshSelectedCostAsync(
        Guid menuItemId,
        bool updateRecipeLinesFromPreview,
        int? selectionLoadVersion = null)
    {
        var preview = await _menuRecipeService.PreviewCostAsync(menuItemId);
        if (selectionLoadVersion.HasValue
            && selectionLoadVersion.Value != Volatile.Read(ref _menuCardLoadVersion))
        {
            return;
        }

        PreviewCost = preview.EstimatedCost;
        PreviewMarginAmount = preview.MarginAmount;
        PreviewMarginRate = preview.MarginRate;
        ResetCollection(Warnings, preview.Warnings);

        if (updateRecipeLinesFromPreview)
        {
            ApplyPreviewToRecipeLines(preview);
        }

        NotifyComputedState();
    }

    private void ApplyPreviewToRecipeLines(MenuCostPreviewDto preview)
    {
        foreach (var previewLine in preview.Lines)
        {
            var editor = RecipeLines.FirstOrDefault(x => x.LineNo == previewLine.LineNo)
                ?? RecipeLines.FirstOrDefault(x => x.ItemId == previewLine.ItemId && x.Quantity == previewLine.Quantity);
            if (editor is null) continue;

            editor.EstimatedUnitCost = previewLine.UnitCost;
            editor.EstimatedLineCost = previewLine.LineCost;
            editor.AvailableQuantity = previewLine.AvailableQuantity;
        }
    }

    private void RefreshCategoryParentOptions()
    {
        var options = new List<MenuCategoryParentOption>
        {
            new() { Label = "Nhóm gốc" }
        };

        options.AddRange(CategoryCards
            .Where(c => c.IsServiceCategory == IsServiceCatalogSelected && !c.IsVirtual)
            .Select(c => new MenuCategoryParentOption
        {
            Id = c.Id,
            Label = BuildCategoryOptionLabel(c, CategoryCards)
        }));

        ResetCollection(NewCategoryParentOptions, options);
    }

    private void RefreshEditCategoryParentOptions()
    {
        var options = new List<MenuCategoryParentOption>
        {
            new() { Label = "Nhóm gốc" }
        };

        options.AddRange(CategoryCards
            .Where(c => c.Id != EditingCategory?.Id && c.IsServiceCategory == IsServiceCatalogSelected && !c.IsVirtual)
            .Select(c => new MenuCategoryParentOption
            {
                Id = c.Id,
                Label = BuildCategoryOptionLabel(c, CategoryCards)
            }));

        ResetCollection(EditCategoryParentOptions, options);
    }

    private static Dictionary<Guid, int> BuildCategoryLevelLookup(IEnumerable<MenuCategoryDto> categories)
    {
        var all = categories.ToDictionary(x => x.Id);
        var levels = new Dictionary<Guid, int>();

        foreach (var category in all.Values)
        {
            var level = 0;
            var parentId = category.ParentCategoryId;
            while (parentId.HasValue && all.TryGetValue(parentId.Value, out var parent))
            {
                level++;
                parentId = parent.ParentCategoryId;
            }

            levels[category.Id] = level;
        }

        return levels;
    }

    private static string BuildCategoryOptionLabel(MenuCategoryDto category, IEnumerable<MenuCategoryDto> categories)
    {
        var levels = BuildCategoryLevelLookup(categories);
        var prefix = levels.TryGetValue(category.Id, out var level) && level > 0
            ? string.Concat(Enumerable.Repeat("  ", level)) + "↳ "
            : string.Empty;
        return prefix + category.Name;
    }

    private static string BuildCategoryOptionLabel(MenuCategoryCardState category, IEnumerable<MenuCategoryCardState> categories)
    {
        var all = categories.ToDictionary(x => x.Id);
        var level = 0;
        var parentId = category.ParentCategoryId;
        while (parentId.HasValue && all.TryGetValue(parentId.Value, out var parent))
        {
            level++;
            parentId = parent.ParentCategoryId;
        }

        var prefix = level > 0 ? string.Concat(Enumerable.Repeat("  ", level)) + "↳ " : string.Empty;
        return prefix + category.Name;
    }

    private static Dictionary<Guid, bool> BuildServiceCategoryLookup(IEnumerable<MenuCategoryDto> categories)
    {
        var all = categories.ToDictionary(x => x.Id);
        var lookup = new Dictionary<Guid, bool>();

        foreach (var category in all.Values)
        {
            lookup[category.Id] = IsServiceCategory(category, all, lookup);
        }

        return lookup;
    }

    private static bool IsServiceCategory(
        MenuCategoryDto category,
        IReadOnlyDictionary<Guid, MenuCategoryDto> all,
        IDictionary<Guid, bool> lookup)
    {
        if (lookup.TryGetValue(category.Id, out var cached))
        {
            return cached;
        }

        if (IsServiceCategoryByNameOrCode(category.Code, category.Name))
        {
            return true;
        }

        if (category.ParentCategoryId.HasValue
            && all.TryGetValue(category.ParentCategoryId.Value, out var parent))
        {
            return IsServiceCategory(parent, all, lookup);
        }

        return false;
    }

    private static bool IsServiceCategoryByNameOrCode(string? code, string? name)
    {
        return (!string.IsNullOrWhiteSpace(code) && code.Contains("service", StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrWhiteSpace(name)
                && (name.Contains("dịch vụ", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("dich vu", StringComparison.OrdinalIgnoreCase)));
    }

    private void RefreshInlineRecipeCostPreview()
    {
        var total = RecipeLines.Sum(x => x.EstimatedLineCost);
        PreviewCost = total;
        PreviewMarginAmount = EditingItem.UnitPrice - total;
        PreviewMarginRate = EditingItem.UnitPrice > 0 ? PreviewMarginAmount / EditingItem.UnitPrice : 0m;
    }

    private void RecipeLines_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (RecipeLineEditor line in e.OldItems)
            {
                line.PropertyChanged -= RecipeLine_PropertyChanged;
            }
        }

        if (e.NewItems is not null)
        {
            foreach (RecipeLineEditor line in e.NewItems)
            {
                line.PropertyChanged += RecipeLine_PropertyChanged;
            }
        }
    }

    private void RecipeLine_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(RecipeLineEditor.EstimatedLineCost)
            or nameof(RecipeLineEditor.EstimatedUnitCost)
            or nameof(RecipeLineEditor.Quantity)
            or nameof(RecipeLineEditor.LossRatePercent))
        {
            RefreshInlineRecipeCostPreview();
            NotifyComputedState();
        }
    }

    private void ClearRecipeState()
    {
        EditingRecipeId = null; EditingRecipeName = "Default"; EditingRecipeIsDefault = true;
        RecipeLines.Clear(); Warnings.Clear();
        SelectedRecipeLine = null;
        SubstituteIngredients.Clear();
        AvailableSubstituteItems.Clear();
        ResetSubstituteForm();
        PreviewCost = 0; PreviewMarginAmount = 0; PreviewMarginRate = 0;
        NotifySubstituteState();
    }

    private void RefreshAvailableSubstituteItems()
    {
        var currentSelectedId = SelectedSubstituteItem?.ItemId;
        var selectedItemId = SelectedRecipeLine?.ItemId;
        var usedIds = SubstituteIngredients.Select(x => x.SubstituteInventoryItemId).ToHashSet();
        var filtered = IngredientLookup.AsEnumerable();

        if (selectedItemId.HasValue)
        {
            filtered = filtered.Where(x => x.ItemId != selectedItemId.Value);
        }

        if (SelectedSubstituteIngredient is null)
        {
            filtered = filtered.Where(x => !usedIds.Contains(x.ItemId));
        }

        if (!string.IsNullOrWhiteSpace(SubstituteSearchText))
        {
            var term = SubstituteSearchText.Trim();
            filtered = filtered.Where(x =>
                x.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || x.ItemCode.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var rows = filtered.OrderBy(x => x.ItemName).ToList();
        ResetCollection(AvailableSubstituteItems, rows);

        if (currentSelectedId.HasValue)
        {
            SelectedSubstituteItem = rows.FirstOrDefault(x => x.ItemId == currentSelectedId.Value);
        }
    }

    private void ResetSubstituteForm()
    {
        SelectedSubstituteIngredient = null;
        SelectedSubstituteItem = null;
        SubstituteConversionRate = 1m;
        SubstitutePriority = 1;
        SubstituteAutoReplace = true;
        SubstituteNotes = string.Empty;
        SubstituteSearchText = string.Empty;
        RefreshAvailableSubstituteItems();
    }

    private void NotifySubstituteState()
    {
        OnPropertyChanged(nameof(HasSelectedRecipeLine));
        OnPropertyChanged(nameof(NoSelectedRecipeLine));
        OnPropertyChanged(nameof(HasSubstituteIngredients));
        OnPropertyChanged(nameof(NoSubstituteIngredients));
        OnPropertyChanged(nameof(CanAddSubstitute));
    }

    private void BuildMenuCards()
    {
        var serviceCategoryIds = CategoryCards
            .Where(x => x.IsServiceCategory)
            .Select(x => x.Id)
            .ToHashSet();

        MenuCards.Clear();
        _itemsWithRecipe.Clear();
        foreach (var item in _allMenuItems)
        {
            var hasRecipe = item.HasRecipe;
            var isLegacyServiceItem = serviceCategoryIds.Contains(item.MenuCategoryId);
            if (hasRecipe) _itemsWithRecipe.Add(item.Id);
            MenuCards.Add(new MenuItemCardState
            {
                Id = item.Id, Name = item.Name, Code = item.Code,
                CategoryId = item.MenuCategoryId,
                CategoryName = item.CategoryName,
                UnitPriceDisplay = $"{item.UnitPrice:N0} đ",
                UnitPriceNumeric = item.UnitPrice,
                RecipeStatusBadge = hasRecipe ? "Có định mức" : "Chưa định mức",
                ActiveStatusBadge = item.IsActive ? "Đang bán" : "Ngưng bán",
                HasRecipe = hasRecipe, IsInactive = !item.IsActive,
                ImagePath = item.ImagePath,
                IsHotelService = item.IsHotelService || isLegacyServiceItem
            });
        }
        RefreshVisibleMenuCategories();
        ApplyFilters();
    }

    public void ApplyFilters()
    {
        var filtered = MenuCards.AsEnumerable();
        if (IsServiceCatalogSelected)
        {
            filtered = filtered.Where(x => x.IsHotelService);
        }
        else
        {
            filtered = filtered.Where(x => !x.IsHotelService);
        }
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var term = SearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(x => x.Name.ToLowerInvariant().Contains(term) || x.Code.ToLowerInvariant().Contains(term));
        }
        if (FilterCategoryId.HasValue && FilterCategoryId.Value != Guid.Empty)
        {
            filtered = filtered.Where(x => x.CategoryId == FilterCategoryId.Value);
        }
        if (IsServiceCatalogSelected && !string.IsNullOrWhiteSpace(SelectedServiceTypeFilter) && !string.Equals(SelectedServiceTypeFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(x => string.Equals(x.ServiceType, SelectedServiceTypeFilter, StringComparison.OrdinalIgnoreCase));
        }
        filtered = ActiveFilterChip switch
        {
            "Có định mức" => filtered.Where(x => x.HasRecipe),
            "Chưa định mức" => filtered.Where(x => !x.HasRecipe),
            "Đang bán" => filtered.Where(x => !x.IsInactive),
            "Ngưng bán" => filtered.Where(x => x.IsInactive),
            _ => filtered
        };
        // Apply sorting
        filtered = SelectedSortOption switch
        {
            "Tên Z-A" => filtered.OrderByDescending(x => x.Name),
            "Giá thấp-cao" => filtered.OrderBy(x => x.UnitPriceNumeric),
            "Giá cao-thấp" => filtered.OrderByDescending(x => x.UnitPriceNumeric),
            "Chưa định mức trước" => filtered.OrderBy(x => x.HasRecipe ? 1 : 0).ThenBy(x => x.Name),
            _ => filtered.OrderBy(x => x.Name) // Tên A-Z (default)
        };
        ResetCollection(FilteredMenuCards, filtered.ToList());
        OnPropertyChanged(nameof(HasMenuItems)); OnPropertyChanged(nameof(NoMenuItems));
        OnPropertyChanged(nameof(LastUpdatedDisplay));
    }

    private void RefreshVisibleMenuCategories()
    {
        if (IsServiceCatalogSelected)
        {
            VisibleMenuCategories.Clear();
            return;
        }

        if (Categories.Count == 0)
        {
            VisibleMenuCategories.Clear();
            return;
        }

        var matchingCategoryIds = _allMenuItems
            .Where(x => IsServiceCatalogSelected ? x.IsHotelService : !x.IsHotelService)
            .Select(x => x.MenuCategoryId)
            .ToHashSet();

        if (EditingItem.MenuCategoryId != Guid.Empty)
        {
            matchingCategoryIds.Add(EditingItem.MenuCategoryId);
        }

        var fallbackCategories = Categories.Where(c =>
            IsServiceCatalogSelected
                ? c.Name.Contains("dịch vụ", StringComparison.OrdinalIgnoreCase)
                  || c.Name.Contains("dich vu", StringComparison.OrdinalIgnoreCase)
                  || c.Code.Contains("service", StringComparison.OrdinalIgnoreCase)
                : !c.Name.Contains("dịch vụ", StringComparison.OrdinalIgnoreCase)
                  && !c.Name.Contains("dich vu", StringComparison.OrdinalIgnoreCase)
                  && !c.Code.Contains("service", StringComparison.OrdinalIgnoreCase));

        var visible = Categories
            .Where(c => matchingCategoryIds.Contains(c.Id))
            .Concat(fallbackCategories)
            .GroupBy(c => c.Id)
            .Select(g => g.First())
            .ToList();

        ResetCollection(VisibleMenuCategories, visible);

        if (FilterCategoryId.HasValue && VisibleMenuCategories.All(x => x.Id != FilterCategoryId.Value))
        {
            FilterCategoryId = null;
        }

        if (EditingItem.MenuCategoryId != Guid.Empty && VisibleMenuCategories.All(x => x.Id != EditingItem.MenuCategoryId))
        {
            EditingItem.MenuCategoryId = Guid.Empty;
        }
    }

    private void AssignDefaultCategoryForCurrentCatalog()
    {
        if (IsServiceCatalogSelected)
        {
            return;
        }

        RefreshVisibleMenuCategories();
        if (EditingItem.MenuCategoryId != Guid.Empty)
        {
            return;
        }

        var defaultCategory = VisibleMenuCategories.FirstOrDefault()
            ?? Categories.FirstOrDefault(c => string.Equals(c.Code, "hotel-service", StringComparison.OrdinalIgnoreCase))
            ?? Categories.FirstOrDefault();

        if (defaultCategory is not null)
        {
            EditingItem.MenuCategoryId = defaultCategory.Id;
        }
    }

    private Guid? GetDefaultServiceParentCategoryId()
    {
        return CategoryCards
            .Where(x => x.IsServiceCategory && !x.ParentCategoryId.HasValue)
            .OrderBy(x => x.Name)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefault();
    }

    partial void OnSelectedSortOptionChanged(string value) => ApplyFilters();
    partial void OnSelectedMenuCategoryFilterChanged(Guid? value)
    {
        FilterCategoryId = value;
        ApplyFilters();
    }

    private void SelectMenuCardById(Guid menuItemId)
    {
        var card = FilteredMenuCards.FirstOrDefault(x => x.Id == menuItemId)
            ?? MenuCards.FirstOrDefault(x => x.Id == menuItemId);
        if (card is not null)
        {
            SelectedMenuCard = card;
        }
    }

    private void UpdateMenuCardRecipeState(Guid menuItemId, bool hasRecipe)
    {
        if (hasRecipe) _itemsWithRecipe.Add(menuItemId);
        else _itemsWithRecipe.Remove(menuItemId);

        UpdateRecipeState(MenuCards.FirstOrDefault(x => x.Id == menuItemId), hasRecipe);
        UpdateRecipeState(FilteredMenuCards.FirstOrDefault(x => x.Id == menuItemId), hasRecipe);
        UpdateRecipeState(CategoryDetailItems.FirstOrDefault(x => x.Id == menuItemId), hasRecipe);
        NotifyComputedState();
    }

    private static void UpdateRecipeState(MenuItemCardState? card, bool hasRecipe)
    {
        if (card is null) return;
        card.HasRecipe = hasRecipe;
        card.RecipeStatusBadge = hasRecipe ? "Có định mức" : "Chưa định mức";
    }

    private void ReIndexRecipeLines() { for (int i = 0; i < RecipeLines.Count; i++) RecipeLines[i].LineNo = i + 1; }

    private void AddMenuHistory(Guid? menuItemId, string action, string detail)
    {
        if (menuItemId is null) return;

        var entry = new MenuHistoryEntry
        {
            MenuItemId = menuItemId.Value,
            OccurredAt = DateTime.Now,
            Action = action,
            Detail = detail,
            UserName = "system"
        };

        if (!_menuHistoryByItem.TryGetValue(menuItemId.Value, out var entries))
        {
            entries = [];
            _menuHistoryByItem[menuItemId.Value] = entries;
        }

        entries.Insert(0, entry);
        if (entries.Count > 100) entries.RemoveAt(entries.Count - 1);

        if (EditingItem.Id == menuItemId)
        {
            MenuHistoryEntries.Insert(0, entry);
            if (MenuHistoryEntries.Count > 100) MenuHistoryEntries.RemoveAt(MenuHistoryEntries.Count - 1);
            OnPropertyChanged(nameof(HasMenuHistory));
            OnPropertyChanged(nameof(NoMenuHistory));
        }
    }

    private void LoadMenuHistory(Guid menuItemId)
    {
        if (_menuHistoryByItem.TryGetValue(menuItemId, out var entries))
        {
            ResetCollection(MenuHistoryEntries, entries);
        }
        else
        {
            MenuHistoryEntries.Clear();
        }

        OnPropertyChanged(nameof(HasMenuHistory));
        OnPropertyChanged(nameof(NoMenuHistory));
    }

    private void ApplyCategoryFilter()
    {
        var filtered = CategoryCards
            .Where(c => c.IsServiceCategory == IsServiceCatalogSelected)
            .AsEnumerable();
        if (!string.IsNullOrWhiteSpace(CategorySearchText))
        {
            var term = CategorySearchText.Trim().ToLowerInvariant();
            filtered = filtered.Where(c =>
                c.Name.ToLowerInvariant().Contains(term)
                || c.Code.ToLowerInvariant().Contains(term));
        }
        ResetCollection(FilteredCategoryCards, filtered.ToList());
    }

    private static RecipeLineEditor ToEditor(MenuRecipeLineDto line) => new()
    {
        Id = line.Id, LineNo = line.LineNo, ItemId = line.ItemId,
        ItemCode = line.ItemCode, ItemName = line.ItemName, UnitName = line.UnitName,
        Quantity = line.Quantity, LossRatePercent = line.LossRate * 100m,
        DefaultWarehouseId = line.DefaultWarehouseId, WarehouseDisplay = line.DefaultWarehouseId?.ToString() ?? string.Empty,
        IsRequired = line.IsRequired, EstimatedUnitCost = line.EstimatedUnitCost,
        EstimatedLineCost = line.EstimatedLineCost, AvailableQuantity = line.AvailableQuantity, Note = line.Note
    };

    private static MenuRecipeLineDto ToDtoLine(RecipeLineEditor line) => new()
    {
        Id = line.Id, LineNo = line.LineNo, ItemId = line.ItemId,
        ItemCode = line.ItemCode, ItemName = line.ItemName, UnitName = line.UnitName,
        Quantity = line.Quantity, LossRate = line.LossRatePercent / 100m,
        DefaultWarehouseId = line.DefaultWarehouseId, IsRequired = line.IsRequired, Note = line.Note
    };

    private void NotifyComputedState()
    {
        OnPropertyChanged(nameof(TotalMenuCount)); OnPropertyChanged(nameof(HasRecipeCount));
        OnPropertyChanged(nameof(NoRecipeCount)); OnPropertyChanged(nameof(CostWarningCount));
        OnPropertyChanged(nameof(HasMenuItems)); OnPropertyChanged(nameof(NoMenuItems));
        OnPropertyChanged(nameof(NoCategories)); OnPropertyChanged(nameof(HasCategories));
        OnPropertyChanged(nameof(IsFirstRun)); OnPropertyChanged(nameof(CanCreateMenu));
        OnPropertyChanged(nameof(ShowAddMenuButton)); OnPropertyChanged(nameof(ShowBootstrapCategoriesButton));
        OnPropertyChanged(nameof(CanCreateCategory)); OnPropertyChanged(nameof(CanSaveCategory));
        OnPropertyChanged(nameof(HasSelectedMenu)); OnPropertyChanged(nameof(NoSelectedMenu));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(HasRecipeLines)); OnPropertyChanged(nameof(NoRecipeLines));
        OnPropertyChanged(nameof(HasSelectedRecipeLine)); OnPropertyChanged(nameof(NoSelectedRecipeLine));
        OnPropertyChanged(nameof(HasSubstituteIngredients)); OnPropertyChanged(nameof(NoSubstituteIngredients));
        OnPropertyChanged(nameof(CanAddSubstitute));
        OnPropertyChanged(nameof(HasMenuHistory)); OnPropertyChanged(nameof(NoMenuHistory));
        OnPropertyChanged(nameof(HasWarnings)); OnPropertyChanged(nameof(NoCostWarnings));
        OnPropertyChanged(nameof(CriticalAllWarningCount)); OnPropertyChanged(nameof(StockAllWarningCount));
        OnPropertyChanged(nameof(CostAllWarningCount)); OnPropertyChanged(nameof(RecipeAllWarningCount));
        OnPropertyChanged(nameof(HasImage)); OnPropertyChanged(nameof(NoImage));
        OnPropertyChanged(nameof(SelectedMenuTitle)); OnPropertyChanged(nameof(HeaderMetaText));
        OnPropertyChanged(nameof(SaveButtonText)); OnPropertyChanged(nameof(StatusPillText));
        OnPropertyChanged(nameof(SelectedCatalogTitle));
        OnPropertyChanged(nameof(CurrentActionButtonText));
        OnPropertyChanged(nameof(CurrentDeleteButtonText));
        OnPropertyChanged(nameof(CurrentDuplicateButtonText));
        OnPropertyChanged(nameof(CurrentActiveStatusButtonText));
        OnPropertyChanged(nameof(CurrentStatusPillText));
        OnPropertyChanged(nameof(SelectedServiceTypeDisplay));
        OnPropertyChanged(nameof(SelectedServicePriceDisplay));
        OnPropertyChanged(nameof(ShowFoodCatalogFilters));
        OnPropertyChanged(nameof(ShowServiceTypeFilter));
        OnPropertyChanged(nameof(ShowFoodWorkspace));
        OnPropertyChanged(nameof(ShowServiceWorkspace));
        OnPropertyChanged(nameof(ShowFoodSidebarState));
        OnPropertyChanged(nameof(ShowServiceSidebarState));
        OnPropertyChanged(nameof(ShowNoFoodCategoryState));
        OnPropertyChanged(nameof(EmptyStateTitle));
        OnPropertyChanged(nameof(EmptyStateDescription));
        OnPropertyChanged(nameof(EmptyStateCreateButtonText));
        OnPropertyChanged(nameof(FormattedPrice)); OnPropertyChanged(nameof(UnitPriceText));
        OnPropertyChanged(nameof(VatRateText)); OnPropertyChanged(nameof(FormattedVatRate));
        OnPropertyChanged(nameof(ServicePriceText));
        OnPropertyChanged(nameof(ServiceVatRateText));
        OnPropertyChanged(nameof(ActiveStatusText)); OnPropertyChanged(nameof(ActiveStatusButtonText));
        OnPropertyChanged(nameof(RecipeStatusText));
        OnPropertyChanged(nameof(IngredientLineTotalDisplay)); OnPropertyChanged(nameof(LossCostDisplay));
        OnPropertyChanged(nameof(CostDisplay)); OnPropertyChanged(nameof(SellingPriceDisplay));
        OnPropertyChanged(nameof(MarginAmountDisplay)); OnPropertyChanged(nameof(MarginRateDisplay));
        OnPropertyChanged(nameof(MarginStatus)); OnPropertyChanged(nameof(MarginBrush));
        OnPropertyChanged(nameof(StatusBannerBrush)); OnPropertyChanged(nameof(SelectedImagePath));
        OnPropertyChanged(nameof(SelectedImageFullPath));
        OnPropertyChanged(nameof(LastUpdatedDisplay));
        OnPropertyChanged(nameof(TotalIngredientsText)); OnPropertyChanged(nameof(TotalEffectiveQuantityDisplay));
        OnPropertyChanged(nameof(HasRecipeFooter));
        NotifyDashboardState();
    }

    private static void ResetCollection<T>(ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values) collection.Add(value);
    }

    private static string CreateSlug(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
            else if (char.IsWhiteSpace(ch) || ch is '-' or '_') builder.Append('-');
        }
        var slug = string.Join('-', builder.ToString().Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(slug) ? $"nhom-mon-{DateTime.UtcNow:yyyyMMddHHmmss}" : slug;
    }

    private static Guid BuildStableServiceGroupId(string serviceType)
    {
        var source = Encoding.UTF8.GetBytes($"service-group:{serviceType.Trim().ToUpperInvariant()}");
        var buffer = new byte[16];
        for (var i = 0; i < source.Length; i++)
        {
            buffer[i % 16] ^= source[i];
        }

        return new Guid(buffer);
    }
}

public sealed class MenuCategoryCardState
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public bool IsServiceCategory { get; set; }
    public bool IsVirtual { get; set; }
    public string ServiceTypeCode { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string DisplayName => Level > 0 ? $"{new string(' ', Level * 2)}↳ {Name}" : Name;
    public string HierarchyText => string.IsNullOrWhiteSpace(ParentCategoryName) ? "Nhóm gốc" : $"Nhóm con · {ParentCategoryName}";
    public string ItemCountText => $"{ItemCount:N0} mục";
}

public sealed class MenuCategoryParentOption
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed partial class MenuItemCardState : ObservableObject
{
    public Guid Id { get; set; } public string Name { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty; public string CategoryName { get; set; } = string.Empty;
    public string UnitPriceDisplay { get; set; } = string.Empty;
    public decimal UnitPriceNumeric { get; set; }
    public string RecipeStatusBadge { get; set; } = string.Empty;
    public string ActiveStatusBadge { get; set; } = string.Empty;
    public bool HasRecipe { get; set; } public bool IsInactive { get; set; }
    public bool IsHotelService { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    [ObservableProperty] private bool isExpanded;
    public ObservableCollection<RecipeLineEditor> RecipeLines { get; set; } = new();
    public string ExpandButtonText => IsExpanded ? "Ẩn NL" : "Xem NL";
}

public sealed partial class RecipeLineEditor : ObservableObject
{
    [ObservableProperty] private Guid id;
    [ObservableProperty] private int lineNo;
    [ObservableProperty] private int? itemId;
    [ObservableProperty] private string itemCode = string.Empty;
    [ObservableProperty] private string itemName = string.Empty;
    [ObservableProperty] private string unitName = string.Empty;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(EffectiveQuantity))] private decimal quantity;
    [ObservableProperty][NotifyPropertyChangedFor(nameof(EffectiveQuantity))] private decimal lossRatePercent;
    [ObservableProperty] private int? defaultWarehouseId;
    [ObservableProperty] private string warehouseDisplay = string.Empty;
    [ObservableProperty] private bool isRequired;
    [ObservableProperty] private decimal estimatedUnitCost;
    [ObservableProperty] private decimal estimatedLineCost;
    [ObservableProperty] private decimal availableQuantity;
    [ObservableProperty] private string? note;
    public decimal EffectiveQuantity => Quantity * (1m + (LossRatePercent / 100m));

    partial void OnQuantityChanged(decimal value) => RecalculateEstimatedLineCost();

    partial void OnLossRatePercentChanged(decimal value) => RecalculateEstimatedLineCost();

    partial void OnEstimatedUnitCostChanged(decimal value) => RecalculateEstimatedLineCost();

    private void RecalculateEstimatedLineCost()
    {
        EstimatedLineCost = Math.Round(EstimatedUnitCost * EffectiveQuantity, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed class MenuHistoryEntry
{
    public Guid MenuItemId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string TimeDisplay => OccurredAt.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
    public string Action { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string UserName { get; set; } = "system";
}

public sealed class ItemWarningDto
{
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning"; // "error" or "warning"
    public string SeverityText => Severity == "error" ? "Nghiêm trọng" : "Cần kiểm tra";
    public string WarningType { get; set; } = "general";
    public string WarningTypeText { get; set; } = "Cảnh báo";
    public string IngredientName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal MissingQuantity { get; set; }
    public string RequiredDisplay => RequiredQuantity > 0 ? $"{RequiredQuantity:N2}" : string.Empty;
    public string AvailableDisplay => RequiredQuantity > 0 ? $"{AvailableQuantity:N2}" : string.Empty;
    public string MissingDisplay => MissingQuantity > 0 ? $"{MissingQuantity:N2}" : string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class CostDetailRow
{
    public Guid MenuItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal MarginRate { get; set; }
    public int LineCount { get; set; }
    public int WarningCount { get; set; }
    public List<CostIngredientRow> Lines { get; set; } = [];
}

public sealed class CostIngredientRow
{
    public int LineNo { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LossRatePercent { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineCost { get; set; }
    public decimal AvailableQuantity { get; set; }
    public string Warning { get; set; } = string.Empty;
}

