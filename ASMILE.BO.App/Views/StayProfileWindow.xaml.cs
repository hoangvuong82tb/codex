using ASMILE.BO.App.Controls;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class StayProfileWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private List<StayRow> _allStays;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IGuestProfileRepository? _guestRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IBusinessDocumentService? _businessDocumentService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly Guid? _initialStayId;
    private int _currentTab;
    private readonly TablePagerViewModel<StayRow> _pager = new() { ItemName = "hồ sơ", SelectedPageSize = "20 / trang" };
    private FolioLedgerDto? _selectedLedger;
    private HotelFolioDto? _selectedFolio;
    private IReadOnlyList<StayTimelineEntryDto> _selectedTimeline = Array.Empty<StayTimelineEntryDto>();
    private IReadOnlyList<BusinessDocumentDto> _selectedDocuments = Array.Empty<BusinessDocumentDto>();
    private int _detailsLoadVersion;

    public StayProfileWindow(Guid? initialStayId = null)
    {
        InitializeComponent();
        _initialStayId = initialStayId;
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _guestRepository = services?.GetService<IGuestProfileRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _businessDocumentService = services?.GetService<IBusinessDocumentService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        _allStays = new List<StayRow>();
        StayGrid.ItemsSource = _pager.Items;
        ConfigurePagerFooter();
        ApplyFilters();
        ShowTabContent(0);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_stayRepository is null || _executionContext?.Current is null)
        {
            ApplyFilters();
            FooterTotal.Text = "Chưa cấu hình được Stay service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var context = _executionContext.Current;
            var page = await _stayRepository.SearchAsync(
                context.Property.PropertyId.Value,
                null,
                null,
                null,
                null,
                new PageRequest(1, 500),
                "-UpdatedAtUtc",
                CancellationToken.None);
            var rows = new List<StayRow>(page.Items.Count);
            foreach (var stay in page.Items)
            {
                GuestProfile? profile = null;
                if (_guestRepository is not null && stay.GuestId is Guid guestId)
                    profile = await _guestRepository.GetAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
                rows.Add(MapPersistedStay(stay, profile));
            }
            _allStays = rows;
            ApplyFilters();
            if (_initialStayId is Guid stayId)
                StayGrid.SelectedItem = _pager.Items.FirstOrDefault(row => row.StayId == stayId);
            FooterTotal.Text = $"Đã tải {_allStays.Count:N0} hồ sơ lưu trú từ dữ liệu Stay thật.";
        }
        catch (Exception ex)
        {
            _allStays = new List<StayRow>();
            ApplyFilters();
            FooterTotal.Text = $"Không tải được hồ sơ lưu trú thật: {ex.Message}";
        }
    }

    private static StayRow MapPersistedStay(HotelStayDto stay, GuestProfile? profile)
    {
        var status = stay.Status switch
        {
            HotelStayStatus.CheckedOut or HotelStayStatus.Completed or HotelStayStatus.Archived => "Đã trả phòng",
            HotelStayStatus.Cancelled => "Đã hủy",
            HotelStayStatus.PendingArrival or HotelStayStatus.PreArrival => "Sắp đến",
            _ => "Đang lưu trú"
        };
        return new StayRow(
            status,
            stay.StayNumber,
            stay.GuestName,
            profile?.Phone ?? "—",
            profile?.Documents.FirstOrDefault() is { } document
                ? $"{document.DocumentType}: {document.DocumentNumber}"
                : "—",
            stay.RoomNumber,
            stay.ScheduledArrivalDate.ToString("dd/MM/yyyy"),
            stay.ScheduledDepartureDate.ToString("dd/MM/yyyy"),
            stay.GuestId,
            stay.PropertyId,
            stay.RowVersion,
            stay.HotelStayId);
    }

    private void ConfigurePagerFooter()
    {
        if (Content is not Grid root) return;
        var footer = root.Children.OfType<Border>().LastOrDefault();
        if (footer?.Child is not Grid footerGrid) return;
        var actions = footerGrid.Children.OfType<StackPanel>().FirstOrDefault();
        if (actions is null) return;

        actions.Children.Clear();
        actions.Children.Add(new ServerPagerControl { DataContext = _pager });
        actions.Children.Add(new Button
        {
            Content = "Đóng",
            Style = (Style)FindResource("GrayBtn"),
            Margin = new Thickness(16, 0, 0, 0)
        });
        ((Button)actions.Children[^1]).Click += OnClose;
    }

    private void ApplyFilters()
    {
        var filtered = _allStays.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            filtered = filtered.Where(r => r.Col3.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase) || r.Col5.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase));
        if (StatusFilter.SelectedItem is ComboBoxItem cbi && cbi.Content is string st && st != "Tất cả")
            filtered = filtered.Where(r => r.Col1 == st);
        var list = filtered.ToList();
        _pager.SetSource(list);
        ResultCount.Text = $"{list.Count} hồ sơ";
        FooterTotal.Text = _pager.PageText;
    }

    private void OnStaySelected(object sender, SelectionChangedEventArgs e)
    {
        if (StayGrid.SelectedItem is StayRow r)
        {
            DetailName.Text = r.Col3;
            DetailMeta.Text = $"{r.Col2} · Phòng {r.Col6} · {r.Col7} → {r.Col8}";
            MetricStatus.Text = r.Col1;
            MetricVisits.Text = "—";
            MetricNights.Text = CalculateNights(r.Col7, r.Col8).ToString(CultureInfo.InvariantCulture);
            MetricLast.Text = r.Col7;
            ShowTabContent(_currentTab);
            _ = LoadSelectedDetailsAsync(r);
        }
    }

    private static int CalculateNights(string arrival, string departure)
    {
        const string dateFormat = "dd/MM/yyyy";
        if (!DateTime.TryParseExact(arrival, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var arrivalDate)
            || !DateTime.TryParseExact(departure, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var departureDate))
        {
            return 1;
        }

        return Math.Max(1, (departureDate - arrivalDate).Days);
    }

    private void OnTabClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button b && b.Tag is string t && int.TryParse(t, out var tab))
            ShowTabContent(tab);
    }

    private void ShowTabContent(int tab)
    {
        _currentTab = tab; TabContent.Children.Clear();
        var r = StayGrid.SelectedItem as StayRow;
        if (r is null) { TabContent.Children.Add(new TextBlock { Text = "Chọn một hồ sơ để xem thông tin.", FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")), Margin = new Thickness(0, 20, 0, 0) }); return; }
        switch (tab)
        {
            case 0: ShowOverview(r); break; case 1: ShowDocuments(r); break; case 2: ShowHistory(r); break; case 3: ShowRoom(r); break; case 4: ShowFinance(r); break;
        }
    }

    private void ShowOverview(StayRow r)
    {
        TabContent.Children.Add(Section("Thông tin khách", Row("Họ và tên", r.Col3), Row("Điện thoại", r.Col4), Row("Giấy tờ", r.Col5)));
        TabContent.Children.Add(Section("Lưu trú hiện tại", Row("Mã HS", r.Col2), Row("Trạng thái", r.Col1), Row("Ngày đến", r.Col7), Row("Ngày đi", r.Col8), Row("Phòng", r.Col6)));
    }
    private void ShowDocuments(StayRow r)
    {
        var children = new List<FrameworkElement>
        {
            Row("Giấy tờ chính", r.Col5)
        };

        if (_selectedDocuments.Count == 0)
        {
            children.Add(Row("Tài liệu đính kèm", r.GuestId is null
                ? "Hồ sơ chưa có GuestProfileId thật"
                : "Chưa có tài liệu giấy tờ thật"));
        }
        else
        {
            children.Add(Row("Tài liệu đính kèm", $"{_selectedDocuments.Count:N0} tài liệu từ BusinessDocuments"));
            foreach (var document in _selectedDocuments.Take(10))
            {
                children.Add(Row(document.DocumentType.ToString(),
                    $"{(string.IsNullOrWhiteSpace(document.FileName) ? document.Title : document.FileName)} · {document.CreatedAt:dd/MM/yyyy HH:mm}"));
            }
        }

        children.Add(Row("Nguồn", "BusinessDocuments / hồ sơ khách thật"));
        TabContent.Children.Add(Section("Giấy tờ", children.ToArray()));
    }

    private void ShowHistory(StayRow r)
    {
        var children = new List<FrameworkElement>();
        if (_selectedTimeline.Count == 0)
        {
            children.Add(Row("Timeline", "Chưa có lịch sử trạng thái thật của Stay"));
        }
        else
        {
            foreach (var entry in _selectedTimeline.Take(12))
            {
                var transition = entry.FromStatus.HasValue
                    ? $"{entry.FromStatus} → {entry.ToStatus}"
                    : entry.ToStatus.ToString();
                children.Add(Row(entry.OccurredAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
                    $"{entry.EventName}: {transition}"));
            }
        }

        children.Add(Row("Nguồn", "HotelStay history thật"));
        TabContent.Children.Add(Section("Lịch sử lưu trú", children.ToArray()));
    }

    private void ShowRoom(StayRow r)
    {
        var children = new List<FrameworkElement>
        {
            Row("Phòng", r.Col6),
            Row("Ngày ở", $"{r.Col7} → {r.Col8}")
        };

        var entries = _selectedLedger?.Entries ?? Array.Empty<HotelFolioEntryDto>();
        if (entries.Count == 0)
        {
            children.Add(Row("Dịch vụ & phát sinh", "Chưa có phát sinh Folio thật"));
        }
        else
        {
            children.Add(Row("Dịch vụ & phát sinh", $"{entries.Count:N0} dòng Folio thật"));
            foreach (var entry in entries.OrderByDescending(x => x.CreatedAtUtc).Take(10))
            {
                children.Add(Row(entry.CreatedAtUtc.ToLocalTime().ToString("dd/MM HH:mm"),
                    $"{entry.Description} · {entry.Amount:N0} VND"));
            }
        }

        children.Add(Row("Nguồn", "Folio ledger thật của Stay"));
        TabContent.Children.Add(Section("Phòng & dịch vụ", children.ToArray()));
    }

    private void ShowFinance(StayRow r)
    {
        var folio = _selectedFolio;
        TabContent.Children.Add(Section("Tài chính",
            Row("Mã lưu trú", r.Col2),
            Row("Folio", folio?.FolioNumber ?? "Chưa có Folio thật"),
            Row("Tổng phát sinh", folio is null ? "—" : $"{folio.TotalCharges:N0} VND"),
            Row("Đã thanh toán", folio is null ? "—" : $"{folio.TotalPayments:N0} VND"),
            Row("Còn phải thu", folio is null ? "—" : $"{folio.OutstandingBalance:N0} VND"),
            Row("Nguồn", "HotelFolio / ledger thật của Stay")));
    }

    private async Task LoadSelectedDetailsAsync(StayRow row)
    {
        var loadVersion = ++_detailsLoadVersion;
        _selectedLedger = null;
        _selectedFolio = null;
        _selectedTimeline = Array.Empty<StayTimelineEntryDto>();
        _selectedDocuments = Array.Empty<BusinessDocumentDto>();

        if (_executionContext?.Current is not { } context || row.StayId is not Guid stayId || stayId == Guid.Empty)
            return;

        try
        {
            var propertyId = context.Property.PropertyId.Value;
            if (_stayRepository is not null)
                _selectedTimeline = await _stayRepository.GetTimelineAsync(propertyId, stayId, CancellationToken.None);

            if (_folioRepository is not null)
            {
                _selectedFolio = await _folioRepository.GetByStayAsync(propertyId, stayId, CancellationToken.None);
                if (_selectedFolio is not null)
                    _selectedLedger = await _folioRepository.GetLedgerAsync(propertyId, _selectedFolio.HotelFolioId, CancellationToken.None);
            }

            if (_businessDocumentService is not null && row.GuestId is Guid guestId && guestId != Guid.Empty)
                _selectedDocuments = await _businessDocumentService.GetDocumentsByCustomerAsync(guestId);
        }
        catch
        {
            if (loadVersion != _detailsLoadVersion)
                return;
            _selectedLedger = null;
            _selectedFolio = null;
            _selectedTimeline = Array.Empty<StayTimelineEntryDto>();
            _selectedDocuments = Array.Empty<BusinessDocumentDto>();
        }

        if (loadVersion == _detailsLoadVersion && StayGrid.SelectedItem is StayRow selected && selected.StayId == row.StayId)
            ShowTabContent(_currentTab);
    }

    private Border Section(string title, params FrameworkElement[] children)
    {
        var sp = new StackPanel();
        foreach (var c in children) sp.Children.Add(c);
        var header = new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0F172A")), Margin = new Thickness(0, 0, 0, 8) };
        var inner = new StackPanel(); inner.Children.Add(header); inner.Children.Add(sp);
        return new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FAFBFD")),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12, 14, 12),
            Margin = new Thickness(0, 0, 0, 10), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F1F5F9")), BorderThickness = new Thickness(1), Child = inner };
    }
    private TextBlock Row(string label, string value)
    {
        var tb = new TextBlock { FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F8198")), Margin = new Thickness(0, 3, 0, 0) };
        tb.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B8DA6")) });
        tb.Inlines.Add(new System.Windows.Documents.Run(value) { FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")) });
        return tb;
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object sender, RoutedEventArgs e) { SearchBox.Text = ""; StatusFilter.SelectedIndex = 0; ApplyFilters(); }
    private async void OnEditToggle(object sender, RoutedEventArgs e)
    {
        if (StayGrid.SelectedItem is not StayRow row)
        {
            MessageBox.Show("Vui lòng chọn một hồ sơ lưu trú.", "Hồ sơ lưu trú", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_guestRepository is null || _executionContext?.Current is not { } context || row.GuestId is not Guid guestId)
        {
            MessageBox.Show("Hồ sơ này chưa có GuestId hoặc chưa cấu hình GuestProfile service dữ liệu thật.", "Hồ sơ lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var profile = await _guestRepository.GetAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
            if (profile is null)
            {
                MessageBox.Show("Không tìm thấy GuestProfile thật để chỉnh sửa.", "Hồ sơ lưu trú", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editor = BuildGuestEditor(profile, context.Actor.ActorId);
            if (editor.ShowDialog() == true)
            {
                await OnLoadedAsyncAndWait();
                if (StayGrid.Items.Count > 0) StayGrid.SelectedIndex = Math.Min(StayGrid.SelectedIndex, StayGrid.Items.Count - 1);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể cập nhật hồ sơ khách thật: " + ex.Message, "Hồ sơ lưu trú", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private Window BuildGuestEditor(GuestProfile profile, ActorId? actorId)
    {
        var editor = new Window
        {
            Title = $"Chỉnh sửa hồ sơ khách — {profile.FullName}",
            Width = 520,
            Height = 520,
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Background = Brushes.White
        };
        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock { Text = profile.FullName, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brush("#102E5A") });
        panel.Children.Add(new TextBlock { Text = $"{profile.GuestNumber} · cập nhật hồ sơ GuestProfile thật", FontSize = 12, Foreground = Brush("#64748B"), Margin = new Thickness(0, 3, 0, 12) });

        var phone = EditorField(panel, "Điện thoại", profile.Phone);
        var email = EditorField(panel, "Email", profile.Email);
        var address = EditorField(panel, "Địa chỉ", profile.Address);
        var nationality = EditorField(panel, "Quốc tịch", profile.Nationality);
        var gender = EditorField(panel, "Giới tính", profile.Gender);
        var dob = EditorField(panel, "Ngày sinh (dd/MM/yyyy)", profile.DateOfBirth?.ToString("dd/MM/yyyy"));
        var error = new TextBlock { Foreground = Brush("#DC2626"), FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
        panel.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
        var cancel = new Button { Content = "Hủy", Style = FindResource("GrayBtn") as Style, Margin = new Thickness(0, 0, 8, 0) };
        var save = new Button { Content = "Lưu hồ sơ thật", Style = FindResource("BlueBtn") as Style };
        cancel.Click += (_, _) => { editor.DialogResult = false; editor.Close(); };
        save.Click += async (_, _) =>
        {
            DateOnly? dateOfBirth = null;
            if (!string.IsNullOrWhiteSpace(dob.Text) && !DateOnly.TryParseExact(dob.Text.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                error.Text = "Ngày sinh không đúng định dạng dd/MM/yyyy.";
                return;
            }
            else if (!string.IsNullOrWhiteSpace(dob.Text))
            {
                dateOfBirth = DateOnly.ParseExact(dob.Text.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
            }

            save.IsEnabled = false;
            try
            {
                profile.UpdateProfile(EmptyToNull(phone.Text), EmptyToNull(email.Text), EmptyToNull(address.Text), EmptyToNull(nationality.Text), EmptyToNull(gender.Text), dateOfBirth, DateTime.UtcNow, actorId);
                await _guestRepository!.UpdateAsync(profile, CancellationToken.None);
                editor.DialogResult = true;
                editor.Close();
            }
            catch (Exception ex)
            {
                save.IsEnabled = true;
                error.Text = "Không thể lưu hồ sơ: " + ex.Message;
            }
        };
        buttons.Children.Add(cancel); buttons.Children.Add(save); panel.Children.Add(buttons);
        editor.Content = panel;
        return editor;
    }

    private TextBox EditorField(Panel panel, string label, string? value)
    {
        panel.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = Brush("#64748B"), Margin = new Thickness(0, 6, 0, 3) });
        var box = new TextBox { Text = value ?? "", Style = FindResource("ModTextBox") as Style };
        panel.Children.Add(box);
        return box;
    }

    private async Task OnLoadedAsyncAndWait()
    {
        if (_stayRepository is null || _executionContext?.Current is not { } context) return;
        var page = await _stayRepository.SearchAsync(context.Property.PropertyId.Value, null, null, null, null, new PageRequest(1, 500), "-UpdatedAtUtc", CancellationToken.None);
        var rows = new List<StayRow>(page.Items.Count);
        foreach (var stay in page.Items)
        {
            GuestProfile? profile = null;
            if (_guestRepository is not null && stay.GuestId is Guid guestId)
                profile = await _guestRepository.GetAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
            rows.Add(MapPersistedStay(stay, profile));
        }
        _allStays = rows;
        ApplyFilters();
        FooterTotal.Text = $"Đã tải {_allStays.Count:N0} hồ sơ lưu trú từ dữ liệu Stay thật.";
    }

    private static string? EmptyToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private async void OnRefresh(object sender, RoutedEventArgs e) => await OnLoadedAsyncAndWait();
}

internal record StayRow(string Col1, string Col2 = "", string Col3 = "", string Col4 = "", string Col5 = "", string Col6 = "", string Col7 = "", string Col8 = "", Guid? GuestId = null, Guid PropertyId = default, byte[]? RowVersion = null, Guid? StayId = null);
