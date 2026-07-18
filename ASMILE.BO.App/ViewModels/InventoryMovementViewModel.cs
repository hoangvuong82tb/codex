using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class InventoryMovementViewModel : ViewModelBase
{
    private readonly IInventoryMovementService _service;
    private readonly IReportEngine _reportEngine;
    private readonly IImportEngine _importEngine;
    private readonly INotificationService _notification;
    private readonly IDialogService _dialog;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _filterDebounceCts;
    private bool _syncingSelectionFromMovement;
    private bool _suppressFilterRefresh;

    public ObservableCollection<InventoryStockBalanceDto> StockBalances { get; } = new();
    public ObservableCollection<InventoryMovementDto> Movements { get; } = new();
    public ObservableCollection<InventoryMovementDto> ProductMovements { get; } = new();
    public ObservableCollection<InventoryLowStockAlertDto> LowStockAlerts { get; } = new();
    public ObservableCollection<InventoryDocumentDto> Documents { get; } = new();
    public ObservableCollection<InventoryMovementDto> SelectedDocumentLines { get; } = new();
    public TablePagerViewModel<InventoryStockBalanceDto> StockBalancePager { get; } = new() { ItemName = "mặt hàng" };
    public TablePagerViewModel<InventoryMovementDto> MovementPager { get; } = new() { ItemName = "giao dịch" };
    public TablePagerViewModel<InventoryMovementDto> ProductMovementPager { get; } = new() { ItemName = "phát sinh" };
    public TablePagerViewModel<InventoryLowStockAlertDto> LowStockAlertPager { get; } = new() { ItemName = "cảnh báo" };
    public TablePagerViewModel<InventoryDocumentDto> DocumentPager { get; } = new() { ItemName = "chứng từ" };
    public TablePagerViewModel<InventoryMovementDto> StockTakePager { get; } = new() { ItemName = "dòng kiểm kê" };
    public TablePagerViewModel<InventoryMovementDto> RecentMovementsPager { get; } = new() { ItemName = "giao dịch" };
    public ObservableCollection<InventoryWarehouseSummaryDto> WarehouseSummaries { get; } = new();
    public ObservableCollection<MetricPoint> DailyInSeries { get; } = new();
    public ObservableCollection<MetricPoint> DailyOutSeries { get; } = new();
    public ObservableCollection<InventoryStockBalanceDto> TopStockValueItems { get; } = new();
    public ObservableCollection<InventoryMovementDto> RecentMovements { get; } = new();
    public ObservableCollection<InventoryRecentMovementLine> RecentSelectedMovements { get; } = new();
    public ObservableCollection<InventoryStockBalanceDto> RiskItems { get; } = new();
    public ObservableCollection<ImportRowDto> ImportPreviewRows { get; } = new();
    public ObservableCollection<ReportExecutionDto> InventoryReportHistory { get; } = new();

    public ObservableCollection<InventoryDocumentType?> DocumentTypeOptions { get; } = new()
    {
        null,
        InventoryDocumentType.OpeningStock,
        InventoryDocumentType.ImportVoucher,
        InventoryDocumentType.ExportVoucher,
        InventoryDocumentType.AdjustmentIn,
        InventoryDocumentType.AdjustmentOut,
        InventoryDocumentType.TransferIn,
        InventoryDocumentType.TransferOut,
        InventoryDocumentType.StockTake
    };

    public ObservableCollection<string> WarehouseOptions { get; } = new() { "Tất cả kho" };
    public ObservableCollection<string> ItemGroupOptions { get; } = new() { "Tất cả nhóm" };
    public ObservableCollection<string> ReportOptions { get; } = new()
    {
        "Tổng hợp tồn kho",
        "Nhập xuất tồn",
        "Thẻ kho",
        "Cảnh báo tồn thấp",
        "Tồn âm",
        "Phân tích giá vốn"
    };
    public ObservableCollection<string> ReportFormatOptions { get; } = new() { "Excel", "PDF", "CSV" };
    public ObservableCollection<string> ImportTemplateOptions { get; } = new() { "Items" };
    public ObservableCollection<InventoryReportCard> ReportCards { get; } = new()
    {
        new("Tổng hợp tồn kho", "INVENTORY_SUMMARY qua ReportEngine", "\uE9D2", "#EAF2FF", "#2563EB", "Nguồn", "StockBalances", "Export", "Excel/PDF/CSV"),
        new("Nhập xuất tồn", "INVENTORY_MOVEMENT qua ReportEngine", "\uE8A5", "#EAFBF1", "#16A34A", "Nguồn", "Receipts/Issues", "Giá vốn", "Persisted cost"),
        new("Thẻ kho", "STOCK_CARD theo hàng/kho đang chọn", "\uE7C3", "#FFF4E8", "#F97316", "Drilldown", "Item/Warehouse", "Export", "Excel/PDF/CSV"),
        new("Cảnh báo tồn thấp", "LOW_STOCK từ StockBalances", "\uE8EC", "#ECFDF5", "#22C55E", "Ngưỡng", "10", "Nguồn", "Persisted"),
        new("Tồn âm", "NEGATIVE_STOCK từ StockBalances", "\uE77B", "#FFF1F2", "#EF4444", "Kiểm soát", "Negative stock", "Nguồn", "Persisted"),
        new("Phân tích giá vốn", "COST_ANALYSIS từ cost layer/transaction", "\uE9D9", "#F3E8FF", "#8B5CF6", "Costing", "Moving average", "Nguồn", "Costing engine")
    };

    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private string selectedWarehouse = "Tất cả kho";
    [ObservableProperty] private string selectedItemGroup = "Tất cả nhóm";
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? toDate = DateTime.Today;
    [ObservableProperty] private InventoryDocumentType? selectedDocumentType;
    [ObservableProperty] private bool lowStockOnly;
    [ObservableProperty] private bool negativeStockOnly;
    [ObservableProperty] private int selectedTabIndex;

    [ObservableProperty] private InventoryStockBalanceDto? selectedStock;
    [ObservableProperty] private InventoryMovementDto? selectedMovement;
    [ObservableProperty] private InventoryDocumentDto? selectedDocument;
    [ObservableProperty] private InventoryLowStockAlertDto? selectedAlert;

    [ObservableProperty] private decimal totalItems;
    [ObservableProperty] private decimal totalStock;
    [ObservableProperty] private decimal totalIn;
    [ObservableProperty] private decimal totalOut;
    [ObservableProperty] private decimal totalValue;
    [ObservableProperty] private int lowStockCount;
    [ObservableProperty] private int negativeStockCount;
    [ObservableProperty] private int recentMovementCount;
    [ObservableProperty] private DateTime lastRefreshAt = DateTime.Now;
    [ObservableProperty] private string selectedReportOption = "Tổng hợp tồn kho";
    [ObservableProperty] private string selectedReportFormat = "Excel";
    [ObservableProperty] private string selectedImportTemplate = "Items";
    [ObservableProperty] private Guid? currentImportSessionId;
    [ObservableProperty] private string importStatusText = "Chưa chọn file import";
    [ObservableProperty] private string importWarningSummary = string.Empty;
    [ObservableProperty] private bool isReportLoading;
    [ObservableProperty] private bool isImportLoading;
    [ObservableProperty] private bool isRowDetailOpen;
    [ObservableProperty] private InventoryRowDetailModel? currentRowDetail;
    [ObservableProperty] private bool isSummaryExpanded;
    [ObservableProperty] private bool isFilterExpanded;

    public string ModuleStatus => "Inventory Command Center";
    public string DataMode => "SQL Server / persisted";
    public string CurrentPeriod => DateTime.Today.ToString("MM/yyyy");
    public string LastRefreshText => $"Cập nhật {LastRefreshAt:HH:mm}";
    public string StatusBarText => $"Bản ghi tồn kho: {StockBalances.Count} • Phát sinh: {Movements.Count} • Giá trị tồn: {TotalValue:N0} • Làm mới: {LastRefreshAt:HH:mm:ss}";
    public string SelectedStockStatusText => SelectedStock?.Status ?? "Chưa chọn";
    public decimal SelectedMovementDifference => SelectedMovement is null
        ? 0
        : SelectedMovement.CountedStockQuantity - SelectedMovement.SystemStockQuantity;
    public string SelectedMovementStatusText => SelectedMovement is null
        ? "Chưa chọn"
        : SelectedMovementDifference == 0 ? "Khớp tồn" : SelectedMovementDifference > 0 ? "Tăng tồn" : "Giảm tồn";

    public InventoryMovementViewModel(IInventoryMovementService service, IReportEngine reportEngine, IImportEngine importEngine, INotificationService notification, IDialogService dialog)
    {
        _service = service;
        _reportEngine = reportEngine;
        _importEngine = importEngine;
        _notification = notification;
        _dialog = dialog;
        _ = RefreshAsync();
    }

    partial void OnKeywordChanged(string value) => QueueFilterRefresh();
    partial void OnSelectedWarehouseChanged(string value) => QueueFilterRefresh();
    partial void OnSelectedItemGroupChanged(string value) => QueueFilterRefresh();
    partial void OnSelectedDocumentTypeChanged(InventoryDocumentType? value) => QueueFilterRefresh();
    partial void OnLowStockOnlyChanged(bool value) => QueueFilterRefresh();
    partial void OnNegativeStockOnlyChanged(bool value) => QueueFilterRefresh();

    partial void OnSelectedStockChanged(InventoryStockBalanceDto? value)
    {
        OnPropertyChanged(nameof(SelectedStockStatusText));
        if (value is null)
        {
            ProductMovements.Clear();
            ProductMovementPager.SetSource(ProductMovements);
            return;
        }

        if (!_syncingSelectionFromMovement)
        {
            _ = LoadProductMovementsAsync(value.ProductId, value.WarehouseId);
        }
    }

    partial void OnSelectedMovementChanged(InventoryMovementDto? value)
    {
        OnPropertyChanged(nameof(SelectedMovementDifference));
        OnPropertyChanged(nameof(SelectedMovementStatusText));

        if (value is null)
        {
            return;
        }

        var stock = StockBalances.FirstOrDefault(x => x.ProductId == value.ProductId && x.WarehouseId == value.WarehouseId);
        if (stock is null || ReferenceEquals(SelectedStock, stock))
        {
            return;
        }

        try
        {
            _syncingSelectionFromMovement = true;
            SelectedStock = stock;
        }
        finally
        {
            _syncingSelectionFromMovement = false;
        }

        _ = LoadProductMovementsAsync(value.ProductId, value.WarehouseId, preserveSelectedMovement: true);
    }

    partial void OnSelectedDocumentChanged(InventoryDocumentDto? value)
    {
        if (value is null)
        {
            SelectedDocumentLines.Clear();
            return;
        }

        _ = LoadDocumentLinesAsync(value.DocumentNo);
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var cancellationToken = _refreshCts.Token;
        var lockTaken = false;
        IsBusy = true;
        try
        {
            await _operationGate.WaitAsync(cancellationToken);
            lockTaken = true;
            _suppressFilterRefresh = true;
            var filter = BuildFilter();
            var dashboard = await _service.GetDashboardAsync(filter, cancellationToken);
            var balances = await _service.GetStockBalancesAsync(filter, cancellationToken);
            var movements = await _service.GetMovementsAsync(filter, cancellationToken);
            var alerts = await _service.GetLowStockAlertsAsync(filter, cancellationToken);
            var documents = await _service.GetDocumentsAsync(filter, cancellationToken);

            StockBalances.ResetWith(balances);
            Movements.ResetWith(movements);
            LowStockAlerts.ResetWith(alerts);
            Documents.ResetWith(documents);
            StockBalancePager.SetSource(StockBalances);
            MovementPager.SetSource(Movements);
            StockTakePager.SetSource(Movements);
            LowStockAlertPager.SetSource(LowStockAlerts);
            DocumentPager.SetSource(Documents);

            TotalItems = dashboard.TotalItems;
            TotalStock = dashboard.TotalStock;
            TotalIn = dashboard.TotalInPeriod;
            TotalOut = dashboard.TotalOutPeriod;
            TotalValue = dashboard.TotalStockValue;
            LowStockCount = dashboard.LowStockCount;
            NegativeStockCount = dashboard.NegativeStockCount;
            RecentMovementCount = dashboard.RecentMovementCount;
            LastRefreshAt = DateTime.Now;

            TopStockValueItems.ResetWith(StockBalances.OrderByDescending(x => x.StockValue).Take(5).ToList());
            RecentMovements.ResetWith(Movements.OrderByDescending(x => x.MovementDate).Take(10).ToList());
            RecentMovementsPager.SetSource(RecentMovements);
            RiskItems.ResetWith(StockBalances.Where(x => x.Status is "Tồn thấp" or "Âm kho").OrderBy(x => x.ClosingQuantity).Take(8).ToList());

            WarehouseSummaries.ResetWith(dashboard.WarehouseSummaries);
            BuildDailySeriesFromMovements(movements);
            BuildFilterOptions(balances);

            SelectedStock ??= StockBalances.FirstOrDefault();
            SelectedMovement ??= Movements.FirstOrDefault();
            SelectedDocument ??= Documents.FirstOrDefault();
            SelectedAlert ??= LowStockAlerts.FirstOrDefault();

            OnPropertyChanged(nameof(LastRefreshText));
            OnPropertyChanged(nameof(StatusBarText));
            OnPropertyChanged(nameof(SelectedStockStatusText));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _dialog.Error($"Tải dữ liệu tồn kho thất bại: {ex.Message}");
        }
        finally
        {
            _suppressFilterRefresh = false;
            if (lockTaken)
            {
                _operationGate.Release();
            }
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ApplyFilterAsync() => await RefreshAsync();

    [RelayCommand]
    private void OpenRowDetail(object? row)
    {
        if (row is null)
        {
            return;
        }

        CurrentRowDetail = InventoryRowDetailModel.From(row);
        IsRowDetailOpen = true;
    }

    [RelayCommand]
    private void CloseRowDetail()
    {
        IsRowDetailOpen = false;
        CurrentRowDetail = null;
    }

    [RelayCommand]
    private async Task ClearFilterAsync()
    {
        Keyword = string.Empty;
        SelectedWarehouse = "Tất cả kho";
        SelectedItemGroup = "Tất cả nhóm";
        FromDate = DateTime.Today.AddMonths(-1);
        ToDate = DateTime.Today;
        SelectedDocumentType = null;
        LowStockOnly = false;
        NegativeStockOnly = false;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AdjustStockAsync()
    {
        if (StockBalances.Count == 0)
        {
            _dialog.Info("Không có hàng hóa để điều chỉnh tồn.");
            return;
        }

        var selectedStock = ResolveSelectedStockForAdjustment();
        var actualQuantity = SelectedMovement is null ? (decimal?)null : SelectedMovement.CountedStockQuantity;
        var reason = SelectedMovement is null || string.IsNullOrWhiteSpace(SelectedMovement.Description)
            ? null
            : SelectedMovement.Description;

        var dialog = new InventoryAdjustmentDialog(StockBalances, selectedStock, actualQuantity, reason)
        {
            Owner = App.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }

        await _service.CreateAdjustmentAsync(
            dialog.Result.Value.Stock.ProductId,
            dialog.Result.Value.Stock.WarehouseId,
            dialog.Result.Value.ActualQuantity,
            dialog.Result.Value.Reason);

        _notification.Show("Đã lưu phiếu điều chỉnh tồn và cập nhật số liệu.");
        await RefreshAsync();
    }

    private InventoryStockBalanceDto? ResolveSelectedStockForAdjustment()
    {
        if (SelectedMovement is not null)
        {
            var stock = StockBalances.FirstOrDefault(x => x.ProductId == SelectedMovement.ProductId && x.WarehouseId == SelectedMovement.WarehouseId);
            if (stock is not null)
            {
                return stock;
            }
        }

        return SelectedStock ?? StockBalances.FirstOrDefault();
    }

    [RelayCommand]
    private async Task RecalculateAsync()
    {
        await _service.RecalculateStockAsync();
        _notification.Show("Đã tính lại tồn kho thành công.");
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task ImportExcelAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Chọn file Excel import kho",
            Filter = "Excel files (*.xlsx;*.xls)|*.xlsx;*.xls|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        IsImportLoading = true;
        ImportPreviewRows.Clear();
        ImportWarningSummary = string.Empty;
        try
        {
            var result = await _importEngine.PreviewAsync(new ImportPreviewRequest
            {
                TemplateCode = SelectedImportTemplate,
                TargetEntity = SelectedImportTemplate,
                FilePath = dialog.FileName,
                CreatedBy = "inventory-ui"
            });

            CurrentImportSessionId = result.ImportSessionId;
            ImportPreviewRows.ResetWith(result.Rows.Take(500));
            ImportStatusText = $"Preview {result.TotalRows:N0} dòng: hợp lệ {result.ValidRows:N0}, cảnh báo {result.WarningRows:N0}, lỗi {result.ErrorRows:N0}.";
            ImportWarningSummary = string.Join(Environment.NewLine, result.Messages.Take(8));

            if (result.IsValid)
            {
                _notification.Show("Import preview hợp lệ. Kiểm tra dữ liệu rồi commit.");
            }
        }
        catch (Exception ex)
        {
            CurrentImportSessionId = null;
            ImportStatusText = "Import preview thất bại.";
            ImportWarningSummary = ex.Message;
        }
        finally
        {
            IsImportLoading = false;
        }
    }

    [RelayCommand]
    private async Task ValidateImportAsync()
    {
        if (!CurrentImportSessionId.HasValue)
        {
            _dialog.Info("Chọn file import trước khi validate.");
            return;
        }

        IsImportLoading = true;
        try
        {
            var result = await _importEngine.ValidateAsync(CurrentImportSessionId.Value);
            ImportPreviewRows.ResetWith(result.Rows.Take(500));
            ImportStatusText = $"Validate: hợp lệ {result.ValidRows:N0}, cảnh báo {result.WarningRows:N0}, lỗi {result.ErrorRows:N0}.";
            ImportWarningSummary = string.Join(Environment.NewLine, result.Messages.Take(8));
        }
        catch (Exception ex)
        {
            ImportWarningSummary = ex.Message;
        }
        finally
        {
            IsImportLoading = false;
        }
    }

    [RelayCommand]
    private async Task CommitImportAsync()
    {
        if (!CurrentImportSessionId.HasValue)
        {
            _dialog.Info("Chưa có phiên import để commit.");
            return;
        }

        if (!_dialog.Confirm("Commit dữ liệu import đã validate vào database?", "Commit import"))
        {
            return;
        }

        IsImportLoading = true;
        try
        {
            var result = await _importEngine.CommitAsync(CurrentImportSessionId.Value);
            ImportStatusText = result.Message;
            ImportWarningSummary = $"Committed {result.CommittedRows:N0} dòng. Lỗi {result.ErrorCount:N0}, cảnh báo {result.WarningCount:N0}.";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ImportWarningSummary = ex.Message;
        }
        finally
        {
            IsImportLoading = false;
        }
    }

    [RelayCommand]
    private async Task DownloadImportErrorsAsync()
    {
        if (!CurrentImportSessionId.HasValue)
        {
            _dialog.Info("Chưa có phiên import để xuất lỗi.");
            return;
        }

        try
        {
            var bytes = await _importEngine.ExportErrorsAsync(CurrentImportSessionId.Value);
            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ASMILE BO", "Imports");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, $"inventory_import_errors_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await File.WriteAllBytesAsync(path, bytes);
            _dialog.Info($"Đã xuất file lỗi import: {path}", "Import errors");
        }
        catch (Exception ex)
        {
            _dialog.Error($"Xuất file lỗi import thất bại: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task PrintStockCardAsync()
    {
        if (SelectedStock is null)
        {
            _dialog.Info("Vui lòng chọn hàng hóa để in thẻ kho.");
            return;
        }

        await _service.PrintStockCardAsync(SelectedStock.ProductId, SelectedStock.WarehouseId);

        var lines = ProductMovements.Take(20)
            .Select(x => $"{x.MovementDate:dd/MM} | {x.DocumentNo} | {DocumentTypeText(x.DocumentType)} | +{x.QuantityIn:N2} -{x.QuantityOut:N2} | Tồn: {x.BalanceQuantity:N2}");

        var content = $"Hàng hóa: {SelectedStock.ProductName} ({SelectedStock.ProductCode})\n" +
                      $"Kho: {SelectedStock.WarehouseName} ({SelectedStock.WarehouseCode})\n" +
                      $"Kỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}\n" +
                      $"Số dòng: {ProductMovements.Count}\n" +
                      $"Tồn cuối: {SelectedStock.ClosingQuantity:N2} {SelectedStock.Unit}\n\n" +
                      string.Join("\n", lines);

        var preview = new PrintPreviewDialog("In thẻ kho", content)
        {
            Owner = App.Current.MainWindow
        };
        preview.ShowDialog();
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            var path = await _service.ExportCsvAsync(BuildFilter());
            _dialog.Info($"Đã xuất báo cáo: {path}", "Xuất báo cáo");
        }
        catch (Exception ex)
        {
            _dialog.Error($"Xuất báo cáo thất bại: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task MoreAsync()
    {
        var dialog = new InventoryMoreOptionsDialog
        {
            Owner = App.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.SelectedAction))
        {
            return;
        }

        switch (dialog.SelectedAction)
        {
            case "open-vfc":
                await OpenVfcLocationAsync();
                break;
            case "lock-period":
                LockInventoryPeriod();
                break;
            case "sync-docs":
                await SyncDocumentsAsync();
                break;
            case "send-alert":
                SendAlertEmail();
                break;
        }
    }

    [RelayCommand]
    private void OpenStockTab() => SelectedTabIndex = 1;

    [RelayCommand]
    private void OpenMovementsTab() => SelectedTabIndex = 2;

    [RelayCommand]
    private void ToggleSummary()
    {
        IsSummaryExpanded = !IsSummaryExpanded;
        if (IsSummaryExpanded)
        {
            IsFilterExpanded = false;
        }
    }

    [RelayCommand]
    private void ToggleFilter()
    {
        IsFilterExpanded = !IsFilterExpanded;
        if (IsFilterExpanded)
        {
            IsSummaryExpanded = false;
        }
    }

    [RelayCommand]
    private async Task OpenLowStockTabAsync()
    {
        SelectedTabIndex = 4;
        LowStockOnly = true;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task OpenNegativeTabAsync()
    {
        SelectedTabIndex = 4;
        NegativeStockOnly = true;
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        var lockTaken = false;
        IsReportLoading = true;
        try
        {
            await _operationGate.WaitAsync();
            lockTaken = true;
            var reportCode = ResolveReportCode(SelectedReportOption);
            var result = await _reportEngine.ExecuteAsync(BuildReportRequest(reportCode));
            InventoryReportHistory.ResetWith(await _reportEngine.GetExecutionHistoryAsync(reportCode, 12));

            var preview = new PrintPreviewDialog($"Báo cáo tồn kho - {SelectedReportOption}", BuildReportPreview(result))
            {
                Owner = App.Current.MainWindow
            };
            preview.ShowDialog();
        }
        catch (Exception ex)
        {
            _dialog.Error($"Chạy báo cáo thất bại: {ex.Message}");
        }
        finally
        {
            if (lockTaken)
            {
                _operationGate.Release();
            }
            IsReportLoading = false;
        }
    }

    [RelayCommand]
    private async Task PreviewReportCardAsync(InventoryReportCard? card)
    {
        if (card is not null)
        {
            SelectedReportOption = card.Title;
        }
        await GenerateReportAsync();
    }

    [RelayCommand]
    private async Task ExportReportCardAsync(InventoryReportCard? card)
    {
        if (card is not null)
        {
            SelectedReportOption = card.Title;
        }

        var lockTaken = false;
        try
        {
            await _operationGate.WaitAsync();
            lockTaken = true;
            var reportCode = ResolveReportCode(SelectedReportOption);
            var request = BuildReportRequest(reportCode);
            byte[] bytes;
            var extension = SelectedReportFormat.Equals("PDF", StringComparison.OrdinalIgnoreCase) ? "pdf"
                : SelectedReportFormat.Equals("CSV", StringComparison.OrdinalIgnoreCase) ? "csv"
                : "xlsx";

            if (extension == "pdf")
            {
                bytes = await _reportEngine.ExportPdfAsync(request);
            }
            else if (extension == "csv")
            {
                bytes = await _reportEngine.ExportCsvAsync(request);
            }
            else
            {
                bytes = await _reportEngine.ExportExcelAsync(request);
            }

            var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ASMILE BO", "Reports");
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, $"{reportCode.ToLowerInvariant()}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
            await File.WriteAllBytesAsync(path, bytes);
            InventoryReportHistory.ResetWith(await _reportEngine.GetExecutionHistoryAsync(reportCode, 12));
            _dialog.Info($"Đã xuất báo cáo: {path}", "Xuất báo cáo");
        }
        catch (Exception ex)
        {
            _dialog.Error($"Xuất báo cáo thất bại: {ex.Message}");
        }
        finally
        {
            if (lockTaken)
            {
                _operationGate.Release();
            }
        }
    }

    [RelayCommand]
    private async Task PrintReportCardAsync(InventoryReportCard? card)
    {
        if (card is not null)
        {
            SelectedReportOption = card.Title;
        }

        await GenerateReportAsync();
    }

    [RelayCommand]
    private async Task ExportCurrentViewAsync() => await ExportAsync();

    [RelayCommand]
    private async Task PrintStockListAsync()
    {
        var content = string.Join("\n", StockBalances.Take(20)
            .Select(x => $"{x.ProductCode} | {x.ProductName} | {x.WarehouseCode} | Tồn: {x.ClosingQuantity:N2} | Giá trị: {x.StockValue:N0}"));

        var preview = new PrintPreviewDialog("Danh sách tồn kho", content)
        {
            Owner = App.Current.MainWindow
        };
        preview.ShowDialog();
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task CreatePurchaseSuggestionAsync()
    {
        if (SelectedAlert is null)
        {
            _dialog.Info("Chọn một cảnh báo tồn thấp để tạo đề xuất nhập.");
            return;
        }

        _dialog.Info($"Đã tạo đề xuất nhập hàng cho {SelectedAlert.ProductCode} - SL gợi ý: {SelectedAlert.SuggestedPurchaseQty:N2}", "Đề xuất nhập hàng");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task HideAlertAsync()
    {
        if (SelectedAlert is null)
        {
            _dialog.Info("Chọn cảnh báo cần ẩn.");
            return;
        }

        LowStockAlerts.Remove(SelectedAlert);
        SelectedAlert = LowStockAlerts.FirstOrDefault();
        _notification.Show("Đã ẩn cảnh báo khỏi danh sách hiện tại.");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenDocumentAsync()
    {
        if (SelectedDocument is null)
        {
            _dialog.Info("Chọn chứng từ cần mở.");
            return;
        }

        SelectedTabIndex = 3;
        await LoadDocumentLinesAsync(SelectedDocument.DocumentNo);
    }

    [RelayCommand]
    private async Task CopyVirtualPathAsync()
    {
        if (SelectedDocument is null)
        {
            _dialog.Info("Chọn chứng từ cần copy đường dẫn.");
            return;
        }

        var virtualPath = $"D:\\ACCOUNTING\\Xuất nhập tồn\\{DateTime.Today:yyyy}\\{DateTime.Today:MM}\\{SelectedDocument.DocumentNo}";
        Clipboard.SetText(virtualPath);
        _notification.Show("Đã copy đường dẫn chứng từ vào clipboard.");
        await Task.CompletedTask;
    }

    private async Task LoadProductMovementsAsync(string productId, string warehouseId, bool preserveSelectedMovement = false)
    {
        await _operationGate.WaitAsync();
        try
        {
            var rows = await _service.GetMovementsByProductAsync(productId, warehouseId, FromDate, ToDate);
            ProductMovements.ResetWith(rows.OrderBy(x => x.MovementDate).ToList());
            ProductMovementPager.SetSource(ProductMovements);
            RecentSelectedMovements.ResetWith(rows
                .OrderByDescending(x => x.MovementDate)
                .Take(3)
                .Select(InventoryRecentMovementLine.From)
                .ToList());

            if (!preserveSelectedMovement)
            {
                SelectedMovement = ProductMovements.LastOrDefault();
            }
        }
        catch (Exception ex)
        {
            _dialog.Error($"Tải phát sinh hàng hóa thất bại: {ex.Message}");
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task LoadDocumentLinesAsync(string documentNo)
    {
        await _operationGate.WaitAsync();
        try
        {
            var rows = await _service.GetDocumentLinesAsync(documentNo);
            SelectedDocumentLines.ResetWith(rows);
        }
        catch (Exception ex)
        {
            _dialog.Error($"Tải dòng chứng từ thất bại: {ex.Message}");
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private InventoryMovementFilterDto BuildFilter() => new()
    {
        Keyword = Keyword,
        WarehouseCode = SelectedWarehouse == "Tất cả kho" ? string.Empty : SelectedWarehouse,
        ItemGroup = SelectedItemGroup == "Tất cả nhóm" ? string.Empty : SelectedItemGroup,
        FromDate = FromDate,
        ToDate = ToDate,
        DocumentType = SelectedDocumentType,
        LowStockOnly = LowStockOnly,
        NegativeStockOnly = NegativeStockOnly
    };

    private void BuildFilterOptions(IEnumerable<InventoryStockBalanceDto> balances)
    {
        var whSet = balances.Select(x => x.WarehouseCode).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var groupSet = balances.Select(x => x.ItemGroup).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();

        SyncOptions(WarehouseOptions, "Tất cả kho", whSet, SelectedWarehouse, v => SelectedWarehouse = v);
        SyncOptions(ItemGroupOptions, "Tất cả nhóm", groupSet, SelectedItemGroup, v => SelectedItemGroup = v);
    }

    private static void SyncOptions(ObservableCollection<string> options, string allText, List<string> values, string selected, Action<string> setSelected)
    {
        options.Clear();
        options.Add(allText);
        foreach (var item in values)
        {
            options.Add(item);
        }

        if (!options.Contains(selected))
        {
            setSelected(allText);
        }
    }

    private void BuildDailySeries(IEnumerable<InventoryDailyFlowDto> flows)
    {
        DailyInSeries.Clear();
        DailyOutSeries.Clear();

        var inMax = Math.Max(1m, flows.Any() ? flows.Max(x => x.QuantityIn) : 1m);
        var outMax = Math.Max(1m, flows.Any() ? flows.Max(x => x.QuantityOut) : 1m);

        foreach (var day in flows.OrderBy(x => x.Date))
        {
            DailyInSeries.Add(new MetricPoint
            {
                Label = day.Date.ToString("dd/MM"),
                Value = day.QuantityIn,
                Percent = (double)(day.QuantityIn / inMax * 220m)
            });

            DailyOutSeries.Add(new MetricPoint
            {
                Label = day.Date.ToString("dd/MM"),
                Value = day.QuantityOut,
                Percent = (double)(day.QuantityOut / outMax * 220m)
            });
        }
    }

    private void BuildDailySeriesFromMovements(IEnumerable<InventoryMovementDto> movements)
    {
        var rows = movements.ToList();
        var today = DateTime.Today;
        var start = today.AddDays(-6);
        var flows = Enumerable.Range(0, 7)
            .Select(i => start.AddDays(i))
            .Select(day => new InventoryDailyFlowDto
            {
                Date = day,
                QuantityIn = rows.Where(x => x.MovementDate.Date == day.Date).Sum(x => x.QuantityIn),
                QuantityOut = rows.Where(x => x.MovementDate.Date == day.Date).Sum(x => x.QuantityOut)
            })
            .ToList();

        BuildDailySeries(flows);
    }

    private async Task OpenVfcLocationAsync()
    {
        var target = SelectedStock?.ProductCode ?? "ALL";
        var virtualPath = $"D:\\ACCOUNTING\\Xuất nhập tồn\\{DateTime.Today:yyyy}\\{DateTime.Today:MM}\\{target}";
        Clipboard.SetText(virtualPath);
        _notification.Show("Đã copy vị trí VFC vào clipboard.");
        await Task.CompletedTask;
    }

    private void LockInventoryPeriod()
    {
        var period = $"{FromDate:MM/yyyy} - {ToDate:MM/yyyy}";
        if (!_dialog.Confirm($"Xác nhận khóa kỳ tồn kho {period}?", "Khóa kỳ tồn kho"))
        {
            return;
        }

        _dialog.Info($"Đã khóa kỳ tồn kho {period} (mô phỏng runtime).", "Khóa kỳ tồn kho");
    }

    private async Task SyncDocumentsAsync()
    {
        await _service.RecalculateStockAsync();
        await RefreshAsync();
        _notification.Show("Đã đồng bộ chứng từ và cập nhật số liệu tồn kho.");
    }

    private void SendAlertEmail()
    {
        var warningCount = LowStockAlerts.Count;
        var criticalItems = LowStockAlerts
            .Where(x => x.Severity.Equals("Critical", StringComparison.OrdinalIgnoreCase))
            .Take(5)
            .Select(x => $"{x.ProductCode} ({x.WarehouseCode}) thiếu {x.MissingQuantity:N0}");

        var body = $"Đã gửi cảnh báo email nội bộ (mô phỏng).\n" +
                   $"Số cảnh báo tồn thấp: {warningCount}\n" +
                   $"Mục ưu tiên: {(criticalItems.Any() ? string.Join(", ", criticalItems) : "Không có")}.\n" +
                   $"Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";

        _dialog.Info(body, "Gửi cảnh báo email");
    }

    private void QueueFilterRefresh()
    {
        if (_suppressFilterRefresh)
        {
            return;
        }

        _filterDebounceCts?.Cancel();
        _filterDebounceCts = new CancellationTokenSource();
        var token = _filterDebounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () => await RefreshAsync());
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private ReportRequestDto BuildReportRequest(string reportCode)
    {
        var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["fromDate"] = FromDate?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
            ["toDate"] = ToDate?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
            ["keyword"] = Keyword,
            ["warehouse"] = SelectedWarehouse == "Tất cả kho" ? null : SelectedWarehouse,
            ["itemGroup"] = SelectedItemGroup == "Tất cả nhóm" ? null : SelectedItemGroup,
            ["asOfDate"] = ToDate?.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN"))
        };

        if (SelectedStock is not null)
        {
            parameters["itemCode"] = SelectedStock.ProductCode;
            parameters["warehouseCode"] = SelectedStock.WarehouseCode;
        }

        return new ReportRequestDto
        {
            ReportCode = reportCode,
            Parameters = parameters,
            ExecutedBy = "inventory-ui"
        };
    }

    private static string ResolveReportCode(string reportName)
    {
        if (reportName.Contains("nhập xuất", StringComparison.OrdinalIgnoreCase) || reportName.Contains("biến động", StringComparison.OrdinalIgnoreCase)) return "INVENTORY_MOVEMENT";
        if (reportName.Contains("thẻ kho", StringComparison.OrdinalIgnoreCase)) return "STOCK_CARD";
        if (reportName.Contains("tồn thấp", StringComparison.OrdinalIgnoreCase)) return "LOW_STOCK";
        if (reportName.Contains("tồn âm", StringComparison.OrdinalIgnoreCase)) return "NEGATIVE_STOCK";
        if (reportName.Contains("giá vốn", StringComparison.OrdinalIgnoreCase) || reportName.Contains("giá trị", StringComparison.OrdinalIgnoreCase)) return "COST_ANALYSIS";
        return "INVENTORY_SUMMARY";
    }

    private static string BuildReportPreview(ReportResultDto result)
    {
        var builder = new StringBuilder();
        builder.AppendLine(result.ReportName);
        builder.AppendLine($"Ngày lập: {result.GeneratedAt:dd/MM/yyyy HH:mm}");
        builder.AppendLine($"Số dòng: {result.RowCount:N0}");
        builder.AppendLine();
        builder.AppendLine(string.Join(" | ", result.Columns.Select(x => x.DisplayName)));
        foreach (var row in result.Rows.Take(35))
        {
            builder.AppendLine(string.Join(" | ", result.Columns.Select(c => FormatReportValue(row.Values.TryGetValue(c.Name, out var value) ? value : null))));
        }

        if (result.RowCount > 35)
        {
            builder.AppendLine($"... còn {result.RowCount - 35:N0} dòng. Dùng Xuất Excel/CSV để xem đầy đủ.");
        }

        return builder.ToString();
    }

    private static string FormatReportValue(object? value)
        => value switch
        {
            null => string.Empty,
            DateTime date => date.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
            decimal amount => amount.ToString("N2", CultureInfo.GetCultureInfo("vi-VN")),
            double number => number.ToString("N2", CultureInfo.GetCultureInfo("vi-VN")),
            _ => value.ToString() ?? string.Empty
        };

    private static string DocumentTypeText(InventoryDocumentType type)
    {
        return type switch
        {
            InventoryDocumentType.OpeningStock => "Tồn đầu kỳ",
            InventoryDocumentType.ImportVoucher => "Phiếu nhập",
            InventoryDocumentType.ExportVoucher => "Phiếu xuất",
            InventoryDocumentType.AdjustmentIn => "Điều chỉnh tăng",
            InventoryDocumentType.AdjustmentOut => "Điều chỉnh giảm",
            InventoryDocumentType.TransferIn => "Chuyển kho đến",
            InventoryDocumentType.TransferOut => "Chuyển kho đi",
            InventoryDocumentType.StockTake => "Kiểm kê",
            _ => type.ToString()
        };
    }
}

public sealed class InventoryRecentMovementLine
{
    public string DateText { get; init; } = string.Empty;
    public string DocumentNo { get; init; } = string.Empty;
    public string QuantityText { get; init; } = string.Empty;
    public string Tone { get; init; } = "Neutral";

    public static InventoryRecentMovementLine From(InventoryMovementDto movement)
    {
        var delta = movement.QuantityIn > 0 ? movement.QuantityIn : -movement.QuantityOut;
        var sign = delta > 0 ? "+" : string.Empty;

        return new InventoryRecentMovementLine
        {
            DateText = movement.MovementDate.ToString("dd/MM/yyyy"),
            DocumentNo = movement.DocumentNo,
            QuantityText = $"{sign}{delta:N0}",
            Tone = delta > 0 ? "Positive" : delta < 0 ? "Negative" : "Neutral"
        };
    }
}

public sealed record InventoryReportCard(
    string Title,
    string Description,
    string IconGlyph,
    string IconBackground,
    string AccentBrush,
    string PrimaryLabel,
    string PrimaryValue,
    string SecondaryLabel,
    string SecondaryValue);

file static class ObservableCollectionExtensions
{
    public static void ResetWith<T>(this ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }
}
