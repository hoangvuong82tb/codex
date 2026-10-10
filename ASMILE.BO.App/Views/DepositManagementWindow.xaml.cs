using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Reservation;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Folio;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class DepositManagementWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private DpItem? _item; private bool _selected;
    private int _page, _pageSize = 20;
    private readonly List<DpItem> _allItems;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly HotelFolioApplicationService? _folioService;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IReservationRepository? _reservationRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private bool _loadedRealData;
    private decimal _required, _have, _depositNow;
    private string _method = "—", _currency = "—", _ref = string.Empty;
    private int _unallocated;

    public DepositManagementWindow()
    {
        InitializeComponent(); Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _folioService = services?.GetService<HotelFolioApplicationService>();
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _reservationRepository = services?.GetService<IReservationRepository>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _allItems = new List<DpItem>(); InitFilters(); RefreshPage(true); GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        _loadedRealData = true;
        if (_folioRepository is null || _folioService is null || _stayRepository is null || _reservationRepository is null || _executionContext?.Current is null)
        {
            _allItems.Clear();
            DepEmptyState.Visibility = Visibility.Visible;
            DepDetailPanel.Visibility = Visibility.Collapsed;
            FooterStepLabel.Text = "Chưa cấu hình được Folio service và property context dữ liệu thật.";
            RefreshPage(true);
            return;
        }

        try
        {
            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            _currency = string.IsNullOrWhiteSpace(_executionContext.Current.Property.CurrencyCode)
                ? "—"
                : _executionContext.Current.Property.CurrencyCode.Trim();
            var page = await _folioRepository.SearchAsync(
                new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
                new PageRequest(1, 200), CancellationToken.None);
            _allItems.Clear();
            // The repositories share the scoped EF DbContext. Mapping a page in
            // parallel starts concurrent operations on that context and can make
            // the deposit screen fail with "A second operation was started...".
            // Keep the I/O sequential until the read model is projected by one
            // repository/query scope.
            foreach (var item in page.Items)
                _allItems.Add(await MapPersistedFolioAsync(propertyId, item));
            _item = null;
            _selected = false;
            DepEmptyState.Visibility = Visibility.Visible;
            DepDetailPanel.Visibility = Visibility.Collapsed;
            RefreshPage(true);
            UpdateSummary();
            UpdateNav();
        }
        catch (Exception ex)
        {
            _allItems.Clear();
            DepEmptyState.Visibility = Visibility.Visible;
            DepDetailPanel.Visibility = Visibility.Collapsed;
            FooterStepLabel.Text = $"Không tải được Folio thật: {ex.Message}";
            RefreshPage(true);
        }
    }

    private async Task<DpItem> MapPersistedFolioAsync(Guid propertyId, FolioLedgerDto ledger)
    {
        var deposited = ledger.Entries
            .Where(entry => entry.EntryType == HotelFolioEntryType.Deposit && !entry.ReversedByEntryId.HasValue)
            .Sum(entry => Math.Abs(entry.Amount));
        var charges = ledger.Entries
            .Where(entry => entry.Amount > 0 && entry.EntryType is not HotelFolioEntryType.Deposit and not HotelFolioEntryType.Payment)
            .Sum(entry => entry.Amount);
        var stay = await _stayRepository!.GetDtoAsync(propertyId, ledger.HotelStayId, CancellationToken.None);
        var reservation = stay?.ReservationId is { } reservationId
            ? await _reservationRepository!.GetDtoAsync(reservationId, propertyId, CancellationToken.None)
            : null;
        var required = reservation?.DepositAmount ?? 0m;
        var hasPolicy = reservation is not null;
        var status = !hasPolicy
            ? "Chưa có policy"
            : required <= 0m
                ? "Không yêu cầu"
                : deposited >= required ? "Đã đủ" : deposited == 0 ? "Chưa có" : "Chưa đủ";
        var source = hasPolicy
            ? $"Reservation {reservation!.ReservationNumber} · {reservation.DepositStatus}"
            : "Chưa có reservation policy";
        return new DpItem("Folio", ledger.GuestName, ledger.RoomNumber, required, deposited, status,
            reservation?.IsVip ?? false, ledger.HotelFolioId, ledger.RowVersion, source,
            reservation?.DepositDueDate, reservation?.GuaranteeType.ToString(), charges, hasPolicy);
    }

    private void InitFilters() { Set(DepVipFilter, "Tất cả","VIP","Không VIP"); Set(DepStatusFilter, "Tất cả","Chưa đủ","Đã đủ","Chưa có","Vượt cọc","Chưa PB"); Set(DepPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn hồ sơ"),("B","Yêu cầu cọc"),("C","Nhận đặt cọc"),("D","Phân bổ"),("E","Biên nhận"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = _selected && _depositNow > 0; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/6 · {_depositNow:N0} {_currency} · Chưa PB: {_unallocated:N0}";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var d = _item; var sel = _selected;
        SummaryStrip.Children.Add(Strip("Khách", d?.Name ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", d?.Room ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Yêu cầu", sel ? $"{_required:N0}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Đã cọc", sel ? $"{_have:N0}" : "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Thiếu", sel ? $"{Math.Max(0, _required - _have):N0}" : "—", _required <= _have)); SummaryStrip.Children.Add(Sep());
        var ready = sel && (!_item!.HasDepositPolicy || _required <= _have + _depositNow);
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Đủ cọc" : "Thiếu", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    private List<DpItem> GetFiltered() { var f = _allItems.AsEnumerable(); var kw = DepSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw)); var v = DepVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); var s = DepStatusFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(s) && s != "Tất cả") f = f.Where(g => g.Status == s); return f.ToList(); }
    private void RefreshPage(bool reset) { if (reset) _page = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _pageSize - 1) / _pageSize); if (_page >= tp) _page = tp - 1; DepGrid.ItemsSource = f.Skip(_page * _pageSize).Take(_pageSize).ToList(); DepPagerSummary.Text = $"Hiển thị {_page * _pageSize + 1}–{Math.Min((_page + 1) * _pageSize, f.Count)} trên tổng {f.Count} hồ sơ"; DepPrevBtn.IsEnabled = _page > 0; DepNextBtn.IsEnabled = _page < tp - 1; BuildBtns(tp); }
    private void BuildBtns(int tp) { DepPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _page ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _page ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _page ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _page = idx; RefreshPage(false); }; nb.Template = DepPrevBtn.Template; DepPageBtns.Children.Add(nb); } }
    private void OnDepPreviewChanged(object s, SelectionChangedEventArgs e) { if (DepGrid.SelectedItem is not DpItem d) return; _item = d; _selected = false; _required = d.Required; _have = d.Have; _depositNow = d.HasDepositPolicy ? Math.Max(0, _required - _have) : 0m; DepEmptyState.Visibility = Visibility.Collapsed; DepDetailPanel.Visibility = Visibility.Visible; DDName.Text = d.Name; DDMeta.Text = $"{d.Type} · {d.Status}"; DDInfo1.Text = $"Phòng: {d.Room}"; DDInfo2.Text = $"Yêu cầu: {d.Required:N0} · Đã cọc: {d.Have:N0}"; DDInfo3.Text = d.HasDepositPolicy ? $"Còn thiếu: {Math.Max(0, d.Required - d.Have):N0}" : "Chưa có policy đặt cọc"; DDInfo4.Text = d.HasDepositPolicy && d.Have > d.Required ? $"Vượt: {d.Have - d.Required:N0}" : "Theo dõi theo policy Reservation"; UpdateSummary(); }
    private void OnSelectDep(object s, RoutedEventArgs e) { if (_item == null) return; _selected = true; if (_item.HasDepositPolicy) _depositNow = Math.Min(_depositNow, Math.Max(0, _required - _have)); BtnSelectDep.Content = "✓ Đã chọn"; BtnSelectDep.IsEnabled = false; UpdateSummary(); UpdateNav(); }
    private void OnApplyDepFilters(object s, RoutedEventArgs e) => RefreshPage(true);
    private void OnClearDepFilters(object s, RoutedEventArgs e) { DepSearchBox.Text = ""; DepVipFilter.SelectedIndex = 0; DepStatusFilter.SelectedIndex = 0; RefreshPage(true); }
    private void OnDepPrevPage(object s, RoutedEventArgs e) { if (_page > 0) { _page--; RefreshPage(false); } }
    private void OnDepNextPage(object s, RoutedEventArgs e) { _page++; RefreshPage(false); }
    private void OnDepPageSizeChanged(object s, SelectionChangedEventArgs e) { _pageSize = DepPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshPage(true); }

    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        Step2Left.Children.Add(STitle("Chính sách đặt cọc")); Step2Left.Children.Add(Row("Nguồn chính sách", _item?.PolicySource ?? "Chưa có policy")); Step2Left.Children.Add(Row("Yêu cầu từ Reservation", _item is null ? "—" : $"{_required:N0} {_currency}")); Step2Left.Children.Add(Row("Currency", _currency)); Step2Left.Children.Add(Row("Hạn cọc", _item?.DepositDueDate?.ToString("dd/MM/yyyy") ?? "Không có hạn cọc")); Step2Left.Children.Add(Row("Bảo đảm", _item?.GuaranteeType ?? "Không có"));
        Step2Right.Children.Add(STitle("Tính toán từ Folio thật")); Step2Right.Children.Add(new Border { Background = Cb("#DCFCE7"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14), Child = new StackPanel { Children = { new TextBlock { Text = $"Yêu cầu cọc: {_required:N0} {_currency}", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = C("#17365D") }, new TextBlock { Text = $"Đã cọc: {_have:N0} {_currency} · Thiếu: {Math.Max(0, _required - _have):N0} {_currency}", FontSize = 12, Foreground = C("#64748B"), Margin = new Thickness(0, 4, 0, 0) } } } });
        var policyReady = _item?.HasDepositPolicy != true || _required <= _have;
        Step2Right.Children.Add(new Border { Background = _item?.HasDepositPolicy != true ? Cb("#EFF6FF") : policyReady ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = _item?.HasDepositPolicy != true ? "ℹ Chưa có policy đặt cọc cho Reservation" : policyReady ? "✓ Đã đủ cọc" : "⚠ Chưa đủ cọc theo chính sách", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = _item?.HasDepositPolicy != true ? C("#2563EB") : policyReady ? C("#16A34A") : C("#A16207") } });
    }

    private void BuildStep3()
    {
        Step3Left.Children.Clear(); Step3Right.Children.Clear();
        Step3Left.Children.Add(STitle("Phương thức đặt cọc"));
        Step3Left.Children.Add(new TextBlock { Text = "Đặt cọc sẽ ghi vào Folio thật. Payment method và external reference chưa có trong AddFolioDepositCommand.", FontSize = 12, Foreground = C("#B45309"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
        Step3Left.Children.Add(new TextBlock { Text = "Số tiền", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 2, 0, 0) });
        var amountBox = new TextBox { Text = _depositNow.ToString("0.##"), Style = FindResource("ModernTextBox") as Style, Width = 200, HorizontalAlignment = HorizontalAlignment.Left };
        amountBox.TextChanged += (_, _) => { if (decimal.TryParse(amountBox.Text, out var value)) { _depositNow = _item?.HasDepositPolicy == true ? Math.Max(0, Math.Min(value, Math.Max(0, _required - _have))) : Math.Max(0, value); UpdateSummary(); UpdateNav(); } };
        Step3Left.Children.Add(amountBox);
        Step3Left.Children.Add(Row("Currency nguồn", _currency));
        Step3Right.Children.Add(STitle("Preview")); Step3Right.Children.Add(Row("Yêu cầu", $"{_required:N0} {_currency}")); Step3Right.Children.Add(Row("Đã cọc", $"{_have:N0} {_currency}")); Step3Right.Children.Add(Row("Lần này", $"{_depositNow:N0} {_currency}")); Step3Right.Children.Add(Row("Tổng sau", $"{_have + _depositNow:N0} {_currency}")); Step3Right.Children.Add(new Border { Background = _have + _depositNow >= _required ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = _have + _depositNow >= _required ? "✓ Đủ cọc" : $"⚠ Còn thiếu {_required - _have - _depositNow:N0}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = _have + _depositNow >= _required ? C("#16A34A") : C("#A16207") } });
    }

    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        Step4Left.Children.Add(STitle("Phân bổ đặt cọc"));
        var folios = _item is null
            ? Array.Empty<object>()
            : new[] { new { Folio = _item.FolioId == Guid.Empty ? "—" : _item.FolioId.ToString(), Room = _item.Room, Guest = _item.Name, Req = _required.ToString("N0"), Done = _have.ToString("N0"), Short = Math.Max(0, _required - _have).ToString("N0"), Now = _depositNow.ToString("N0"), Status = _required <= _have + _depositNow ? "Đủ" : "Chưa đủ" } }.Cast<object>().ToArray();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = folios, MaxHeight = 220 }; dg.Columns.Add(new DataGridTextColumn { Header = "Folio", Binding = new System.Windows.Data.Binding("Folio"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("Room"), Width = 55 }); dg.Columns.Add(new DataGridTextColumn { Header = "Khách", Binding = new System.Windows.Data.Binding("Guest"), Width = 120 }); dg.Columns.Add(new DataGridTextColumn { Header = "Yêu cầu", Binding = new System.Windows.Data.Binding("Req"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Đã PB", Binding = new System.Windows.Data.Binding("Done"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Thiếu", Binding = new System.Windows.Data.Binding("Short"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Lần này", Binding = new System.Windows.Data.Binding("Now"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "TT", Binding = new System.Windows.Data.Binding("Status"), Width = 70 });
        Step4Left.Children.Add(dg); Step4Left.Children.Add(new TextBlock { Text = "Khoản cọc sẽ được ghi trực tiếp vào Folio đang chọn; chưa có API phân bổ sang nhiều Folio.", FontSize = 11, Foreground = C("#B45309"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 0) });
        _unallocated = 0;
        Step4Right.Children.Add(STitle("Tổng hợp")); Step4Right.Children.Add(Row("Tổng cọc", $"{_depositNow:N0} {_currency}")); Step4Right.Children.Add(Row("Đã phân bổ", $"{_depositNow - _unallocated:N0} {_currency}")); Step4Right.Children.Add(Row("Chưa phân bổ", $"{_unallocated:N0} {_currency}"));
    }

    private void BuildStep5()
    {
        Step5Left.Children.Clear(); Step5Right.Children.Clear();
        Step5Left.Children.Add(STitle("Xem trước biên nhận")); Step5Left.Children.Add(new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(10), Padding = new Thickness(20), Child = new StackPanel { Children = { new TextBlock { Text = "BIÊN NHẬN ĐẶT CỌC", FontSize = 18, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) }, new TextBlock { Text = $"Số: {_ref}", FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 2, 0, 0) }, new TextBlock { Text = $"Khách: {_item?.Name ?? "—"}", FontSize = 13, Foreground = C("#17365D"), Margin = new Thickness(0, 2, 0, 0) }, new TextBlock { Text = $"Số tiền: {_depositNow:N0} {_currency}", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) }, new TextBlock { Text = $"Phương thức: {_method}", FontSize = 13, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 0) }, new TextBlock { Text = $"Hoàn cọc: Có thể hoàn · Hạn: Trước nhận phòng", FontSize = 11, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) } } } });
        Step5Right.Children.Add(STitle("Tùy chọn")); Step5Right.Children.Add(new ComboBox { Style = FindResource("ModernComboBox") as Style, ItemsSource = new[] { "In biên nhận","Gửi Email","Xuất PDF","A4","A5","Thermal","Song ngữ" }, SelectedIndex = 0, Width = 200 });
    }

    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear(); if (_item == null) return;
        Step6Left.Children.Add(STitle("Tóm tắt đặt cọc")); Step6Left.Children.Add(Row("Khách", _item.Name)); Step6Left.Children.Add(Row("Phòng", _item.Room)); Step6Left.Children.Add(Row("Yêu cầu", $"{_required:N0} {_currency}")); Step6Left.Children.Add(Row("Đã cọc trước", $"{_have:N0} {_currency}")); Step6Left.Children.Add(Row("Cọc lần này", $"{_depositNow:N0} {_currency}")); Step6Left.Children.Add(Row("Tổng sau", $"{_have + _depositNow:N0} {_currency}"));
        Step6Right.Children.Add(STitle("Xác nhận")); var items = new[] { ("Đã chọn hồ sơ", _selected), ("Policy đã được đọc từ Reservation", _item?.PolicySource is not null), ("Số tiền hợp lệ", _depositNow > 0), ("Phương thức ghi Folio", true), ("Phân bổ vào Folio", true), ("Biên nhận sẵn sàng", true) };
        var allOk = true; foreach (var (l, ok) in items) { Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng xác nhận" : "Cần kiểm tra", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns9((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_item is null || !_selected || _depositNow <= 0)
            return;
        if (!_loadedRealData || _folioService is null || _executionContext?.Current is null || _item.FolioId == Guid.Empty || _item.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Folio chưa có dữ liệu thật hợp lệ để ghi đặt cọc. Không tạo giao dịch giả.", "Đặt cọc", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = await _folioService.AddDepositAsync(
            new AddFolioDepositCommand(_item.FolioId, _item.RowVersion, _depositNow, $"Đặt cọc {_ref}"),
            CancellationToken.None);
        if (result.IsFailure)
        {
            MessageBox.Show(result.Error?.Message ?? "Không thể ghi nhận đặt cọc.", "Đặt cọc", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        MessageBox.Show("Đã ghi nhận đặt cọc vào Folio thật.", "Đặt cọc", MessageBoxButton.OK, MessageBoxImage.Information);
        await OnLoadedAsyncRefresh();
    }

    private async Task OnLoadedAsyncRefresh()
    {
        if (_folioRepository is null || _executionContext?.Current is null)
            return;
        var propertyId = _executionContext.Current.Property.PropertyId.Value;
        var page = await _folioRepository.SearchAsync(
            new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
            new PageRequest(1, 200), CancellationToken.None);
        _allItems.Clear();
        foreach (var item in page.Items)
            _allItems.Add(await MapPersistedFolioAsync(propertyId, item));
        _item = null;
        _selected = false;
        GoTo(1);
        RefreshPage(true);
    }
    private void OnRefresh(object s, RoutedEventArgs e) => OnLoadedAsync(this, e);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record DpItem(string Type, string Name, string Room, decimal Required, decimal Have, string Status, bool IsVip, Guid FolioId = default, byte[]? RowVersion = null, string? PolicySource = null, DateOnly? DepositDueDate = null, string? GuaranteeType = null, decimal Charges = 0m, bool HasDepositPolicy = false) { public string ReqDisplay => $"{Required:N0}"; public string HaveDisplay => $"{Have:N0}"; public string ShortDisplay => $"{Math.Max(0, Required - Have):N0}"; public string VipDisplay => IsVip ? "★" : ""; }
internal static class DpExt { public static TextBlock WithRuns9(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
