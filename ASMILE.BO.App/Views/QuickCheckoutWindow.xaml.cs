using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;
using ASMILE.BO.Core.FrontOffice.Folio;

namespace ASMILE.BO.App.Views;

public partial class QuickCheckoutWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private QcGuest? _guest;
    private bool _selected;
    private int _guestPage, _guestPageSize = 20;
    private readonly List<QcGuest> _allGuests = new();
    private int _folioBalance;
    private string _paymentMethod = "Chưa có dữ liệu thật";
    private string _invoiceType = "Cá nhân";
    private string _currency = "VND";
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private bool _persistedMode;

    public QuickCheckoutWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        InitFilters(); RefreshGuestPage(true); GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_stayRepository is null || _folioRepository is null || _contextAccessor?.Current is not { } context)
        {
            FooterStepLabel.Text = "Chưa cấu hình Stay/Folio service và property context dữ liệu thật.";
            return;
        }

        try
        {
            _currency = context.Property.CurrencyCode ?? "VND";
            var stays = await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, CancellationToken.None);
            _allGuests.Clear();
            foreach (var stay in stays)
            {
                var folio = await _folioRepository.GetByStayAsync(context.Property.PropertyId.Value, stay.HotelStayId, CancellationToken.None);
                var ledger = folio is null
                    ? null
                    : await _folioRepository.GetLedgerAsync(context.Property.PropertyId.Value, folio.HotelFolioId, CancellationToken.None);
                _allGuests.Add(MapRealGuest(stay, folio, ledger, _currency));
            }

            _persistedMode = true;
            _guest = null;
            _selected = false;
            RefreshGuestPage(true);
            UpdateSummary();
            FooterStepLabel.Text = $"Dữ liệu thật · {_allGuests.Count:N0} khách đang lưu trú";
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            RefreshGuestPage(true);
            FooterStepLabel.Text = "Không thể tải danh sách Check-out nhanh: " + ex.Message;
        }
    }

    private static QcGuest MapRealGuest(HotelStayDto stay, HotelFolioDto? folio, FolioLedgerDto? ledger, string currency)
    {
        var balance = decimal.ToInt32(decimal.Round(folio?.OutstandingBalance ?? 0m, 0, MidpointRounding.AwayFromZero));
        return new QcGuest(
            stay.RoomNumber,
            stay.GuestName,
            stay.ScheduledArrivalDate.ToString("dd/MM"),
            stay.ScheduledDepartureDate.ToString("dd/MM"),
            stay.RoomTypeCode,
            $"{stay.Adults + stay.Children}",
            false,
            folio?.FolioNumber ?? "—",
            balance,
            folio is null ? "Cần kiểm tra" : balance == 0 ? "Sẵn sàng" : "Cần kiểm tra",
            stay.HotelStayId,
            stay.RowVersion,
            currency,
            ledger?.Entries ?? Array.Empty<HotelFolioEntryDto>());
    }


    private void InitFilters() { Set(GuestVipFilter, "Tất cả","VIP","Không VIP"); Set(GuestStatusFilter, "Tất cả","Sẵn sàng","Cần kiểm tra","Không đủ ĐK"); Set(GuestPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 4); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn hồ sơ"),("B","Kiểm tra nhanh"),("C","Thanh toán & HĐ"),("D","Xác nhận") };
        for (int i = 0; i < 4; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 3) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 4) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = _selected && _folioBalance == 0; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/4 · {new[] { "Chọn hồ sơ","Kiểm tra nhanh","Thanh toán & HĐ","Xác nhận" }[_step - 1]}";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var g = _guest; var sel = _selected;
        SummaryStrip.Children.Add(Strip("Khách", g?.Name ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", g?.Room ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Ngày đi", g?.Departure ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Còn P/Thu", sel ? $"{_folioBalance:N0}" : "—", _folioBalance == 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("TT", sel ? _paymentMethod : "—", sel)); SummaryStrip.Children.Add(Sep());
        var ready = sel && _folioBalance == 0;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Chưa SS", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    // STEP 1
    private List<QcGuest> GetFiltered() { var f = _allGuests.AsEnumerable(); var kw = GuestSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw) || g.Folio.ToLower().Contains(kw)); var v = GuestVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); var s = GuestStatusFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(s) && s != "Tất cả") f = f.Where(g => g.ReadyStatus == s); return f.ToList(); }
    private void RefreshGuestPage(bool reset) { if (reset) _guestPage = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _guestPageSize - 1) / _guestPageSize); if (_guestPage >= tp) _guestPage = tp - 1; GuestGrid.ItemsSource = f.Skip(_guestPage * _guestPageSize).Take(_guestPageSize).ToList(); GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} trên tổng {f.Count} hồ sơ"; GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1; BuildBtns(tp); }
    private void BuildBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _guestPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _guestPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _guestPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; nb.Template = GuestPrevBtn.Template; GuestPageBtns.Children.Add(nb); } }
    private void OnGuestPreviewChanged(object s, SelectionChangedEventArgs e) { if (GuestGrid.SelectedItem is not QcGuest g) return; _guest = g; _folioBalance = g.Balance; _selected = false; GuestEmptyState.Visibility = Visibility.Collapsed; GuestDetailPanel.Visibility = Visibility.Visible; GDName.Text = g.Name; GDMeta.Text = g.StayId == Guid.Empty ? "—" : g.StayId.ToString(); GDRoom.Text = $"Phòng: {g.Room} · {g.RoomType}"; GDDates.Text = $"Ngày: {g.Arrival} → {g.Departure}"; GDFolio.Text = $"Folio: {g.Folio}"; GDVip.Text = g.IsVip ? "★ VIP" : "Khách thường"; GDBalance.Text = $"Còn phải thu: {g.Balance:N0} {g.Currency}"; GDStatus.Text = $"Trạng thái: {g.ReadyStatus}"; GDStatus.Foreground = g.ReadyStatus == "Sẵn sàng" ? C("#16A34A") : C("#EA580C"); BtnSelectGuest.IsEnabled = g.ReadyStatus == "Sẵn sàng"; UpdateSummary(); }
    private void OnSelectGuest(object s, RoutedEventArgs e) { if (_guest == null || _guest.ReadyStatus != "Sẵn sàng") return; _selected = true; BtnSelectGuest.Content = "✓ Đã chọn"; BtnSelectGuest.IsEnabled = false; UpdateSummary(); UpdateNav(); }
    private void OnApplyGuestFilters(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilters(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestVipFilter.SelectedIndex = 0; GuestStatusFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }

    // STEP 2: Quick check
    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        Step2Left.Children.Add(STitle("Folio & dịch vụ")); if (_guest == null) return;
        var entries = _guest.Entries ?? Array.Empty<HotelFolioEntryDto>();
        Step2Left.Children.Add(Row("Số khoản mục ledger", entries.Count.ToString("N0"))); Step2Left.Children.Add(Row("Còn phải thu", $"{_folioBalance:N0} {_guest.Currency}"));
        Step2Left.Children.Add(Row("Trạng thái Folio", _folioBalance == 0 ? "Đã cân" : "Còn phải thu"));
        Step2Left.Children.Add(Row("Kiểm tra dịch vụ", entries.Count == 0 ? "Không có khoản mục trong ledger" : "Đã nạp từ ledger thật; hậu kiểm ở bước Chuẩn bị Check-out"));
        Step2Right.Children.Add(STitle("Phòng & bàn giao"));
        Step2Right.Children.Add(Row("Stay", _guest.StayId == Guid.Empty ? "Thiếu định danh" : "Đã có StayId thật"));
        Step2Right.Children.Add(Row("Housekeeping", "Kiểm tra và tạo task ở Chuẩn bị Check-out"));
        Step2Right.Children.Add(Row("Key-card", "Đọc/thu hồi thật ở Chuẩn bị Check-out"));
        Step2Right.Children.Add(Row("Hành lý/Lost & Found", "Không tự đánh dấu; xử lý tại module tương ứng nếu phát sinh"));
    }
    private static FrameworkElement CheckItem(string label, bool ok) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155") } } };

    // STEP 3: Payment & Invoice
    private void BuildStep3()
    {
        Step3Left.Children.Clear(); Step3Right.Children.Clear();
        Step3Left.Children.Add(STitle("Phương án thanh toán"));
        Step3Left.Children.Add(Row("Còn phải thu", $"{_folioBalance:N0} {_guest?.Currency ?? _currency}")); Step3Left.Children.Add(Row("Phương thức", "Xử lý tại Thu ngân/Folio thật")); Step3Left.Children.Add(Row("Người thanh toán", _guest?.Name ?? "—"));
        Step3Left.Children.Add(new Border { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 6, 0, 0), Child = new TextBlock { Text = "Luồng Check-out nhanh chỉ lọc hồ sơ thật; không tự tạo payment hoặc allocation.", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#2563EB") } });
        Step3Right.Children.Add(STitle("Hóa đơn")); Step3Right.Children.Add(Row("Loại hóa đơn", "Chọn tại Folio/Express Check-out")); Step3Right.Children.Add(Row("Tên đơn vị", "Đọc từ GuestProfile/Company nếu có dữ liệu")); Step3Right.Children.Add(Row("Mã số thuế", "Chỉ hiển thị khi hồ sơ thật có thông tin"));
    }

    // STEP 4: Confirm
    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        if (_guest == null) return;
        Step4Left.Children.Add(STitle("Tóm tắt"));
        Step4Left.Children.Add(Row("Khách", _guest.Name)); Step4Left.Children.Add(Row("Phòng", _guest.Room + " · " + _guest.RoomType));
        Step4Left.Children.Add(Row("Ngày đi", _guest.Departure)); Step4Left.Children.Add(Row("Folio", $"Còn {_folioBalance:N0} {_guest.Currency}"));
        Step4Left.Children.Add(Row("Thanh toán", _paymentMethod)); Step4Left.Children.Add(Row("Hóa đơn", _invoiceType));
        Step4Right.Children.Add(STitle("Kiểm tra sẵn sàng"));
        var items = new[] { ("Đã chọn hồ sơ", _selected), ("Có StayId và row version thật", _guest.StayId != Guid.Empty && _guest.RowVersion is { Length: 8 }), ("Folio đã cân", _folioBalance == 0), ("Dịch vụ đã nạp từ ledger", true) };
        var allOk = true; foreach (var (l, ok) in items) { Step4Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step4Right.Children.Add(new TextBlock { Text = "HSK và key-card sẽ được kiểm tra/ghi nhận thật ở cửa sổ Chuẩn bị Check-out.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = C("#64748B"), Margin = new Thickness(0, 8, 0, 0) });
        Step4Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng Check-out" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    // Helpers
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns4((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 4) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_guest is null) return;
        if (!_persistedMode || _guest.StayId == Guid.Empty || _guest.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Hồ sơ chưa có định danh/row version hợp lệ từ dữ liệu thật.", "Check-out nhanh", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_folioBalance != 0)
        {
            MessageBox.Show("Folio chưa cân. Hãy xử lý số dư thật trước khi chuyển sang quy trình Check-out.", "Check-out nhanh", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // The quick list is only a real-data pre-filter. The actual checkout
        // must still pass the standard housekeeping/key-card checks and commit
        // through the existing real checkout command.
        var preparation = new CheckoutPreparationWindow(_guest.StayId)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.CanResize,
            ShowInTaskbar = false
        };
        preparation.ShowDialog();
        OnLoadedAsync(this, new RoutedEventArgs());
    }
    private async void OnRefresh(object s, RoutedEventArgs e) { await Task.CompletedTask; OnLoadedAsync(this, new RoutedEventArgs()); }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record QcGuest(string Room, string Name, string Arrival, string Departure, string RoomType, string GuestCount, bool IsVip, string Folio, int Balance, string ReadyStatus, Guid StayId = default, byte[]? RowVersion = null, string Currency = "VND", IReadOnlyList<HotelFolioEntryDto>? Entries = null) { public string VipDisplay => IsVip ? "★" : ""; public string BalanceDisplay => $"{Balance:N0}"; }
internal static class QcExt { public static TextBlock WithRuns4(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
