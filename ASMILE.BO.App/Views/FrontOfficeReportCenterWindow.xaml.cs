using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.App.Helpers;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class FrontOfficeReportCenterWindow : Window
{
    private readonly List<ReportItem> _allReports;
    private readonly string _categoryFilter;
    private readonly string _sourceModule;
    private readonly IReportingQueryService? _reporting;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly ICurrentUserService? _currentUser;
    private readonly IAuditLogService? _auditLog;
    private IReadOnlyList<FolioBusinessDateGapRow>? _currentFolioGaps;
    private ReportExportData? _currentData;
    private int _loadVersion;
    private bool _isInitializing;
    private bool _isApplyingFilters;
    private bool _isLoading;
    private readonly SemaphoreSlim _queryGate = new(1, 1);
    private readonly IServiceScope? _serviceScope;

    public FrontOfficeReportCenterWindow(string categoryFilter = "All", string sourceModule = "")
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _categoryFilter = categoryFilter;
        _sourceModule = sourceModule;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _reporting = scopedServices?.GetService<IReportingQueryService>();
        _executionContext = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _currentUser = scopedServices?.GetService<ICurrentUserService>();
        _auditLog = scopedServices?.GetService<IAuditLogService>();
        _allReports = BuildReportCatalog();
        _isInitializing = true;
        var categories = new List<string> { "Tất cả" };
        categories.AddRange(_allReports.Select(report => report.Category).Distinct());
        ReportCategoryCombo.ItemsSource = categories;
        ReportCategoryCombo.SelectedItem = ResolveCategoryFilter(_categoryFilter) ?? "Tất cả";
        var businessDate = _executionContext?.Current?.BusinessDate.BusinessDate
            ?? DateOnly.FromDateTime(DateTime.Today);
        FromDatePicker.SelectedDate = businessDate.AddDays(-6).ToDateTime(TimeOnly.MinValue);
        ToDatePicker.SelectedDate = businessDate.ToDateTime(TimeOnly.MinValue);
        _isInitializing = false;
        var hasUnsupportedModuleFilter = _categoryFilter is not ("All" or "") && ResolveCategoryFilter(_categoryFilter) is null;
        FooterStatus.Text = hasUnsupportedModuleFilter
            ? $"Mở từ {_sourceModule} · báo cáo chuyên biệt cho module này chưa được kết nối; đang hiển thị các báo cáo có dữ liệu thật."
            : string.IsNullOrWhiteSpace(_sourceModule)
                ? "Dữ liệu được truy vấn theo cơ sở Front Office đang hoạt động."
                : $"Mở từ {_sourceModule} · dữ liệu được truy vấn theo cơ sở Front Office đang hoạt động.";
        ApplyExportPermission();
        ApplyFilters(reloadWhenSelectionChanges: true);
        Closed += OnClosed;
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        await _queryGate.WaitAsync();
        try { _serviceScope?.Dispose(); }
        finally { _queryGate.Release(); _queryGate.Dispose(); }
    }

    private void ApplyExportPermission()
    {
        var canExport = _currentUser?.HasPermission("reports.export") == true;
        PrintReportButton.IsEnabled = canExport;
        ExportPdfButton.IsEnabled = canExport;
        ExportExcelButton.IsEnabled = canExport;
        ExportCsvButton.IsEnabled = canExport;
        if (!canExport)
        {
            ReportStatus.Text = "Bạn chỉ có quyền xem báo cáo; quyền In/Xuất chưa được cấp.";
        }
    }

    private static List<ReportItem> BuildReportCatalog()
    {
        return
        [
            new("operational", "Tổng hợp vận hành", "Vận hành & công suất", "Khách đã đến/đã đi, dự kiến đến/dự kiến đi, đang lưu trú, no-show, hủy và ảnh chụp trạng thái phòng hiện tại.", "Dữ liệu thật · vận hành"),
            new("occupancy", "Công suất phòng", "Vận hành & công suất", "Tổng phòng, phòng bán, phòng trống, tỷ lệ lấp đầy, ADR và RevPAR theo ngày.", "Dữ liệu thật · lưu trú/phòng"),
            new("revenue", "Doanh thu & thanh toán", "Doanh thu & thu ngân", "Doanh thu phòng, doanh thu khác, thanh toán, đặt cọc, hoàn tiền và số dư Folio theo ngày.", "Dữ liệu thật · Folio"),
            new("cashier", "Thu ngân & đối soát ca", "Doanh thu & thu ngân", "Theo dõi tiền đầu ca/cuối ca, thu, chi, hoàn, chuyển quỹ, điều chỉnh và trạng thái đối soát theo ca.", "Dữ liệu thật · ca thu ngân"),
            new("folio-gaps", "Kiểm soát Folio thiếu ngày", "Kiểm soát Folio", "Danh sách chỉ đọc các bút toán Folio thiếu ngày hạch toán để kế toán đối chiếu trước khi backfill có phê duyệt.", "Dữ liệu thật · kiểm soát sổ liệu"),
        ];
    }

    private static string? ResolveCategoryFilter(string moduleFilter) => moduleFilter switch
    {
        "Operations" => "Vận hành & công suất",
        "Operational" or "FrontOffice" or "Reservation" => "Vận hành & công suất",
        "Occupancy" => "Vận hành & công suất",
        "Finance" => "Doanh thu & thu ngân",
        "Revenue" or "Cashiering" or "AccountsReceivable" => "Doanh thu & thu ngân",
        "FolioGaps" => "Kiểm soát Folio",
        _ => null
    };

    private void ApplyFilters(bool reloadWhenSelectionChanges)
    {
        var category = ReportCategoryCombo.SelectedItem as string;
        var keyword = SearchBox.Text?.Trim();
        var selectedCode = (ReportGrid.SelectedItem as ReportItem)?.Code;
        var reports = _allReports.Where(report =>
                (string.IsNullOrWhiteSpace(category) || category == "Tất cả" || report.Category == category) &&
                (string.IsNullOrWhiteSpace(keyword) ||
                 report.ReportName.Contains(keyword, StringComparison.CurrentCultureIgnoreCase) ||
                 report.Description.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();

        var selected = reports.FirstOrDefault(report => report.Code == selectedCode) ?? reports.FirstOrDefault();
        _isApplyingFilters = true;
        try
        {
            ReportGrid.ItemsSource = reports;
            ResultCount.Text = $"{reports.Count} báo cáo";
            ReportGrid.SelectedItem = selected;
        }
        finally
        {
            _isApplyingFilters = false;
        }
        UpdateDateControls(selected);

        if (selected is null)
        {
            _loadVersion++;
            _currentData = null;
            _isLoading = false;
            ResultGrid.ItemsSource = null;
            ResultGrid.Columns.Clear();
            ReportTitle.Text = "Không có báo cáo phù hợp";
            ReportDescription.Text = "Thay đổi từ khóa hoặc danh mục để xem báo cáo khả dụng.";
            ReportSummary.Text = "Chưa có dữ liệu được truy vấn.";
            ReportStatus.Text = string.Empty;
        }
        else if (reloadWhenSelectionChanges && selected.Code != selectedCode)
        {
            _ = LoadSelectedReportAsync(selected);
        }
    }

    private async void OnReportSelected(object s, SelectionChangedEventArgs e)
    {
        if (_isInitializing || _isApplyingFilters)
            return;
        await LoadSelectedReportAsync(ReportGrid.SelectedItem as ReportItem);
    }

    private void OnCategoryChanged(object s, SelectionChangedEventArgs e)
    {
        if (!_isInitializing)
            ApplyFilters(reloadWhenSelectionChanges: true);
    }

    private void OnSearchTextChanged(object s, TextChangedEventArgs e)
    {
        if (!_isInitializing)
            ApplyFilters(reloadWhenSelectionChanges: true);
    }

    private async void OnApplyFilters(object s, RoutedEventArgs e)
    {
        if (!TryGetSelectedDates(ReportGrid.SelectedItem as ReportItem, out _, out var error))
        {
            ReportStatus.Text = error;
            return;
        }

        ApplyFilters(reloadWhenSelectionChanges: false);
        if (ReportGrid.SelectedItem is ReportItem report)
            await LoadSelectedReportAsync(report);
    }

    private async Task LoadSelectedReportAsync(ReportItem? report)
    {
        var version = ++_loadVersion;
        _currentData = null;
        _currentFolioGaps = null;
        FolioBackfillButton.Visibility = Visibility.Collapsed;
        ResultGrid.ItemsSource = null;
        ResultGrid.Columns.Clear();
        if (report is null)
        {
            _isLoading = false;
            return;
        }

        ReportTitle.Text = report.ReportName;
        ReportDescription.Text = report.Description;
        ReportSummary.Text = "Đang truy vấn dữ liệu…";
        ReportStatus.Text = "Đang tải từ cơ sở dữ liệu của cơ sở đang hoạt động…";
        _isLoading = true;
        await _queryGate.WaitAsync();
        try
        {
            if (version != _loadVersion)
                return;
            if (!TryGetSelectedDates(report, out var range, out var error))
                throw new InvalidOperationException(error);

            var data = await QueryRealReportAsync(report, range.From, range.To);
            if (version != _loadVersion)
                return;

            _currentData = data;
            BindResultRows(data);
            ReportSummary.Text = data.Summary;
            FolioBackfillButton.Visibility = report.Code == "folio-gaps"
                && _currentFolioGaps is { Count: > 0 }
                && _currentUser?.HasPermission("reports.backfill") == true
                ? Visibility.Visible
                : Visibility.Collapsed;
            ReportStatus.Text = $"{report.DataSource} · {data.Rows.Count:N0} dòng · Truy vấn lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}.";
        }
        catch (Exception ex)
        {
            if (version != _loadVersion)
                return;
            ReportSummary.Text = "Không thể tải báo cáo.";
            ReportStatus.Text = ex.Message;
        }
        finally
        {
            _queryGate.Release();
            if (version == _loadVersion)
                _isLoading = false;
        }
    }

    private void BindResultRows(ReportExportData data)
    {
        var table = new DataTable();
        foreach (var header in data.Headers)
            table.Columns.Add(header);
        foreach (var row in data.Rows)
        {
            var values = Enumerable.Range(0, data.Headers.Count)
                .Select(index => (object)(index < row.Count ? row[index] : string.Empty))
                .ToArray();
            table.Rows.Add(values);
        }
        ResultGrid.ItemsSource = table.DefaultView;
    }

    private void UpdateDateControls(ReportItem? report)
    {
        var isDailyReport = report?.Code == "operational";
        FromDatePanel.IsEnabled = !isDailyReport;
        ToDateLabel.Text = isDailyReport ? "Ngày báo cáo" : "Đến ngày";
    }

    private bool TryGetSelectedDates(ReportItem? report, out (DateOnly From, DateOnly To) range, out string error)
    {
        range = default;
        error = string.Empty;
        if (ToDatePicker.SelectedDate is not { } toDate)
        {
            error = "Vui lòng chọn ngày báo cáo.";
            return false;
        }

        var to = DateOnly.FromDateTime(toDate);
        if (report?.Code == "operational")
        {
            range = (to, to);
            return true;
        }

        if (FromDatePicker.SelectedDate is not { } fromDate)
        {
            error = "Vui lòng chọn ngày bắt đầu.";
            return false;
        }
        var from = DateOnly.FromDateTime(fromDate);
        if (from > to)
        {
            error = "Ngày bắt đầu không được sau ngày kết thúc.";
            return false;
        }
        if (to.DayNumber - from.DayNumber > 365)
        {
            error = "Khoảng thời gian tối đa là 366 ngày.";
            return false;
        }

        range = (from, to);
        return true;
    }

    private async Task<ReportExportData> QueryRealReportAsync(ReportItem report, DateOnly from, DateOnly to)
    {
        if (_reporting is null || _executionContext?.Current is not { } context)
            throw new InvalidOperationException("Chưa có dịch vụ báo cáo hoặc chưa chọn cơ sở Front Office.");

        var propertyId = context.Property.PropertyId.Value;
        if (report.Code == "cashier")
        {
            var result = await _reporting.GetCashierReportAsync(new CashierReportQuery(propertyId, from, to), default);
            if (!result.IsSuccess || result.Value is null)
                throw new InvalidOperationException(result.Error?.Message ?? "Không đọc được báo cáo thu ngân.");

            var rows = result.Value.Select(x => (IReadOnlyList<string>)[
                x.BusinessDate.ToString("yyyy-MM-dd"), x.ShiftNumber, x.CashierName, x.Status,
                x.OpeningCash.ToString("N0"), x.CashIn.ToString("N0"), x.CashOut.ToString("N0"),
                x.Refund.ToString("N0"), x.TransferIn.ToString("N0"), x.TransferOut.ToString("N0"),
                x.Adjustment.ToString("N0"), x.ExpectedCash.ToString("N0"),
                x.ActualCash?.ToString("N0") ?? "Chưa kiểm đếm", x.Difference?.ToString("N0") ?? "Chưa có",
                x.ReconciliationStatus ?? "Chưa đối soát"
            ]).ToList();
            return new ReportExportData(
                $"{from:dd/MM/yyyy} – {to:dd/MM/yyyy} · {result.Value.Count} ca · Thu {result.Value.Sum(x => x.CashIn):N0} · Chi {result.Value.Sum(x => x.CashOut):N0} · Hoàn {result.Value.Sum(x => x.Refund):N0} · Điều chỉnh {result.Value.Sum(x => x.Adjustment):N0} · Chênh lệch {(result.Value.Any(x => !x.Difference.HasValue) ? "còn ca chưa kiểm đếm" : result.Value.Sum(x => x.Difference ?? 0m).ToString("N0"))}",
                ["Ngày", "Số ca", "Thu ngân", "Trạng thái ca", "Đầu ca", "Thu", "Chi", "Hoàn", "Chuyển vào", "Chuyển ra", "Điều chỉnh", "Dự kiến", "Thực tế", "Chênh lệch", "Đối soát"],
                rows);
        }
        if (report.Code == "folio-gaps")
        {
            var result = await _reporting.GetFolioBusinessDateGapReportAsync(new FolioBusinessDateGapQuery(propertyId, from, to), default);
            if (!result.IsSuccess || result.Value is null)
                throw new InvalidOperationException(result.Error?.Message ?? "Không đọc được danh sách Folio cần đối chiếu.");

            _currentFolioGaps = result.Value;

            var rows = result.Value.Select(x => (IReadOnlyList<string>)[
                x.EntryId.ToString("D"), x.FolioId.ToString("D"), x.EntryType, x.Amount.ToString("N0"),
                x.CreatedAtUtc.ToString("dd/MM/yyyy HH:mm 'UTC'"), x.SuggestedBusinessDate.ToString("dd/MM/yyyy"), x.Description
            ]).ToList();
            return new ReportExportData(
                $"{from:dd/MM/yyyy} – {to:dd/MM/yyyy} · {result.Value.Count} bút toán thiếu ngày hạch toán · Giá trị ròng {result.Value.Sum(x => x.Amount):N0}. Ngày đề xuất chỉ suy ra từ thời điểm tạo theo múi giờ cơ sở, không phải lệnh tự động sửa sổ liệu.",
                ["Mã bút toán", "Mã Folio", "Loại", "Giá trị", "Tạo lúc (UTC)", "Ngày đề xuất", "Diễn giải"],
                rows);
        }
        if (report.Code == "revenue")
        {
            var result = await _reporting.GetRevenueReportAsync(new RevenueReportQuery(propertyId, from, to), default);
            if (!result.IsSuccess || result.Value is null)
                throw new InvalidOperationException(result.Error?.Message ?? "Không đọc được báo cáo doanh thu.");

            var rows = result.Value.Select(x => (IReadOnlyList<string>)[
                x.Date.ToString("yyyy-MM-dd"),
                x.RoomRevenue.ToString("N0"),
                x.OtherRevenue.ToString("N0"),
                x.TotalRevenue.ToString("N0"),
                x.TotalPayments.ToString("N0"),
                x.TotalDeposits.ToString("N0"),
                x.TotalRefunds.ToString("N0"),
                x.FolioBalance.ToString("N0")
            ]).ToList();
            return new ReportExportData(
                $"{from:dd/MM/yyyy} – {to:dd/MM/yyyy} · {result.Value.Count} ngày · Tổng doanh thu {result.Value.Sum(x => x.TotalRevenue):N0} · Đã thu {result.Value.Sum(x => x.TotalPayments):N0} · Đặt cọc {result.Value.Sum(x => x.TotalDeposits):N0} · Hoàn tiền {result.Value.Sum(x => x.TotalRefunds):N0} · Số dư Folio {result.Value.Sum(x => x.FolioBalance):N0}",
                ["Ngày", "Doanh thu phòng", "Doanh thu khác", "Tổng doanh thu", "Thanh toán", "Đặt cọc", "Hoàn tiền", "Số dư Folio"],
                rows);
        }
        if (report.Code == "occupancy")
        {
            var result = await _reporting.GetOccupancyReportAsync(new OccupancyReportQuery(propertyId, from, to), default);
            if (!result.IsSuccess || result.Value is null)
                throw new InvalidOperationException(result.Error?.Message ?? "Không đọc được báo cáo công suất phòng.");

            var rows = result.Value.Select(x => (IReadOnlyList<string>)[
                x.Date.ToString("yyyy-MM-dd"),
                x.TotalRooms.ToString(),
                x.RoomsSold.ToString(),
                x.RoomsAvailable.ToString(),
                $"{x.OccupancyPct:N1}%",
                x.ADR.ToString("N0"),
                x.RevPAR.ToString("N0")
            ]).ToList();
            return new ReportExportData(
                $"{from:dd/MM/yyyy} – {to:dd/MM/yyyy} · {result.Value.Count} ngày · Công suất bình quân {(result.Value.Count == 0 ? 0 : result.Value.Average(x => x.OccupancyPct)):N1}% · ADR bình quân {(result.Value.Count == 0 ? 0 : result.Value.Average(x => x.ADR)):N0}",
                ["Ngày", "Tổng phòng", "Phòng bán", "Phòng trống", "Công suất", "ADR", "RevPAR"],
                rows);
        }
        if (report.Code != "operational")
            throw new InvalidOperationException("Mã báo cáo không được hỗ trợ.");

        var operational = await _reporting.GetOperationalReportAsync(new OperationalReportQuery(propertyId, to), default);
        if (!operational.IsSuccess || operational.Value is null)
            throw new InvalidOperationException(operational.Error?.Message ?? "Không đọc được báo cáo vận hành.");

        var value = operational.Value;
        var roomStatusScope = value.IsRoomStatusHistorical
            ? $"Tình trạng phòng được tái dựng từ lịch sử, có dữ liệu cho {value.RoomStatusHistoryCoverage} phòng tại thời điểm cuối ngày."
            : "Tình trạng phòng là ảnh chụp hiện tại lúc truy vấn; dữ liệu lịch sử chưa có cho ngày được chọn.";
        return new ReportExportData(
                $"Ngày vận hành {to:dd/MM/yyyy} · {value.Arrivals} đã đến · {value.Departures} đã đi · {value.InHouse} đang lưu trú · {value.NoShows} no-show · {value.Cancellations} hủy. {roomStatusScope}",
            ["Ngày", "Khách đến", "Khách đi", "No-show", "Hủy", "Đang lưu trú", "Dự kiến đến", "Dự kiến đi", "Phòng sạch trống", "Phòng bẩn", "Phòng OOO"],
            [[
                value.Date.ToString("dd/MM/yyyy"), value.Arrivals.ToString("N0"), value.Departures.ToString("N0"),
                value.NoShows.ToString("N0"), value.Cancellations.ToString("N0"), value.InHouse.ToString("N0"),
                value.ExpectedArrivals.ToString("N0"), value.ExpectedDepartures.ToString("N0"),
                value.VacantClean.ToString("N0"), value.VacantDirty.ToString("N0"), value.OutOfOrder.ToString("N0")
            ]]);
    }

    private async void OnRefresh(object s, RoutedEventArgs e)
    {
        if (ReportGrid.SelectedItem is ReportItem report)
            await LoadSelectedReportAsync(report);
    }

    private async void OnRefreshSelected(object s, RoutedEventArgs e)
    {
        if (ReportGrid.SelectedItem is ReportItem report)
            await LoadSelectedReportAsync(report);
        else
            ReportStatus.Text = "Vui lòng chọn một báo cáo.";
    }

    private async void OnPrint(object s, RoutedEventArgs e) => await RunSelectedReportActionAsync("In");
    private async void OnExportPdf(object s, RoutedEventArgs e) => await RunSelectedReportActionAsync("PDF");
    private async void OnExportExcel(object s, RoutedEventArgs e) => await RunSelectedReportActionAsync("Excel");
    private async void OnExportCsv(object s, RoutedEventArgs e) => await RunSelectedReportActionAsync("CSV");

    private async void OnFolioBackfill(object s, RoutedEventArgs e)
    {
        if (_currentFolioGaps is not { Count: > 0 })
        {
            ReportStatus.Text = "Chưa có dòng Folio thiếu ngày để backfill.";
            return;
        }
        if (_currentUser?.HasPermission("reports.backfill") != true)
        {
            ReportStatus.Text = "Bạn không có quyền backfill ngày hạch toán Folio (reports.backfill).";
            return;
        }

        var dialog = new FolioBusinessDateBackfillWindow(_currentFolioGaps)
        {
            Owner = this
        };
        if (dialog.ShowDialog() == true)
        {
            ReportStatus.Text = "Đã backfill Folio; đang tải lại danh sách để xác nhận kết quả…";
            if (ReportGrid.SelectedItem is ReportItem report)
                await LoadSelectedReportAsync(report);
        }
    }

    private async Task RunSelectedReportActionAsync(string action)
    {
        if (_currentUser?.HasPermission("reports.export") != true)
        {
            ReportStatus.Text = "Bạn không có quyền In/Xuất báo cáo (reports.export).";
            return;
        }

        if (ReportGrid.SelectedItem is not ReportItem report)
        {
            ReportStatus.Text = "Vui lòng chọn một báo cáo trước khi thao tác.";
            return;
        }
        if (_isLoading)
        {
            ReportStatus.Text = "Báo cáo đang được tải. Vui lòng đợi hoàn tất rồi thử lại.";
            return;
        }

        var data = _currentData;
        if (data is null)
        {
            await LoadSelectedReportAsync(report);
            data = _currentData;
        }
        if (data is null)
            return;

        try
        {
            var exportData = PrepareExportData(report, data, out var sensitiveDataMasked);
            if (action == "PDF")
            {
                var path = PdfExportHelper.Export(report.ReportName, "front_office_report", exportData.Headers, exportData.Rows);
                if (path is null)
                {
                    ReportStatus.Text = "Đã hủy xuất PDF.";
                    return;
                }
                ReportStatus.Text = $"Đã xuất PDF: {path}";
                await AuditExportAsync(report, exportData, action, path, sensitiveDataMasked);
                MessageBox.Show(ReportStatus.Text, "Báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (action == "CSV")
            {
                var path = CsvExportHelper.Export("front_office_report", exportData.Headers, exportData.Rows);
                ReportStatus.Text = $"Đã xuất CSV (có thể mở bằng Excel): {path}";
                await AuditExportAsync(report, exportData, action, path, sensitiveDataMasked);
                MessageBox.Show(ReportStatus.Text, "Báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (action == "Excel")
            {
                var path = ExcelExportHelper.Export(report.ReportName, "front_office_report", exportData.Summary, exportData.Headers, exportData.Rows);
                if (path is null)
                {
                    ReportStatus.Text = "Đã hủy xuất Excel.";
                    return;
                }
                ReportStatus.Text = $"Đã xuất Excel: {path}";
                await AuditExportAsync(report, exportData, action, path, sensitiveDataMasked);
                MessageBox.Show(ReportStatus.Text, "Báo cáo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var content = new StringBuilder()
                    .AppendLine(report.ReportName)
                    .AppendLine(new string('=', 80))
                    .AppendLine(exportData.Summary)
                    .AppendLine()
                    .AppendLine(string.Join(" | ", exportData.Headers))
                    .AppendLine(new string('-', 120));
                foreach (var row in exportData.Rows)
                    content.AppendLine(string.Join(" | ", row));
                new PrintPreviewDialog(report.ReportName, content.ToString()) { Owner = this }.ShowDialog();
                await AuditExportAsync(report, exportData, action, "PrintPreview", sensitiveDataMasked);
            }
        }
        catch (Exception ex)
        {
            ReportStatus.Text = $"Không thể {action.ToLowerInvariant()} báo cáo: {ex.Message}";
        }
    }

    private ReportExportData PrepareExportData(ReportItem report, ReportExportData data, out bool sensitiveDataMasked)
    {
        sensitiveDataMasked = false;
        if (report.Code != "cashier" || _currentUser?.HasPermission("reports.export.sensitive") == true)
            return data;

        var cashierColumn = data.Headers
            .Select((header, index) => (header, index))
            .FirstOrDefault(x => string.Equals(x.header, "Thu ngân", StringComparison.OrdinalIgnoreCase));
        if (cashierColumn.header is null || cashierColumn.index < 0)
            return data;

        sensitiveDataMasked = true;
        var rows = data.Rows.Select(row =>
        {
            var values = row.ToArray();
            if (cashierColumn.index < values.Length)
                values[cashierColumn.index] = "Đã ẩn theo phân quyền";
            return (IReadOnlyList<string>)values;
        }).ToList();
        return data with
        {
            Summary = $"{data.Summary} · Tên thu ngân đã được ẩn khi xuất/in.",
            Rows = rows
        };
    }

    private async Task AuditExportAsync(ReportItem report, ReportExportData data, string action, string path, bool sensitiveDataMasked)
    {
        if (_auditLog is null)
            return;

        try
        {
            var range = TryGetSelectedDates(report, out var selectedRange, out _)
                ? $"{selectedRange.From:yyyy-MM-dd}..{selectedRange.To:yyyy-MM-dd}"
                : "unknown";
            var propertyId = _executionContext?.Current?.Property.PropertyId.Value.ToString("D") ?? "unknown";
            var username = _currentUser?.CurrentUser?.UserName ?? "system";
            await _auditLog.LogAsync(
                "REPORT_EXPORT",
                $"report={report.Code};format={action};property={propertyId};range={range};rows={data.Rows.Count};sensitive={(sensitiveDataMasked ? "masked" : "full")};target={path}",
                username);
        }
        catch (Exception ex)
        {
            ReportStatus.Text += $" Cảnh báo: chưa ghi được audit xuất báo cáo ({ex.Message}).";
        }
    }

    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record ReportItem(string Code, string ReportName, string Category, string Description, string DataSource);

internal sealed record ReportExportData(
    string Summary,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);
