using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Folio;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class FolioReviewWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private FrGuest? _guest; private bool _selected;
    private int _guestPage, _guestPageSize = 20;
    private readonly List<FrGuest> _allGuests = new();
    private int _folioCount, _totalFolio, _deposit, _paid, _due;
    private int _warnings, _blocking;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private bool _persistedMode;
    private string Currency => _contextAccessor?.Current?.Property.CurrencyCode?.Trim() is { Length: > 0 } value ? value : "—";

    public FolioReviewWindow()
    {
        InitializeComponent(); Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _paymentRepository = services?.GetService<IHotelPaymentRepository>();
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
            var stays = await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, CancellationToken.None);
            _allGuests.Clear();
            var financialDataWarning = false;
            foreach (var stay in stays)
            {
                var folio = await _folioRepository.GetByStayAsync(context.Property.PropertyId.Value, stay.HotelStayId, CancellationToken.None);
                var entries = folio is null ? Array.Empty<FrEntry>() : await LoadEntriesAsync(context.Property.PropertyId.Value, folio.HotelFolioId);
                OutstandingBalanceDto? outstanding = null;
                DepositSummaryDto? deposits = null;
                if (folio is not null && _paymentRepository is not null)
                {
                    try
                    {
                        outstanding = await _paymentRepository.GetOutstandingAsync(context.Property.PropertyId.Value, folio.HotelFolioId, CancellationToken.None);
                        deposits = await _paymentRepository.GetDepositSummaryAsync(context.Property.PropertyId.Value, folio.HotelFolioId, CancellationToken.None);
                    }
                    catch
                    {
                        // Keep the real Folio visible, but make the financial degradation explicit.
                        financialDataWarning = true;
                    }
                }
                _allGuests.Add(MapRealGuest(stay, folio, entries, context.Property.CurrencyCode, outstanding, deposits));
            }

            _persistedMode = true;
            _guest = null;
            _selected = false;
            RefreshGuestPage(true);
            UpdateSummary();
            FooterStepLabel.Text = $"Dữ liệu thật · {_allGuests.Count:N0} khách đang lưu trú" +
                (financialDataWarning ? " · Chưa tải đủ số liệu thanh toán, cần kiểm tra lại" : "");
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            RefreshGuestPage(true);
            FooterStepLabel.Text = "Không thể tải danh sách Folio: " + ex.Message;
        }
    }

    private async Task<IReadOnlyList<FrEntry>> LoadEntriesAsync(Guid propertyId, Guid folioId)
    {
        var ledger = await _folioRepository!.GetLedgerAsync(propertyId, folioId, CancellationToken.None);
        return ledger?.Entries.Select(e => new FrEntry(
            e.CreatedAtUtc.ToLocalTime().ToString("dd/MM HH:mm"),
            e.Description ?? e.EntryType.ToString(),
            e.EntryType.ToString(),
            decimal.ToInt32(decimal.Round(e.Amount, 0, MidpointRounding.AwayFromZero)),
            "Đã ghi nhận")).ToArray() ?? Array.Empty<FrEntry>();
    }

    private static FrGuest MapRealGuest(
        HotelStayDto stay,
        HotelFolioDto? folio,
        IReadOnlyList<FrEntry> entries,
        string? currency,
        OutstandingBalanceDto? outstanding = null,
        DepositSummaryDto? deposits = null)
    {
        var totalCharges = outstanding?.TotalCharges ?? folio?.TotalCharges ?? 0m;
        var totalPayments = outstanding?.TotalPayments ?? folio?.TotalPayments ?? 0m;
        var dueValue = outstanding?.Outstanding ?? folio?.OutstandingBalance ?? 0m;
        var due = decimal.ToInt32(decimal.Round(dueValue, 0, MidpointRounding.AwayFromZero));
        var depositTotal = deposits?.TotalDeposits ?? 0m;
        var depositApplied = deposits?.AppliedDeposits ?? outstanding?.TotalDepositApplied ?? 0m;
        var depositRemaining = deposits?.RemainingDeposits ?? Math.Max(0m, depositTotal - depositApplied);
        return new FrGuest(
            stay.RoomNumber,
            stay.GuestName,
            stay.ScheduledArrivalDate.ToString("dd/MM"),
            stay.ScheduledDepartureDate.ToString("dd/MM"),
            false,
            stay.StayNumber,
            folio is null ? 0 : 1,
            due,
            folio is null ? "Chưa có Folio" : due == 0 ? "Đã cân" : "Cần kiểm tra",
            folio is null ? 1 : 0,
            decimal.ToInt32(decimal.Round(totalCharges, 0, MidpointRounding.AwayFromZero)),
            decimal.ToInt32(decimal.Round(totalPayments, 0, MidpointRounding.AwayFromZero)),
            entries,
            folio?.FolioNumber ?? "—",
            string.IsNullOrWhiteSpace(currency) ? "—" : currency.Trim(),
            decimal.ToInt32(decimal.Round(depositTotal, 0, MidpointRounding.AwayFromZero)),
            decimal.ToInt32(decimal.Round(depositApplied, 0, MidpointRounding.AwayFromZero)),
            decimal.ToInt32(decimal.Round(depositRemaining, 0, MidpointRounding.AwayFromZero)),
            outstanding is not null || deposits is not null);
    }

    private void InitFilters() { Set(GuestVipFilter, "Tất cả","VIP","Không VIP"); Set(GuestFolioFilter, "Tất cả","Đã cân","Cần kiểm tra","Chưa KT","Có tranh chấp"); Set(GuestPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn hồ sơ"),("B","Tổng quan Folio"),("C","Khoản mục & DV"),("D","Người thanh toán"),("E","Đối chiếu & CB"),("F","Kết luận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = _selected; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/6 · {new[] { "Chọn hồ sơ","Tổng quan Folio","Khoản mục & DV","Người thanh toán","Đối chiếu & CB","Kết luận" }[_step - 1]} · {_folioCount} Folio · {_warnings} WB · {_blocking} BLK";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var g = _guest; var sel = _selected;
        SummaryStrip.Children.Add(Strip("Khách", g?.Name ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", g?.Room ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Hồ sơ", g?.StayCode ?? "—", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Folio", sel ? $"{_folioCount}" : "0", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Còn P/Thu", sel ? $"{_due:N0}" : "—", _due == 0)); SummaryStrip.Children.Add(Sep());
        var ready = sel && _due <= 0 && _blocking == 0;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Chưa SS", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    private List<FrGuest> GetFiltered() { var f = _allGuests.AsEnumerable(); var kw = GuestSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw) || g.StayCode.ToLower().Contains(kw)); var v = GuestVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); var s = GuestFolioFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(s) && s != "Tất cả") f = f.Where(g => g.Status == s); return f.ToList(); }
    private void RefreshGuestPage(bool reset) { if (reset) _guestPage = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _guestPageSize - 1) / _guestPageSize); if (_guestPage >= tp) _guestPage = tp - 1; GuestGrid.ItemsSource = f.Skip(_guestPage * _guestPageSize).Take(_guestPageSize).ToList(); GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} trên tổng {f.Count} hồ sơ"; GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1; BuildBtns(tp); }
    private void BuildBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _guestPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _guestPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _guestPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; nb.Template = GuestPrevBtn.Template; GuestPageBtns.Children.Add(nb); } }
    private void OnGuestPreviewChanged(object s, SelectionChangedEventArgs e) { if (GuestGrid.SelectedItem is not FrGuest g) return; _guest = g; _selected = false; _folioCount = g.FolioCount; _totalFolio = g.TotalCharges; _paid = g.TotalPayments; _deposit = g.DepositTotal; _due = g.Balance; _warnings = g.WarnCount; GuestEmptyState.Visibility = Visibility.Collapsed; GuestDetailPanel.Visibility = Visibility.Visible; GDName.Text = g.Name; GDMeta.Text = g.StayCode; GDInfo1.Text = $"Phòng: {g.Room} · {g.FolioCount} Folio"; GDInfo2.Text = $"Ngày: {g.Arrival} → {g.Departure}"; GDInfo3.Text = $"Còn phải thu: {g.Balance:N0} {g.Currency} · Cảnh báo: {g.WarnCount}"; GDInfo4.Text = $"Trạng thái: {g.Status}"; UpdateSummary(); }
    private void OnSelectGuest(object s, RoutedEventArgs e) { if (_guest == null) return; _selected = true; BtnSelectGuest.Content = "✓ Đã chọn"; BtnSelectGuest.IsEnabled = false; UpdateSummary(); UpdateNav(); }
    private void OnApplyGuestFilters(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilters(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestVipFilter.SelectedIndex = 0; GuestFolioFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }

    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        if (_guest is null) return;
        Step2Left.Children.Add(STitle("Folio trong hồ sơ thật"));
        var folios = new[] { new { Type = "Stay", Code = _guest.FolioCode, Room = _guest.Room, Guest = _guest.Name, Payer = "Theo dữ liệu Folio", Total = _guest.TotalCharges.ToString("N0"), Deposit = _deposit.ToString("N0"), Paid = _guest.TotalPayments.ToString("N0"), Due = _guest.Balance.ToString("N0"), Refund = "—", Currency = _guest.Currency, Status = _guest.Status } };
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = folios, MaxHeight = 280 }; dg.Columns.Add(new DataGridTextColumn { Header = "Loại", Binding = new System.Windows.Data.Binding("Type"), Width = 60 }); dg.Columns.Add(new DataGridTextColumn { Header = "Mã Folio", Binding = new System.Windows.Data.Binding("Code"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("Room"), Width = 50 }); dg.Columns.Add(new DataGridTextColumn { Header = "Khách", Binding = new System.Windows.Data.Binding("Guest"), Width = 100 }); dg.Columns.Add(new DataGridTextColumn { Header = "Người TT", Binding = new System.Windows.Data.Binding("Payer"), Width = 100 }); dg.Columns.Add(new DataGridTextColumn { Header = "Tổng", Binding = new System.Windows.Data.Binding("Total"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Đặt cọc", Binding = new System.Windows.Data.Binding("Deposit"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Đã TT", Binding = new System.Windows.Data.Binding("Paid"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Còn nợ", Binding = new System.Windows.Data.Binding("Due"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Dư", Binding = new System.Windows.Data.Binding("Refund"), Width = 60 }); dg.Columns.Add(new DataGridTextColumn { Header = "Tệ", Binding = new System.Windows.Data.Binding("Currency"), Width = 50 }); dg.Columns.Add(new DataGridTextColumn { Header = "TT", Binding = new System.Windows.Data.Binding("Status"), Width = 80 });
        Step2Left.Children.Add(dg); Step2Right.Children.Add(STitle("Chi tiết Folio")); Step2Right.Children.Add(Row("Mã", _guest.FolioCode)); Step2Right.Children.Add(Row("Người TT", "Theo dữ liệu Folio; chưa có payer mapping")); Step2Right.Children.Add(Row("Tổng", $"{_guest.TotalCharges:N0} {_guest.Currency}")); Step2Right.Children.Add(Row("Đặt cọc", $"{_guest.DepositTotal:N0} {_guest.Currency}")); Step2Right.Children.Add(Row("Đã TT", $"{_guest.TotalPayments:N0} {_guest.Currency}")); Step2Right.Children.Add(Row("Còn P/Thu", $"{_guest.Balance:N0} {_guest.Currency}"));
    }

    private void BuildStep3()
    {
        Step3Left.Children.Clear(); Step3Right.Children.Clear();
        if (_guest is null) return;
        var entries = _guest.Entries ?? Array.Empty<FrEntry>();
        Step3Left.Children.Add(STitle("Khoản mục Folio thật")); var items = entries.Select(entry => new { Time = entry.Time, Folio = _guest.FolioCode, Room = _guest.Room, Item = entry.Item, Dept = entry.Dept, Qty = "1", Price = Math.Abs(entry.Amount).ToString("N0"), Debit = entry.Amount > 0 ? entry.Amount.ToString("N0") : "0", Credit = entry.Amount < 0 ? Math.Abs(entry.Amount).ToString("N0") : "0", Status = entry.Status }).ToArray();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = items, MaxHeight = 280 }; dg.Columns.Add(new DataGridTextColumn { Header = "TG", Binding = new System.Windows.Data.Binding("Time"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Folio", Binding = new System.Windows.Data.Binding("Folio"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("Room"), Width = 50 }); dg.Columns.Add(new DataGridTextColumn { Header = "Khoản mục", Binding = new System.Windows.Data.Binding("Item"), Width = 100 }); dg.Columns.Add(new DataGridTextColumn { Header = "BP", Binding = new System.Windows.Data.Binding("Dept"), Width = 45 }); dg.Columns.Add(new DataGridTextColumn { Header = "SL", Binding = new System.Windows.Data.Binding("Qty"), Width = 55 }); dg.Columns.Add(new DataGridTextColumn { Header = "Đơn giá", Binding = new System.Windows.Data.Binding("Price"), Width = 85 }); dg.Columns.Add(new DataGridTextColumn { Header = "Nợ", Binding = new System.Windows.Data.Binding("Debit"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Có", Binding = new System.Windows.Data.Binding("Credit"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "TT", Binding = new System.Windows.Data.Binding("Status"), Width = 70 });
        Step3Left.Children.Add(dg); Step3Right.Children.Add(STitle("Tình trạng khoản mục")); Step3Right.Children.Add(new TextBlock { Text = entries.Count == 0 ? "Không có khoản mục trong Folio thật." : $"Đã đọc {entries.Count:N0} khoản mục từ ledger.", FontSize = 11, Foreground = entries.Count == 0 ? C("#64748B") : C("#16A34A"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) });
    }

    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        if (_guest is null) return;
        var entries = _guest.Entries ?? Array.Empty<FrEntry>();
        Step4Left.Children.Add(STitle("Payer mapping từ dữ liệu thật")); var pt = entries.Select(entry => new { Item = entry.Item, Room = _guest.Room, Src = _guest.FolioCode, Payer = "Chưa có mapping", Dest = _guest.FolioCode, Amount = Math.Abs(entry.Amount).ToString("N0"), Status = "Cần xác định" }).ToArray();
        var dg = new DataGrid { Style = FindResource("ModernDataGrid") as Style, ItemsSource = pt, MaxHeight = 200 }; dg.Columns.Add(new DataGridTextColumn { Header = "Khoản mục", Binding = new System.Windows.Data.Binding("Item"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("Room"), Width = 50 }); dg.Columns.Add(new DataGridTextColumn { Header = "Folio nguồn", Binding = new System.Windows.Data.Binding("Src"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Người TT", Binding = new System.Windows.Data.Binding("Payer"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Folio đích", Binding = new System.Windows.Data.Binding("Dest"), Width = 80 }); dg.Columns.Add(new DataGridTextColumn { Header = "Số tiền", Binding = new System.Windows.Data.Binding("Amount"), Width = 90 }); dg.Columns.Add(new DataGridTextColumn { Header = "TT", Binding = new System.Windows.Data.Binding("Status"), Width = 80 });
        Step4Left.Children.Add(dg); Step4Right.Children.Add(STitle("Tổng hợp theo người trả")); Step4Right.Children.Add(Row("Payer", "Chưa có payer mapping trong dữ liệu thật")); Step4Right.Children.Add(STitle("Phân bổ đặt cọc")); Step4Right.Children.Add(Row("Tổng ĐC", $"{_guest.DepositTotal:N0} {_guest.Currency}")); Step4Right.Children.Add(Row("Đã sử dụng", $"{_guest.DepositApplied:N0} {_guest.Currency}")); Step4Right.Children.Add(Row("Chưa phân bổ", $"{_guest.DepositRemaining:N0} {_guest.Currency}")); Step4Right.Children.Add(new TextBlock { Text = _guest.FinancialDataLoaded ? "Số liệu lấy từ payment repository thật." : "Chưa tải được summary thanh toán; không dùng số liệu fallback để kết luận.", FontSize = 11, Foreground = _guest.FinancialDataLoaded ? C("#16A34A") : C("#B45309"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) });
    }

    private void BuildStep5()
    {
        Step5Left.Children.Clear(); Step5Right.Children.Clear();
        if (_guest is null) return;
        var entries = _guest.Entries ?? Array.Empty<FrEntry>();
        Step5Left.Children.Add(STitle("Đối chiếu từ ledger thật")); Step5Left.Children.Add(Row("Folio", _guest.FolioCode)); Step5Left.Children.Add(Row("Khoản mục", $"{entries.Count:N0} khoản đã đọc")); Step5Left.Children.Add(Row("Tổng charge", $"{_guest.TotalCharges:N0} {_guest.Currency}")); Step5Left.Children.Add(Row("Đã thanh toán", $"{_guest.TotalPayments:N0} {_guest.Currency}")); Step5Left.Children.Add(Row("Còn phải thu", $"{_guest.Balance:N0} {_guest.Currency}"));
        Step5Right.Children.Add(STitle("Cảnh báo")); Step5Right.Children.Add(new TextBlock { Text = _guest.WarnCount == 0 ? "Không có cảnh báo từ dữ liệu đã tải." : $"Có {_guest.WarnCount:N0} cảnh báo cần đối chiếu thêm.", FontSize = 11, Foreground = _guest.WarnCount == 0 ? C("#16A34A") : C("#A16207"), TextWrapping = TextWrapping.Wrap });
        _blocking = _guest.WarnCount > 0 ? 1 : 0; UpdateNav();
    }

    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear();
        if (_guest == null) return;
        Step6Left.Children.Add(STitle("Tóm tắt kiểm tra Folio")); Step6Left.Children.Add(Row("Khách", _guest.Name)); Step6Left.Children.Add(Row("Phòng", _guest.Room)); Step6Left.Children.Add(Row("Hồ sơ", _guest.StayCode)); Step6Left.Children.Add(Row("Folio", _guest.FolioCode)); Step6Left.Children.Add(Row("Tổng", $"{_totalFolio:N0} {_guest.Currency}")); Step6Left.Children.Add(Row("Đặt cọc", $"{_guest.DepositTotal:N0} {_guest.Currency}")); Step6Left.Children.Add(Row("Đã TT", $"{_paid:N0} {_guest.Currency}")); Step6Left.Children.Add(Row("Còn P/Thu", $"{_due:N0} {_guest.Currency}")); Step6Left.Children.Add(Row("Cảnh báo", $"{_warnings}"));
        Step6Right.Children.Add(STitle("Kết luận kiểm tra")); var items = new[] { ("Đã chọn hồ sơ", _selected), ("Đã đọc Folio thật", _persistedMode), ("Đã đọc số liệu thanh toán thật", _guest.FinancialDataLoaded), ("Không còn khoản cần đối chiếu", _warnings == 0), ("Payer đã xác định", false), ("Đặt cọc đã phân bổ", false), ("Không blocking issue", _blocking == 0) };
        var allOk = true; foreach (var (l, ok) in items) { Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = l, FontSize = 12, Foreground = C("#334155") } } }); if (!ok) allOk = false; }
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng Check-out" : "Cần kiểm tra thêm", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#A16207") } });
        Step6Right.Children.Add(new WrapPanel { Margin = new Thickness(0, 8, 0, 0), Children = { new Button { Content = "Mở Thu ngân", Style = FindResource("SecondaryButton") as Style, Height = 28, FontSize = 11, Margin = new Thickness(0, 0, 6, 0) }, new Button { Content = "In biên bản KT", Style = FindResource("SecondaryButton") as Style, Height = 28, FontSize = 11 } } });
    }

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns7((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private void OnConfirm(object s, RoutedEventArgs e) { if (_guest == null || _selected == false) return; MessageBox.Show($"Kiểm tra Folio chỉ đọc dữ liệu thật và chưa ghi thay đổi.\n\nKhách: {_guest.Name}\nPhòng: {_guest.Room}\nFolio: {_guest.FolioCode}\nTổng: {_totalFolio:N0} {_guest.Currency}\nCòn phải thu: {_due:N0} {_guest.Currency}\nCảnh báo: {_warnings}\n\nNếu cần điều chỉnh, mở Folio Workspace để thực hiện bằng nghiệp vụ có kiểm soát.", "Kiểm tra Folio", MessageBoxButton.OK, MessageBoxImage.Information); }
    private void OnRefresh(object s, RoutedEventArgs e) => OnLoadedAsync(this, e);
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record FrGuest(string Room, string Name, string Arrival, string Departure, bool IsVip, string StayCode, int FolioCount, int Balance, string Status, int WarnCount, int TotalCharges = 0, int TotalPayments = 0, IReadOnlyList<FrEntry>? Entries = null, string FolioCode = "—", string Currency = "—", int DepositTotal = 0, int DepositApplied = 0, int DepositRemaining = 0, bool FinancialDataLoaded = false) { public string VipDisplay => IsVip ? "★" : ""; public string BalanceDisplay => $"{Balance:N0}"; }
internal record FrEntry(string Time, string Item, string Dept, int Amount, string Status) { public string AmountDisplay => $"{Amount:N0}"; }
internal static class FrExt { public static TextBlock WithRuns7(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
