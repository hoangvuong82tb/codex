using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Guest;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class StayInformationAdjustmentWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step;
    private AdjGuest? _guest;
    private int _guestPage, _guestPageSize = 20;
    private readonly List<AdjGuest> _allGuests;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IGuestProfileRepository? _guestRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly HotelStayApplicationService? _stayService;
    private TextBox? _guestNameBox;
    private TextBox? _guestPhoneBox;
    private TextBox? _requestsBox;
    private TextBox? _notesBox;
    private ComboBox? _reasonBox;
    private string _guestName = string.Empty;
    private string _guestPhone = string.Empty;
    private string _specialRequests = string.Empty;
    private string _internalNotes = string.Empty;
    private string _reason = string.Empty;
    private bool _isSubmitting;

    public StayInformationAdjustmentWindow()
    {
        InitializeComponent();
        var owner = System.Windows.Application.Current?.MainWindow;
        if (owner is not null && !ReferenceEquals(owner, this))
        {
            Owner = owner;
        }
        _allGuests = new List<AdjGuest>();
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _guestRepository = services?.GetService<IGuestProfileRepository>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _stayService = services?.GetService<HotelStayApplicationService>();
        InitFilters();
        RefreshGuestPage(true);
        GoTo(1);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_stayRepository is null || _contextAccessor?.Current is not { } context)
        {
            FooterStepLabel.Text = "Chưa cấu hình Stay service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var stays = await _stayRepository.GetCurrentInHouseAsync(context.Property.PropertyId.Value, CancellationToken.None);
            _allGuests.Clear();
            foreach (var stay in stays)
            {
                GuestProfile? profile = null;
                if (_guestRepository is not null && stay.GuestId is Guid guestId)
                    profile = await _guestRepository.GetAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);

                _allGuests.Add(new AdjGuest(stay.RoomNumber, stay.GuestName,
                    stay.ScheduledArrivalDate.ToString("dd/MM/yyyy"), stay.ScheduledDepartureDate.ToString("dd/MM/yyyy"),
                    stay.RoomTypeCode, (stay.Adults + stay.Children).ToString(),
                    profile?.VipLevel is not null && profile.VipLevel != VipLevel.Normal,
                    stay.StayNumber, "—", "—", stay.Source, profile)
                {
                    GuestPhone = stay.GuestPhone ?? string.Empty,
                    SpecialRequests = stay.SpecialRequests ?? string.Empty,
                    InternalNotes = stay.InternalNotes ?? string.Empty,
                    Adults = stay.Adults,
                    Children = stay.Children
                });
                _allGuests[^1] = _allGuests[^1] with
                {
                    StayId = stay.HotelStayId,
                    RoomId = stay.RoomId,
                    RowVersion = stay.RowVersion
                };
            }
            RefreshGuestPage(true);
            FooterStepLabel.Text = $"Dữ liệu thật · {_allGuests.Count:N0} khách đang lưu trú";
        }
        catch (Exception ex)
        {
            _allGuests.Clear();
            RefreshGuestPage(true);
            FooterStepLabel.Text = "Không tải được Stay thật: " + ex.Message;
        }
    }

    private void InitFilters() { Set(GuestRoomTypeFilter, "Tất cả","Standard","Superior","Deluxe","Junior","Suite"); Set(GuestVipFilter, "Tất cả","VIP","Không VIP"); Set(GuestPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn hồ sơ"),("B","Khách & liên hệ"),("C","Lưu trú"),("D","Phòng hiện tại"),("E","Yêu cầu & ghi chú"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        FooterStepLabel.Text = $"Bước {_step}/6 · {new[] { "Chọn hồ sơ","Khách & liên hệ","Lưu trú","Phòng hiện tại","Yêu cầu & ghi chú","Xác nhận" }[_step - 1]}";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var g = _guest;
        SummaryStrip.Children.Add(Strip("Khách", g?.Name ?? "Chưa chọn", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", g?.Room ?? "—", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Lưu trú", g != null ? $"{g.Arrival} → {g.Departure}" : "—", g != null)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Hạng", g?.RoomType ?? "—", g != null)); SummaryStrip.Children.Add(Sep());
        var changed = HasChanges();
        SummaryStrip.Children.Add(Strip("Thay đổi", changed ? "Có" : "Chưa nhập", changed)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(new Border { Background = g != null ? Cb("#DCFCE7") : Cb("#F3F4F6"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = g != null ? "Sẵn sàng" : "Chưa sẵn sàng", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = g != null ? C("#16A34A") : C("#94A3B8") } });
    }

    // STEP 1
    private List<AdjGuest> GetFiltered() { var f = _allGuests.AsEnumerable(); var kw = GuestSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Room.Contains(kw) || g.Folio.ToLower().Contains(kw)); var t = GuestRoomTypeFilter.SelectedItem?.ToString(); if (!string.IsNullOrEmpty(t) && t != "Tất cả") f = f.Where(g => g.RoomType == t); var v = GuestVipFilter.SelectedItem?.ToString(); if (v == "VIP") f = f.Where(g => g.IsVip); else if (v == "Không VIP") f = f.Where(g => !g.IsVip); return f.ToList(); }
    private void RefreshGuestPage(bool reset) { if (reset) _guestPage = 0; var f = GetFiltered(); var tp = Math.Max(1, (f.Count + _guestPageSize - 1) / _guestPageSize); if (_guestPage >= tp) _guestPage = tp - 1; GuestGrid.ItemsSource = f.Skip(_guestPage * _guestPageSize).Take(_guestPageSize).ToList(); GuestPagerSummary.Text = $"Hiển thị {_guestPage * _guestPageSize + 1}–{Math.Min((_guestPage + 1) * _guestPageSize, f.Count)} trên tổng {f.Count} hồ sơ"; GuestPrevBtn.IsEnabled = _guestPage > 0; GuestNextBtn.IsEnabled = _guestPage < tp - 1; BuildGuestBtns(tp); }
    private void BuildGuestBtns(int tp) { GuestPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _guestPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _guestPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _guestPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _guestPage = idx; RefreshGuestPage(false); }; nb.Template = GuestPrevBtn.Template; GuestPageBtns.Children.Add(nb); } }
    private void OnGuestSelectionChanged(object s, SelectionChangedEventArgs e)
    {
        if (GuestGrid.SelectedItem is not AdjGuest g) return;
        _guest = g;
        _guestName = g.Name;
        _guestPhone = g.GuestPhone;
        _specialRequests = g.SpecialRequests;
        _internalNotes = g.InternalNotes;
        _reason = string.Empty;
        GuestEmptyState.Visibility = Visibility.Collapsed;
        GuestDetailPanel.Visibility = Visibility.Visible;
        GDName.Text = g.Name;
        GDMeta.Text = g.Folio;
        GDRoom.Text = $"Phòng: {g.Room} · {g.RoomType}";
        GDDates.Text = $"Ngày: {g.Arrival} → {g.Departure}";
        GDFolio.Text = $"Stay: {g.Folio}";
        GDVip.Text = g.IsVip ? $"★ VIP · {g.Profile?.VipLevel}" : "Khách thường";
        GDCount.Text = $"Số khách: {g.Adults} người lớn · {g.Children} trẻ em";
        GDBalance.Text = "Folio: xem tại chức năng Folio";
        GDSource.Text = $"Nguồn: {g.Source}";
        UpdateSummary();
    }
    private void OnApplyGuestFilters(object s, RoutedEventArgs e) => RefreshGuestPage(true);
    private void OnClearGuestFilters(object s, RoutedEventArgs e) { GuestSearchBox.Text = ""; GuestRoomTypeFilter.SelectedIndex = 0; GuestVipFilter.SelectedIndex = 0; RefreshGuestPage(true); }
    private void OnGuestPrevPage(object s, RoutedEventArgs e) { if (_guestPage > 0) { _guestPage--; RefreshGuestPage(false); } }
    private void OnGuestNextPage(object s, RoutedEventArgs e) { _guestPage++; RefreshGuestPage(false); }
    private void OnGuestPageSizeChanged(object s, SelectionChangedEventArgs e) { _guestPageSize = GuestPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGuestPage(true); }

    // STEP 2: Editable stay guest snapshot; guest master profile is read-only here.
    private void BuildStep2()
    {
        Step2Left.Children.Clear(); Step2Right.Children.Clear();
        _guestNameBox = null;
        _guestPhoneBox = null;
        Step2Left.Children.Add(STitle("Thông tin khách lưu trú"));
        if (_guest == null) return;
        var leftGrid = new Grid(); leftGrid.ColumnDefinitions.Add(new ColumnDefinition()); leftGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) }); leftGrid.ColumnDefinitions.Add(new ColumnDefinition());
        var c1 = new StackPanel(); var c2 = new StackPanel();
        var profile = _guest.Profile;
        _guestNameBox = AddEditableField(c1, "Tên khách trên hồ sơ lưu trú", _guestName, OnGuestNameChanged, 200);
        AddField(c1, "Giới tính (hồ sơ khách)", profile?.Gender ?? "—", true);
        AddField(c1, "Ngày sinh", profile?.DateOfBirth?.ToString("dd/MM/yyyy") ?? "—", true);
        AddField(c1, "Quốc tịch", profile?.Nationality ?? "—", true);
        _guestPhoneBox = AddEditableField(c2, "Điện thoại liên hệ", _guestPhone, OnGuestPhoneChanged, 50);
        AddField(c2, "Email (hồ sơ khách)", profile?.Email ?? "—", true);
        AddField(c2, "Giấy tờ", profile?.Documents.Count > 0 ? $"{profile.Documents.Count} giấy tờ trong hồ sơ" : "—", true);
        AddField(c2, "VIP Level", profile?.VipLevel.ToString() ?? "—", true);
        AddField(c1, "Địa chỉ", profile?.Address ?? "—", true);
        AddField(c2, "Công ty", profile?.CompanyName ?? "—", true);
        Grid.SetColumn(c1, 0); Grid.SetColumn(c2, 2); leftGrid.Children.Add(c1); leftGrid.Children.Add(c2);
        Step2Left.Children.Add(leftGrid);
        Step2Right.Children.Add(STitle("Phân biệt dữ liệu"));
        Step2Right.Children.Add(new Border { Background = Cb("#F8FAFC"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Child = new TextBlock { Text = "Tên và điện thoại tại đây được lưu vào hồ sơ Stay đang lưu trú. Hồ sơ khách chuẩn (giới tính, ngày sinh, giấy tờ, email, địa chỉ) chỉ đọc và không bị ghi đè.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = C("#475569") } });
        Step2Right.Children.Add(STitle("Số khách đang ở"));
        Step2Right.Children.Add(Row("Người lớn", _guest.Adults.ToString()));
        Step2Right.Children.Add(Row("Trẻ em", _guest.Children.ToString()));
        Step2Right.Children.Add(Row("Tổng", _guest.GuestCount));
    }

    // STEP 3: Stay dates are intentionally read-only; date changes belong to Extend Stay.
    private void BuildStep3()
    {
        Step3Left.Children.Clear(); Step3Right.Children.Clear();
        if (_guest == null) return;
        Step3Left.Children.Add(STitle("Thời gian lưu trú hiện tại"));
        Step3Left.Children.Add(Row("Ngày đến", _guest.Arrival));
        Step3Left.Children.Add(Row("Ngày đi", _guest.Departure));
        Step3Left.Children.Add(Row("Số đêm", (DateOnly.ParseExact(_guest.Departure, "dd/MM/yyyy").DayNumber - DateOnly.ParseExact(_guest.Arrival, "dd/MM/yyyy").DayNumber).ToString()));
        Step3Left.Children.Add(new Border { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = "Ngày lưu trú chỉ xem tại đây. Dùng mục ‘Gia hạn lưu trú’ nếu cần thay đổi ngày đi để hệ thống kiểm tra phòng và giá.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = C("#1D4ED8") } });
        Step3Right.Children.Add(STitle("Trạng thái Stay"));
        Step3Right.Children.Add(Row("Mã lưu trú", _guest.Folio));
        Step3Right.Children.Add(Row("Phòng", _guest.Room));
        Step3Right.Children.Add(Row("Nguồn đặt", _guest.Source));
        Step3Right.Children.Add(new Border { Background = Cb("#F8FAFC"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = "Luồng này không thực hiện thay đổi ngày, phòng, giá hay nguồn đặt.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = C("#64748B") } });
    }

    // STEP 4: Room is read-only here; room moves have their own availability workflow.
    private void BuildStep4()
    {
        Step4Left.Children.Clear(); Step4Right.Children.Clear();
        if (_guest == null) return;
        Step4Left.Children.Add(STitle("Phòng đang sử dụng"));
        Step4Left.Children.Add(Row("Số phòng", _guest.Room));
        Step4Left.Children.Add(Row("Hạng phòng", _guest.RoomType));
        Step4Left.Children.Add(new Border { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = "Đổi phòng phải đi qua bước kiểm tra phòng trống, đặt phòng trùng lịch và ghi nhận lịch sử. Vui lòng dùng mục ‘Đổi phòng’ riêng.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = C("#1D4ED8") } });
        Step4Right.Children.Add(STitle("Phạm vi chức năng"));
        Step4Right.Children.Add(Row("Thay đổi phòng", "Không thực hiện trong luồng này"));
        Step4Right.Children.Add(Row("Điều chỉnh giá", "Không thực hiện trong luồng này"));
        Step4Right.Children.Add(Row("Rate plan", "Không sửa tại đây"));
        Step4Right.Children.Add(new Border { Background = Cb("#F8FAFC"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = "Các trường không có thao tác lưu đã được bỏ để tránh tạo cảm giác dữ liệu giả.", FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = C("#64748B") } });
    }

    // STEP 5: Persisted request/notes fields only.
    private void BuildStep5()
    {
        Step5Left.Children.Clear(); Step5Right.Children.Clear();
        _requestsBox = null;
        _notesBox = null;
        Step5Left.Children.Add(STitle("Yêu cầu đặc biệt của khách"));
        _requestsBox = AddEditableMultiline(Step5Left, _specialRequests, OnRequestsChanged, 150, 2000);
        Step5Left.Children.Add(new TextBlock { Text = "Lưu vào trường yêu cầu đặc biệt của hồ sơ Stay.", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 6, 0, 0) });
        Step5Right.Children.Add(STitle("Ghi chú nội bộ"));
        _notesBox = AddEditableMultiline(Step5Right, _internalNotes, OnNotesChanged, 150, 1000);
        Step5Right.Children.Add(new TextBlock { Text = "Chỉ nhân viên nội bộ xem được. Nội dung được lưu cùng Stay và ghi lịch sử điều chỉnh.", FontSize = 11, TextWrapping = TextWrapping.Wrap, Foreground = C("#64748B"), Margin = new Thickness(0, 6, 0, 0) });
        Step5Right.Children.Add(new TextBlock { Text = "Lý do điều chỉnh", Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 12, 0, 0) });
        _reasonBox = new ComboBox { Style = FindResource("ModernComboBox") as Style, ItemsSource = new[] { "Khách yêu cầu cập nhật", "Khách thay đổi thông tin liên hệ", "Bổ sung yêu cầu lưu trú", "Cập nhật ghi chú vận hành", "Khác" } };
        if (!string.IsNullOrWhiteSpace(_reason)) _reasonBox.SelectedItem = _reason;
        _reasonBox.SelectionChanged += (_, _) => { _reason = _reasonBox.SelectedItem?.ToString() ?? string.Empty; if (_step == 6) BuildStep6(); };
        Step5Right.Children.Add(_reasonBox);
    }

    // STEP 6: Verify actual persisted changes before enabling save.
    private void BuildStep6()
    {
        Step6Left.Children.Clear(); Step6Right.Children.Clear();
        if (_guest == null) return;
        Step6Left.Children.Add(STitle("Tóm tắt thay đổi"));
        Step6Left.Children.Add(Row("Tên khách", $"{_guest.Name} → {_guestName}"));
        Step6Left.Children.Add(Row("Điện thoại", $"{Display(_guest.GuestPhone)} → {Display(_guestPhone)}"));
        Step6Left.Children.Add(Row("Phòng / ngày", $"{_guest.Room} · {_guest.Arrival} → {_guest.Departure} (không đổi)"));
        Step6Left.Children.Add(Row("Yêu cầu", HasValueChanged(_guest.SpecialRequests, _specialRequests) ? "Đã thay đổi" : "Không đổi"));
        Step6Left.Children.Add(Row("Ghi chú", HasValueChanged(_guest.InternalNotes, _internalNotes) ? "Đã thay đổi" : "Không đổi"));
        Step6Left.Children.Add(Row("Lý do", string.IsNullOrWhiteSpace(_reason) ? "Chưa chọn" : _reason));
        Step6Right.Children.Add(STitle("Kiểm tra trước khi lưu"));
        var hasChanges = HasChanges();
        var hasVersion = _guest.RowVersion is { Length: 8 };
        var hasName = !string.IsNullOrWhiteSpace(_guestName);
        var hasReason = !string.IsNullOrWhiteSpace(_reason);
        var items = new[] { ("Đang chọn Stay", _guest.StayId != Guid.Empty), ("RowVersion hợp lệ", hasVersion), ("Tên khách đã nhập", hasName), ("Có thay đổi cần lưu", hasChanges), ("Đã chọn lý do", hasReason) };
        var allOk = true;
        foreach (var (label, ok) in items)
        {
            Step6Right.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "× ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626") }, new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155") } } });
            if (!ok) allOk = false;
        }
        BtnConfirm.IsEnabled = allOk && !_isSubmitting;
        Step6Right.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEF2F2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "Sẵn sàng lưu hồ sơ Stay" : "Cần hoàn thiện các mục chưa đạt", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626") } });
    }

    // Helpers
    private void AddField(Panel p, string label, string value, bool isReadOnly = false) { p.Children.Add(new TextBlock { Text = label, Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 0) }); p.Children.Add(new TextBox { Text = value, Style = FindResource("ModernTextBox") as Style, IsReadOnly = isReadOnly }); }
    private TextBox AddEditableField(Panel p, string label, string value, TextChangedEventHandler changed, int maxLength)
    {
        p.Children.Add(new TextBlock { Text = label, Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 0) });
        var box = new TextBox { Text = value, Style = FindResource("ModernTextBox") as Style, MaxLength = maxLength };
        box.TextChanged += changed;
        p.Children.Add(box);
        return box;
    }
    private TextBox AddEditableMultiline(Panel p, string value, TextChangedEventHandler changed, double height, int maxLength)
    {
        var box = new TextBox { Text = value, Style = FindResource("ModernTextBox") as Style, Height = height, MaxLength = maxLength, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, AcceptsTab = true };
        box.TextChanged += changed;
        p.Children.Add(box);
        return box;
    }
    private void AddCombo(Panel p, string label, string selected, params string[] items) { p.Children.Add(new TextBlock { Text = label, Style = FindResource("FieldLabel") as Style, Margin = new Thickness(0, 6, 0, 0) }); var cb = new ComboBox { Style = FindResource("ModernComboBox") as Style, ItemsSource = items }; for (int i = 0; i < items.Length; i++) if (items[i] == selected) cb.SelectedIndex = i; p.Children.Add(cb); }
    private void OnGuestNameChanged(object sender, TextChangedEventArgs e) { _guestName = _guestNameBox?.Text ?? string.Empty; UpdateSummary(); if (_step == 6) BuildStep6(); }
    private void OnGuestPhoneChanged(object sender, TextChangedEventArgs e) { _guestPhone = _guestPhoneBox?.Text ?? string.Empty; UpdateSummary(); if (_step == 6) BuildStep6(); }
    private void OnRequestsChanged(object sender, TextChangedEventArgs e) { _specialRequests = _requestsBox?.Text ?? string.Empty; if (_step == 6) BuildStep6(); }
    private void OnNotesChanged(object sender, TextChangedEventArgs e) { _internalNotes = _notesBox?.Text ?? string.Empty; if (_step == 6) BuildStep6(); }
    private bool HasChanges() => _guest is not null &&
        (HasValueChanged(_guest.Name, _guestName) || HasValueChanged(_guest.GuestPhone, _guestPhone) ||
         HasValueChanged(_guest.SpecialRequests, _specialRequests) || HasValueChanged(_guest.InternalNotes, _internalNotes));
    private static bool HasValueChanged(string? original, string? current) => !string.Equals(Normalize(original), Normalize(current), StringComparison.Ordinal);
    private static string Normalize(string? value) => (value ?? string.Empty).Trim();
    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns2((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (_isSubmitting) return;
        if (_guest is null || _stayService is null || _guest.StayId == Guid.Empty || _guest.RowVersion is not { Length: 8 })
        {
            MessageBox.Show("Chưa đủ dữ liệu thật để điều chỉnh: cần chọn Stay và RowVersion hợp lệ.", "Xác nhận điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!HasChanges())
        {
            MessageBox.Show("Chưa có thông tin nào thay đổi để lưu.", "Xác nhận điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_guestName))
        {
            MessageBox.Show("Tên khách không được để trống.", "Xác nhận điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_reason))
        {
            MessageBox.Show("Vui lòng chọn lý do điều chỉnh.", "Xác nhận điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _isSubmitting = true;
        BtnConfirm.IsEnabled = false;
        try
        {
            var result = await _stayService.UpdateInformationAsync(
                new UpdateStayInformationCommand(
                    _guest.StayId,
                    _guest.RowVersion,
                    _guestName,
                    _guestPhone,
                    _specialRequests,
                    _internalNotes,
                    _reason),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể lưu thông tin Stay.", "Xác nhận điều chỉnh", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var saved = result.Value!;
            MessageBox.Show($"Đã cập nhật thông tin lưu trú {saved.StayNumber} cho khách {saved.GuestName}.\nLịch sử và audit đã được ghi nhận.", "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể lưu thông tin lưu trú: " + ex.Message, "Điều chỉnh lưu trú", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isSubmitting = false;
            if (IsLoaded && _step == 6) BuildStep6();
        }
    }
    private async void OnRefresh(object s, RoutedEventArgs e) { await Task.CompletedTask; OnLoadedAsync(this, new RoutedEventArgs()); }
    private void OnClose(object s, RoutedEventArgs e) => Close();
}

internal record AdjGuest(string Room, string Name, string Arrival, string Departure, string RoomType, string GuestCount, bool IsVip, string Folio, string Balance, string FolioStatus, string Source, GuestProfile? Profile = null)
{
    public Guid StayId { get; init; }
    public Guid RoomId { get; init; }
    public byte[]? RowVersion { get; init; }
    public string GuestPhone { get; init; } = string.Empty;
    public string SpecialRequests { get; init; } = string.Empty;
    public string InternalNotes { get; init; } = string.Empty;
    public int Adults { get; init; }
    public int Children { get; init; }
    public string VipDisplay => IsVip ? "★" : "";
}
internal static class SIAExt { public static TextBlock WithRuns2(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
