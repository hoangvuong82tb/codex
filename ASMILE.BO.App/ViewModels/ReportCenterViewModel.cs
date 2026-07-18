using ASMILE.BO.App.Services;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ReportCenterViewModel : ViewModelBase
{
    private static readonly HashSet<string> SupportedReportCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "GENERAL_LEDGER",
        "TRIAL_BALANCE",
        "INVENTORY_SUMMARY",
        "INVENTORY_MOVEMENT",
        "STOCK_CARD",
        "LOW_STOCK",
        "NEGATIVE_STOCK",
        "COST_ANALYSIS",
        "AP_AGING",
        "AR_AGING",
        "BALANCE_SHEET",
        "INCOME_STATEMENT",
        "CASH_FLOW",
        "CASH_BOOK",
        "BANK_BOOK",
        "BANK_RECONCILIATION",
        "DAILY_CASHFLOW",
        "CASH_BALANCE",
        "FIXED_ASSET_REGISTER",
        "DEPRECIATION_SCHEDULE",
        "ASSET_INCREASE_DECREASE",
        "TOOL_ASSET_REGISTER",
        "TOOL_ASSET_ALLOCATION",
        "PREPAID_EXPENSE_SCHEDULE",
        "VAT_INPUT",
        "VAT_OUTPUT",
        "BREAKFAST_CAFE_DAILY_SALES",
        "BREAKFAST_CAFE_PAYMENT_SUMMARY",
        "BREAKFAST_CAFE_ITEM_SALES",
        "BREAKFAST_CAFE_KITCHEN_TICKET",
        "ALACARTE_ORDER_SUMMARY",
        "ALACARTE_QUOTE_SUMMARY",
        "ALACARTE_RESERVATION_SCHEDULE",
        "ALACARTE_DEPOSIT_SUMMARY",
        "BANQUET_EVENT_SUMMARY",
        "BANQUET_CONTRACT_SUMMARY",
        "BANQUET_DEPOSIT_SUMMARY",
        "BANQUET_SERVICE_DETAIL"
    };

    private readonly IReportEngine _reportEngine;
    private readonly INotificationService _notificationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly SemaphoreSlim _engineGate = new(1, 1);
    private CancellationTokenSource? _executionCts;
    private ReportRequestDto? _lastRequest;
    private List<ReportGridRow> _allResultRows = [];
    private List<ReportGridRow> _currentResultRows = [];

    public ObservableCollection<ReportCategoryItem> Categories { get; } = new();
    public ObservableCollection<ReportCatalogItem> Reports { get; } = new();
    public ObservableCollection<ReportCatalogItem> FilteredReports { get; } = new();
    public ObservableCollection<ReportCatalogItem> FavoriteReports { get; } = new();
    public ObservableCollection<ReportGroupSection> ReportGroups { get; } = new();
    public ObservableCollection<ReportParameterInput> Parameters { get; } = new();
    public ObservableCollection<ReportColumnViewModel> ResultColumns { get; } = new();
    public TablePagerViewModel<ReportGridRow> ResultPager { get; } = new() { ItemName = "dòng báo cáo" };
    public TablePagerViewModel<ReportExecutionDto> HistoryPager { get; } = new() { ItemName = "lần chạy" };

    [ObservableProperty] private ReportCategoryItem? selectedCategory;
    [ObservableProperty] private ReportCatalogItem? selectedReport;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string statusText = "Sẵn sàng";
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isExecuting;
    [ObservableProperty] private bool isExporting;
    [ObservableProperty] private string selectedReportDescription = "Chọn một báo cáo để xem tham số và chạy dữ liệu thật.";
    [ObservableProperty] private bool isResultSearchVisible;
    [ObservableProperty] private string resultSearchText = string.Empty;
    [ObservableProperty] private bool isResultFilterActive;

    public bool CanCancel => IsExecuting || IsExporting;
    public bool HasResult => ResultPager.TotalCount > 0;
    public bool HasFavoriteReports => FavoriteReports.Count > 0;

    public ReportCenterViewModel(IReportEngine reportEngine, INotificationService notificationService, ICurrentUserService currentUserService)
    {
        _reportEngine = reportEngine;
        _notificationService = notificationService;
        _currentUserService = currentUserService;
        _ = LoadAsync();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedCategoryChanged(ReportCategoryItem? value) => ApplyFilter();

    partial void OnSelectedReportChanged(ReportCatalogItem? value)
    {
        _ = LoadSelectedReportAsync(value);
    }

    partial void OnIsExecutingChanged(bool value) => OnPropertyChanged(nameof(CanCancel));

    partial void OnIsExportingChanged(bool value) => OnPropertyChanged(nameof(CanCancel));

    partial void OnResultSearchTextChanged(string value)
    {
        if (IsResultFilterActive)
        {
            ApplyResultFilter();
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var templates = await WithEngineLockAsync(token => _reportEngine.GetTemplatesAsync(null, token));
            BuildReportCatalog(templates);
            await RefreshHistoryAsync(null);
            StatusText = $"Đã tải {Reports.Count} báo cáo từ ReportEngine.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Không tải được danh sách báo cáo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExecuteReportAsync()
    {
        if (SelectedReport is null)
        {
            _notificationService.Show("Vui lòng chọn báo cáo trước khi chạy.");
            return;
        }

        await ExecuteInternalAsync(SelectedReport.Code, exportFormat: null);
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        if (_lastRequest is null)
        {
            await ExecuteReportAsync();
            return;
        }

        await ExecuteInternalAsync(_lastRequest.ReportCode, exportFormat: null, _lastRequest.Parameters);
    }

    [RelayCommand]
    private void CancelExecution()
    {
        _executionCts?.Cancel();
        StatusText = "Đang hủy tác vụ báo cáo...";
    }

    [RelayCommand]
    private async Task RefreshSelectedReportAsync()
    {
        await LoadSelectedReportAsync(SelectedReport);
    }

    [RelayCommand]
    private void ToggleResultSearch()
    {
        IsResultSearchVisible = !IsResultSearchVisible;
        if (!IsResultSearchVisible)
        {
            ResultSearchText = string.Empty;
            if (IsResultFilterActive)
            {
                IsResultFilterActive = false;
                ApplyResultFilter();
            }
        }

        StatusText = IsResultSearchVisible
            ? "Đã bật tìm kiếm trong bảng kết quả."
            : "Đã ẩn tìm kiếm trong bảng kết quả.";
    }

    [RelayCommand]
    private void ToggleResultFilter()
    {
        if (_allResultRows.Count == 0)
        {
            StatusText = "Chưa có dữ liệu để lọc.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ResultSearchText))
        {
            IsResultSearchVisible = true;
            StatusText = "Nhập từ khóa trước khi lọc dữ liệu.";
            return;
        }

        IsResultFilterActive = !IsResultFilterActive;
        ApplyResultFilter();
        StatusText = IsResultFilterActive
            ? $"Đã lọc dữ liệu theo từ khóa: {ResultSearchText}"
            : "Đã bỏ lọc dữ liệu.";
    }

    [RelayCommand]
    private void CopyResult()
    {
        if (_currentResultRows.Count == 0 || ResultColumns.Count == 0)
        {
            StatusText = "Chưa có dữ liệu để sao chép.";
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine(string.Join('\t', ResultColumns.Select(x => x.DisplayName)));
        foreach (var row in _currentResultRows)
        {
            builder.AppendLine(string.Join('\t', ResultColumns.Select(column => row.GetValue(column.Name))));
        }

        Clipboard.SetText(builder.ToString());
        StatusText = $"Đã sao chép {_currentResultRows.Count:N0} dòng vào clipboard.";
        _notificationService.Show("Đã sao chép dữ liệu báo cáo.");
    }

    [RelayCommand]
    private async Task ExportExcelAsync() => await ExportAsync("excel");

    [RelayCommand]
    private async Task ExportCsvAsync() => await ExportAsync("csv");

    [RelayCommand]
    private async Task ExportPdfAsync() => await ExportAsync("pdf");

    [RelayCommand]
    private void ToggleFavorite(ReportCatalogItem? report)
    {
        report ??= SelectedReport;
        if (report is null)
        {
            return;
        }

        report.IsFavorite = !report.IsFavorite;
        RefreshFavorites();
    }

    private async Task ExportAsync(string format)
    {
        if (SelectedReport is null)
        {
            _notificationService.Show("Vui lòng chọn báo cáo trước khi xuất file.");
            return;
        }

        await ExecuteInternalAsync(SelectedReport.Code, format);
    }

    private async Task ExecuteInternalAsync(string reportCode, string? exportFormat, Dictionary<string, string?>? parameterOverride = null)
    {
        _executionCts?.Cancel();
        _executionCts = new CancellationTokenSource();
        var token = _executionCts.Token;
        HasError = false;
        ErrorMessage = string.Empty;
        IsExecuting = exportFormat is null;
        IsExporting = exportFormat is not null;
        StatusText = exportFormat is null ? "Đang chạy báo cáo..." : $"Đang xuất {exportFormat.ToUpperInvariant()}...";

        var request = new ReportRequestDto
        {
            ReportCode = reportCode,
            ExecutedBy = _currentUserService.CurrentUser?.DisplayName ?? "Administrator",
            Parameters = parameterOverride ?? BuildParameterDictionary()
        };
        _lastRequest = request;

        try
        {
            if (exportFormat is null)
            {
                var result = await WithEngineLockAsync(ct => _reportEngine.ExecuteAsync(request, ct), token);
                LoadResult(result);
                StatusText = $"Hoàn tất: {result.RowCount:N0} dòng, tạo lúc {result.GeneratedAt:dd/MM/yyyy HH:mm}.";
            }
            else
            {
                var bytes = exportFormat switch
                {
                    "excel" => await WithEngineLockAsync(ct => _reportEngine.ExportExcelAsync(request, ct), token),
                    "csv" => await WithEngineLockAsync(ct => _reportEngine.ExportCsvAsync(request, ct), token),
                    "pdf" => await WithEngineLockAsync(ct => _reportEngine.ExportPdfAsync(request, ct), token),
                    _ => throw new InvalidOperationException($"Chưa hỗ trợ định dạng {exportFormat}.")
                };
                var path = await SaveExportAsync(reportCode, exportFormat, bytes, token);
                if (string.IsNullOrWhiteSpace(path))
                {
                    StatusText = "Đã hủy chọn nơi lưu file.";
                    return;
                }

                StatusText = $"Đã xuất file: {path}";
                _notificationService.Show($"Đã xuất báo cáo: {Path.GetFileName(path)}");
            }

            await RefreshHistoryAsync(reportCode);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Đã hủy tác vụ báo cáo.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Báo cáo không thực hiện được.";
        }
        finally
        {
            IsExecuting = false;
            IsExporting = false;
            _executionCts?.Dispose();
            _executionCts = null;
        }
    }

    private async Task LoadSelectedReportAsync(ReportCatalogItem? report)
    {
        Parameters.Clear();
        ResultColumns.Clear();
        _allResultRows.Clear();
        _currentResultRows.Clear();
        ResultSearchText = string.Empty;
        IsResultFilterActive = false;
        IsResultSearchVisible = false;
        ResultPager.SetSource([]);
        OnPropertyChanged(nameof(HasResult));

        if (report is null)
        {
            SelectedReportDescription = "Chọn một báo cáo để xem tham số và chạy dữ liệu thật.";
            await RefreshHistoryAsync(null);
            return;
        }

        SelectedReportDescription = report.Description;
        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;
        try
        {
            var parameters = await WithEngineLockAsync(token => _reportEngine.GetParametersAsync(report.Code, token));
            foreach (var parameter in parameters.OrderBy(x => x.SortOrder))
            {
                Parameters.Add(new ReportParameterInput(parameter));
            }

            await RefreshHistoryAsync(report.Code);
            StatusText = Parameters.Count == 0
                ? "Báo cáo dùng tham số mặc định từ ReportEngine."
                : $"Đã tải {Parameters.Count} tham số báo cáo.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            StatusText = "Không tải được tham số báo cáo.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildReportCatalog(IReadOnlyList<ReportTemplate> templates)
    {
        Reports.Clear();
        Categories.Clear();

        var usableTemplates = templates
            .Where(x => SupportedReportCodes.Contains(x.Code))
            .Select(x => new ReportCatalogItem(x.Code, x.Name, NormalizeCategory(x.Category), x.Description ?? string.Empty, x.DataSource))
            .ToList();

        if (usableTemplates.Count == 0)
        {
            usableTemplates = BuildFallbackCatalog();
        }

        foreach (var item in usableTemplates.OrderBy(x => x.Category).ThenBy(x => x.Name))
        {
            Reports.Add(item);
        }

        Categories.Add(new ReportCategoryItem("Tất cả", "Tất cả", Reports.Count));
        foreach (var group in Reports.GroupBy(x => x.Category).OrderBy(x => x.Key))
        {
            Categories.Add(new ReportCategoryItem(group.Key, group.Key, group.Count()));
        }

        SelectedCategory = Categories.FirstOrDefault();
        ApplyFilter();
        RefreshFavorites();
        SelectedReport ??= FilteredReports.FirstOrDefault();
    }

    private void ApplyFilter()
    {
        var category = SelectedCategory?.Name;
        var keyword = SearchText.Trim();
        var filtered = Reports.Where(x =>
            (string.IsNullOrWhiteSpace(category) || category == "Tất cả" || x.Category == category)
            && (string.IsNullOrWhiteSpace(keyword)
                || x.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

        FilteredReports.Clear();
        foreach (var item in filtered)
        {
            FilteredReports.Add(item);
        }

        RefreshGroups();
    }

    private void RefreshFavorites()
    {
        FavoriteReports.Clear();
        foreach (var report in Reports.Where(x => x.IsFavorite).OrderBy(x => x.Name))
        {
            FavoriteReports.Add(report);
        }

        OnPropertyChanged(nameof(HasFavoriteReports));
    }

    private void RefreshGroups()
    {
        ReportGroups.Clear();
        foreach (var group in FilteredReports
                     .GroupBy(x => x.Category)
                     .OrderBy(x => x.Key))
        {
            var section = new ReportGroupSection(group.Key);
            foreach (var item in group.OrderBy(x => x.Name))
            {
                section.Items.Add(item);
            }

            ReportGroups.Add(section);
        }
    }

    private async Task RefreshHistoryAsync(string? reportCode)
    {
        var history = await WithEngineLockAsync(token => _reportEngine.GetExecutionHistoryAsync(reportCode, 80, token));
        HistoryPager.SetSource(history);
    }

    private async Task<T> WithEngineLockAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        await _engineGate.WaitAsync(cancellationToken);
        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            _engineGate.Release();
        }
    }

    private Dictionary<string, string?> BuildParameterDictionary()
    {
        return Parameters
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => x.Name, x => string.IsNullOrWhiteSpace(x.Value) ? null : x.Value.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    private void LoadResult(ReportResultDto result)
    {
        ResultColumns.Clear();
        foreach (var column in result.Columns)
        {
            ResultColumns.Add(new ReportColumnViewModel(column.Name, column.DisplayName, column.DataType));
        }

        _allResultRows = result.Rows.Select(row => new ReportGridRow(result.Columns, row)).ToList();
        ResultSearchText = string.Empty;
        IsResultFilterActive = false;
        IsResultSearchVisible = false;
        ApplyResultFilter();
        OnPropertyChanged(nameof(HasResult));
    }

    private void ApplyResultFilter()
    {
        IEnumerable<ReportGridRow> rows = _allResultRows;
        var keyword = ResultSearchText.Trim();
        if (IsResultFilterActive && !string.IsNullOrWhiteSpace(keyword))
        {
            rows = rows.Where(x => x.Contains(keyword));
        }

        _currentResultRows = rows.ToList();
        ResultPager.SetSource(_currentResultRows);
        OnPropertyChanged(nameof(HasResult));
    }

    private static async Task<string?> SaveExportAsync(string reportCode, string format, byte[] bytes, CancellationToken cancellationToken)
    {
        var extension = format switch
        {
            "excel" => "xlsx",
            "csv" => "csv",
            "pdf" => "pdf",
            _ => format
        };

        var safeCode = string.Concat(reportCode.Select(ch => char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_'));
        var defaultFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ASMILE BO", "Reports");
        Directory.CreateDirectory(defaultFolder);
        var dialog = new SaveFileDialog
        {
            Title = "Chọn vị trí lưu báo cáo",
            InitialDirectory = defaultFolder,
            FileName = $"{safeCode}_{DateTime.Now:yyyyMMdd_HHmmss}",
            DefaultExt = $".{extension}",
            AddExtension = true,
            OverwritePrompt = true,
            Filter = format switch
            {
                "excel" => "Excel Workbook (*.xlsx)|*.xlsx",
                "csv" => "CSV UTF-8 (*.csv)|*.csv",
                "pdf" => "PDF Document (*.pdf)|*.pdf",
                _ => "All files (*.*)|*.*"
            }
        };

        if (dialog.ShowDialog() != true)
        {
            return null;
        }

        var path = dialog.FileName;
        await File.WriteAllBytesAsync(path, bytes, cancellationToken);
        return path;
    }

    private static string NormalizeCategory(string? category)
    {
        return category?.Trim() switch
        {
            "Accounting" => "Kế toán",
            "Inventory" => "Kho",
            "Payable" => "Công nợ phải trả",
            "Receivable" => "Công nợ phải thu",
            "Tax" => "Thuế",
            "Financial" => "Tài chính",
            "BreakfastCafe" => "Ăn sáng và Cà phê",
            "Alacarte" => "Báo cáo Alacarte",
            "Banquet" => "Báo cáo Tiệc",
            null or "" => "Khác",
            var value => value
        };
    }

    private static List<ReportCatalogItem> BuildFallbackCatalog() =>
    [
        new("GENERAL_LEDGER", "Sổ cái", "Kế toán", "Báo cáo sổ cái từ Journal persisted.", "Journal"),
        new("TRIAL_BALANCE", "Cân đối phát sinh", "Kế toán", "Tổng hợp phát sinh Nợ/Có theo tài khoản.", "Journal"),
        new("INVENTORY_SUMMARY", "Tổng hợp tồn kho", "Kho", "Tồn kho theo hàng hóa và kho.", "Inventory"),
        new("INVENTORY_MOVEMENT", "Nhập xuất tồn", "Kho", "Biến động nhập xuất tồn theo chứng từ.", "Inventory"),
        new("AP_AGING", "Tuổi nợ phải trả", "Công nợ phải trả", "Phân tích tuổi nợ nhà cung cấp.", "Payable"),
        new("AR_AGING", "Tuổi nợ phải thu", "Công nợ phải thu", "Phân tích tuổi nợ khách hàng.", "Receivable"),
        new("BALANCE_SHEET", "Bảng cân đối kế toán", "Tài chính", "Báo cáo tài sản, nợ phải trả và vốn chủ sở hữu.", "Financial"),
        new("INCOME_STATEMENT", "Kết quả kinh doanh", "Tài chính", "Báo cáo doanh thu, chi phí và lợi nhuận.", "Financial"),
        new("CASH_FLOW", "Lưu chuyển tiền tệ", "Tài chính", "Báo cáo dòng tiền cơ bản.", "Financial"),
        new("VAT_INPUT", "Bảng kê VAT đầu vào", "Thuế", "Giao dịch VAT đầu vào đã ghi nhận.", "VAT"),
        new("VAT_OUTPUT", "Bảng kê VAT đầu ra", "Thuế", "Giao dịch VAT đầu ra đã ghi nhận.", "VAT"),
        new("BREAKFAST_CAFE_DAILY_SALES", "Tổng hợp bán hàng ăn sáng và cà phê", "Ăn sáng và Cà phê", "Theo dõi đơn bán hàng, bàn và doanh thu của module Ăn sáng - Cà phê.", "BreakfastCafe"),
        new("BREAKFAST_CAFE_PAYMENT_SUMMARY", "Chi tiết thanh toán ăn sáng và cà phê", "Ăn sáng và Cà phê", "Theo dõi phương thức thanh toán, người thu và chứng từ thu tiền.", "BreakfastCafe"),
        new("BREAKFAST_CAFE_ITEM_SALES", "Top món bán ăn sáng và cà phê", "Ăn sáng và Cà phê", "Thống kê món bán theo số lượng, số đơn và doanh thu.", "BreakfastCafe"),
        new("BREAKFAST_CAFE_KITCHEN_TICKET", "Phiếu bếp/bar ăn sáng và cà phê", "Ăn sáng và Cà phê", "Theo dõi trạng thái gửi bếp/bar của từng order.", "BreakfastCafe"),
        new("ALACARTE_ORDER_SUMMARY", "Tổng hợp đơn À La Carte", "Báo cáo Alacarte", "Theo dõi đơn mở, khách, phòng/khu vực và doanh thu của module À La Carte.", "Alacarte"),
        new("ALACARTE_QUOTE_SUMMARY", "Tổng hợp báo giá À La Carte", "Báo cáo Alacarte", "Theo dõi báo giá, ngày phục vụ, khách và trạng thái xử lý.", "Alacarte"),
        new("ALACARTE_RESERVATION_SCHEDULE", "Lịch đặt bàn À La Carte", "Báo cáo Alacarte", "Theo dõi booking, thời gian đến, phòng/bàn và khách đặt.", "Alacarte"),
        new("ALACARTE_DEPOSIT_SUMMARY", "Tổng hợp đặt cọc À La Carte", "Báo cáo Alacarte", "Theo dõi phiếu đặt cọc theo đơn, booking và hợp đồng.", "Alacarte"),
        new("BANQUET_EVENT_SUMMARY", "Tổng hợp sự kiện tiệc", "Báo cáo Tiệc", "Theo dõi lịch tiệc, khách hàng, sảnh và tình trạng đặt cọc/hợp đồng.", "Banquet")
        ,
        new("BANQUET_CONTRACT_SUMMARY", "Tổng hợp hợp đồng tiệc", "Báo cáo Tiệc", "Theo dõi giá trị hợp đồng, đặt cọc và công nợ còn lại.", "Banquet"),
        new("BANQUET_DEPOSIT_SUMMARY", "Chi tiết đặt cọc tiệc", "Báo cáo Tiệc", "Theo dõi phiếu thu đặt cọc, phương thức thanh toán và người nhận.", "Banquet"),
        new("BANQUET_SERVICE_DETAIL", "Chi tiết dịch vụ tiệc", "Báo cáo Tiệc", "Theo dõi dịch vụ phát sinh, nhà cung cấp và giá trị từng sự kiện.", "Banquet")
    ];
}

public sealed partial class ReportCatalogItem : ObservableObject
{
    [ObservableProperty] private bool isFavorite;

    public ReportCatalogItem(string code, string name, string category, string description, string dataSource)
    {
        Code = code;
        Name = name;
        Category = category;
        Description = description;
        DataSource = dataSource;
    }

    public string Code { get; }
    public string Name { get; }
    public string Category { get; }
    public string Description { get; }
    public string DataSource { get; }
}

public sealed record ReportCategoryItem(string Name, string DisplayName, int Count)
{
    public string Header => $"{DisplayName} ({Count})";
}

public sealed partial class ReportParameterInput : ObservableObject
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");

    public ReportParameterInput(ReportParameterDto source)
    {
        Name = source.Name;
        DisplayName = source.DisplayName;
        DataType = source.DataType;
        IsRequired = source.IsRequired;
        Value = source.DefaultValue ?? string.Empty;
    }

    public string Name { get; }
    public string DisplayName { get; }
    public string DataType { get; }
    public bool IsRequired { get; }
    [ObservableProperty] private string value = string.Empty;

    public DateTime? DateValue
    {
        get
        {
            if (DateTime.TryParse(Value, ViCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            return null;
        }
        set
        {
            Value = value?.ToString("dd/MM/yyyy", ViCulture) ?? string.Empty;
        }
    }

    partial void OnValueChanged(string value)
    {
        OnPropertyChanged(nameof(DateValue));
    }
}

public sealed class ReportGroupSection
{
    public ReportGroupSection(string title)
    {
        Title = title;
    }

    public string Title { get; }
    public ObservableCollection<ReportCatalogItem> Items { get; } = new();
}

public sealed record ReportColumnViewModel(string Name, string DisplayName, string DataType);

public sealed class ReportGridRow
{
    public ReportGridRow(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row)
    {
        foreach (var column in columns)
        {
            row.Values.TryGetValue(column.Name, out var value);
            Values[column.Name] = FormatValue(value, column.DataType);
        }
    }

    public Dictionary<string, string> Values { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string GetValue(string columnName)
        => Values.TryGetValue(columnName, out var value) ? value : string.Empty;

    public bool Contains(string keyword)
        => Values.Values.Any(value => !string.IsNullOrWhiteSpace(value) && value.Contains(keyword, StringComparison.OrdinalIgnoreCase));

    private static string FormatValue(object? value, string dataType)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is DateTime date)
        {
            return date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        if (value is decimal dec)
        {
            return dec.ToString("N0", CultureInfo.InvariantCulture);
        }

        if (value is double dbl)
        {
            return dbl.ToString("N2", CultureInfo.InvariantCulture);
        }

        if (value is float flt)
        {
            return flt.ToString("N2", CultureInfo.InvariantCulture);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }
}
