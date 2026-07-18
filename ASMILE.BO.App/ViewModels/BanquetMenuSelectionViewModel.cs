using ASMILE.BO.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class BanquetMenuSelectionViewModel : ObservableObject
{
    private const string AllGroupsLabel = "Tất cả";
    private const string UncategorizedGroupLabel = "Chưa phân nhóm";

    private readonly BanquetRestaurantViewModel _parent;
    private readonly HashSet<string> _draftPresetGroups = new(System.StringComparer.OrdinalIgnoreCase);

    public BanquetMenuSelectionViewModel(BanquetRestaurantViewModel parent, bool isCustomMenu)
    {
        _parent = parent;
        IsCustomMenu = isCustomMenu;
        PackageListSource = parent.MenuPackages;
        GroupListSource = parent.BanquetMenuGroups;
        PackageListSource.CollectionChanged += PackageListSource_CollectionChanged;
        GroupListSource.CollectionChanged += PackageListSource_CollectionChanged;

        ItemsSource = parent.BanquetFilteredFoodItems;
        if (parent.BanquetMenuCategories.Count == 0)
        {
            try { _ = parent.EnsureMenuItemsLoadedAsync(); } catch { }
        }
        BuildCategoryTree(parent.BanquetMenuCategories);
        SelectedCategoryId = Guid.Empty;

        if (isCustomMenu)
        {
            Title = "Chọn món theo yêu cầu";
            SeedSelectedItems(parent.SelectedBanquetCustomFoods);
        }
        else
        {
            Title = "Chọn thực đơn có sẵn";
            RefreshPresetGroups();
            RefreshFilteredPackages();
            if (FilteredPackages.Count > 0)
            {
                SelectedPackage = FilteredPackages[0];
            }
        }
    }

    public bool IsCustomMenu { get; }
    public bool ShowPresetLayout => !IsCustomMenu;
    public string Title { get; }
    public object ItemsSource { get; }
    public ObservableCollection<BanquetMenuPackageDto> PackageListSource { get; }
    public ObservableCollection<BanquetMenuGroupDto> GroupListSource { get; }
    public ObservableCollection<BanquetMenuPackageDto> FilteredPackages { get; } = new();
    public ObservableCollection<string> PresetGroups { get; } = new();
    public ObservableCollection<string> PackageSortOptions { get; } = new()
    {
        "Thứ tự mặc định",
        "Giá thấp đến cao",
        "Giá cao đến thấp",
        "Tên A - Z",
        "Tên Z - A"
    };
    public ObservableCollection<string> MenuPackageGroups { get; } = new();
    public ObservableCollection<CategoryNode> CategoryNodes { get; } = new();
    public ObservableCollection<SelectedFoodItem> SelectedItems { get; } = new();
    public ObservableCollection<PresetPackageLineItem> SelectedPackageLines { get; } = new();

    public int TotalItems => SelectedItems.Sum(x => x.Quantity);
    public decimal TotalAmount => SelectedItems.Sum(x => x.LineTotal);
    public int FilteredPackageCount => FilteredPackages.Count;
    public bool HasSelectedPackage => SelectedPackage is not null;
    public bool ShowPresetEmptyState => ShowPresetLayout && !IsEditingPackage && SelectedPackage is null;
    public bool ShowPresetPreview => ShowPresetLayout && !IsEditingPackage && SelectedPackage is not null;
    public bool ShowPresetEditor => ShowPresetLayout && IsEditingPackage;
    public bool ShowPresetEditorPlaceholder => ShowPresetLayout && !IsEditingPackage;
    public bool IsPresetCatalogTab => ShowPresetLayout && SelectedPresetTab == 0;
    public bool IsPresetGroupTab => ShowPresetLayout && SelectedPresetTab == 1;
    public bool IsPresetEditorTab => ShowPresetLayout && SelectedPresetTab == 2;
    public string SelectedPackageGroupDisplay => NormalizeGroupLabel(SelectedPackage?.GroupName);

    [ObservableProperty] private Guid? selectedCategoryId;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string packageSearchText = string.Empty;
    [ObservableProperty] private string selectedPresetGroup = AllGroupsLabel;
    [ObservableProperty] private string selectedPackageSort = "Thứ tự mặc định";
    [ObservableProperty] private BanquetMenuPackageDto? selectedPackage;
    [ObservableProperty] private Guid? editingPackageId;
    [ObservableProperty] private string packageName = string.Empty;
    [ObservableProperty] private string packageCode = string.Empty;
    [ObservableProperty] private string packageGroupName = string.Empty;
    [ObservableProperty] private string draftPresetGroupName = string.Empty;
    [ObservableProperty] private decimal packagePrice;
    [ObservableProperty] private decimal packageVatRate = 8m;
    [ObservableProperty] private bool isEditingPackage;
    [ObservableProperty] private int selectedPresetTab;

    private void PackageListSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPresetGroups();
        RefreshFilteredPackages();
        if (SelectedPackage is not null)
        {
            SelectedPackage = PackageListSource.FirstOrDefault(x => x.Id == SelectedPackage.Id) ?? SelectedPackage;
        }
    }

    private void BuildCategoryTree(ObservableCollection<MenuCategoryDto> categories)
    {
        CategoryNodes.Clear();
        CategoryNodes.Add(new CategoryNode
        {
            Category = new MenuCategoryDto { Id = Guid.Empty, Name = AllGroupsLabel },
            IsExpanded = true
        });

        var roots = categories.Where(c => c.ParentCategoryId is null).OrderBy(c => c.Name);
        foreach (var root in roots)
        {
            var node = new CategoryNode { Category = root };
            AddChildren(node, categories);
            CategoryNodes.Add(node);
        }
    }

    private void AddChildren(CategoryNode parent, ObservableCollection<MenuCategoryDto> all)
    {
        foreach (var cat in all.Where(c => c.ParentCategoryId == parent.Category.Id).OrderBy(c => c.Name))
        {
            var node = new CategoryNode { Category = cat, Depth = parent.Depth + 1 };
            AddChildren(node, all);
            parent.Children.Add(node);
        }
    }

    private void SeedSelectedItems(ObservableCollection<BanquetSelectedFoodItemVm> selectedFoods)
    {
        SelectedItems.Clear();
        foreach (var food in selectedFoods.OrderBy(x => x.LineNo))
        {
            SelectedItems.Add(new SelectedFoodItem
            {
                LineNo = food.LineNo,
                FoodItem = food.FoodItem,
                Quantity = food.Quantity
            });
        }
        RefreshLineNumbers();
        RefreshTotals();
    }

    private void RefreshPresetGroups()
    {
        var groups = PackageListSource
            .Select(x => NormalizeGroupLabel(x.GroupName))
            .Concat(GroupListSource.Select(x => x.Name))
            .Concat(_draftPresetGroups)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        PresetGroups.Clear();
        PresetGroups.Add(AllGroupsLabel);
        MenuPackageGroups.Clear();
        foreach (var group in groups)
        {
            PresetGroups.Add(group);
            if (!string.Equals(group, UncategorizedGroupLabel, System.StringComparison.OrdinalIgnoreCase))
            {
                MenuPackageGroups.Add(group);
            }
        }

        if (!PresetGroups.Contains(SelectedPresetGroup))
        {
            SelectedPresetGroup = AllGroupsLabel;
        }
    }

    private void RefreshFilteredPackages()
    {
        IEnumerable<BanquetMenuPackageDto> query = PackageListSource;
        if (!string.Equals(SelectedPresetGroup, AllGroupsLabel, System.StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(x => NormalizeGroupLabel(x.GroupName) == SelectedPresetGroup);
        }

        if (!string.IsNullOrWhiteSpace(PackageSearchText))
        {
            var keyword = PackageSearchText.Trim();
            query = query.Where(x =>
                x.Name.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ||
                x.Code.Contains(keyword, System.StringComparison.OrdinalIgnoreCase) ||
                NormalizeGroupLabel(x.GroupName).Contains(keyword, System.StringComparison.OrdinalIgnoreCase));
        }

        query = SelectedPackageSort switch
        {
            "Giá thấp đến cao" => query.OrderBy(x => x.Price).ThenBy(x => x.Name),
            "Giá cao đến thấp" => query.OrderByDescending(x => x.Price).ThenBy(x => x.Name),
            "Tên A - Z" => query.OrderBy(x => x.Name),
            "Tên Z - A" => query.OrderByDescending(x => x.Name),
            _ => query.OrderBy(x => NormalizeGroupLabel(x.GroupName)).ThenBy(x => x.Name)
        };

        var rows = query.ToArray();

        SyncCollection(FilteredPackages, rows);
        OnPropertyChanged(nameof(FilteredPackageCount));

        if (SelectedPackage is not null && rows.All(x => x.Id != SelectedPackage.Id))
        {
            SelectedPackage = null;
        }
    }

    private static void SyncCollection<T>(ObservableCollection<T> collection, IEnumerable<T> rows)
    {
        collection.Clear();
        foreach (var row in rows)
        {
            collection.Add(row);
        }
    }

    private static string NormalizeGroupLabel(string? value)
        => string.IsNullOrWhiteSpace(value) ? UncategorizedGroupLabel : value.Trim();

    partial void OnSelectedCategoryIdChanged(Guid? value)
    {
        _parent.SelectedBanquetMenuCategoryId = value == Guid.Empty ? null : value;
    }

    partial void OnSearchTextChanged(string value)
    {
        _parent.BanquetMenuSearchText = value;
    }

    partial void OnSelectedPresetTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsPresetCatalogTab));
        OnPropertyChanged(nameof(IsPresetGroupTab));
        OnPropertyChanged(nameof(IsPresetEditorTab));
    }

    partial void OnPackageSearchTextChanged(string value)
    {
        RefreshFilteredPackages();
    }

    partial void OnSelectedPresetGroupChanged(string value)
    {
        RefreshFilteredPackages();
    }

    partial void OnSelectedPackageSortChanged(string value)
    {
        RefreshFilteredPackages();
    }

    partial void OnSelectedPackageChanged(BanquetMenuPackageDto? value)
    {
        LoadSelectedPackageLines(value);
        OnPropertyChanged(nameof(HasSelectedPackage));
        OnPropertyChanged(nameof(SelectedPackageGroupDisplay));
        OnPropertyChanged(nameof(ShowPresetEmptyState));
        OnPropertyChanged(nameof(ShowPresetPreview));
    }

    partial void OnIsEditingPackageChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowPresetEditor));
        OnPropertyChanged(nameof(ShowPresetEditorPlaceholder));
        OnPropertyChanged(nameof(ShowPresetEmptyState));
        OnPropertyChanged(nameof(ShowPresetPreview));
    }

    [RelayCommand] private void ToggleCategory(CategoryNode node) => node.IsExpanded = !node.IsExpanded;
    [RelayCommand] private void SelectCategory(Guid? categoryId) => SelectedCategoryId = categoryId;
    [RelayCommand] private void SelectPresetGroup(string? groupName) => SelectedPresetGroup = string.IsNullOrWhiteSpace(groupName) ? AllGroupsLabel : groupName;

    [RelayCommand]
    private async Task CreatePresetGroupAsync()
    {
        var groupName = DraftPresetGroupName.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return;
        }

        var saved = await _parent.SaveMenuGroupFromModalAsync(groupName);
        _draftPresetGroups.Remove(groupName);
        RefreshPresetGroups();
        DraftPresetGroupName = saved.Name;
        SelectedPresetGroup = saved.Name;
    }
    [RelayCommand]
    private void SelectPresetTab(string? value)
    {
        if (int.TryParse(value, out var index) && index is >= 0 and <= 2)
        {
            SelectedPresetTab = index;
        }
    }

    [RelayCommand]
    private void AddItem(PosMenuItemCard? item)
    {
        if (item is null || (!IsCustomMenu && !IsEditingPackage))
        {
            return;
        }

        var existing = SelectedItems.FirstOrDefault(x => x.FoodItem.Id == item.Id);
        if (existing is not null)
        {
            existing.Quantity++;
            existing.NotifyChanges();
        }
        else
        {
            SelectedItems.Add(new SelectedFoodItem { FoodItem = item, Quantity = 1 });
        }

        RefreshLineNumbers();
        RefreshTotals();
    }

    [RelayCommand]
    private void IncreaseQty(SelectedFoodItem? item)
    {
        if (item is null) return;
        item.Quantity++;
        item.NotifyChanges();
        RefreshTotals();
    }

    [RelayCommand]
    private void DecreaseQty(SelectedFoodItem? item)
    {
        if (item is null) return;
        if (item.Quantity <= 1)
        {
            SelectedItems.Remove(item);
            RefreshLineNumbers();
        }
        else
        {
            item.Quantity--;
            item.NotifyChanges();
        }

        RefreshTotals();
    }

    [RelayCommand]
    private void RemoveItem(SelectedFoodItem? item)
    {
        if (item is null) return;
        if (SelectedItems.Remove(item))
        {
            RefreshLineNumbers();
            RefreshTotals();
        }
    }

    [RelayCommand]
    private void SelectPackage(BanquetMenuPackageDto? package)
    {
        if (package is null) return;
        IsEditingPackage = false;
        SelectedPackage = package;
        PackageSearchText = package.Name;
        SelectedPresetTab = 0;
    }

    [RelayCommand]
    private void OpenNewPackageEditor()
    {
        SelectedPackage = null;
        EditingPackageId = null;
        PackageName = string.Empty;
        PackageCode = BuildAutoMenuCode();
        PackageGroupName = string.Empty;
        DraftPresetGroupName = string.Empty;
        PackagePrice = 0m;
        PackageVatRate = 8m;
        SearchText = string.Empty;
        SelectedCategoryId = Guid.Empty;
        SelectedItems.Clear();
        RefreshTotals();
        IsEditingPackage = true;
        SelectedPresetTab = 2;
    }

    [RelayCommand]
    private void EditSelectedPackage()
    {
        if (SelectedPackage is null)
        {
            return;
        }

        EditingPackageId = SelectedPackage.Id;
        PackageName = SelectedPackage.Name;
        PackageCode = SelectedPackage.Code;
        PackageGroupName = SelectedPackage.GroupName ?? string.Empty;
        DraftPresetGroupName = PackageGroupName;
        PackagePrice = SelectedPackage.Price;
        PackageVatRate = SelectedPackage.VatRate;
        SearchText = string.Empty;
        SelectedCategoryId = Guid.Empty;

        SelectedItems.Clear();
        foreach (var line in SelectedPackage.Lines.OrderBy(x => x.LineNo))
        {
            var foodItem = _parent.BanquetFoodItems.FirstOrDefault(x => x.Id == line.MenuItemId)
                ?? new PosMenuItemCard(new MenuItemDto
                {
                    Id = line.MenuItemId,
                    MenuCategoryId = Guid.Empty,
                    CategoryName = string.Empty,
                    Code = line.MenuItemCode,
                    Name = line.MenuItemName,
                    UnitName = line.UnitName,
                    UnitPrice = line.UnitPrice,
                    IsActive = true
                });

            SelectedItems.Add(new SelectedFoodItem
            {
                FoodItem = foodItem,
                Quantity = (int)line.Qty
            });
        }

        IsEditingPackage = true;
        RefreshLineNumbers();
        RefreshTotals();
        SelectedPresetTab = 2;
    }

    [RelayCommand]
    private void EditPackage(BanquetMenuPackageDto? package)
    {
        if (package is null)
        {
            return;
        }

        SelectedPackage = package;
        EditSelectedPackage();
    }

    [RelayCommand]
    private void CancelPackageEditor()
    {
        IsEditingPackage = false;
        SelectedItems.Clear();
        RefreshTotals();
        SelectedPresetTab = 0;
        if (SelectedPackage is not null)
        {
            LoadSelectedPackageLines(SelectedPackage);
        }
    }

    [RelayCommand]
    private void StartMenuFromDraftGroup()
    {
        var groupName = DraftPresetGroupName.Trim();
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            _draftPresetGroups.Add(groupName);
        }

        OpenNewPackageEditor();
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            PackageGroupName = groupName;
            SelectedPresetGroup = groupName;
        }
    }

    [RelayCommand]
    private async Task ApplySelectedPackageAsync(BanquetMenuPackageDto? package = null)
    {
        package ??= SelectedPackage;
        if (package is null)
        {
            return;
        }

        SelectedPackage = package;
        await _parent.AttachPresetMenuSelectionAsync(package);
        _parent.CloseMenuSelection(true);
    }

    private void RefreshLineNumbers()
    {
        for (int i = 0; i < SelectedItems.Count; i++)
        {
            SelectedItems[i].LineNo = i + 1;
        }
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(TotalItems));
        OnPropertyChanged(nameof(TotalAmount));
        if (!IsCustomMenu && IsEditingPackage)
        {
            PackagePrice = TotalAmount;
        }
    }

    private static string BuildAutoMenuCode()
        => $"TD-{System.DateTime.Now:yyMMdd}-{System.DateTime.Now:HHmmss}";

    private void LoadSelectedPackageLines(BanquetMenuPackageDto? package)
    {
        SelectedPackageLines.Clear();
        if (package is null)
        {
            return;
        }

        foreach (var line in package.Lines.OrderBy(x => x.LineNo))
        {
            SelectedPackageLines.Add(new PresetPackageLineItem
            {
                LineNo = line.LineNo,
                ItemName = line.MenuItemName,
                UnitName = line.UnitName,
                Quantity = line.Qty,
                UnitPrice = line.UnitPrice
            });
        }
    }

    [RelayCommand] private void Close() => _parent.CloseMenuSelection(false);

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        await _parent.ApplyCustomMenuSelectionAsync(SelectedItems);
        _parent.CloseMenuSelection(true);
    }

    [RelayCommand] private Task SeedSamples() => _parent.SeedSampleMenuPackagesAsync();

    [RelayCommand]
    private async Task SavePackageAsync()
    {
        if (string.IsNullOrWhiteSpace(PackageName) || SelectedItems.Count == 0)
        {
            return;
        }

        if (PackagePrice <= 0)
        {
            PackagePrice = SelectedItems.Sum(x => x.LineTotal);
        }

        var savedPackage = await _parent.SaveMenuPackageFromModalAsync(
            EditingPackageId,
            string.IsNullOrWhiteSpace(PackageCode) ? BuildAutoMenuCode() : PackageCode.Trim(),
            PackageName.Trim(),
            PackageGroupName.Trim(),
            PackagePrice,
            PackageVatRate,
            SelectedItems.Select(x => new SaveBanquetMenuPackageLineRequest
            {
                MenuItemId = x.FoodItem.Id,
                CourseType = "Main",
                Qty = x.Quantity,
                PricingRule = "PerTable"
            }).ToArray());

        SelectedPackage = savedPackage;
        SelectedPresetGroup = string.IsNullOrWhiteSpace(savedPackage.GroupName) ? AllGroupsLabel : NormalizeGroupLabel(savedPackage.GroupName);
        DraftPresetGroupName = savedPackage.GroupName ?? string.Empty;
        IsEditingPackage = false;
        SelectedItems.Clear();
        RefreshTotals();
        SelectedPresetTab = 0;
    }
}

public sealed partial class CategoryNode : ObservableObject
{
    public MenuCategoryDto Category { get; set; } = null!;
    public ObservableCollection<CategoryNode> Children { get; } = new();
    [ObservableProperty] private bool isExpanded;
    public int Depth { get; set; }
    public bool HasChildren => Children.Count > 0;
}

public sealed class SelectedFoodItem : ObservableObject
{
    private int _lineNo;
    private int _quantity = 1;

    public int LineNo
    {
        get => _lineNo;
        set
        {
            _lineNo = value;
            OnPropertyChanged();
        }
    }

    public PosMenuItemCard FoodItem { get; set; } = null!;

    public int Quantity
    {
        get => _quantity;
        set
        {
            _quantity = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
        }
    }

    public string UnitName => FoodItem.UnitName;
    public decimal UnitPrice => FoodItem.UnitPrice;
    public decimal LineTotal => Quantity * UnitPrice;

    public void NotifyChanges()
    {
        OnPropertyChanged(nameof(Quantity));
        OnPropertyChanged(nameof(LineTotal));
    }
}

public sealed class PresetPackageLineItem
{
    public int LineNo { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount => Quantity * UnitPrice;
}
