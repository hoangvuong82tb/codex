using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Rate;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class ExchangeRateWindow : Window
{
    private readonly List<RateRow> _allRates = [];
    private readonly IExchangeRateQueryService? _query;
    private readonly IExchangeRateCommandService? _commands;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);
    private bool _isClosed;
    private int _currentPage;
    private const int PageSize = 20;

    public ExchangeRateWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _query = scopedServices?.GetService<IExchangeRateQueryService>();
        _commands = scopedServices?.GetService<IExchangeRateCommandService>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        ShowTab(0); UpdateMetrics(); ApplyFilters();
        FooterTotal.Text = "Đang tải tỷ giá từ database thật...";
        Loaded += OnLoadedAsync;
        Closed += OnClosed;
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e) => await ReloadAsync();

    private async void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        await _dataOperationGate.WaitAsync();
        try { _serviceScope?.Dispose(); }
        finally { _dataOperationGate.Release(); _dataOperationGate.Dispose(); }
    }

    private async Task ReloadAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { await ReloadCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task ReloadCoreAsync()
    {
        if (_isClosed) return;
        _allRates.Clear();
        if (_query is null || _contextAccessor?.Current is not { } context)
        { FooterTotal.Text = "Chưa cấu hình context hoặc query tỷ giá thật; không hiển thị dữ liệu mẫu."; ApplyFilters(); return; }
        try
        {
            var rows = await _query.SearchAsync(new ExchangeRateSearchRequest(context.Property.PropertyId.Value), CancellationToken.None);
            _allRates.AddRange(rows.Select(Map));
            FooterTotal.Text = $"Đã tải {_allRates.Count:N0} tỷ giá từ database thật.";
        }
        catch (Exception ex) { FooterTotal.Text = $"Không tải được tỷ giá thật: {ex.Message}"; }
        ApplyFilters();
    }

    private static RateRow Map(ExchangeRateDto x) => new(x.HotelExchangeRateId, x.RowVersion, x.CurrencyCode, x.CurrencyName, x.Symbol,
        Format(x.BuyRate), Format(x.SellRate), Format(x.HotelRate), x.EffectiveFromUtc.ToLocalTime().ToString("dd/MM/yyyy"),
        x.EffectiveToUtc?.ToLocalTime().ToString("dd/MM/yyyy") ?? "—", Label(x.DisplayState), Badge(x.DisplayState), Color(x.DisplayState),
        x.UpdatedAtUtc.ToLocalTime().ToString("dd/MM HH:mm"), x.IsDefault, x.EffectiveFromUtc, x.EffectiveToUtc, x.Notes, x.DisplayState);

    private static string Format(decimal value) => value.ToString("N2", CultureInfo.GetCultureInfo("vi-VN"));
    private static string Label(ExchangeRateDisplayState state) => state switch { ExchangeRateDisplayState.Effective => "Hiệu lực", ExchangeRateDisplayState.Expiring => "Sắp hết hạn", ExchangeRateDisplayState.Expired => "Hết hạn", ExchangeRateDisplayState.Scheduled => "Chờ hiệu lực", ExchangeRateDisplayState.Draft => "Nháp", _ => "Không hoạt động" };
    private static string Badge(ExchangeRateDisplayState state) => state switch { ExchangeRateDisplayState.Effective => "#DCFCE7", ExchangeRateDisplayState.Expiring or ExchangeRateDisplayState.Scheduled => "#FFF7ED", ExchangeRateDisplayState.Expired => "#FEE2E2", _ => "#F1F5F9" };
    private static string Color(ExchangeRateDisplayState state) => state switch { ExchangeRateDisplayState.Effective => "#16A34A", ExchangeRateDisplayState.Expiring or ExchangeRateDisplayState.Scheduled => "#EA580C", ExchangeRateDisplayState.Expired => "#DC2626", _ => "#64748B" };

    private void ApplyFilters()
    {
        IEnumerable<RateRow> rows = _allRates;
        var keyword = SearchBox.Text?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword)) rows = rows.Where(x => x.CurrencyCode.Contains(keyword, StringComparison.OrdinalIgnoreCase) || x.CurrencyName.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        if (StatusFilter.SelectedItem is ComboBoxItem item && item.Content is string status && status != "Tất cả") rows = rows.Where(x => x.Status == status);
        var list = rows.ToList(); _currentPage = Math.Min(_currentPage, Math.Max(0, (list.Count - 1) / PageSize)); ApplyPage(list); UpdateMetrics(list);
    }

    private void ApplyPage(List<RateRow> list)
    { RateGrid.ItemsSource = list.Skip(_currentPage * PageSize).Take(PageSize).ToList(); ResultCount.Text = $"{list.Count} ngoại tệ"; if (_allRates.Count > 0) FooterTotal.Text = $"Đã tải {_allRates.Count:N0} tỷ giá từ database thật · Đang lọc {list.Count:N0}"; }
    private void UpdateMetrics(List<RateRow>? list = null)
    {
        list ??= _allRates; MetricActive.Text = list.Count.ToString(); MetricEffective.Text = list.Count(x => x.DisplayState == ExchangeRateDisplayState.Effective).ToString(); MetricExpiring.Text = list.Count(x => x.DisplayState == ExchangeRateDisplayState.Expiring).ToString(); MetricExpired.Text = list.Count(x => x.DisplayState == ExchangeRateDisplayState.Expired).ToString(); MetricDefault.Text = list.FirstOrDefault(x => x.IsDefault)?.CurrencyCode ?? "—";
    }

    private void OnRateSelected(object sender, SelectionChangedEventArgs e)
    {
        if (RateGrid.SelectedItem is not RateRow r) { DetailEmpty.Visibility = Visibility.Visible; DetailContent.Visibility = Visibility.Collapsed; TabContent.Children.Clear(); return; }
        DetailEmpty.Visibility = Visibility.Collapsed; DetailContent.Visibility = Visibility.Visible; DetailCode.Text = r.CurrencyCode; DetailName.Text = r.CurrencyName; DetailStatus.Text = r.Status; DetailStatus.Foreground = C(r.StatusColor); DetailStatusBadge.Background = Cb(r.StatusBadge); DetailMeta.Text = $"1 {r.CurrencyCode} = {r.SellRate} VND · {r.EffectiveDate} → {r.ExpiredDate}"; ShowTab(CurrentTab);
    }
    private int CurrentTab => int.TryParse(TabContent.Tag as string, out var value) ? value : 0;
    private void ShowTab(int tab) { TabContent.Tag = tab.ToString(); TabContent.Children.Clear(); if (RateGrid.SelectedItem is not RateRow r) return; if (tab == 0) ShowOverview(r); else if (tab == 1) ShowHistory(r); else if (tab == 2) ShowApply(r); else ShowNotes(r); }
    private void ShowOverview(RateRow r) { TabContent.Children.Add(S("Thông tin", R("Mã", r.CurrencyCode), R("Tên", r.CurrencyName), R("Symbol", r.Symbol), R("Mặc định", r.IsDefault ? "Có" : "Không"))); TabContent.Children.Add(S("Tỷ giá", R("Mua vào", r.BuyRate), R("Bán ra", r.SellRate), R("Hotel Rate", r.HotelRate))); TabContent.Children.Add(S("Hiệu lực", R("Ngày HL", r.EffectiveDate), R("Ngày HH", r.ExpiredDate), R("Cập nhật", r.LastUpdated), R("Trạng thái", r.Status))); }
    private void ShowHistory(RateRow r) => TabContent.Children.Add(S("Lịch sử thay đổi", R("Cập nhật gần nhất", r.LastUpdated), R("Nguồn", "Audit log thật của Front Office"), R("Ghi chú", "Chi tiết lịch sử được lưu theo từng thao tác thêm, sửa và đặt mặc định.")));
    private void ShowApply(RateRow r) => TabContent.Children.Add(S("Áp dụng cho", R("Folio", "Theo currency của giao dịch"), R("Cashiering", "Theo currency của giao dịch"), R("Deposit", "Theo currency của giao dịch"), R("Refund", "Theo currency của giao dịch")));
    private void ShowNotes(RateRow r) => TabContent.Children.Add(S("Ghi chú", R("Nội bộ", string.IsNullOrWhiteSpace(r.Notes) ? "—" : r.Notes!)));
    private static SolidColorBrush C(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static SolidColorBrush Cb(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static FrameworkElement S(string title, params FrameworkElement[] children) { var all = new StackPanel(); all.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#0F172A"), Margin = new Thickness(0, 0, 0, 6) }); foreach (var child in children) all.Children.Add(child); return new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), BorderBrush = Cb("#F1F5F9"), BorderThickness = new Thickness(1), Child = all }; }
    private static TextBlock R(string label, string value) { var tb = new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }; tb.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = C("#94A3B8") }); tb.Inlines.Add(new System.Windows.Documents.Run(value) { FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }); return tb; }
    private void OnTabClick(object s, RoutedEventArgs e) { if (s is Button b && int.TryParse(b.Tag?.ToString(), out var tab)) ShowTab(tab); }
    private void OnFilterChanged(object s, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object s, RoutedEventArgs e) { SearchBox.Text = string.Empty; StatusFilter.SelectedIndex = 0; ApplyFilters(); }
    private async void OnRefresh(object s, RoutedEventArgs e) => await ReloadAsync();
    private void OnPrevPage(object s, RoutedEventArgs e) { if (_currentPage > 0) { _currentPage--; ApplyFilters(); } }
    private void OnNextPage(object s, RoutedEventArgs e) { if ((_currentPage + 1) * PageSize < _allRates.Count) { _currentPage++; ApplyFilters(); } }
    private async void OnAddRate(object s, RoutedEventArgs e) => await EditAsync(null, false);
    private async void OnEdit(object s, RoutedEventArgs e) { if (RateGrid.SelectedItem is RateRow row) await EditAsync(row, false); else Warn("Vui lòng chọn ngoại tệ."); }
    private async void OnCopy(object s, RoutedEventArgs e) { if (RateGrid.SelectedItem is RateRow row) await EditAsync(row, true); else Warn("Vui lòng chọn ngoại tệ."); }
    private async Task EditAsync(RateRow? row, bool copy)
    {
        await _dataOperationGate.WaitAsync();
        try { await EditCoreAsync(row, copy); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task EditCoreAsync(RateRow? row, bool copy)
    {
        if (_isClosed) return;
        if (_commands is null || _contextAccessor?.Current is not { } context) { Warn("Chưa cấu hình command hoặc property context thật."); return; }
        ExchangeRateDto? source = null;
        if (row is not null && _query is not null) source = (await _query.SearchAsync(new ExchangeRateSearchRequest(context.Property.PropertyId.Value, row.CurrencyCode), CancellationToken.None)).FirstOrDefault(x => x.HotelExchangeRateId == row.Id);
        var editor = new ExchangeRateEditorWindow(source, copy) { Owner = this };
        if (editor.ShowDialog() != true) return;
        try
        {
            var v = editor.Values!;
            if (row is null || copy) await _commands.CreateAsync(new CreateExchangeRateRequest(context.Property.PropertyId.Value, v.CurrencyCode, v.CurrencyName, v.Symbol, v.BuyRate, v.SellRate, v.HotelRate, v.EffectiveFromUtc, v.EffectiveToUtc, v.IsDefault, v.Notes), CancellationToken.None);
            else await _commands.UpdateAsync(new UpdateExchangeRateRequest(row.Id, v.CurrencyCode, v.CurrencyName, v.Symbol, v.BuyRate, v.SellRate, v.HotelRate, v.EffectiveFromUtc, v.EffectiveToUtc, v.Notes, row.RowVersion), CancellationToken.None);
            await ReloadCoreAsync();
        }
        catch (Exception ex) { Warn($"Không lưu được tỷ giá thật: {ex.Message}"); }
    }
    private async void OnSetDefault(object s, RoutedEventArgs e)
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            if (_isClosed || RateGrid.SelectedItem is not RateRow row) { Warn("Vui lòng chọn ngoại tệ."); return; }
            if (_commands is null) { Warn("Chưa cấu hình command tỷ giá thật."); return; }
            try { await _commands.SetDefaultAsync(new SetDefaultExchangeRateRequest(row.Id, row.RowVersion), CancellationToken.None); await ReloadCoreAsync(); }
            catch (Exception ex) { Warn($"Không đặt được tỷ giá mặc định: {ex.Message}"); }
        }
        finally { _dataOperationGate.Release(); }
    }
    private void OnViewHistory(object s, RoutedEventArgs e) { if (RateGrid.SelectedItem is RateRow) ShowTab(1); else Warn("Vui lòng chọn ngoại tệ."); }
    private static void Warn(string message) => MessageBox.Show(message, "Tỷ giá", MessageBoxButton.OK, MessageBoxImage.Warning);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal sealed record RateRow(Guid Id, byte[]? RowVersion, string CurrencyCode, string CurrencyName, string Symbol, string BuyRate, string SellRate, string HotelRate, string EffectiveDate, string ExpiredDate, string Status, string StatusBadge, string StatusColor, string LastUpdated, bool IsDefault, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, string? Notes, ExchangeRateDisplayState DisplayState);
