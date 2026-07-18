using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Shared.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class VirtualExplorerViewModel : ViewModelBase
{
    private const string IconRoot = @"D:\ASMILE-BO\Windows_Icon\Icons";
    private const string DesktopIconPath = IconRoot + @"\folders\desktop.ico";
    private const string ThisPcIconPath = IconRoot + @"\files\windowdesktop.ico";
    private const string NetworkIconPath = IconRoot + @"\folders\network.ico";
    private const string DriveIconPath = IconRoot + @"\devices\drives\drive.ico";
    private const string HomeIconPath = IconRoot + @"\emblems\homegroup.ico";
    private const string GalleryIconPath = IconRoot + @"\applications\photos.ico";
    private const string WorkspaceIconPath = IconRoot + @"\applications\operations.ico";
    private const string SearchIconPath = IconRoot + @"\folders\search.ico";
    private const string ExplorerWindowIconPath = IconRoot + @"\logo\u2.png";

    private readonly IVirtualExplorerService _virtualExplorerService;
    private readonly IBusinessDocumentService _businessDocumentService;
    private readonly IInventoryAppService _inventoryAppService;
    private readonly IPayableService _payableService;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly IWindowManagerService _windowManagerService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAuthorizationService _authorizationService;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IPathSecurityService _pathSecurityService;
    private readonly TaskCompletionSource<bool> _initializeTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private readonly List<string> _history = new();
    private readonly List<VirtualExplorerNodeDto> _currentItemsSource = new();
    private readonly HashSet<string> _navigationVisitedPaths = new(StringComparer.OrdinalIgnoreCase);
    private int _historyIndex = -1;
    private CancellationTokenSource? _searchCts;

    public ObservableCollection<VirtualExplorerNodeDto> RootItems { get; } = new();
    public ObservableCollection<VirtualExplorerNodeDto> CurrentItems { get; } = new();
    public ObservableCollection<ExplorerBreadcrumbSegment> BreadcrumbItems { get; } = new();
    public ObservableCollection<ExplorerNavNode> NavigationNodes { get; } = new();
    public ObservableCollection<ExplorerDriveItemModel> DriveItems { get; } = new();
    public ObservableCollection<ExplorerDriveItemModel> NetworkLocationItems { get; } = new();
    public ObservableCollection<ExplorerTabItem> Tabs { get; } = new();

    [ObservableProperty]
    private VirtualExplorerNodeDto? selectedItem;

    [ObservableProperty]
    private ExplorerNavNode? selectedNavigationNode;

    [ObservableProperty]
    private string currentPath = "This PC";

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool isSearchResult;

    [ObservableProperty]
    private string statusText = "Sẵn sàng";

    [ObservableProperty]
    private bool hasNoItems = true;

    [ObservableProperty]
    private bool canGoBack;

    [ObservableProperty]
    private bool canGoForward;

    [ObservableProperty]
    private bool canGoUp;

    [ObservableProperty]
    private bool isPreviewPaneVisible = true;

    [ObservableProperty]
    private bool isDetailsPaneVisible;

    [ObservableProperty]
    private bool isNavigationPaneVisible = true;

    [ObservableProperty]
    private bool isCompactView;

    [ObservableProperty]
    private bool isItemCheckBoxes;

    [ObservableProperty]
    private bool isFileNameExtensions = true;

    [ObservableProperty]
    private bool isHiddenItems;

    [ObservableProperty]
    private string currentSortBy = "Name";

    [ObservableProperty]
    private bool isSortDescending;

    [ObservableProperty]
    private string currentGroupBy = "None";

    [ObservableProperty]
    private string currentViewMode = "Details";

    [ObservableProperty]
    private double itemIconSize = 32;

    [ObservableProperty]
    private string searchPlaceholder = "Search This PC";

    [ObservableProperty]
    private bool isThisPcView;

    [ObservableProperty]
    private string previewTitle = "Select a file to preview.";

    [ObservableProperty]
    private string previewCode = "-";

    [ObservableProperty]
    private string previewDate = "-";

    [ObservableProperty]
    private string previewEntity = "-";

    [ObservableProperty]
    private string previewAmount = "-";

    [ObservableProperty]
    private string previewStatus = "-";

    [ObservableProperty]
    private bool isPreviewDocument;

    [ObservableProperty]
    private int currentItemCount;

    [ObservableProperty]
    private ExplorerTabItem? activeTab;

    public bool IsRightPaneVisible => IsPreviewPaneVisible;

    public bool IsSortByNameChecked => CurrentSortBy == "Name";
    public bool IsSortByDateModifiedChecked => CurrentSortBy == "DateModified";
    public bool IsSortByTypeChecked => CurrentSortBy == "Type";
    public bool IsSortBySizeChecked => CurrentSortBy == "Size";

    public bool IsGroupByNoneChecked => CurrentGroupBy == "None";
    public bool IsGroupByNameChecked => CurrentGroupBy == "Name";
    public bool IsGroupByDateModifiedChecked => CurrentGroupBy == "DateModified";
    public bool IsGroupByTypeChecked => CurrentGroupBy == "Type";
    public bool IsGroupBySizeChecked => CurrentGroupBy == "Size";

    public bool IsExtraLargeIconsChecked => CurrentViewMode == "ExtraLargeIcons";
    public bool IsLargeIconsChecked => CurrentViewMode == "LargeIcons";
    public bool IsMediumIconsChecked => CurrentViewMode == "MediumIcons";
    public bool IsSmallIconsChecked => CurrentViewMode == "SmallIcons";
    public bool IsTilesChecked => CurrentViewMode == "Tiles";
    public bool IsListChecked => CurrentViewMode == "List";
    public bool IsDetailsChecked => CurrentViewMode == "Details";
    public bool IsContentChecked => CurrentViewMode == "Content";
    public bool IsIconViewMode => IsExtraLargeIconsChecked || IsLargeIconsChecked || IsMediumIconsChecked || IsSmallIconsChecked || IsTilesChecked;
    public bool IsDetailsViewMode => !IsIconViewMode;

    public VirtualExplorerViewModel(
        IVirtualExplorerService virtualExplorerService,
        IBusinessDocumentService businessDocumentService,
        IInventoryAppService inventoryAppService,
        IPayableService payableService,
        INotificationService notificationService,
        IDialogService dialogService,
        IWindowManagerService windowManagerService,
        IServiceProvider serviceProvider,
        IAuthorizationService authorizationService,
        ISecurityAuditService securityAuditService,
        IPathSecurityService pathSecurityService)
    {
        _virtualExplorerService = virtualExplorerService;
        _businessDocumentService = businessDocumentService;
        _inventoryAppService = inventoryAppService;
        _payableService = payableService;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _windowManagerService = windowManagerService;
        _serviceProvider = serviceProvider;
        _authorizationService = authorizationService;
        _securityAuditService = securityAuditService;
        _pathSecurityService = pathSecurityService;

        _ = InitializeAsync();
    }

    partial void OnCurrentPathChanged(string value)
    {
        SearchPlaceholder = $"Search {GetLastSegment(value)}";

        if (ActiveTab is not null)
        {
            ActiveTab.Path = value;
            ActiveTab.Title = BuildTabTitle(value);
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _ = SearchWithDebounceAsync(value);
    }

    partial void OnSelectedItemChanged(VirtualExplorerNodeDto? value)
    {
        _ = UpdatePreviewAsync(value);
    }

    partial void OnIsPreviewPaneVisibleChanged(bool value)
    {
        if (!value && IsDetailsPaneVisible)
        {
            IsDetailsPaneVisible = false;
        }

        OnPropertyChanged(nameof(IsRightPaneVisible));
    }

    partial void OnIsDetailsPaneVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(IsRightPaneVisible));
    }

    partial void OnCurrentSortByChanged(string value)
    {
        OnPropertyChanged(nameof(IsSortByNameChecked));
        OnPropertyChanged(nameof(IsSortByDateModifiedChecked));
        OnPropertyChanged(nameof(IsSortByTypeChecked));
        OnPropertyChanged(nameof(IsSortBySizeChecked));
    }

    partial void OnCurrentGroupByChanged(string value)
    {
        OnPropertyChanged(nameof(IsGroupByNoneChecked));
        OnPropertyChanged(nameof(IsGroupByNameChecked));
        OnPropertyChanged(nameof(IsGroupByDateModifiedChecked));
        OnPropertyChanged(nameof(IsGroupByTypeChecked));
        OnPropertyChanged(nameof(IsGroupBySizeChecked));
    }

    partial void OnCurrentViewModeChanged(string value)
    {
        OnPropertyChanged(nameof(IsExtraLargeIconsChecked));
        OnPropertyChanged(nameof(IsLargeIconsChecked));
        OnPropertyChanged(nameof(IsMediumIconsChecked));
        OnPropertyChanged(nameof(IsSmallIconsChecked));
        OnPropertyChanged(nameof(IsTilesChecked));
        OnPropertyChanged(nameof(IsListChecked));
        OnPropertyChanged(nameof(IsDetailsChecked));
        OnPropertyChanged(nameof(IsContentChecked));
        OnPropertyChanged(nameof(IsIconViewMode));
        OnPropertyChanged(nameof(IsDetailsViewMode));
    }

    [RelayCommand]
    private async Task OpenAsync(VirtualExplorerNodeDto? node)
    {
        if (node is null)
        {
            return;
        }

        SelectedItem = node;

        if (node.NodeType == VirtualNodeType.Document)
        {
            if (await TryOpenSupplierQuickCheckFromDocumentAsync(node))
            {
                return;
            }

            if (await TryOpenPayableDebtStatementDetailFromDocumentAsync(node))
            {
                return;
            }

            if (await TryOpenReceiptDetailFromDocumentAsync(node))
            {
                return;
            }

            if (await TryOpenPaymentVoucherDetailFromDocumentAsync(node))
            {
                return;
            }

            StatusText = $"Đã chọn chứng từ: {node.DisplayName}";
            return;
        }

        await NavigateToAsync(node.VirtualPath, true);
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private async Task BackAsync()
    {
        if (_historyIndex <= 0)
        {
            return;
        }

        _historyIndex--;
        await NavigateToAsync(_history[_historyIndex], false);
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private async Task ForwardAsync()
    {
        if (_historyIndex >= _history.Count - 1)
        {
            return;
        }

        _historyIndex++;
        await NavigateToAsync(_history[_historyIndex], false);
    }

    [RelayCommand(CanExecute = nameof(CanGoUp))]
    private async Task UpAsync()
    {
        if (!CanGoUp)
        {
            return;
        }

        var parent = GetParentPath(CurrentPath);
        if (string.IsNullOrWhiteSpace(parent))
        {
            parent = "This PC";
        }

        await NavigateToAsync(parent, true);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (IsSearchResult && !string.IsNullOrWhiteSpace(SearchText))
        {
            await SearchAsync();
            return;
        }

        await NavigateToAsync(CurrentPath, false);
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        var keyword = InputSanitizer.SanitizeSearchKeyword(SearchText);
        SearchText = keyword;
        if (string.IsNullOrWhiteSpace(keyword))
        {
            if (IsSearchResult)
            {
                var fallback = _historyIndex >= 0 && _historyIndex < _history.Count
                    ? _history[_historyIndex]
                    : "This PC";
                await NavigateToAsync(fallback, false);
            }

            return;
        }

        IsBusy = true;
        try
        {
            await _securityAuditService.LogAsync(
                SecurityEventType.SearchExecuted,
                "VirtualFileCenter",
                "Search",
                null,
                "Thực hiện tìm kiếm",
                new Dictionary<string, string> { ["keyword"] = keyword });

            var rows = await _virtualExplorerService.SearchAsync(keyword);
            _currentItemsSource.Clear();
            _currentItemsSource.AddRange(rows);
            ApplyCurrentItemsProjection();

            IsSearchResult = true;
            IsThisPcView = false;
            CurrentItemCount = CurrentItems.Count;
            HasNoItems = CurrentItems.Count == 0;
            StatusText = $"{CurrentItems.Count} items";
            BuildBreadcrumb($"Search\\{keyword}");
            UpdateNavigationState();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        if (!await EnsurePermissionAsync(Permission.PrintDocuments, SecurityEventType.PermissionDenied, "Print"))
        {
            return;
        }

        if (SelectedItem is not null && await TryResolveReceiptFromDocumentNodeAsync(SelectedItem) is { } receipt)
        {
            OpenReceiptPrintPreviewModule(receipt);
            await _securityAuditService.LogAsync(SecurityEventType.PrintDocument, "VirtualFileCenter", "Receipt", receipt.Id.ToString(), "Mở xem trước in phiếu nhập");
            return;
        }

        var docId = SelectedItem?.DocumentId;
        if (!docId.HasValue)
        {
            _notificationService.Show("Chọn một chứng từ để in.");
            return;
        }

        await _businessDocumentService.PrintAsync(docId.Value);
        _notificationService.Show("Đã gửi lệnh in chứng từ.");
        await _securityAuditService.LogAsync(SecurityEventType.PrintDocument, "VirtualFileCenter", "BusinessDocument", docId.Value.ToString(), "Gửi lệnh in chứng từ");
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (!await EnsurePermissionAsync(Permission.ExportData, SecurityEventType.PermissionDenied, "ExportPdf"))
        {
            return;
        }

        if (SelectedItem is not null && await TryResolveReceiptFromDocumentNodeAsync(SelectedItem) is { } receipt)
        {
            await ExportReceiptPdfAsync(receipt);
            return;
        }

        if (SelectedItem is not null && await TryResolvePayableDebtStatementFromDocumentNodeAsync(SelectedItem) is { } debtStatement)
        {
            var period = PromptPayableDebtStatementPeriod(debtStatement.SupplierCode, debtStatement.SupplierName);
            if (period is null)
            {
                return;
            }

            var filteredStatement = await TryResolvePayableDebtStatementFromDocumentNodeAsync(SelectedItem, period.Value.FromDate, period.Value.ToDate);
            if (filteredStatement is not null)
            {
                await ExportPayableDebtStatementPdfAsync(filteredStatement);
            }
            return;
        }

        if (SelectedItem is not null && await TryResolvePaymentVoucherFromDocumentNodeAsync(SelectedItem) is { } paymentVoucher)
        {
            await ExportPaymentVoucherPdfAsync(paymentVoucher);
            return;
        }

        var docId = SelectedItem?.DocumentId;
        if (!docId.HasValue)
        {
            _notificationService.Show("Chọn một chứng từ để xuất PDF.");
            return;
        }

        await _businessDocumentService.ExportPdfAsync(docId.Value);
        _notificationService.Show("Đã xuất PDF thành công.");
        await _securityAuditService.LogAsync(SecurityEventType.ExportDocument, "VirtualFileCenter", "BusinessDocument", docId.Value.ToString(), "Xuất PDF chứng từ");
    }

    [RelayCommand]
    private void ViewDetails()
    {
        if (SelectedItem is null)
        {
            _notificationService.Show("Chưa chọn mục để xem chi tiết.");
            return;
        }

        var lines = new[]
        {
            $"Tên: {SelectedItem.DisplayName}",
            $"Đường dẫn ảo: {SelectedItem.VirtualPath}",
            $"Loại node: {SelectedItem.NodeType}",
            $"Module: {SelectedItem.Module}",
            $"Entity: {SelectedItem.EntityType}",
            $"EntityId: {SelectedItem.EntityId}",
            $"DocumentId: {SelectedItem.DocumentId}"
        };

        _dialogService.Info(string.Join(Environment.NewLine, lines), "Node details");
    }

    [RelayCommand]
    private async Task OpenLocationAsync()
    {
        if (!await EnsurePermissionAsync(Permission.OpenSensitiveFolder, SecurityEventType.PermissionDenied, "OpenLocation"))
        {
            return;
        }

        if (SelectedItem is null)
        {
            return;
        }

        var parentPath = GetParentPath(SelectedItem.VirtualPath);
        if (!string.IsNullOrWhiteSpace(parentPath))
        {
            if (!_pathSecurityService.ValidateVirtualPath(parentPath))
            {
                _notificationService.Show("Đường dẫn không hợp lệ.");
                return;
            }

            await _securityAuditService.LogAsync(SecurityEventType.OpenSensitiveFolder, "VirtualFileCenter", "Path", parentPath, "Mở vị trí tài liệu");
            await NavigateToAsync(parentPath, true);
        }
    }

    private async Task ExportPaymentVoucherPdfAsync(PaymentVoucherRow voucher)
    {
        var safeDocNo = string.IsNullOrWhiteSpace(voucher.DocumentNo) ? $"PC-{Guid.NewGuid():N}" : voucher.DocumentNo;
        var saveDialog = new SaveFileDialog
        {
            Title = "Xuất phiếu chi ra PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = $"{_pathSecurityService.SanitizeFileName(safeDocNo)}.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (saveDialog.ShowDialog() != true)
        {
            return;
        }

        if (!_pathSecurityService.IsAllowedExtension(saveDialog.FileName))
        {
            _notificationService.Show("Định dạng file xuất không hợp lệ.");
            return;
        }

        var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"asmile-{safeDocNo}-{Guid.NewGuid():N}.html");
        try
        {
            var html = BuildPaymentVoucherExportHtml(voucher);
            await File.WriteAllTextAsync(tempHtmlPath, html, new UTF8Encoding(false));

            var browserExe = ResolvePdfBrowserExecutable();
            if (string.IsNullOrWhiteSpace(browserExe))
            {
                _dialogService.Error("Xuất PDF thất bại.\nKhông tìm thấy Microsoft Edge/Google Chrome để in PDF.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = browserExe,
                Arguments = $"--headless --disable-gpu --print-to-pdf-no-header --print-to-pdf=\"{saveDialog.FileName}\" \"file:///{tempHtmlPath.Replace("\\", "/")}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _notificationService.Show("Không thể khởi chạy trình xuất PDF.");
                return;
            }

            await process.WaitForExitAsync();
            if (process.ExitCode == 0 && File.Exists(saveDialog.FileName))
            {
                _notificationService.Show("Đã xuất PDF thành công.");
                await _securityAuditService.LogAsync(
                    SecurityEventType.ExportDocument,
                    "VirtualFileCenter",
                    "PaymentVoucher",
                    safeDocNo,
                    "Xuất PDF phiếu chi");
            }
            else
            {
                _notificationService.Show("Xuất PDF thất bại.");
            }
        }
        catch
        {
            _dialogService.Error("Xuất PDF thất bại. Vui lòng kiểm tra nhật ký hệ thống.");
            await _securityAuditService.LogAsync(
                SecurityEventType.SecurityError,
                "VirtualFileCenter",
                "Export",
                safeDocNo,
                "Xuất PDF phiếu chi thất bại");
        }
        finally
        {
            try
            {
                if (File.Exists(tempHtmlPath))
                {
                    File.Delete(tempHtmlPath);
                }
            }
            catch
            {
            }
        }
    }

    [RelayCommand]
    private void CopyVirtualPath()
    {
        if (SelectedItem is null)
        {
            return;
        }

        Clipboard.SetText(SelectedItem.VirtualPath);
        _notificationService.Show("Đã sao chép đường dẫn ảo.");
    }

    [RelayCommand]
    private async Task NavigateSidebarAsync(ExplorerNavNode? navNode)
    {
        if (navNode is null || string.IsNullOrWhiteSpace(navNode.VirtualPath))
        {
            return;
        }

        await NavigateToAsync(navNode.VirtualPath, true);
    }

    [RelayCommand]
    private async Task NavigateBreadcrumbAsync(ExplorerBreadcrumbSegment? segment)
    {
        if (segment is null || string.IsNullOrWhiteSpace(segment.Path))
        {
            return;
        }

        await NavigateToAsync(segment.Path, true);
    }

    [RelayCommand]
    private void TogglePreviewPane()
    {
        IsPreviewPaneVisible = !IsPreviewPaneVisible;
    }

    [RelayCommand]
    private async Task AddTabAsync()
    {
        var newTab = new ExplorerTabItem
        {
            Title = "This PC",
            Path = "This PC",
            IsActive = false
        };

        Tabs.Add(newTab);
        await ActivateTabAsync(newTab);
    }

    [RelayCommand]
    private async Task ActivateTabAsync(ExplorerTabItem? tab)
    {
        if (tab is null)
        {
            return;
        }

        foreach (var item in Tabs)
        {
            item.IsActive = ReferenceEquals(item, tab);
        }

        ActiveTab = tab;
        SearchText = string.Empty;
        _history.Clear();
        _historyIndex = -1;
        await NavigateToAsync(string.IsNullOrWhiteSpace(tab.Path) ? "This PC" : tab.Path, true);
    }

    [RelayCommand]
    private async Task CloseTabAsync(ExplorerTabItem? tab)
    {
        if (tab is null)
        {
            return;
        }

        if (Tabs.Count <= 1)
        {
            if (!tab.IsActive)
            {
                await ActivateTabAsync(tab);
            }

            return;
        }

        var closedIndex = Tabs.IndexOf(tab);
        var wasActive = tab.IsActive;
        Tabs.Remove(tab);

        if (!wasActive)
        {
            return;
        }

        var nextIndex = Math.Clamp(closedIndex - 1, 0, Tabs.Count - 1);
        await ActivateTabAsync(Tabs[nextIndex]);
    }

    [RelayCommand]
    private async Task OpenTabInNewWindowAsync(ExplorerTabItem? tab)
    {
        if (tab is null)
        {
            return;
        }

        var targetPath = string.IsNullOrWhiteSpace(tab.Path) ? CurrentPath : tab.Path;
        var normalizedPath = NormalizePath(targetPath);
        var title = BuildTabTitle(normalizedPath);
        var module = new AppModuleInfo(
            $"virtual-file-center-{Guid.NewGuid():N}",
            title,
            "\uE8B7",
            "Virtual File Center",
            "#3A7BFF",
            false,
            false,
            ExplorerWindowIconPath);

        var window = _windowManagerService.OpenOrActivate(module, () =>
            ActivatorUtilities.CreateInstance<VirtualExplorerViewModel>(_serviceProvider));

        if (window.ContentViewModel is VirtualExplorerViewModel explorerViewModel)
        {
            await explorerViewModel.NavigateFromExternalAsync(normalizedPath);
        }

        // Move-tab behavior: after opening in a new window, remove this tab from current window.
        if (!Tabs.Contains(tab))
        {
            return;
        }

        if (Tabs.Count <= 1)
        {
            var currentWindow = _windowManagerService.OpenWindows.FirstOrDefault(x => ReferenceEquals(x.ContentViewModel, this));
            if (currentWindow is not null)
            {
                _windowManagerService.Close(currentWindow);
            }

            return;
        }

        await CloseTabAsync(tab);
    }

    [RelayCommand] private void New() => _notificationService.Show("New command (đang mô phỏng).");
    [RelayCommand] private void Cut() => _notificationService.Show("Cut command (đang mô phỏng).");
    [RelayCommand] private void Copy() => _notificationService.Show("Copy command (đang mô phỏng).");
    [RelayCommand] private void Paste() => _notificationService.Show("Paste command (đang mô phỏng).");
    [RelayCommand] private void Rename() => _notificationService.Show("Rename command (đang mô phỏng).");
    [RelayCommand] private void Share() => _notificationService.Show("Share command (đang mô phỏng).");
    [RelayCommand] private void Delete() => _notificationService.Show("Delete command (đang mô phỏng).");
    [RelayCommand] private void More() => _notificationService.Show("More command (đang mô phỏng).");

    [RelayCommand]
    private void SetSortBy(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return;
        }

        CurrentSortBy = sortBy;
        ApplyCurrentItemsProjection();
    }

    [RelayCommand]
    private void SetSortDirection(string? descending)
    {
        IsSortDescending = bool.TryParse(descending, out var parsed) && parsed;
        ApplyCurrentItemsProjection();
    }

    [RelayCommand]
    private void SetGroupBy(string? groupBy)
    {
        if (string.IsNullOrWhiteSpace(groupBy))
        {
            return;
        }

        CurrentGroupBy = groupBy;
        ApplyCurrentItemsProjection();
    }

    [RelayCommand]
    private void SetViewMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
        {
            return;
        }

        CurrentViewMode = mode;
        ItemIconSize = mode switch
        {
            "ExtraLargeIcons" => 64,
            "LargeIcons" => 48,
            "MediumIcons" => 36,
            "SmallIcons" => 24,
            "Tiles" => 28,
            _ => 32
        };
    }

    [RelayCommand]
    private void ToggleNavigationPane()
    {
        IsNavigationPaneVisible = !IsNavigationPaneVisible;
    }

    [RelayCommand]
    private void ToggleDetailsPane()
    {
        IsDetailsPaneVisible = !IsDetailsPaneVisible;
    }

    [RelayCommand]
    private void ToggleCompactView()
    {
        IsCompactView = !IsCompactView;
    }

    [RelayCommand]
    private void ToggleItemCheckBoxes()
    {
        IsItemCheckBoxes = !IsItemCheckBoxes;
    }

    [RelayCommand]
    private void ToggleFileNameExtensions()
    {
        IsFileNameExtensions = !IsFileNameExtensions;
    }

    [RelayCommand]
    private void ToggleHiddenItems()
    {
        IsHiddenItems = !IsHiddenItems;
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var rootNodes = await _virtualExplorerService.GetRootAsync();
            RootItems.Clear();
            foreach (var node in rootNodes.OrderBy(x => x.SortOrder))
            {
                RootItems.Add(node);
            }

            var thisPcNodesTask = _virtualExplorerService.GetChildrenAsync("This PC");
            var networkNodesTask = _virtualExplorerService.GetChildrenAsync("Network");
            await Task.WhenAll(thisPcNodesTask, networkNodesTask);

            var thisPcNodes = await thisPcNodesTask;
            var networkNodes = await networkNodesTask;

            await BuildNavigationTreeAsync(thisPcNodes, networkNodes);
            BuildThisPcSections(thisPcNodes, networkNodes);

            if (Tabs.Count == 0)
            {
                Tabs.Add(new ExplorerTabItem
                {
                    Title = "This PC",
                    Path = "This PC",
                    IsActive = true
                });
            }

            ActiveTab = Tabs[0];
            await NavigateToAsync("This PC", true);
        }
        catch (Exception ex)
        {
            _dialogService.Error($"Không thể khởi tạo Virtual File Center.{Environment.NewLine}{ex.Message}");
        }
        finally
        {
            IsBusy = false;
            _initializeTcs.TrySetResult(true);
        }
    }

    public async Task NavigateFromExternalAsync(string path)
    {
        await _initializeTcs.Task;
        var normalized = NormalizePath(path);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return;
        }

        await NavigateToAsync(normalized, true);
    }

    private async Task BuildNavigationTreeAsync(
        IReadOnlyList<VirtualExplorerNodeDto>? thisPcNodes = null,
        IReadOnlyList<VirtualExplorerNodeDto>? networkNodes = null)
    {
        _navigationVisitedPaths.Clear();
        NavigationNodes.Clear();

        var homeNode = new ExplorerNavNode("nav-home", "Home", "\uE80F", "Desktop", false, HomeIconPath) { IsExpanded = false };
        var galleryNode = new ExplorerNavNode("nav-gallery", "Gallery", "\uE91B", "Desktop", false, GalleryIconPath) { IsExpanded = false };
        var workspaceNode = new ExplorerNavNode("nav-workspace", "Admin Workspace", "\uE7C3", "Desktop", false, WorkspaceIconPath) { IsExpanded = false };
        NavigationNodes.Add(homeNode);
        NavigationNodes.Add(galleryNode);
        NavigationNodes.Add(workspaceNode);

        var thisPcNode = new ExplorerNavNode("nav-thispc", "This PC", "\uE8B7", "This PC", false, ThisPcIconPath) { IsExpanded = true };
        var driveNodes = thisPcNodes ?? await _virtualExplorerService.GetChildrenAsync("This PC");
        foreach (var drive in driveNodes.Where(x => x.NodeType == VirtualNodeType.Drive).OrderBy(x => x.SortOrder))
        {
            var driveNavNode = CreateNavigationNodeFromDto(drive, $"drive-{drive.Id}");
            AddLazyPlaceholderIfNeeded(driveNavNode);
            thisPcNode.Children.Add(driveNavNode);
        }

        var networkNode = new ExplorerNavNode("nav-network", "Network", "\uE968", "Network", false, NetworkIconPath) { IsExpanded = false };
        var networkChildren = networkNodes ?? await _virtualExplorerService.GetChildrenAsync("Network");
        foreach (var child in networkChildren.OrderBy(x => x.SortOrder))
        {
            var networkChild = CreateNavigationNodeFromDto(child, $"net-{child.Id}");
            AddLazyPlaceholderIfNeeded(networkChild);
            networkNode.Children.Add(networkChild);
        }

        NavigationNodes.Add(thisPcNode);
        NavigationNodes.Add(networkNode);
    }

    private async Task PopulateNavigationChildrenAsync(
        ExplorerNavNode parentNode,
        string parentPath,
        int currentDepth,
        int maxDepth,
        string nodeIdPrefix)
    {
        if (currentDepth > maxDepth || string.IsNullOrWhiteSpace(parentPath))
        {
            return;
        }

        var normalizedPath = NormalizePath(parentPath);
        if (!_navigationVisitedPaths.Add(normalizedPath))
        {
            return;
        }

        var children = await _virtualExplorerService.GetChildrenAsync(normalizedPath);
        foreach (var child in children.OrderBy(x => x.SortOrder))
        {
            var childNode = CreateNavigationNodeFromDto(child, $"{nodeIdPrefix}-{child.Id}");
            parentNode.Children.Add(childNode);

            if (child.NodeType is VirtualNodeType.Folder
                or VirtualNodeType.EntityFolder
                or VirtualNodeType.Drive
                or VirtualNodeType.NetworkItem
                or VirtualNodeType.SystemTool
                or VirtualNodeType.Report)
            {
                await PopulateNavigationChildrenAsync(childNode, child.VirtualPath, currentDepth + 1, maxDepth, $"{nodeIdPrefix}-{child.Id}");
            }
        }
    }

    private ExplorerNavNode CreateNavigationNodeFromDto(VirtualExplorerNodeDto node, string id)
    {
        var navNode = new ExplorerNavNode(
            id,
            node.DisplayName,
            "\uE8B7",
            node.VirtualPath,
            false,
            ResolveNavigationIconPath(node))
        {
            IsExpanded = false
        };

        navNode.HasUnrealizedChildren = node.HasChildren;
        navNode.ChildrenLoaded = false;
        return navNode;
    }

    private static void AddLazyPlaceholderIfNeeded(ExplorerNavNode node)
    {
        if (!node.HasUnrealizedChildren || node.ChildrenLoaded)
        {
            return;
        }

        if (node.Children.Count > 0)
        {
            return;
        }

        node.Children.Add(new ExplorerNavNode($"{node.Id}-placeholder", "Loading...", string.Empty, null, true, null));
    }

    public async Task EnsureNavigationChildrenLoadedAsync(ExplorerNavNode? parentNode)
    {
        if (parentNode is null
            || parentNode.IsHeader
            || parentNode.ChildrenLoaded
            || !parentNode.HasUnrealizedChildren
            || string.IsNullOrWhiteSpace(parentNode.VirtualPath))
        {
            return;
        }

        var normalizedPath = NormalizePath(parentNode.VirtualPath);
        if (!_navigationVisitedPaths.Add(normalizedPath))
        {
            parentNode.ChildrenLoaded = true;
            return;
        }

        var children = await _virtualExplorerService.GetChildrenAsync(normalizedPath);
        parentNode.Children.Clear();
        foreach (var child in children.OrderBy(x => x.SortOrder))
        {
            var childNode = CreateNavigationNodeFromDto(child, $"{parentNode.Id}-{child.Id}");
            AddLazyPlaceholderIfNeeded(childNode);
            parentNode.Children.Add(childNode);
        }

        parentNode.ChildrenLoaded = true;
    }

    private static string ResolveNavigationIconPath(VirtualExplorerNodeDto node)
    {
        var customIcon = ResolveCustomIconPath(node.Icon);
        if (!string.IsNullOrWhiteSpace(customIcon))
        {
            return customIcon;
        }

        return node.NodeType switch
        {
            VirtualNodeType.Drive => DriveIconPath,
            VirtualNodeType.NetworkItem => NetworkIconPath,
            VirtualNodeType.Document => IconRoot + @"\files\text.ico",
            VirtualNodeType.Report => IconRoot + @"\files\text.ico",
            _ => IconRoot + @"\folders\folder.ico"
        };
    }

    private static string? ResolveCustomIconPath(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return null;
        }

        if (Path.IsPathRooted(icon) && File.Exists(icon))
        {
            return icon;
        }

        var candidates = new[]
        {
            Path.Combine(IconRoot, "folders", icon),
            Path.Combine(IconRoot, "files", icon),
            Path.Combine(IconRoot, "devices", "drives", icon),
            Path.Combine(IconRoot, "applications", icon),
            Path.Combine(IconRoot, icon)
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private void BuildThisPcSections(
        IReadOnlyList<VirtualExplorerNodeDto>? thisPcNodes = null,
        IReadOnlyList<VirtualExplorerNodeDto>? networkNodes = null)
    {
        DriveItems.Clear();
        NetworkLocationItems.Clear();

        var drives = thisPcNodes ?? Array.Empty<VirtualExplorerNodeDto>();
        foreach (var drive in drives.Where(x => x.NodeType == VirtualNodeType.Drive).OrderBy(x => x.SortOrder))
        {
            DriveItems.Add(MapDriveItem(drive));
        }

        var networkLocations = networkNodes ?? Array.Empty<VirtualExplorerNodeDto>();
        foreach (var location in networkLocations.OrderBy(x => x.SortOrder))
        {
            NetworkLocationItems.Add(new ExplorerDriveItemModel
            {
                Id = location.Id,
                DisplayName = location.DisplayName,
                Description = "Virtual network endpoint",
                IconGlyph = "\uE968",
                IconPath = NetworkIconPath,
                IsNetworkLocation = true,
                VirtualPath = location.VirtualPath,
                UsedPercent = 0.45
            });
        }
    }

    private async Task NavigateToAsync(string path, bool pushHistory)
    {
        IsBusy = true;
        try
        {
            var normalized = NormalizePath(path);
            var children = await _virtualExplorerService.GetChildrenAsync(normalized);
            _currentItemsSource.Clear();
            _currentItemsSource.AddRange(children);
            ApplyCurrentItemsProjection();

            CurrentPath = normalized;
            IsSearchResult = false;
            IsThisPcView = normalized.Equals("This PC", StringComparison.OrdinalIgnoreCase);
            CurrentItemCount = IsThisPcView ? DriveItems.Count + NetworkLocationItems.Count : CurrentItems.Count;
            HasNoItems = CurrentItemCount == 0;
            StatusText = $"{CurrentItemCount} items";
            SelectedItem = null;

            BuildBreadcrumb(normalized);
            UpdateNavigationSelection(normalized);

            if (pushHistory)
            {
                PushHistory(normalized);
            }

            UpdateNavigationState();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdatePreviewAsync(VirtualExplorerNodeDto? node)
    {
        if (node is null)
        {
            ResetPreview();
            return;
        }

        if (node.DocumentId.HasValue)
        {
            var doc = await _businessDocumentService.GetDocumentAsync(node.DocumentId.Value);
            if (doc is not null)
            {
                PreviewTitle = doc.Title;
                PreviewCode = doc.Code;
                PreviewDate = doc.DocumentDate.ToString("dd/MM/yyyy");
                PreviewEntity = $"{doc.EntityType} - {doc.EntityId}";
                PreviewAmount = $"{doc.Amount:N0} {doc.Currency}";
                PreviewStatus = doc.Status.ToString();
                IsPreviewDocument = true;
                return;
            }

            // Generated virtual documents (e.g. approved receipt files) may not exist in BusinessDocument store.
            // Fallback to domain source to keep preview panel accurate.
            var code = ExtractDocumentCode(node.Name ?? node.DisplayName ?? string.Empty);
            if (code.StartsWith("PN-", StringComparison.OrdinalIgnoreCase))
            {
                var receipts = await _inventoryAppService.SearchReceiptsAsync(null, null, code, null);
                var receipt = receipts.FirstOrDefault(x => string.Equals(x.ReceiptNo, code, StringComparison.OrdinalIgnoreCase));
                if (receipt is not null)
                {
                    PreviewTitle = $"{receipt.ReceiptNo} - Phiếu nhập {receipt.ReceiptNo}";
                    PreviewCode = receipt.ReceiptNo;
                    PreviewDate = receipt.ReceiptDate.ToString("dd/MM/yyyy");
                    PreviewEntity = "Supplier";
                    PreviewAmount = $"{receipt.TotalAmount:N0} VND";
                    PreviewStatus = receipt.IsApproved ? "Đã duyệt" : "Chờ duyệt";
                    IsPreviewDocument = true;
                    return;
                }
            }

            if (code.StartsWith("PC-", StringComparison.OrdinalIgnoreCase))
            {
                var voucher = await TryResolvePaymentVoucherFromDocumentNodeAsync(node);
                if (voucher is not null)
                {
                    PreviewTitle = $"{voucher.DocumentNo} - {voucher.Note}";
                    PreviewCode = voucher.DocumentNo;
                    PreviewDate = voucher.Date.ToString("dd/MM/yyyy HH:mm");
                    PreviewEntity = "Supplier";
                    PreviewAmount = $"{voucher.Amount:N0} VND";
                    PreviewStatus = voucher.Status;
                    IsPreviewDocument = true;
                    return;
                }
            }

            if (code.StartsWith("DCN-", StringComparison.OrdinalIgnoreCase))
            {
                var statement = await TryResolvePayableDebtStatementFromDocumentNodeAsync(node);
                if (statement is not null)
                {
                    PreviewTitle = statement.Title;
                    PreviewCode = statement.Code;
                    PreviewDate = statement.ToDate.ToString("dd/MM/yyyy");
                    PreviewEntity = $"Supplier - {statement.SupplierName}";
                    PreviewAmount = $"{statement.ClosingBalance:N0} VND";
                    PreviewStatus = "Đối chiếu công nợ";
                    IsPreviewDocument = true;
                    return;
                }
            }
        }

        PreviewTitle = node.DisplayName ?? string.Empty;
        PreviewCode = node.Name ?? string.Empty;
        PreviewDate = node.DateModified.HasValue ? node.DateModified.Value.ToString("dd/MM/yyyy HH:mm") : "-";
        PreviewEntity = node.EntityType?.ToString() ?? "-";
        PreviewAmount = "-";
        PreviewStatus = node.NodeType == VirtualNodeType.Document ? "Active" : node.NodeType.ToString();
        IsPreviewDocument = false;
    }

    private void ApplyCurrentItemsProjection()
    {
        IEnumerable<VirtualExplorerNodeDto> query = _currentItemsSource;

        query = CurrentSortBy switch
        {
            "DateModified" => IsSortDescending
                ? query.OrderByDescending(x => x.DateModified ?? DateTime.MinValue).ThenBy(x => x.DisplayName)
                : query.OrderBy(x => x.DateModified ?? DateTime.MinValue).ThenBy(x => x.DisplayName),
            "Type" => IsSortDescending
                ? query.OrderByDescending(x => x.NodeType.ToString()).ThenBy(x => x.DisplayName)
                : query.OrderBy(x => x.NodeType.ToString()).ThenBy(x => x.DisplayName),
            "Size" => IsSortDescending
                ? query.OrderByDescending(GetPseudoSize).ThenBy(x => x.DisplayName)
                : query.OrderBy(GetPseudoSize).ThenBy(x => x.DisplayName),
            _ => IsSortDescending
                ? query.OrderByDescending(x => x.DisplayName)
                : query.OrderBy(x => x.DisplayName)
        };

        CurrentItems.Clear();
        foreach (var node in query)
        {
            CurrentItems.Add(node);
        }

        CurrentItemCount = IsThisPcView ? DriveItems.Count + NetworkLocationItems.Count : CurrentItems.Count;
        HasNoItems = CurrentItemCount == 0;
        StatusText = $"{CurrentItemCount} items";
    }

    private static long GetPseudoSize(VirtualExplorerNodeDto node)
    {
        if (node.DocumentId.HasValue)
        {
            return 1_024_000;
        }

        return node.HasChildren ? 32_768 : 4_096;
    }

    private void ResetPreview()
    {
        PreviewTitle = "Select a file to preview.";
        PreviewCode = "-";
        PreviewDate = "-";
        PreviewEntity = "-";
        PreviewAmount = "-";
        PreviewStatus = "-";
        IsPreviewDocument = false;
    }

    private async Task SearchWithDebounceAsync(string keyword)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        var token = _searchCts.Token;
        try
        {
            await Task.Delay(280, token);
            if (token.IsCancellationRequested)
            {
                return;
            }

            await SearchAsync();
        }
        catch (TaskCanceledException)
        {
            // ignore
        }
    }

    private void PushHistory(string path)
    {
        if (_historyIndex >= 0 && _historyIndex < _history.Count
            && _history[_historyIndex].Equals(path, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_historyIndex < _history.Count - 1)
        {
            _history.RemoveRange(_historyIndex + 1, _history.Count - _historyIndex - 1);
        }

        _history.Add(path);
        _historyIndex = _history.Count - 1;
    }

    private void UpdateNavigationSelection(string path)
    {
        SelectedNavigationNode = FindNavigationNode(path);
    }

    private ExplorerNavNode? FindNavigationNode(string path)
    {
        foreach (var node in NavigationNodes)
        {
            if (string.Equals(node.VirtualPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            var child = node.Children.FirstOrDefault(x => string.Equals(x.VirtualPath, path, StringComparison.OrdinalIgnoreCase));
            if (child is not null)
            {
                return child;
            }
        }

        return null;
    }

    private void BuildBreadcrumb(string path)
    {
        BreadcrumbItems.Clear();

        var normalized = NormalizePath(path);
        if (normalized.StartsWith("Search\\", StringComparison.OrdinalIgnoreCase))
        {
            BreadcrumbItems.Add(new ExplorerBreadcrumbSegment("Desktop", "Desktop", "\uE80F", DesktopIconPath));
            BreadcrumbItems.Add(new ExplorerBreadcrumbSegment("Search", "This PC", "\uE721", SearchIconPath) { ShowSeparator = true });
            return;
        }

        BreadcrumbItems.Add(new ExplorerBreadcrumbSegment("Desktop", "Desktop", "\uE80F", DesktopIconPath));
        var parts = normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        var current = "";
        for (var i = 0; i < parts.Length; i++)
        {
            current = i == 0 ? parts[i] : $"{current}\\{parts[i]}";
            var icon = i == 0 && parts[i].Equals("This PC", StringComparison.OrdinalIgnoreCase) ? "\uE8B7" : "\uE8B7";
            var iconPath = i == 0 && parts[i].Equals("This PC", StringComparison.OrdinalIgnoreCase)
                ? ThisPcIconPath
                : parts[i].Contains(':') ? DriveIconPath : IconRoot + @"\folders\folder.ico";
            BreadcrumbItems.Add(new ExplorerBreadcrumbSegment(parts[i], current, icon, iconPath) { ShowSeparator = true });
        }
    }

    private void UpdateNavigationState()
    {
        CanGoBack = _historyIndex > 0;
        CanGoForward = _historyIndex >= 0 && _historyIndex < _history.Count - 1;
        CanGoUp = !string.IsNullOrWhiteSpace(CurrentPath)
                  && !CurrentPath.Equals("This PC", StringComparison.OrdinalIgnoreCase)
                  && !CurrentPath.Equals("Desktop", StringComparison.OrdinalIgnoreCase)
                  && !CurrentPath.Equals("Network", StringComparison.OrdinalIgnoreCase);

        BackCommand.NotifyCanExecuteChanged();
        ForwardCommand.NotifyCanExecuteChanged();
        UpCommand.NotifyCanExecuteChanged();
    }

    private static ExplorerDriveItemModel MapDriveItem(VirtualExplorerNodeDto drive)
    {
        var (description, percent, alert) = drive.DisplayName switch
        {
            "C: SYSTEM" => ("128 GB free of 256 GB", 0.50, false),
            "D: ACCOUNTING" => ("2.4 GB metadata / 120,450 documents", 0.66, false),
            "E: SALES" => ("1.1 GB metadata / 86,120 documents", 0.58, false),
            "F: HR" => ("960 MB metadata / 54,880 documents", 0.47, false),
            "G: LOGS" => ("12.7 GB logs / 3,102,882 entries", 0.83, true),
            _ => ("Virtual drive metadata", 0.40, false)
        };

        return new ExplorerDriveItemModel
        {
            Id = drive.Id,
            DisplayName = drive.DisplayName,
            Description = description,
            IconGlyph = "\uE7F1",
            IconPath = DriveIconPath,
            UsedPercent = percent,
            IsAlert = alert,
            VirtualPath = drive.VirtualPath
        };
    }

    private static string GetParentPath(string path)
    {
        var normalized = NormalizePath(path);
        var index = normalized.LastIndexOf('\\');
        return index <= 0 ? string.Empty : normalized[..index];
    }

    private static string NormalizePath(string path)
    {
        return (path ?? string.Empty).Trim().Replace('/', '\\');
    }

    private static string GetLastSegment(string path)
    {
        var normalized = NormalizePath(path);
        var parts = normalized.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "This PC" : parts[^1];
    }

    private static string BuildTabTitle(string path)
    {
        var title = GetLastSegment(path);
        return string.IsNullOrWhiteSpace(title) ? "This PC" : title;
    }

    private async Task<bool> EnsurePermissionAsync(Permission permission, SecurityEventType denyEvent, string action)
    {
        if (_authorizationService.HasPermission(permission))
        {
            return true;
        }

        await _securityAuditService.LogAsync(denyEvent, "VirtualFileCenter", "Permission", action, $"Từ chối thao tác: {action}");
        _notificationService.Show("Bạn không có quyền thực hiện thao tác này.");
        return false;
    }

    private async Task<bool> TryOpenPayableDebtStatementDetailFromDocumentAsync(VirtualExplorerNodeDto node)
    {
        var summary = await TryResolvePayableDebtStatementSummaryFromDocumentNodeAsync(node);
        if (summary is null)
        {
            return false;
        }

        var period = PromptPayableDebtStatementPeriod(summary.SupplierCode, summary.SupplierName);
        if (period is null)
        {
            return true;
        }

        var statement = await TryResolvePayableDebtStatementFromDocumentNodeAsync(node, period.Value.FromDate, period.Value.ToDate);
        if (statement is null)
        {
            return false;
        }

        var moduleKey = $"payable-debt-statement-{statement.SupplierCode.Trim().ToLowerInvariant()}-{statement.FromDate:yyyyMMdd}-{statement.ToDate:yyyyMMdd}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            moduleKey,
            $"Đối chiếu công nợ {statement.SupplierCode}",
            "\uE8C8",
            "Đối chiếu công nợ phải trả",
            "#8B5CF6",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\payment-voucher.ico");

        var transactionsToDate = statement.Transactions
            .Where(x => x.TransactionDate <= statement.ToDate)
            .ToList();

        var vm = new PayableDebtDetailModuleViewModel(
            statement.SupplierCode,
            statement.SupplierName,
            transactionsToDate,
            statement.FromDate,
            statement.ToDate,
            _windowManagerService,
            moduleKey,
            includePaymentRows: true);

        vm.RequestClose += () =>
        {
            var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
            if (opened is not null)
            {
                _windowManagerService.Close(opened);
            }
        };

        _windowManagerService.OpenOrActivate(module, () => vm);
        return true;
    }

    private async Task<bool> TryOpenSupplierQuickCheckFromDocumentAsync(VirtualExplorerNodeDto node)
    {
        var quickCheck = await TryResolveSupplierQuickCheckAsync(node);
        if (quickCheck is null)
        {
            return false;
        }

        var period = PromptPayableDebtStatementPeriod(
            quickCheck.SupplierCode,
            quickCheck.SupplierName,
            quickCheck.Kind == SupplierQuickCheckKind.Import
                ? "Chọn kỳ kiểm tra phiếu nhập"
                : "Chọn kỳ kiểm tra thanh toán - tạm ứng",
            quickCheck.Kind == SupplierQuickCheckKind.Import
                ? "Chọn kỳ kiểm tra phiếu nhập"
                : "Chọn kỳ kiểm tra thanh toán - tạm ứng",
            quickCheck.Kind == SupplierQuickCheckKind.Import
                ? "Dữ liệu kiểm tra sẽ gồm các phiếu nhập trong khoảng ngày đã chọn."
                : "Dữ liệu kiểm tra sẽ gồm các phiếu chi và tạm ứng trong khoảng ngày đã chọn.",
            "Xem danh sách");
        if (period is null)
        {
            return true;
        }

        var transactions = await _payableService.GetTransactionsBySupplierAsync(
            quickCheck.SupplierId,
            period.Value.FromDate,
            period.Value.ToDate);

        var moduleKey = $"supplier-quick-check-{quickCheck.Kind.ToString().ToLowerInvariant()}-{quickCheck.SupplierCode.Trim().ToLowerInvariant()}-{period.Value.FromDate:yyyyMMdd}-{period.Value.ToDate:yyyyMMdd}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var title = quickCheck.Kind == SupplierQuickCheckKind.Import
            ? $"Kiểm tra phiếu nhập {quickCheck.SupplierCode}"
            : $"Kiểm tra thanh toán {quickCheck.SupplierCode}";
        var module = new AppModuleInfo(
            moduleKey,
            title,
            "\uE8A5",
            "Kiểm tra nhanh chứng từ nhà cung cấp",
            quickCheck.Kind == SupplierQuickCheckKind.Import ? "#2563EB" : "#F2994A",
            isPinned: false,
            showOnDesktop: false,
            iconPath: quickCheck.Kind == SupplierQuickCheckKind.Import
                ? @"D:\ASMILE-BO\Windows_Icon\Icons\applications\import.ico"
                : @"D:\ASMILE-BO\Windows_Icon\Icons\applications\payment-voucher.ico");

        var vm = new SupplierQuickCheckModuleViewModel(
            title,
            quickCheck.SupplierCode,
            quickCheck.SupplierName,
            period.Value.FromDate,
            period.Value.ToDate,
            transactions,
            quickCheck.Kind,
            _windowManagerService,
            moduleKey);
        vm.RequestClose += () =>
        {
            var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
            if (opened is not null)
            {
                _windowManagerService.Close(opened);
            }
        };

        _windowManagerService.OpenOrActivate(module, () => vm);
        return true;
    }

    private async Task<PayableDebtStatementExport?> TryResolvePayableDebtStatementFromDocumentNodeAsync(VirtualExplorerNodeDto node, DateTime? selectedFromDate = null, DateTime? selectedToDate = null)
    {
        var summary = await TryResolvePayableDebtStatementSummaryFromDocumentNodeAsync(node);
        if (summary is null)
        {
            return null;
        }

        var code = ExtractDocumentCode(node.Name ?? node.DisplayName ?? string.Empty);
        var fromDate = selectedFromDate?.Date ?? DateTime.MinValue.Date;
        var toDate = selectedToDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Today.Date.AddDays(1).AddTicks(-1);
        var transactions = await _payableService.GetTransactionsBySupplierAsync(summary.SupplierId, null, toDate);
        var ordered = transactions
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.DocumentNo)
            .ToList();

        var openingBalance = ordered
            .Where(x => x.TransactionDate < fromDate)
            .Sum(x => x.CreditAmount - x.DebitAmount);
        var periodPayable = ordered
            .Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate)
            .Sum(x => x.CreditAmount);
        var periodPaid = ordered
            .Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate)
            .Sum(x => x.DebitAmount);
        var closingBalance = Math.Max(0, openingBalance + periodPayable - periodPaid);

        return new PayableDebtStatementExport(
            code,
            $"Đối chiếu công nợ {fromDate:dd/MM/yyyy} - {toDate:dd/MM/yyyy} - {summary.SupplierName}",
            summary.SupplierCode,
            summary.SupplierName,
            fromDate,
            toDate.Date,
            Math.Max(0, openingBalance),
            periodPayable,
            periodPaid,
            closingBalance,
            ordered.Where(x => x.TransactionDate >= fromDate && x.TransactionDate <= toDate).ToList());
    }

    private async Task<PayableSummaryDto?> TryResolvePayableDebtStatementSummaryFromDocumentNodeAsync(VirtualExplorerNodeDto node)
    {
        var rawName = node.Name ?? node.DisplayName ?? string.Empty;
        var code = ExtractDocumentCode(rawName);
        if (!code.StartsWith("DCN-", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var supplierNameFromPath = TryExtractSupplierNameFromVirtualPath(node.VirtualPath);
        var summaries = await _payableService.GetDebtSummariesAsync(new PayableFilterDto());
        return summaries.FirstOrDefault(x =>
                string.Equals(x.SupplierName?.Trim(), supplierNameFromPath.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? summaries.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.SupplierCode) &&
                code.Contains(x.SupplierCode, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<SupplierQuickCheckDescriptor?> TryResolveSupplierQuickCheckAsync(VirtualExplorerNodeDto node)
    {
        var code = ExtractDocumentCode(node.Name ?? node.DisplayName ?? string.Empty);
        SupplierQuickCheckKind kind;
        if (code.StartsWith("THPN-", StringComparison.OrdinalIgnoreCase))
        {
            kind = SupplierQuickCheckKind.Import;
        }
        else if (code.StartsWith("THTT-", StringComparison.OrdinalIgnoreCase))
        {
            kind = SupplierQuickCheckKind.Payment;
        }
        else
        {
            return null;
        }

        var supplierNameFromPath = TryExtractSupplierNameFromVirtualPath(node.VirtualPath);
        var summaries = await _payableService.GetDebtSummariesAsync(new PayableFilterDto());
        var summary = summaries.FirstOrDefault(x =>
                string.Equals(x.SupplierName?.Trim(), supplierNameFromPath.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? summaries.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.SupplierCode) &&
                code.Contains(x.SupplierCode, StringComparison.OrdinalIgnoreCase));

        return summary is null
            ? null
            : new SupplierQuickCheckDescriptor(summary.SupplierId, summary.SupplierCode, summary.SupplierName, kind);
    }

    private (DateTime FromDate, DateTime ToDate)? PromptPayableDebtStatementPeriod(
        string supplierCode,
        string supplierName,
        string windowTitle = "Chọn kỳ đối chiếu công nợ",
        string headerText = "Chọn kỳ đối chiếu công nợ",
        string hintText = "Dữ liệu đối chiếu sẽ gồm phiếu nhập, phiếu chi và số dư còn lại trong khoảng ngày đã chọn.",
        string applyText = "Xem đối chiếu")
    {
        var today = DateTime.Today;
        var dialog = new PayableDebtStatementFilterDialog(
            supplierCode,
            supplierName,
            new DateTime(today.Year, today.Month, 1),
            today,
            windowTitle,
            headerText,
            hintText,
            applyText);

        var owner = System.Windows.Application.Current?.Windows
            .OfType<Window>()
            .FirstOrDefault(x => x.IsActive)
            ?? System.Windows.Application.Current?.MainWindow;
        if (owner is not null && !ReferenceEquals(owner, dialog))
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true
            ? (dialog.FromDate, dialog.ToDate)
            : null;
    }

    private sealed record SupplierQuickCheckDescriptor(
        string SupplierId,
        string SupplierCode,
        string SupplierName,
        SupplierQuickCheckKind Kind);

    private async Task ExportPayableDebtStatementPdfAsync(PayableDebtStatementExport statement)
    {
        var saveDialog = new SaveFileDialog
        {
            Title = "Xuất đối chiếu công nợ ra PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = $"{_pathSecurityService.SanitizeFileName(statement.Code)}.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (saveDialog.ShowDialog() != true)
        {
            return;
        }

        if (!_pathSecurityService.IsAllowedExtension(saveDialog.FileName))
        {
            _notificationService.Show("Định dạng file xuất không hợp lệ.");
            return;
        }

        var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"asmile-{statement.Code}-{Guid.NewGuid():N}.html");
        try
        {
            var html = BuildPayableDebtStatementExportHtml(statement);
            await File.WriteAllTextAsync(tempHtmlPath, html, new UTF8Encoding(false));

            var browserExe = ResolvePdfBrowserExecutable();
            if (string.IsNullOrWhiteSpace(browserExe))
            {
                _dialogService.Error("Xuất PDF thất bại.\nKhông tìm thấy Microsoft Edge/Google Chrome để in PDF.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = browserExe,
                Arguments = $"--headless --disable-gpu --print-to-pdf-no-header --print-to-pdf=\"{saveDialog.FileName}\" \"file:///{tempHtmlPath.Replace("\\", "/")}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _notificationService.Show("Không thể khởi chạy trình xuất PDF.");
                return;
            }

            await process.WaitForExitAsync();
            if (process.ExitCode == 0 && File.Exists(saveDialog.FileName))
            {
                _notificationService.Show("Đã xuất đối chiếu công nợ ra PDF.");
                await _securityAuditService.LogAsync(SecurityEventType.ExportDocument, "VirtualFileCenter", "PayableDebtStatement", statement.Code, "Xuất PDF đối chiếu công nợ phải trả");
            }
            else
            {
                _dialogService.Error("Xuất PDF thất bại. Trình duyệt không tạo được file PDF.");
            }
        }
        finally
        {
            try
            {
                if (File.Exists(tempHtmlPath))
                {
                    File.Delete(tempHtmlPath);
                }
            }
            catch
            {
                // ignore temp cleanup failures
            }
        }
    }

    private async Task<bool> TryOpenReceiptDetailFromDocumentAsync(VirtualExplorerNodeDto node)
    {
        var receipt = await TryResolveReceiptFromDocumentNodeAsync(node);
        if (receipt is null)
        {
            return false;
        }

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
        return true;
    }

    private async Task<StockReceiptDto?> TryResolveReceiptFromDocumentNodeAsync(VirtualExplorerNodeDto node)
    {
        var code = ExtractDocumentCode(node.Name ?? node.DisplayName ?? string.Empty);
        if (!code.StartsWith("PN-", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var receipts = await _inventoryAppService.SearchReceiptsAsync(null, null, code, null);
        return receipts.FirstOrDefault(x => string.Equals(x.ReceiptNo, code, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> TryOpenPaymentVoucherDetailFromDocumentAsync(VirtualExplorerNodeDto node)
    {
        var voucher = await TryResolvePaymentVoucherFromDocumentNodeAsync(node);
        if (voucher is null)
        {
            return false;
        }

        var safeDocNo = string.IsNullOrWhiteSpace(voucher.DocumentNo)
            ? Guid.NewGuid().ToString("N")
            : voucher.DocumentNo.Replace(" ", string.Empty);
        var moduleKey = $"payment-voucher-detail-{safeDocNo.ToLowerInvariant()}";

        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            moduleKey,
            $"Chi tiết {voucher.DocumentNo}",
            "\uE8C8",
            "Chi tiết phiếu chi",
            "#8B5CF6",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\payment-voucher.ico");

        var vm = new PaymentVoucherDetailModuleViewModel(voucher, _windowManagerService, moduleKey);
        vm.RequestClose += () =>
        {
            var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == moduleKey);
            if (opened is not null)
            {
                _windowManagerService.Close(opened);
            }
        };

        _windowManagerService.OpenOrActivate(module, () => vm);
        return true;
    }

    private async Task<PaymentVoucherRow?> TryResolvePaymentVoucherFromDocumentNodeAsync(VirtualExplorerNodeDto node)
    {
        var rawName = node.Name ?? node.DisplayName ?? string.Empty;
        var parsedCode = ExtractDocumentCode(rawName);
        var entityIdCode = string.IsNullOrWhiteSpace(node.EntityId?.ToString()) ? string.Empty : node.EntityId!.ToString()!;
        var candidateCodes = new List<string>();
        if (!string.IsNullOrWhiteSpace(parsedCode))
        {
            candidateCodes.Add(parsedCode);
        }

        if (!string.IsNullOrWhiteSpace(entityIdCode))
        {
            candidateCodes.Add(entityIdCode);
        }

        var pcCode = candidateCodes
            .FirstOrDefault(c => c.StartsWith("PC-", StringComparison.OrdinalIgnoreCase))
            ?? candidateCodes.FirstOrDefault()
            ?? string.Empty;

        var approved = !string.IsNullOrWhiteSpace(pcCode)
            ? await _payableService.GetPaymentVoucherApprovedAsync(pcCode)
            : null;
        var summaries = await _payableService.GetDebtSummariesAsync(new PayableFilterDto());
        foreach (var summary in summaries)
        {
            var txs = await _payableService.GetTransactionsBySupplierAsync(summary.SupplierId, null, null);
            var tx = txs
                .Where(t =>
                    (!string.IsNullOrWhiteSpace(pcCode) &&
                     string.Equals(t.DocumentNo, pcCode, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrWhiteSpace(parsedCode) &&
                     (t.Description?.Contains(parsedCode, StringComparison.OrdinalIgnoreCase) ?? false)) ||
                    (!string.IsNullOrWhiteSpace(rawName) &&
                     (t.Description?.Contains(rawName, StringComparison.OrdinalIgnoreCase) ?? false)))
                .OrderByDescending(t => t.TransactionDate)
                .FirstOrDefault();
            if (tx is null)
            {
                continue;
            }

            var amount = tx.CreditAmount > 0 ? tx.CreditAmount : tx.DebitAmount;
            var documentNo = !string.IsNullOrWhiteSpace(tx.DocumentNo) ? tx.DocumentNo : (string.IsNullOrWhiteSpace(pcCode) ? parsedCode : pcCode);
            return new PaymentVoucherRow
            {
                SupplierId = tx.SupplierId,
                Date = tx.TransactionDate,
                DocumentNo = documentNo,
                SupplierCode = tx.SupplierCode,
                SupplierName = tx.SupplierName,
                Amount = amount,
                Method = "Chuyển khoản",
                DebitAccountCode = "331",
                CreditAccountCode = "111",
                Note = string.IsNullOrWhiteSpace(tx.Description) ? $"Ghi chú: thanh toán chứng từ {documentNo}" : tx.Description,
                WorkflowStatus = approved == true ? VoucherWorkflowStatus.Approved : VoucherWorkflowStatus.Pending,
                Status = approved == true ? "Đã duyệt" : "Chờ duyệt"
            };
        }

        // Fallback: trong một số luồng VFC có chứng từ file nhưng dữ liệu công nợ
        // chưa đồng bộ transaction theo DocumentNo. Vẫn cho phép xuất PDF từ metadata node.
        var supplierNameFromPath = TryExtractSupplierNameFromVirtualPath(node.VirtualPath);
        var noteFromName = TryExtractVoucherNote(node.Name ?? node.DisplayName ?? string.Empty);
        var fallbackAmount = ExtractAmountFromNodeName(node.Name ?? node.DisplayName ?? string.Empty);

        var fallbackCode = !string.IsNullOrWhiteSpace(pcCode)
            ? pcCode
            : (!string.IsNullOrWhiteSpace(parsedCode) ? parsedCode : $"PC-{DateTime.Now:yyyyMMddHHmmss}");

        return new PaymentVoucherRow
        {
            SupplierId = node.EntityId?.ToString() ?? string.Empty,
            Date = node.DateModified ?? DateTime.Now,
            DocumentNo = fallbackCode,
            SupplierCode = string.Empty,
            SupplierName = string.IsNullOrWhiteSpace(supplierNameFromPath) ? "Nhà cung cấp" : supplierNameFromPath,
            Amount = fallbackAmount <= 0 ? 0m : fallbackAmount,
            Method = "Chuyển khoản",
            DebitAccountCode = "331",
            CreditAccountCode = "111",
            Note = string.IsNullOrWhiteSpace(noteFromName) ? $"Ghi chú: thanh toán chứng từ {fallbackCode}" : noteFromName,
            WorkflowStatus = approved == true ? VoucherWorkflowStatus.Approved : VoucherWorkflowStatus.Pending,
            Status = approved == true ? "Đã duyệt" : "Chờ duyệt"
        };
    }

    private static string TryExtractSupplierNameFromVirtualPath(string? virtualPath)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
        {
            return string.Empty;
        }

        var marker = "Nhà cung cấp\\";
        var idx = virtualPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return string.Empty;
        }

        var rest = virtualPath[(idx + marker.Length)..];
        var segment = rest.Split('\\', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return segment ?? string.Empty;
    }

    private static string TryExtractVoucherNote(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var parts = raw.Split(" - ", 2, StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return string.Empty;
        }

        return parts[1];
    }

    private static decimal ExtractAmountFromNodeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return 0m;
        }

        // Nhận các số dạng 30,000,000 hoặc 30000000 trong tên file.
        var matches = Regex.Matches(raw, @"\d[\d,\.]{2,}");
        if (matches.Count == 0)
        {
            return 0m;
        }

        var candidate = matches[^1].Value
            .Replace(",", string.Empty)
            .Replace(".", string.Empty);

        return decimal.TryParse(candidate, out var amount) ? amount : 0m;
    }

    private static string ExtractDocumentCode(string raw)
    {
        var text = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var first = text.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
        if (Regex.IsMatch(first, @"^[A-Z]{2}-[A-Z0-9]+$", RegexOptions.IgnoreCase))
        {
            return first.ToUpperInvariant();
        }

        var token = first.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? first;
        return token.ToUpperInvariant();
    }

    private void OpenReceiptPrintPreviewModule(StockReceiptDto receipt)
    {
        var previewModuleKey = $"receipt-print-preview-{receipt.Id}";
        var existing = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == previewModuleKey);
        if (existing is not null)
        {
            _windowManagerService.Close(existing);
        }

        var module = new AppModuleInfo(
            previewModuleKey,
            $"Xem trước in {receipt.ReceiptNo}",
            "\uE8A7",
            "Xem trước in phiếu nhập",
            "#3B6EF5",
            isPinned: false,
            showOnDesktop: false,
            iconPath: @"D:\ASMILE-BO\Windows_Icon\Icons\applications\import.ico");

        var previewVm = new StockReceiptPrintPreviewModuleViewModel(
            previewModuleKey,
            receipt.ReceiptNo,
            StockReceiptPrintDocumentFactory.Build(receipt),
            _windowManagerService);

        _windowManagerService.OpenOrActivate(module, () => previewVm);
    }

    private async Task ExportReceiptPdfAsync(StockReceiptDto receipt)
    {
        var saveDialog = new SaveFileDialog
        {
            Title = "Xuất phiếu nhập ra PDF",
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = $"{_pathSecurityService.SanitizeFileName(receipt.ReceiptNo)}.pdf",
            DefaultExt = ".pdf",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (saveDialog.ShowDialog() != true)
        {
            return;
        }
        if (!_pathSecurityService.IsAllowedExtension(saveDialog.FileName))
        {
            _notificationService.Show("Định dạng file xuất không hợp lệ.");
            return;
        }

        var tempHtmlPath = Path.Combine(Path.GetTempPath(), $"asmile-{receipt.ReceiptNo}-{Guid.NewGuid():N}.html");
        try
        {
            var html = BuildReceiptExportHtml(receipt);
            await File.WriteAllTextAsync(tempHtmlPath, html, new UTF8Encoding(false));

            var browserExe = ResolvePdfBrowserExecutable();
            if (string.IsNullOrWhiteSpace(browserExe))
            {
                _dialogService.Error("Xuất PDF thất bại.\nKhông tìm thấy Microsoft Edge/Google Chrome để in PDF.");
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = browserExe,
                Arguments = $"--headless --disable-gpu --print-to-pdf-no-header --print-to-pdf=\"{saveDialog.FileName}\" \"file:///{tempHtmlPath.Replace("\\", "/")}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                _notificationService.Show("Không thể khởi chạy trình xuất PDF.");
                return;
            }

            await process.WaitForExitAsync();
            if (process.ExitCode == 0 && File.Exists(saveDialog.FileName))
            {
                _notificationService.Show("Đã xuất PDF thành công.");
                await _securityAuditService.LogAsync(SecurityEventType.ExportDocument, "VirtualFileCenter", "Receipt", receipt.Id.ToString(), "Xuất PDF phiếu nhập");
            }
            else
            {
                _notificationService.Show("Xuất PDF thất bại.");
            }
        }
        catch
        {
            _dialogService.Error("Xuất PDF thất bại. Vui lòng kiểm tra nhật ký hệ thống.");
            await _securityAuditService.LogAsync(SecurityEventType.SecurityError, "VirtualFileCenter", "Export", receipt.Id.ToString(), "Xuất PDF thất bại");
        }
        finally
        {
            try
            {
                if (File.Exists(tempHtmlPath))
                {
                    File.Delete(tempHtmlPath);
                }
            }
            catch
            {
            }
        }
    }

    private static string BuildReceiptExportHtml(StockReceiptDto receipt)
    {
        static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
        var rows = string.Join(Environment.NewLine, receipt.Details.Select(d => $"""
            <tr>
              <td class="c">{d.LineNo}</td>
              <td>{E(d.ItemCode)}</td>
              <td>{E(d.ItemName)}</td>
              <td>{E(d.Unit)}</td>
              <td>{E(d.WarehouseCode)}</td>
              <td class="r">{d.Quantity:N0}</td>
              <td class="r">{d.UnitPrice:N0}</td>
              <td class="r">{d.Amount:N0}</td>
            </tr>
            """));

        var htmlTemplate = """
        <!doctype html>
        <html lang="vi">
        <head>
          <meta charset="utf-8" />
          <title>Phiếu nhập __RECEIPT_NO__</title>
          <style>
            @page { size: A4; margin: 16mm; }
            body { font-family: "Segoe UI", Arial, sans-serif; color: #0f172a; font-size: 13px; }
            h1 { text-align: center; margin: 8px 0 16px; font-size: 28px; }
            .company { font-weight: 700; font-size: 18px; margin-bottom: 8px; }
            .info { display: grid; grid-template-columns: 1fr 1fr; row-gap: 6px; column-gap: 24px; margin-bottom: 14px; }
            .bold { font-weight: 700; }
            table { width: 100%; border-collapse: collapse; }
            th, td { border: 1px dashed #b8c4d6; padding: 7px 8px; vertical-align: middle; }
            th { background: #f1f5f9; text-align: left; }
            .c { text-align: center; }
            .r { text-align: right; }
          </style>
        </head>
        <body>
          <div class="company">ASMILE BO</div>
          <h1>PHIẾU NHẬP KHO</h1>
          <div class="info">
            <div class="bold">Nhà cung cấp: __SUPPLIER__</div>
            <div></div>
            <div>Số phiếu: __RECEIPT_NO__</div>
            <div>Ngày chứng từ: __DATE__</div>
            <div>Người nhận hàng: __RECEIVER__</div>
            <div>Tổng tiền: __TOTAL__</div>
          </div>
          <table>
            <thead>
              <tr>
                <th style="width:6%">STT</th>
                <th style="width:12%">Mã hàng</th>
                <th style="width:25%">Tên hàng hóa</th>
                <th style="width:10%">ĐV tính</th>
                <th style="width:8%">Kho</th>
                <th style="width:11%">Số lượng</th>
                <th style="width:12%">Đơn giá</th>
                <th style="width:16%">Thành tiền</th>
              </tr>
            </thead>
            <tbody>
              __ROWS__
            </tbody>
          </table>
        </body>
        </html>
        """;

        return htmlTemplate
            .Replace("__RECEIPT_NO__", E(receipt.ReceiptNo))
            .Replace("__SUPPLIER__", E(receipt.SupplierName))
            .Replace("__DATE__", receipt.ReceiptDate.ToString("dd/MM/yyyy"))
            .Replace("__RECEIVER__", E(receipt.ReceiverName))
            .Replace("__TOTAL__", receipt.TotalAmount.ToString("N0"))
            .Replace("__ROWS__", rows);
    }

    private static string BuildPaymentVoucherExportHtml(PaymentVoucherRow voucher)
    {
        static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
        var docNo = E(voucher.DocumentNo);
        var supplierName = E(voucher.SupplierName);
        var supplierCode = E(voucher.SupplierCode);
        var status = E(voucher.Status);
        var method = E(voucher.Method);
        var note = E(string.IsNullOrWhiteSpace(voucher.Note) ? $"Ghi chú: thanh toán chứng từ {voucher.DocumentNo}" : voucher.Note);
        var debitCredit = E($"{voucher.DebitAccountCode}/{voucher.CreditAccountCode}");

        var htmlTemplate = """
        <!doctype html>
        <html lang="vi">
        <head>
          <meta charset="utf-8" />
          <title>Phiếu chi __DOC_NO__</title>
          <style>
            @page { size: A4; margin: 12mm; }
            body { font-family: "Segoe UI", Arial, sans-serif; color: #0f172a; font-size: 13px; }
            h1 { text-align: center; margin: 8px 0 14px; font-size: 44px; letter-spacing: 0.3px; }
            .company { font-weight: 700; font-size: 18px; margin-bottom: 8px; }
            .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 6px 24px; margin-bottom: 14px; }
            .meta .line { white-space: pre-wrap; }
            .meta .strong { font-weight: 700; color: #2563eb; }
            table { width: 100%; border-collapse: collapse; margin-top: 10px; table-layout: fixed; }
            th, td { border: 1px solid #cbd5e1; padding: 8px; vertical-align: top; }
            th { background: #f1f5f9; font-weight: 700; text-align: center; }
            td.r { text-align: right; }
            td.c { text-align: center; }
            .note { white-space: pre-wrap; word-break: normal; overflow-wrap: break-word; }
            .sign { margin-top: 28px; display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; text-align: center; color: #334155; }
            .small { color: #64748b; font-size: 12px; }
          </style>
        </head>
        <body>
          <div class="company">ASMILE BO</div>
          <h1>PHIẾU CHI</h1>
          <div class="meta">
            <div class="line"><strong>Nhà cung cấp:</strong> __SUPPLIER_NAME__ (__SUPPLIER_CODE__)</div>
            <div class="line"><strong>Ngày chứng từ:</strong> __VOUCHER_DATE__</div>
            <div class="line"><strong>Số phiếu:</strong> __DOC_NO__</div>
            <div class="line"><strong>Tổng tiền:</strong> <span class="strong">__AMOUNT__</span></div>
            <div class="line"><strong>Trạng thái:</strong> __STATUS__</div>
            <div class="line"><strong>Tài khoản nợ/có:</strong> __DEBIT_CREDIT__</div>
          </div>
          <table>
            <thead>
              <tr>
                <th style="width:56px;">STT</th>
                <th style="width:180px;">Chứng từ</th>
                <th style="width:300px;">Diễn giải</th>
                <th style="width:150px;">Phương thức</th>
                <th style="width:150px;">Thành tiền</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td class="c">1</td>
                <td>__DOC_NO__</td>
                <td class="note">__NOTE__</td>
                <td>__METHOD__</td>
                <td class="r">__AMOUNT__</td>
              </tr>
            </tbody>
          </table>
          <div class="sign">
            <div>
              <div>Người lập phiếu</div>
              <div class="small">(Ký, ghi rõ họ tên)</div>
            </div>
            <div>
              <div>Kế toán</div>
              <div class="small">(Ký, ghi rõ họ tên)</div>
            </div>
            <div>
              <div>Người nhận tiền</div>
              <div class="small">(Ký, ghi rõ họ tên)</div>
            </div>
          </div>
        </body>
        </html>
        """;

        return htmlTemplate
            .Replace("__DOC_NO__", docNo)
            .Replace("__SUPPLIER_NAME__", supplierName)
            .Replace("__SUPPLIER_CODE__", supplierCode)
            .Replace("__VOUCHER_DATE__", voucher.Date.ToString("dd/MM/yyyy"))
            .Replace("__AMOUNT__", voucher.Amount.ToString("N0"))
            .Replace("__STATUS__", status)
            .Replace("__METHOD__", method)
            .Replace("__NOTE__", note)
            .Replace("__DEBIT_CREDIT__", debitCredit);
    }

    private static string BuildPayableDebtStatementExportHtml(PayableDebtStatementExport statement)
    {
        static string E(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);

        var rows = BuildPayableDebtStatementRows(statement.Transactions)
            .Select((row, index) => $"""
              <tr>
                <td class="c">{index + 1}</td>
                <td>{E(row.TransactionDate.ToString("dd/MM/yyyy"))}</td>
                <td>{E(row.DocumentNo)}</td>
                <td>{E(row.DocumentTypeText)}</td>
                <td>{E(row.Description)}</td>
                <td class="r">{row.PayableAmount:N0}</td>
                <td class="r">{row.PaidAmount:N0}</td>
                <td class="r strong">{row.RemainingAmount:N0}</td>
                <td>{E(row.RemainingAmount <= 0 ? "Đã trả" : "Còn nợ")}</td>
              </tr>
            """);

        var htmlTemplate = """
        <!doctype html>
        <html lang="vi">
        <head>
          <meta charset="utf-8" />
          <title>Đối chiếu công nợ __CODE__</title>
          <style>
            @page { size: A4 landscape; margin: 10mm; }
            body { font-family: "Segoe UI", Arial, sans-serif; color: #0f172a; font-size: 12.5px; }
            h1 { text-align: center; margin: 8px 0 14px; font-size: 30px; letter-spacing: 0.2px; }
            .company { font-weight: 700; font-size: 17px; margin-bottom: 8px; }
            .meta { display: grid; grid-template-columns: 1fr 1fr; gap: 6px 24px; margin-bottom: 12px; }
            .cards { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; margin: 10px 0 12px; }
            .card { border: 1px solid #cbd5e1; border-radius: 8px; padding: 9px; background: #f8fafc; }
            .label { color: #64748b; font-size: 11px; margin-bottom: 4px; }
            .value { font-size: 17px; font-weight: 700; color: #1e40af; }
            table { width: 100%; border-collapse: collapse; table-layout: fixed; }
            th, td { border: 1px solid #cbd5e1; padding: 7px; vertical-align: top; }
            th { background: #e2e8f0; font-weight: 700; text-align: center; }
            td.r { text-align: right; }
            td.c { text-align: center; }
            .strong { font-weight: 700; color: #1d4ed8; }
            .sign { margin-top: 24px; display: grid; grid-template-columns: repeat(3, 1fr); gap: 24px; text-align: center; color: #334155; }
            .small { color: #64748b; font-size: 11px; }
          </style>
        </head>
        <body>
          <div class="company">ASMILE BO</div>
          <h1>BIÊN BẢN ĐỐI CHIẾU CÔNG NỢ</h1>
          <div class="meta">
            <div><strong>Nhà cung cấp:</strong> __SUPPLIER_NAME__ (__SUPPLIER_CODE__)</div>
            <div><strong>Kỳ đối chiếu:</strong> __PERIOD__</div>
            <div><strong>Mã file:</strong> __CODE__</div>
            <div><strong>Ngày lập:</strong> __CREATED_DATE__</div>
          </div>
          <div class="cards">
            <div class="card"><div class="label">Công nợ đầu kỳ</div><div class="value">__OPENING__</div></div>
            <div class="card"><div class="label">Phát sinh phải trả</div><div class="value">__PAYABLE__</div></div>
            <div class="card"><div class="label">Đã trả trong kỳ</div><div class="value">__PAID__</div></div>
            <div class="card"><div class="label">Tồn còn lại</div><div class="value">__CLOSING__</div></div>
          </div>
          <table>
            <thead>
              <tr>
                <th style="width:44px;">STT</th>
                <th style="width:92px;">Ngày</th>
                <th style="width:130px;">Chứng từ</th>
                <th style="width:110px;">Loại</th>
                <th>Diễn giải</th>
                <th style="width:130px;">Phải trả</th>
                <th style="width:130px;">Đã trả</th>
                <th style="width:130px;">Còn lại</th>
                <th style="width:90px;">Trạng thái</th>
              </tr>
            </thead>
            <tbody>
              __ROWS__
            </tbody>
          </table>
          <div class="sign">
            <div>
              <div>Đại diện nhà cung cấp</div>
              <div class="small">(Ký, ghi rõ họ tên)</div>
            </div>
            <div>
              <div>Kế toán công nợ</div>
              <div class="small">(Ký, ghi rõ họ tên)</div>
            </div>
            <div>
              <div>Kế toán trưởng</div>
              <div class="small">(Ký, ghi rõ họ tên)</div>
            </div>
          </div>
        </body>
        </html>
        """;

        return htmlTemplate
            .Replace("__CODE__", E(statement.Code))
            .Replace("__SUPPLIER_NAME__", E(statement.SupplierName))
            .Replace("__SUPPLIER_CODE__", E(statement.SupplierCode))
            .Replace("__PERIOD__", E($"{statement.FromDate:dd/MM/yyyy} - {statement.ToDate:dd/MM/yyyy}"))
            .Replace("__CREATED_DATE__", DateTime.Today.ToString("dd/MM/yyyy"))
            .Replace("__OPENING__", statement.OpeningBalance.ToString("N0"))
            .Replace("__PAYABLE__", statement.PeriodPayable.ToString("N0"))
            .Replace("__PAID__", statement.PeriodPaid.ToString("N0"))
            .Replace("__CLOSING__", statement.ClosingBalance.ToString("N0"))
            .Replace("__ROWS__", string.Join(Environment.NewLine, rows));
    }

    private static IReadOnlyList<PayableDebtStatementRow> BuildPayableDebtStatementRows(IReadOnlyList<PayableTransactionDto> transactions)
    {
        var runningBalance = 0m;
        return transactions
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.DocumentNo)
            .Select(tx =>
            {
                runningBalance = Math.Max(0, runningBalance + tx.CreditAmount - tx.DebitAmount);
                return new PayableDebtStatementRow(
                    tx.TransactionDate,
                    tx.DocumentNo,
                    tx.Description,
                    ResolvePayableDocumentTypeText(tx.DocumentType),
                    Math.Max(0, tx.CreditAmount),
                    Math.Max(0, tx.DebitAmount),
                    runningBalance);
            })
            .ToList();
    }

    private static string ResolvePayableDocumentTypeText(PayableDocumentType documentType) => documentType switch
    {
        PayableDocumentType.ImportVoucher => "Phiếu nhập",
        PayableDocumentType.PurchaseInvoice => "Hóa đơn mua",
        PayableDocumentType.PaymentVoucher => "Phiếu chi",
        PayableDocumentType.AdvancePayment => "Tạm ứng",
        PayableDocumentType.Adjustment => "Điều chỉnh",
        PayableDocumentType.OpeningBalance => "Số dư đầu kỳ",
        _ => documentType.ToString()
    };

    private static (int Year, int Month) ExtractDebtStatementPeriod(string code, string? virtualPath, DateTime? fallbackDate)
    {
        var match = Regex.Match(code ?? string.Empty, @"DCN-(\d{4})(\d{2})-", RegexOptions.IgnoreCase);
        if (match.Success
            && int.TryParse(match.Groups[1].Value, out var y)
            && int.TryParse(match.Groups[2].Value, out var m)
            && m is >= 1 and <= 12)
        {
            return (y, m);
        }

        var parts = NormalizePath(virtualPath ?? string.Empty).Split('\\', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (int.TryParse(parts[i], out y)
                && int.TryParse(parts[i + 1], out m)
                && y >= 2000
                && m is >= 1 and <= 12)
            {
                return (y, m);
            }
        }

        var date = fallbackDate ?? DateTime.Today;
        return (date.Year, date.Month);
    }

    private sealed record PayableDebtStatementExport(
        string Code,
        string Title,
        string SupplierCode,
        string SupplierName,
        DateTime FromDate,
        DateTime ToDate,
        decimal OpeningBalance,
        decimal PeriodPayable,
        decimal PeriodPaid,
        decimal ClosingBalance,
        IReadOnlyList<PayableTransactionDto> Transactions);

    private sealed record PayableDebtStatementRow(
        DateTime TransactionDate,
        string DocumentNo,
        string Description,
        string DocumentTypeText,
        decimal PayableAmount,
        decimal PaidAmount,
        decimal RemainingAmount);

    private static string? ResolvePdfBrowserExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "Edge", "Application", "msedge.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
            "msedge.exe",
            "chrome.exe"
        };

        foreach (var candidate in candidates)
        {
            if (candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && !candidate.Contains(Path.DirectorySeparatorChar))
            {
                return candidate;
            }

            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}






