using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.Guest;
using ASMILE.BO.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class GroupGuestManagementWindow : Window
{
    private int _step; private bool _hasChanges;

    // Guest data
    private readonly ObservableCollection<GmGroup> _groups = new();
    private GmGroup? _selectedGroup;
    private readonly ObservableCollection<GmGuest> _guests = new();
    private GmGuest? _selectedGuest;
    private string _searchText = "";
    private readonly Dictionary<Guid, GroupReservationSummaryDto> _groupSummaries = new();
    private readonly IGroupReservationQueryService? _groupQuery;
    private readonly IRoomingListService? _roomingListService;
    private readonly IGuestProfileRepository? _guestRepository;
    private readonly IBusinessDocumentService? _businessDocumentService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private IServiceScope? _dataScope;
    private IReadOnlyList<BusinessDocumentDto> _guestAttachments = Array.Empty<BusinessDocumentDto>();

    // Guest profile fields (Step B)
    private string _gmFirstName = "";
    private string _gmGender = "", _gmNationality = "", _gmLanguage = "";
    private DateTime? _gmBirthday;
    private string _gmPhone = "", _gmEmail = "", _gmAddress = "", _gmCity = "", _gmCountry = "";
    private string _gmCompany = "", _gmDepartment = "", _gmPosition = "";
    private string _gmAllergy = "", _gmMedical = "", _gmDiet = "", _gmSpecialNeed = "";
    private bool _gmWheelchair, _gmExtraBed, _gmBabyCot;

    // Documents (Step C)
    private string _gmPassport = "", _gmPassportIssue = "", _gmPassportExpiry = "", _gmPassportPlace = "";
    private string _gmVisaNo = "", _gmVisaType = "", _gmVisaIssue = "", _gmVisaExpiry = "";
    private string _gmIdCard = "", _gmDriverLicense = "";

    // Stay (Step D)
    private string _gmRoomType = "", _gmRoomNumber = "", _gmRatePlan = "", _gmMealPlan = "", _gmBedType = "";
    private string _gmRoommate = "";
    private bool _gmPickup, _gmDropoff, _gmLateCO, _gmEarlyCI, _gmQuiet, _gmNearLift, _gmHighFloor;
    private string _gmFlight = "", _gmEta = "", _gmEtd = "";

    // Confirm
    private bool _confirmProfile, _confirmDocs, _confirmStay, _confirmAll;

    // UI
    private readonly Border[] _stepBoxes = new Border[7];
    private readonly TextBlock[] _stepLbls = new TextBlock[7];
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F", "G" };
    private static readonly string[] StepTitles = { "Danh sách đoàn", "Danh sách khách", "Hồ sơ", "Giấy tờ", "Lưu trú", "Lịch sử", "Xác nhận" };

    public GroupGuestManagementWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _dataScope = services?.CreateScope();
        var scopedServices = _dataScope?.ServiceProvider;
        _groupQuery = scopedServices?.GetService<IGroupReservationQueryService>();
        _roomingListService = scopedServices?.GetService<IRoomingListService>();
        _guestRepository = scopedServices?.GetService<IGuestProfileRepository>();
        _businessDocumentService = scopedServices?.GetService<IBusinessDocumentService>();
        _executionContext = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        BuildStepBar();
        GoTo(0);
        Loaded += async (_, _) => await LoadPersistedGroupsAsync();
        Closed += OnWindowClosed;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        Closed -= OnWindowClosed;
        _dataScope?.Dispose();
        _dataScope = null;
    }

    private async Task LoadPersistedGroupsAsync()
    {
        _groups.Clear();
        _groupSummaries.Clear();
        _guests.Clear();
        _selectedGroup = null;
        _selectedGuest = null;

        if (_groupQuery is null || _executionContext?.Current is not { } context)
        {
            FooterLabel.Text = "Chưa cấu hình Group query và property context dữ liệu thật.";
            BuildContent(0);
            UpdateStatus();
            return;
        }

        var propertyId = context.Property.PropertyId.Value;

        try
        {
            var summaries = await _groupQuery.GetGroupsAsync(propertyId, CancellationToken.None);
            foreach (var summary in summaries)
            {
                _groupSummaries[summary.GroupReservationId] = summary;
                _groups.Add(new GmGroup
                {
                    GroupGuid = summary.GroupReservationId,
                    Code = summary.GroupCode,
                    Name = summary.GroupName,
                    Company = summary.CompanyName ?? summary.AgencyName ?? "—",
                    Arrival = summary.ArrivalDate.ToDateTime(TimeOnly.MinValue),
                    Departure = summary.DepartureDate.ToDateTime(TimeOnly.MinValue),
                    TotalGuests = summary.TotalAdults + summary.TotalChildren,
                    Status = summary.Status.ToString()
                });
            }
            FooterLabel.Text = $"Đã tải {_groups.Count} đoàn từ dữ liệu thật · Chọn một đoàn để xem khách.";
            BuildContent(0);
            UpdateStatus();
        }
        catch (Exception ex)
        {
            FooterLabel.Text = "Không tải được danh sách đoàn thật: " + ex.Message;
            BuildContent(0);
            UpdateStatus();
        }
    }

    private void LoadGuestToForm(GmGuest g)
    {
        _gmFirstName = g.Name;
        _gmGender = g.Gender; _gmNationality = g.Nationality; _gmLanguage = "Tiếng Việt";
        _gmBirthday = g.Birthday;
        _gmPhone = g.Phone; _gmEmail = g.Email; _gmAddress = ""; _gmCity = ""; _gmCountry = g.Nationality;
        _gmCompany = g.Company; _gmDepartment = ""; _gmPosition = "";
        _gmAllergy = g.Allergy; _gmMedical = ""; _gmDiet = ""; _gmSpecialNeed = "";
        _gmWheelchair = false; _gmExtraBed = false; _gmBabyCot = false;
        _gmPassport = g.Passport; _gmPassportIssue = ""; _gmPassportExpiry = ""; _gmPassportPlace = "";
        _gmVisaNo = g.Visa; _gmVisaType = ""; _gmVisaIssue = ""; _gmVisaExpiry = "";
        _gmIdCard = ""; _gmDriverLicense = "";
        _gmRoomType = g.RoomType; _gmRoomNumber = g.RoomNumber; _gmRatePlan = "GROUP"; _gmMealPlan = g.MealPlan; _gmBedType = "";
        _gmRoommate = "";
        _gmPickup = false; _gmDropoff = false; _gmLateCO = false; _gmEarlyCI = false; _gmQuiet = false; _gmNearLift = false; _gmHighFloor = false;
        _gmFlight = ""; _gmEta = ""; _gmEtd = "";
    }

    // ═══ STEP BAR ═══
    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        for (int i = 0; i < StepLetters.Length; i++)
        {
            var idx = i;
            var lbl = new TextBlock { Text = "Chưa bắt đầu", FontSize = 9, Foreground = C("#64748B") }; _stepLbls[i] = lbl;
            var letter = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(7), Background = Cb("#F1F5F9"), Child = new TextBlock { Text = StepLetters[i], FontSize = 13, FontWeight = FontWeights.Bold, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            var box = new Border { Width = 150, Height = 60, CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 2, 0), Background = Brushes.White, BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand, Padding = new Thickness(10, 7, 10, 7), Child = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition() }, Children = { letter, new StackPanel { Margin = new Thickness(8, 0, 0, 0), Children = { new TextBlock { Text = StepTitles[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#64748B") }, lbl } } } } };
            Grid.SetColumn(letter, 0); Grid.SetColumn((StackPanel)((Grid)box.Child).Children[1], 1);
            box.MouseLeftButtonDown += (_, _) => { if (idx <= _step || CanEnter(idx)) GoTo(idx); };
            _stepBoxes[i] = box; StepBar.Children.Add(box);
            if (i < StepLetters.Length - 1) StepBar.Children.Add(new TextBlock { Text = "▸", FontSize = 14, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });
        }
    }

    private void RefreshStepper()
    {
        for (int i = 0; i < StepLetters.Length; i++)
        {
            if (i < _step) { _stepBoxes[i].Background = Cb("#F0FDF4"); _stepBoxes[i].BorderBrush = Cb("#86EFAC"); _stepBoxes[i].BorderThickness = new Thickness(1); _stepLbls[i].Text = "Hoàn thành"; _stepLbls[i].Foreground = C("#166534"); }
            else if (i == _step) { _stepBoxes[i].Background = Cb("#EFF6FF"); _stepBoxes[i].BorderBrush = Cb("#195FCE"); _stepBoxes[i].BorderThickness = new Thickness(1.5); _stepLbls[i].Text = "Đang nhập"; _stepLbls[i].Foreground = C("#1E40AF"); }
            else { _stepBoxes[i].Background = Brushes.White; _stepBoxes[i].BorderBrush = Cb("#E2E8F0"); _stepBoxes[i].BorderThickness = new Thickness(1); _stepLbls[i].Text = "Chưa bắt đầu"; _stepLbls[i].Foreground = C("#64748B"); }
        }
    }

    private bool CanEnter(int s) => s switch
    {
        0 => true,
        1 => _selectedGroup != null,
        2 or 3 or 4 or 5 or 6 => _selectedGroup != null && _selectedGuest != null,
        _ => false
    };
    private bool CanProceed() => _step switch { 0 => _selectedGroup != null, 1 => _selectedGuest != null, 2 => true, 3 => true, 4 => true, 5 => true, 6 => false, _ => false };

    private void GoTo(int s)
    {
        if (s >= 2 && _selectedGuest != null) LoadGuestToForm(_selectedGuest);
        _step = s; RefreshStepper(); BuildContent(s); UpdateFooter(); UpdateStatus();
    }

    private async void SelectGroup(GmGroup group)
    {
        _selectedGroup = group;
        _selectedGuest = null;
        _searchText = "";
        _guests.Clear();
        if (group.GroupGuid != Guid.Empty && _groupSummaries.TryGetValue(group.GroupGuid, out var summary))
        {
            var index = 0;
            foreach (var item in summary.RoomingList)
            {
                _guests.Add(new GmGuest
                {
                    Id = ++index,
                    GuestGuid = item.GuestId ?? Guid.Empty,
                    Name = item.GuestName ?? "Chưa gán khách",
                    RoomNumber = item.RoomNumber,
                    RoomType = item.RoomType,
                    Arrival = group.Arrival,
                    Departure = group.Departure,
                    CheckedIn = item.IsCheckedIn,
                    Status = item.IsCheckedOut ? "Đã trả phòng" : item.IsCheckedIn ? "Đã nhận phòng" : "Chưa nhận phòng",
                    Vip = item.IsVip,
                    Leader = index == 1,
                    Gender = "—",
                    Nationality = "—",
                    MealPlan = "—"
                });
            }
        }
        BuildContent(0);
        UpdateFooter();
        UpdateStatus();
        await Task.CompletedTask;
    }

    private async Task EnrichGuestFromProfileAsync(GmGuest guest)
    {
        if (guest.GuestGuid == Guid.Empty || _guestRepository is null || _executionContext?.Current is not { } context) return;
        var profile = await _guestRepository.GetAsync(context.Property.PropertyId.Value, guest.GuestGuid, CancellationToken.None);
        if (_selectedGuest != guest) return;

        if (profile is not null)
        {
            guest.Name = profile.FullName;
            guest.Phone = profile.Phone ?? "";
            guest.Email = profile.Email ?? "";
            guest.Gender = profile.Gender ?? "—";
            guest.Birthday = profile.DateOfBirth?.ToDateTime(TimeOnly.MinValue) ?? guest.Birthday;
            guest.Nationality = profile.Nationality ?? "—";
            guest.Company = profile.CompanyName ?? "";
            guest.Vip = profile.VipLevel != VipLevel.Normal;
            guest.Passport = profile.Documents.FirstOrDefault(document => document.DocumentType == GuestDocumentType.Passport)?.DocumentNumber ?? "";
            guest.Visa = profile.Documents.FirstOrDefault(document => document.DocumentType == GuestDocumentType.Visa)?.DocumentNumber ?? "";
            LoadGuestToForm(guest);
        }

        try
        {
            var history = await _guestRepository.GetHistoryAsync(
                new GuestHistoryQuery(guest.GuestGuid, null, null, null, null, 1, 50),
                CancellationToken.None);
            guest.Timeline.Clear();
            foreach (var entry in history.OrderByDescending(item => item.OccurredAtUtc))
            {
                guest.Timeline.Add(new GmTimelineEntry
                {
                    Date = entry.OccurredAtUtc.ToLocalTime(),
                    Event = string.IsNullOrWhiteSpace(entry.EventType) ? "History" : entry.EventType,
                    User = string.IsNullOrWhiteSpace(entry.Module) ? "System" : entry.Module,
                    Detail = entry.Description
                });
            }
        }
        catch
        {
            // Keep the real profile usable if a history provider is temporarily unavailable.
            guest.Timeline.Clear();
        }

        try
        {
            _guestAttachments = _businessDocumentService is null || guest.GuestGuid == Guid.Empty
                ? Array.Empty<BusinessDocumentDto>()
                : await _businessDocumentService.GetDocumentsByEntityAsync(BusinessEntityType.Customer, guest.GuestGuid);
        }
        catch
        {
            _guestAttachments = Array.Empty<BusinessDocumentDto>();
        }

        BuildContent(_step);
        UpdateStatus();
    }

    private void OnNext(object s, RoutedEventArgs e) { if (_step < StepTitles.Length - 1 && CanProceed()) { _hasChanges = true; GoTo(_step + 1); } }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 0) GoTo(_step - 1); }
    private void OnCancel(object s, RoutedEventArgs e) { if (_hasChanges && MessageBox.Show("Huỷ thay đổi đang thực hiện?", "Xác nhận", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return; Close(); }
    private void OnClose(object s, RoutedEventArgs e) => OnCancel(s, e);
    private async void OnRefresh(object s, RoutedEventArgs e) { _hasChanges = false; await LoadPersistedGroupsAsync(); GoTo(0); }
    private void OnWindowKeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Escape) OnCancel(s, e); else if (e.Key == Key.Enter && CanProceed() && _step < StepTitles.Length - 1) OnNext(s, e); }

    private void UpdateFooter()
    {
        FooterLabel.Text = $"Bước {_step + 1}/{StepTitles.Length} · {StepTitles[_step]}"; BtnBack.IsEnabled = _step > 0;
        var warn = GetWarnings();
        FooterWarn.Visibility = warn.Count > 0 ? Visibility.Visible : Visibility.Collapsed; FooterWarn.Text = $"⚠ {warn.Count}";
        if (_step == StepTitles.Length - 1) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = CanConfirm(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; BtnNext.IsEnabled = CanProceed(); }
    }

    private void UpdateStatus()
    {
        StatusPanel.Children.Clear();
        if (_selectedGroup != null) StatusPanel.Children.Add(Chip("Đoàn", _selectedGroup.Name, "#7C3AED"));
        StatusPanel.Children.Add(Chip("Tổng KH", $"{_guests.Count}", "#195FCE"));
        StatusPanel.Children.Add(Chip("Đã check-in", $"{_guests.Count(g => g.CheckedIn)}", "#16A34A"));
        StatusPanel.Children.Add(Chip("VIP", $"{_guests.Count(g => g.Vip)}", "#D97706"));
        StatusPanel.Children.Add(Chip("Leader", $"{_guests.Count(g => g.Leader)}", "#7C3AED"));
        StatusPanel.Children.Add(Chip("Passport", $"{_guests.Count(g => !string.IsNullOrEmpty(g.Passport))}/{_guests.Count}", "#0891B2"));
        if (_selectedGuest != null) StatusPanel.Children.Add(Chip("Đã chọn", _selectedGuest.Name, "#195FCE"));
    }

    private static Border Chip(string l, string v, string c) => new() { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}1A")), CornerRadius = new CornerRadius(7), Padding = new Thickness(9, 4, 9, 4), Margin = new Thickness(0, 0, 7, 0), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}33")), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = l + ": ", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c)), FontWeight = FontWeights.SemiBold }, new TextBlock { Text = v, FontSize = 11, Foreground = C("#334155") } } } };

    // ═══ CONTENT ═══
    private void BuildContent(int s) { ContentPanel.Children.Clear(); switch (s) { case 0: BuildGroupChooser(); break; case 1: BuildA(); break; case 2: BuildB(); break; case 3: BuildC(); break; case 4: BuildD(); break; case 5: BuildE(); break; case 6: BuildF(); break; } }

    // ═══ A: GUEST LIST ═══
    private void BuildGroupChooser()
    {
        ContentPanel.Children.Add(STitle("Ch\u1ECDn \u0111o\u00E0n"));
        ContentPanel.Children.Add(new TextBlock { Text = "Ch\u1ECDn m\u1ED9t \u0111o\u00E0n tr\u01B0\u1EDBc khi xem danh s\u00E1ch kh\u00E1ch trong \u0111o\u00E0n.", FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 0, 0, 12) });
        var shell = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(7, GridUnitType.Star) }, new ColumnDefinition { Width = new GridLength(12) }, new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) } } };
        var list = new StackPanel();
        foreach (var group in _groups)
        {
            var card = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand };
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition());
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var info = new StackPanel();
            info.Children.Add(new TextBlock { Text = $"{group.Code}  ·  {group.Name}", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") });
            info.Children.Add(new TextBlock { Text = $"{group.Company}  |  {group.Arrival:dd/MM} → {group.Departure:dd/MM}  |  {group.TotalGuests} khách", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 4, 0, 0) });
            Grid.SetColumn(info, 0);
            grid.Children.Add(info);
            var status = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 4, 8, 4), Background = Cb("#DCFCE7"), Child = new TextBlock { Text = group.Status, FontSize = 11, Foreground = C("#16A34A"), FontWeight = FontWeights.SemiBold } };
            Grid.SetColumn(status, 1);
            grid.Children.Add(status);
            card.Child = grid;
            if (_selectedGroup?.GroupGuid == group.GroupGuid)
            {
                card.BorderBrush = Cb("#7C3AED");
                card.BorderThickness = new Thickness(2);
                card.Background = Cb("#F5F3FF");
            }
            card.MouseLeftButtonDown += (_, _) => SelectGroup(group);
            list.Children.Add(card);
        }
        var listCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(10), Child = new ScrollViewer { MaxHeight = 560, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = list } };
        Grid.SetColumn(listCard, 0); shell.Children.Add(listCard);
        var detailCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14), Child = BuildGroupDetailCard() };
        Grid.SetColumn(detailCard, 2); shell.Children.Add(detailCard);
        ContentPanel.Children.Add(shell);
    }

    private FrameworkElement BuildGroupDetailCard()
    {
        var panel = new StackPanel();
        panel.Children.Add(SecTitle("Th\u00F4ng tin \u0111o\u00E0n"));
        if (_selectedGroup == null)
        {
            panel.Children.Add(new TextBlock { Text = "Ch\u1ECDn m\u1ED9t \u0111o\u00E0n \u0111\u1EC3 xem chi ti\u1EBFt.", FontSize = 12, Foreground = C("#94A3B8"), Margin = new Thickness(0, 8, 0, 0) });
            return panel;
        }
        panel.Children.Add(new TextBlock { Text = _selectedGroup.Name, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") });
        panel.Children.Add(new TextBlock { Text = _selectedGroup.Code, FontSize = 11, Foreground = C("#7C3AED"), Margin = new Thickness(0, 3, 0, 10) });
        panel.Children.Add(Row("C\u00F4ng ty", _selectedGroup.Company));
        panel.Children.Add(Row("Tr\u1EA1ng th\u00E1i", _selectedGroup.Status));
        panel.Children.Add(Row("\u0110\u1EBFn", _selectedGroup.Arrival.ToString("dd/MM/yyyy")));
        panel.Children.Add(Row("\u0110i", _selectedGroup.Departure.ToString("dd/MM/yyyy")));
        panel.Children.Add(Row("S\u1ED1 \u0111\u00EAm", $"{(_selectedGroup.Departure - _selectedGroup.Arrival).Days}"));
        panel.Children.Add(Row("S\u1ED1 kh\u00E1ch", $"{_selectedGroup.TotalGuests}"));
        panel.Children.Add(new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 12, 0, 12) });
        panel.Children.Add(new TextBlock { Text = "Ch\u1ECDn \u0111o\u00E0n xong, nh\u1EA5n Ti\u1EBFp t\u1EE5c \u0111\u1EC3 m\u1EDF danh s\u00E1ch kh\u00E1ch.", FontSize = 11, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap });
        return panel;
    }

    private void BuildA()
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(STitle("Danh sách khách"));
        // Dashboard
        var kpis = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        kpis.Children.Add(Kpi("Tổng", _guests.Count.ToString(), "#195FCE"));
        kpis.Children.Add(Kpi("Đã C/I", _guests.Count(g => g.CheckedIn).ToString(), "#16A34A"));
        kpis.Children.Add(Kpi("Chưa C/I", _guests.Count(g => !g.CheckedIn).ToString(), "#D97706"));
        kpis.Children.Add(Kpi("VIP", _guests.Count(g => g.Vip).ToString(), "#7C3AED"));
        kpis.Children.Add(Kpi("Leader", _guests.Count(g => g.Leader).ToString(), "#2563EB"));
        kpis.Children.Add(Kpi("Thiếu PP", _guests.Count(g => string.IsNullOrEmpty(g.Passport)).ToString(), "#DC2626"));
        ContentPanel.Children.Add(kpis);

        // Toolbar
        var tbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        tbar.Children.Add(ActionBtn("Nhập Rooming List CSV", () => _ = ImportRoomingListAsync()));
        tbar.Children.Add(ActionBtn("Export CSV", ExportGuestsCsv));
        tbar.Children.Add(ActionBtn("In", PrintGuests));
        ContentPanel.Children.Add(tbar);

        // Search + 2-panel
        var mainGrid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(7, GridUnitType.Star) }, new ColumnDefinition { Width = new GridLength(10) }, new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) } }, MinHeight = 400 };
        var leftCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(8) };
        var leftStack = new StackPanel();
        var sb = new TextBox { Text = _searchText, Height = 30, FontSize = 11, Margin = new Thickness(0, 0, 0, 6) }; sb.TextChanged += (_, _) => { _searchText = sb.Text; BuildA(); }; leftStack.Children.Add(sb);
        var filtered = _guests.Where(g => string.IsNullOrEmpty(_searchText) || g.Name.ToLower().Contains(_searchText.ToLower()) || g.Passport.ToLower().Contains(_searchText.ToLower())).ToList();
        var dg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = filtered, MaxHeight = 440 };
        dg.SelectionChanged += async (_, _) =>
        {
            if (dg.SelectedItem is not GmGuest g) return;
            _selectedGuest = g;
            BuildA();
            UpdateStatus();
            UpdateFooter();
            await EnrichGuestFromProfileAsync(g);
        };
        dg.Columns.Add(new DataGridTextColumn { Header = "Tên", Binding = new System.Windows.Data.Binding("Name"), Width = 130 });
        dg.Columns.Add(new DataGridTextColumn { Header = "GT", Binding = new System.Windows.Data.Binding("Gender"), Width = 45 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Quốc tịch", Binding = new System.Windows.Data.Binding("Nationality"), Width = 80 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Passport", Binding = new System.Windows.Data.Binding("Passport"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Phone", Binding = new System.Windows.Data.Binding("Phone"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("RoomNumber"), Width = 65 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Đến", Binding = new System.Windows.Data.Binding("ArrivalDisplay"), Width = 80 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Đi", Binding = new System.Windows.Data.Binding("DepartureDisplay"), Width = 80 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("Status"), Width = 110 });
        leftStack.Children.Add(dg); leftCard.Child = leftStack; Grid.SetColumn(leftCard, 0); mainGrid.Children.Add(leftCard);

        // RIGHT: Quick Summary
        var rightCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(12) };
        var rightStack = new StackPanel();
        if (_selectedGuest != null)
        {
            var g = _selectedGuest;
            rightStack.Children.Add(new TextBlock { Text = g.Name, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) });
            var badges = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
            if (g.Vip) badges.Children.Add(Badge("★ VIP", "#D97706", "#FEF3C7"));
            if (g.Leader) badges.Children.Add(Badge("Leader", "#2563EB", "#DBEAFE"));
            if (g.CheckedIn) badges.Children.Add(Badge("Đã C/I", "#16A34A", "#DCFCE7"));
            rightStack.Children.Add(badges);
            rightStack.Children.Add(Sec("Thông tin cá nhân"));
            rightStack.Children.Add(Row("Giới tính", g.Gender));
            rightStack.Children.Add(Row("Ngày sinh", g.Birthday.ToString("dd/MM/yyyy")));
            rightStack.Children.Add(Row("Quốc tịch", g.Nationality));
            rightStack.Children.Add(Row("Passport", string.IsNullOrEmpty(g.Passport) ? "Thiếu ⚠" : g.Passport));
            rightStack.Children.Add(Row("Phone", g.Phone));
            rightStack.Children.Add(Row("Email", g.Email));
            rightStack.Children.Add(Sec("Lưu trú"));
            rightStack.Children.Add(Row("Phòng", string.IsNullOrEmpty(g.RoomNumber) ? "Chưa phân" : g.RoomNumber));
            rightStack.Children.Add(Row("Hạng", g.RoomType));
            rightStack.Children.Add(Row("Đến/Đi", $"{g.Arrival:dd/MM} → {g.Departure:dd/MM}"));
            rightStack.Children.Add(Row("Meal", g.MealPlan));
            rightStack.Children.Add(Row("Trạng thái", g.Status));
            if (!string.IsNullOrEmpty(g.Allergy)) rightStack.Children.Add(new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 6, 0, 0), Background = Cb("#FEF2F2"), Child = new TextBlock { Text = $"⚠ Dị ứng: {g.Allergy}", FontSize = 11, Foreground = C("#DC2626"), FontWeight = FontWeights.SemiBold } });
        }
        else rightStack.Children.Add(new TextBlock { Text = "Chọn khách từ danh sách", FontSize = 13, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center });
        rightCard.Child = rightStack; Grid.SetColumn(rightCard, 2); mainGrid.Children.Add(rightCard);
        ContentPanel.Children.Add(mainGrid);
        ContentPanel.Children.Add(new TextBlock { Text = $"Hiển thị {filtered.Count}/{_guests.Count} khách", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 4, 0, 0) });
    }

    // ═══ B: PROFILE ═══
    private void WrapGuestSidebar()
    {
        var existing = ContentPanel.Children.Cast<UIElement>().ToList();
        ContentPanel.Children.Clear();

        var layout = new Grid
        {
            MinHeight = 500,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(7, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(12) },
                new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }
            }
        };
        layout.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding(nameof(ScrollViewer.ViewportWidth)) { Source = ContentScroll });

        var leftStack = new StackPanel();
        foreach (var element in existing) leftStack.Children.Add(element);
        var leftCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14) };
        leftCard.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = leftStack };
        Grid.SetColumn(leftCard, 0);
        layout.Children.Add(leftCard);

        var detailCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14) };
        var detail = new StackPanel();
        BuildGuestDetailCard(detail);
        detailCard.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = detail };
        Grid.SetColumn(detailCard, 2);
        layout.Children.Add(detailCard);
        ContentPanel.Children.Add(layout);
    }

    private void BuildGuestDetailCard(StackPanel detail)
    {
        if (_selectedGuest == null)
        {
            detail.Children.Add(new TextBlock { Text = "Ch\u1ECDn m\u1ED9t kh\u00E1ch \u0111\u1EC3 xem th\u00F4ng tin.", FontSize = 13, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
            return;
        }

        var g = _selectedGuest;
        detail.Children.Add(new TextBlock { Text = g.Name, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), TextWrapping = TextWrapping.Wrap });
        var badges = new WrapPanel { Margin = new Thickness(0, 7, 0, 8) };
        if (g.Vip) badges.Children.Add(Badge("\u2605 VIP", "#D97706", "#FEF3C7"));
        if (g.Leader) badges.Children.Add(Badge("Leader", "#2563EB", "#DBEAFE"));
        if (g.CheckedIn) badges.Children.Add(Badge("\u0110\u00E3 nh\u1EADn ph\u00F2ng", "#16A34A", "#DCFCE7"));
        detail.Children.Add(badges);
        detail.Children.Add(Sec("Th\u00F4ng tin c\u00E1 nh\u00E2n"));
        detail.Children.Add(Row("Gi\u1EDBi t\u00EDnh", g.Gender));
        detail.Children.Add(Row("Ng\u00E0y sinh", g.Birthday.ToString("dd/MM/yyyy")));
        detail.Children.Add(Row("Qu\u1ED1c t\u1ECBch", g.Nationality));
        detail.Children.Add(Row("Passport", string.IsNullOrEmpty(g.Passport) ? "Thi\u1EBFu" : g.Passport));
        detail.Children.Add(Row("Visa", string.IsNullOrEmpty(g.Visa) ? "Thi\u1EBFu" : g.Visa));
        detail.Children.Add(Row("Phone", g.Phone));
        detail.Children.Add(Row("Email", g.Email));
        detail.Children.Add(Sec("L\u01B0u tr\u00FA"));
        detail.Children.Add(Row("Ph\u00F2ng", string.IsNullOrEmpty(g.RoomNumber) ? "Ch\u01B0a ph\u00E2n" : g.RoomNumber));
        detail.Children.Add(Row("H\u1EA1ng", g.RoomType));
        detail.Children.Add(Row("\u0110\u1EBFn/\u0110i", $"{g.Arrival:dd/MM} \u2192 {g.Departure:dd/MM}"));
        detail.Children.Add(Row("Meal", g.MealPlan));
        detail.Children.Add(Row("Tr\u1EA1ng th\u00E1i", g.Status));
        if (!string.IsNullOrEmpty(g.Allergy))
            detail.Children.Add(new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 8, 0, 0), Background = Cb("#FEF2F2"), Child = new TextBlock { Text = $"\u26A0 D\u1ECB \u1EE9ng: {g.Allergy}", FontSize = 11, Foreground = C("#DC2626"), FontWeight = FontWeights.SemiBold, TextWrapping = TextWrapping.Wrap } });
    }

    private void BuildB()
    {
        if (_selectedGuest == null) { ContentPanel.Children.Add(STitle("Chọn khách từ Bước A trước")); return; }
        ContentPanel.Children.Add(STitle($"Hồ sơ: {_selectedGuest.Name}"));
        ContentPanel.Children.Add(new Border
        {
            Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7),
            Margin = new Thickness(0, 0, 0, 10), BorderBrush = Cb("#BFDBFE"), BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = "Có thể lưu thật: giới tính, ngày sinh, quốc tịch, phone, email và địa chỉ. Họ tên/công ty, thông tin y tế và phân loại đoàn chỉ hiển thị vì chưa có command cập nhật tương ứng.",
                FontSize = 11, Foreground = C("#1E40AF"), TextWrapping = TextWrapping.Wrap
            }
        });
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(12) }, new ColumnDefinition() } };
        // LEFT COLUMN
        var left = new StackPanel();
        left.Children.Add(SecTitle("Thông tin cá nhân"));
        left.Children.Add(ReadOnlyField("Họ tên", _gmFirstName));
        left.Children.Add(Grid2(Field("Giới tính", _gmGender, v => _gmGender = v), Field("Ngày sinh", _gmBirthday?.ToString("dd/MM/yyyy") ?? "", v => { if (DateTime.TryParse(v, out var d)) _gmBirthday = d; })));
        left.Children.Add(Field("Quốc tịch", _gmNationality, v => _gmNationality = v));
        left.Children.Add(ReadOnlyField("Ngôn ngữ", _gmLanguage));
        left.Children.Add(SecTitle("Thông tin liên hệ"));
        left.Children.Add(Field("Phone", _gmPhone, v => _gmPhone = v));
        left.Children.Add(Field("Email", _gmEmail, v => _gmEmail = v));
        left.Children.Add(Field("Địa chỉ", _gmAddress, v => _gmAddress = v));
        left.Children.Add(Grid2(Field("Thành phố", _gmCity, v => _gmCity = v), Field("Quốc gia", _gmCountry, v => _gmCountry = v)));
        Grid.SetColumn(left, 0); grid.Children.Add(left);

        // RIGHT COLUMN
        var right = new StackPanel();
        right.Children.Add(SecTitle("Thông tin doanh nghiệp"));
        right.Children.Add(ReadOnlyField("Công ty", _gmCompany));
        right.Children.Add(Grid2(ReadOnlyField("Phòng ban", _gmDepartment), ReadOnlyField("Chức vụ", _gmPosition)));
        right.Children.Add(SecTitle("Thông tin y tế"));
        right.Children.Add(ReadOnlyField("Dị ứng", _gmAllergy));
        right.Children.Add(ReadOnlyField("Y tế", _gmMedical));
        right.Children.Add(ReadOnlyField("Chế độ ăn", _gmDiet));
        right.Children.Add(ReadOnlyField("Nhu cầu ĐB", _gmSpecialNeed));
        right.Children.Add(new WrapPanel { Margin = new Thickness(0, 6, 0, 0), Children = { ReadOnlyCheck("Xe lăn", _gmWheelchair), ReadOnlyCheck("Extra Bed", _gmExtraBed), ReadOnlyCheck("Baby Cot", _gmBabyCot) } });
        right.Children.Add(SecTitle("Phân loại đoàn"));
        right.Children.Add(new WrapPanel { Children = { ReadOnlyCheck("Leader", _selectedGuest.Leader), ReadOnlyCheck("VIP", _selectedGuest.Vip) } });
        right.Children.Add(ActionBtn("Lưu hồ sơ khách", () => _ = SaveGuestProfileAsync()));
        Grid.SetColumn(right, 2); grid.Children.Add(right);
        ContentPanel.Children.Add(grid);
        WrapGuestSidebar();
    }

    // ═══ C: DOCUMENTS ═══
    private void BuildC()
    {
        if (_selectedGuest == null) return;
        ContentPanel.Children.Add(STitle($"Giấy tờ: {_selectedGuest.Name}"));
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(12) }, new ColumnDefinition() } };
        var left = new StackPanel();
        left.Children.Add(SecTitle("Hộ chiếu (Passport)"));
        left.Children.Add(Field("Số hộ chiếu", _gmPassport, v => _gmPassport = v));
        left.Children.Add(Grid2(Field("Ngày cấp", _gmPassportIssue, v => _gmPassportIssue = v), Field("Ngày hết hạn", _gmPassportExpiry, v => _gmPassportExpiry = v)));
        left.Children.Add(Field("Nơi cấp", _gmPassportPlace, v => _gmPassportPlace = v));
        left.Children.Add(SecTitle("Visa"));
        left.Children.Add(Field("Số Visa", _gmVisaNo, v => _gmVisaNo = v));
        left.Children.Add(Grid2(Field("Loại Visa", _gmVisaType, v => _gmVisaType = v), Field("Ngày cấp", _gmVisaIssue, v => _gmVisaIssue = v)));
        left.Children.Add(Field("Ngày hết hạn", _gmVisaExpiry, v => _gmVisaExpiry = v));
        Grid.SetColumn(left, 0); grid.Children.Add(left);

        var right = new StackPanel();
        right.Children.Add(SecTitle("Giấy tờ khác"));
        right.Children.Add(Field("CMND/CCCD", _gmIdCard, v => _gmIdCard = v));
        right.Children.Add(Field("GPLX", _gmDriverLicense, v => _gmDriverLicense = v));
        right.Children.Add(Sep());
        // Validation
        right.Children.Add(SecTitle("Kiểm tra giấy tờ"));
        var passOk = !string.IsNullOrEmpty(_gmPassport); var visaOk = !string.IsNullOrEmpty(_gmVisaNo);
        right.Children.Add(ValRow("Passport", passOk)); right.Children.Add(ValRow("Visa", visaOk));
        right.Children.Add(ValRow("CMND/CCCD", !string.IsNullOrEmpty(_gmIdCard)));
        var upWp = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        right.Children.Add(upWp);
         upWp.Children.Add(ActionBtn("📎 Upload Scan", () => _ = UploadGuestAttachmentAsync()));
         upWp.Children.Add(ActionBtn("👁 Xem trước", OpenLatestGuestAttachment));
         right.Children.Add(new TextBlock { Text = _guestAttachments.Count == 0 ? "Chưa có file scan thật." : $"Đã lưu {_guestAttachments.Count} file scan thật.", FontSize = 11, Foreground = C(_guestAttachments.Count == 0 ? "#94A3B8" : "#16A34A"), Margin = new Thickness(0, 6, 0, 0) });
        Grid.SetColumn(right, 2); grid.Children.Add(right);
        ContentPanel.Children.Add(grid);
        WrapGuestSidebar();
    }

    // ═══ D: STAY ═══
    private void BuildD()
    {
        if (_selectedGuest == null) return;
        ContentPanel.Children.Add(STitle($"Lưu trú: {_selectedGuest.Name}"));
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(12) }, new ColumnDefinition() } };
        var left = new StackPanel();
        left.Children.Add(SecTitle("Thông tin lưu trú"));
        left.Children.Add(Grid2(Field("Phòng", _gmRoomNumber, v => _gmRoomNumber = v), Field("Hạng phòng", _gmRoomType, v => _gmRoomType = v)));
        left.Children.Add(Grid2(Field("Rate Plan", _gmRatePlan, v => _gmRatePlan = v), Field("Meal Plan", _gmMealPlan, v => _gmMealPlan = v)));
        left.Children.Add(Field("Loại giường", _gmBedType, v => _gmBedType = v));
        left.Children.Add(Field("Ở cùng", _gmRoommate, v => _gmRoommate = v));
        left.Children.Add(SecTitle("Sở thích phòng"));
        left.Children.Add(new WrapPanel { Children = { Chk("Tầng cao", _gmHighFloor, v => _gmHighFloor = v), Chk("Yên tĩnh", _gmQuiet, v => _gmQuiet = v), Chk("Gần thang máy", _gmNearLift, v => _gmNearLift = v), Chk("Late C/O", _gmLateCO, v => _gmLateCO = v), Chk("Early C/I", _gmEarlyCI, v => _gmEarlyCI = v) } });
        Grid.SetColumn(left, 0); grid.Children.Add(left);

        var right = new StackPanel();
        right.Children.Add(SecTitle("Đưa đón sân bay"));
        right.Children.Add(new WrapPanel { Children = { Chk("Đón sân bay", _gmPickup, v => _gmPickup = v), Chk("Tiễn sân bay", _gmDropoff, v => _gmDropoff = v) } });
        right.Children.Add(Field("Số hiệu bay", _gmFlight, v => _gmFlight = v));
        right.Children.Add(Grid2(Field("ETA", _gmEta, v => _gmEta = v), Field("ETD", _gmEtd, v => _gmEtd = v)));
        right.Children.Add(Sep());
        right.Children.Add(SecTitle("Tóm tắt lưu trú"));
        right.Children.Add(Row("Đến", _selectedGuest.Arrival.ToString("dd/MM/yyyy")));
        right.Children.Add(Row("Đi", _selectedGuest.Departure.ToString("dd/MM/yyyy")));
        right.Children.Add(Row("Số đêm", $"{(int)(_selectedGuest.Departure - _selectedGuest.Arrival).TotalDays}"));
        right.Children.Add(Row("Phòng", string.IsNullOrEmpty(_gmRoomNumber) ? "Chưa phân" : _gmRoomNumber));
        right.Children.Add(Row("Meal", _gmMealPlan));
        Grid.SetColumn(right, 2); grid.Children.Add(right);
        ContentPanel.Children.Add(grid);
        WrapGuestSidebar();
    }

    // ═══ E: HISTORY ═══
    private void BuildE()
    {
        if (_selectedGuest == null) return;
        ContentPanel.Children.Add(STitle($"Lịch sử: {_selectedGuest.Name}"));
        var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(12) }, new ColumnDefinition() } };
        var left = new StackPanel();
        left.Children.Add(SecTitle("Timeline"));
        if (_selectedGuest.Timeline.Count == 0)
            left.Children.Add(new TextBlock { Text = "Chưa có sự kiện lịch sử cho khách này.", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 4, 0, 0) });
        foreach (var te in _selectedGuest.Timeline)
        {
            left.Children.Add(new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Background = Cb("#EFF6FF"), Child = new StackPanel { Children = { new TextBlock { Text = $"{te.Date:dd/MM/yyyy HH:mm} — {te.Event}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, new TextBlock { Text = $"{te.User}: {te.Detail}", FontSize = 11, Foreground = C("#64748B") } } } });
        }
        Grid.SetColumn(left, 0); grid.Children.Add(left);

        var right = new StackPanel();
        right.Children.Add(SecTitle("Audit Log"));
        right.Children.Add(new TextBlock { Text = "Ghi nhận thay đổi từ Guest History thật", FontSize = 12, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 8) });
        if (_selectedGuest.Timeline.Count == 0)
            right.Children.Add(new TextBlock { Text = "Chưa có audit event.", FontSize = 11, Foreground = C("#94A3B8") });
        foreach (var te in _selectedGuest.Timeline.Take(12))
            right.Children.Add(new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 2, 0, 2), Background = Cb("#F8FAFC"), Child = new StackPanel { Children = { new TextBlock { Text = $"{te.Date:dd/MM/yyyy HH:mm} · {te.Event}", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }, new TextBlock { Text = $"{te.User}: {te.Detail}", FontSize = 10, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap } } } });
        Grid.SetColumn(right, 2); grid.Children.Add(right);
        ContentPanel.Children.Add(grid);
        WrapGuestSidebar();
    }

    // ═══ F: CONFIRM ═══
    private void BuildF()
    {
        if (_selectedGuest == null) return;
        ContentPanel.Children.Add(STitle($"Xác nhận: {_selectedGuest.Name}"));
        ContentPanel.Children.Add(new Border { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Cb("#BFDBFE"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = "Dữ liệu đoàn/phòng/khách đọc từ hệ thống thật. Khi xác nhận, các trường hồ sơ được hỗ trợ và số giấy tờ mới sẽ được lưu thật; file scan và các trường chưa có command sẽ không ghi giả.", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#1E40AF"), TextWrapping = TextWrapping.Wrap } });
        var s = new StackPanel();
        s.Children.Add(Prev("Họ tên", _selectedGuest.Name));
        s.Children.Add(Prev("Quốc tịch", _gmNationality));
        s.Children.Add(Prev("Passport", _gmPassport));
        s.Children.Add(Prev("Phone", _gmPhone));
        s.Children.Add(Prev("Phòng", string.IsNullOrEmpty(_gmRoomNumber) ? "Chưa phân" : _gmRoomNumber));
        s.Children.Add(Prev("Lưu trú", $"{_selectedGuest.Arrival:dd/MM} → {_selectedGuest.Departure:dd/MM}"));
        s.Children.Add(Prev("VIP/Leader", $"{( _selectedGuest.Vip ? "★ VIP " : "")}{( _selectedGuest.Leader ? "Leader" : "")}"));
        s.Children.Add(Prev("Dị ứng", string.IsNullOrEmpty(_gmAllergy) ? "Không" : _gmAllergy));
        ContentPanel.Children.Add(s);
        ContentPanel.Children.Add(Sep());
        ContentPanel.Children.Add(MakeCfm("Tôi đã kiểm tra hồ sơ cá nhân", _confirmProfile, v => { _confirmProfile = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfm("Tôi đã kiểm tra giấy tờ", _confirmDocs, v => { _confirmDocs = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfm("Tôi đã kiểm tra thông tin lưu trú", _confirmStay, v => { _confirmStay = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfm("Tôi xác nhận thông tin đúng", _confirmAll, v => { _confirmAll = v; UpdateFooter(); }));
        WrapGuestSidebar();
    }

    private async void OnConfirm(object s, RoutedEventArgs e) => await SaveGuestProfileAsync(showMessage: true);

    private async Task SaveGuestProfileAsync(bool showMessage = false)
    {
        if (_selectedGuest is null || _guestRepository is null || _executionContext?.Current is not { } context)
        {
            ShowUnavailable("Lưu hồ sơ khách", "Chưa có khách, repository hoặc property context dữ liệu thật.");
            return;
        }

        if (_selectedGuest.GuestGuid == Guid.Empty || context.Property.PropertyId.Value == Guid.Empty)
        {
            ShowUnavailable("Lưu hồ sơ khách", "Khách đang chọn chưa có GuestId thật để cập nhật.");
            return;
        }

        try
        {
            var profile = await _guestRepository.GetAsync(context.Property.PropertyId.Value, _selectedGuest.GuestGuid, CancellationToken.None);
            if (profile is null)
            {
                ShowUnavailable("Lưu hồ sơ khách", "Không tìm thấy hồ sơ khách thật tương ứng.");
                return;
            }

            var now = DateTime.UtcNow;
            profile.UpdateProfile(
                KeepWhenBlank(_gmPhone, profile.Phone),
                KeepWhenBlank(_gmEmail, profile.Email),
                KeepWhenBlank(_gmAddress, profile.Address),
                KeepWhenBlank(_gmNationality, profile.Nationality),
                KeepWhenBlank(_gmGender, profile.Gender),
                _gmBirthday.HasValue ? DateOnly.FromDateTime(_gmBirthday.Value) : profile.DateOfBirth,
                now,
                context.Actor.ActorId);

            AddDocumentIfNew(profile, GuestDocumentType.Passport, _gmPassport, _gmCountry, ParseDate(_gmPassportExpiry), now);
            AddDocumentIfNew(profile, GuestDocumentType.Visa, _gmVisaNo, _gmCountry, ParseDate(_gmVisaExpiry), now);
            AddDocumentIfNew(profile, GuestDocumentType.NationalId, _gmIdCard, _gmCountry, null, now);
            await _guestRepository.UpdateAsync(profile, CancellationToken.None);

            var dto = await _guestRepository.GetDtoAsync(context.Property.PropertyId.Value, profile.GuestProfileId, CancellationToken.None);
            if (dto is not null)
            {
                _selectedGuest.Name = dto.FullName;
                _selectedGuest.Phone = dto.Phone ?? string.Empty;
                _selectedGuest.Email = dto.Email ?? string.Empty;
                _selectedGuest.Gender = dto.Gender ?? profile.Gender ?? "—";
                _selectedGuest.Birthday = dto.DateOfBirth?.ToDateTime(TimeOnly.MinValue)
                    ?? profile.DateOfBirth?.ToDateTime(TimeOnly.MinValue)
                    ?? _selectedGuest.Birthday;
                _selectedGuest.Nationality = dto.Nationality ?? "—";
                _selectedGuest.Company = dto.CompanyName ?? string.Empty;
                _selectedGuest.Vip = dto.VipLevel != VipLevel.Normal;
                _selectedGuest.Passport = profile.Documents.FirstOrDefault(document => document.DocumentType == GuestDocumentType.Passport)?.DocumentNumber ?? _selectedGuest.Passport;
                _selectedGuest.Visa = profile.Documents.FirstOrDefault(document => document.DocumentType == GuestDocumentType.Visa)?.DocumentNumber ?? _selectedGuest.Visa;
                LoadGuestToForm(_selectedGuest);
            }

            _hasChanges = false;
            BuildContent(_step);
            UpdateFooter();
            UpdateStatus();
            FooterLabel.Text = "Đã lưu hồ sơ khách thật; các trường không có command vẫn giữ nguyên.";
            if (showMessage)
            {
                MessageBox.Show(this, "Đã lưu các trường hồ sơ được hỗ trợ và số giấy tờ mới vào dữ liệu thật.", "Khách trong đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không thể lưu hồ sơ khách thật: " + ex.Message, "Khách trong đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task UploadGuestAttachmentAsync()
    {
        if (_selectedGuest is null || _selectedGuest.GuestGuid == Guid.Empty || _businessDocumentService is null)
        {
            ShowUnavailable("Upload scan", "Khách chưa có GuestId thật hoặc dịch vụ lưu tài liệu chưa được đăng ký.");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Tài liệu và ảnh (*.pdf;*.png;*.jpg;*.jpeg;*.doc;*.docx)|*.pdf;*.png;*.jpg;*.jpeg;*.doc;*.docx|Tất cả tệp (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var actor = _executionContext?.Current?.Actor.ActorId?.Value.ToString() ?? "system";
            await _businessDocumentService.UploadAttachmentAsync(
                BusinessEntityType.Customer,
                _selectedGuest.GuestGuid,
                $"Scan giấy tờ - {_selectedGuest.Name}",
                dialog.FileName,
                actor,
                CancellationToken.None);
            _guestAttachments = await _businessDocumentService.GetDocumentsByEntityAsync(BusinessEntityType.Customer, _selectedGuest.GuestGuid);
            FooterLabel.Text = $"Đã lưu file scan thật cho {_selectedGuest.Name}.";
            BuildContent(_step);
            UpdateFooter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không thể lưu file scan thật: " + ex.Message, "Upload scan", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenLatestGuestAttachment()
    {
        var document = _guestAttachments.FirstOrDefault();
        if (document is null || string.IsNullOrWhiteSpace(document.PhysicalFilePath) || !File.Exists(document.PhysicalFilePath))
        {
            ShowUnavailable("Xem trước scan", "Khách này chưa có file scan thật để mở.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(document.PhysicalFilePath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không thể mở file scan: " + ex.Message, "Xem trước scan", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string? KeepWhenBlank(string input, string? current) => string.IsNullOrWhiteSpace(input) ? current : input;

    private static DateOnly? ParseDate(string value) => DateTime.TryParse(value, out var date) ? DateOnly.FromDateTime(date) : null;

    private static void AddDocumentIfNew(GuestProfile profile, GuestDocumentType type, string number, string? country, DateOnly? expiry, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(number) || profile.Documents.Any(document => document.DocumentType == type && string.Equals(document.DocumentNumber, number.Trim(), StringComparison.OrdinalIgnoreCase))) return;
        profile.AddDocument(type, number.Trim(), country, expiry, now);
    }

    private async Task ImportRoomingListAsync()
    {
        if (_selectedGroup is null || _roomingListService is null)
        {
            ShowUnavailable("Nhập Rooming List", "Chưa chọn đoàn hoặc chưa cấu hình dịch vụ Rooming List thật.");
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Rooming List CSV (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            Multiselect = false,
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            var current = await _roomingListService.ExportAsync(_selectedGroup.GroupGuid, CancellationToken.None);
            var currentByAssignment = current.ToDictionary(item => item.AssignmentId);
            var rows = ParseCsv(await File.ReadAllTextAsync(dialog.FileName, CancellationToken.None));
            if (rows.Count < 2)
                throw new InvalidOperationException("Tệp CSV phải có dòng tiêu đề và ít nhất một dòng dữ liệu.");

            var header = rows[0]
                .Select((value, index) => new { Key = value.Trim(), Index = index })
                .Where(item => !string.IsNullOrWhiteSpace(item.Key))
                .ToDictionary(item => item.Key, item => item.Index, StringComparer.OrdinalIgnoreCase);
            var requiredHeaders = new[] { "AssignmentId", "RoomNumber", "RoomType", "GuestId", "GuestName", "Adults", "Children", "VIP" };
            var missing = requiredHeaders.Where(name => !header.ContainsKey(name)).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Thiếu cột bắt buộc: " + string.Join(", ", missing));

            var items = new List<RoomingListItemDto>();
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row.All(string.IsNullOrWhiteSpace))
                    continue;

                var assignmentText = CsvValue(row, header["AssignmentId"]);
                if (!Guid.TryParse(assignmentText, out var assignmentId) || !currentByAssignment.TryGetValue(assignmentId, out var existing))
                    throw new InvalidOperationException($"Dòng {rowIndex + 1}: AssignmentId không tồn tại trong Rooming List của đoàn.");

                var roomNumber = CsvValue(row, header["RoomNumber"]);
                var roomType = CsvValue(row, header["RoomType"]);
                if (!string.Equals(roomNumber, existing.RoomNumber, StringComparison.Ordinal)
                    || !string.Equals(roomType, existing.RoomType, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Dòng {rowIndex + 1}: không được đổi phòng/hạng phòng khi nhập; hãy dùng phân phòng.");

                var guestIdText = CsvValue(row, header["GuestId"]);
                Guid? guestId = string.IsNullOrWhiteSpace(guestIdText) ? null : Guid.Parse(guestIdText);
                if (!int.TryParse(CsvValue(row, header["Adults"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var adults)
                    || !int.TryParse(CsvValue(row, header["Children"]), NumberStyles.Integer, CultureInfo.InvariantCulture, out var children))
                    throw new InvalidOperationException($"Dòng {rowIndex + 1}: Adults/Children phải là số nguyên.");

                items.Add(existing with
                {
                    GuestId = guestId,
                    GuestName = NullIfEmpty(CsvValue(row, header["GuestName"])),
                    Adults = adults,
                    Children = children,
                    IsVip = ParseBoolean(CsvValue(row, header["VIP"]))
                });
            }

            if (items.Count == 0)
                throw new InvalidOperationException("Tệp không có dòng Rooming List hợp lệ.");

            var result = await _roomingListService.ImportAsync(_selectedGroup.GroupGuid, items, CancellationToken.None);
            if (result.Errors > 0)
            {
                MessageBox.Show(this,
                    $"Không lưu Rooming List. Có lỗi: {result.Errors}/{result.TotalRows}." + Environment.NewLine
                    + string.Join(Environment.NewLine, result.ErrorDetails.Take(8)),
                    "Nhập Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_groupQuery is not null && _executionContext?.Current is { } context)
            {
                var summaries = await _groupQuery.GetGroupsAsync(context.Property.PropertyId.Value, CancellationToken.None);
                var refreshed = summaries.FirstOrDefault(summary => summary.GroupReservationId == _selectedGroup.GroupGuid);
                if (refreshed is not null)
                    _groupSummaries[_selectedGroup.GroupGuid] = refreshed;
            }

            SelectGroup(_selectedGroup);
            FooterLabel.Text = $"Đã nhập và lưu {result.Imported:N0} dòng Rooming List vào dữ liệu thật.";
            MessageBox.Show(this, $"Đã nhập và lưu {result.Imported:N0} dòng Rooming List vào dữ liệu thật.", "Nhập Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không thể nhập Rooming List: " + ex.Message, "Nhập Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static List<List<string>> ParseCsv(string text)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < text.Length && text[i + 1] == '"')
                {
                    value.Append('"');
                    i++;
                }
                else
                    quoted = !quoted;
            }
            else if (ch == ',' && !quoted)
            {
                row.Add(value.ToString());
                value.Clear();
            }
            else if ((ch == '\r' || ch == '\n') && !quoted)
            {
                if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    i++;
                row.Add(value.ToString());
                rows.Add(row);
                row = new List<string>();
                value.Clear();
            }
            else
                value.Append(ch);
        }
        if (value.Length > 0 || row.Count > 0)
        {
            row.Add(value.ToString());
            rows.Add(row);
        }
        return rows;
    }

    private static string CsvValue(IReadOnlyList<string> row, int index) => index < row.Count ? row[index].Trim() : string.Empty;
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static bool ParseBoolean(string value) => value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
        || value.Trim() is "1" or "yes" or "y" or "có" or "co";

    private void ExportGuestsCsv()
    {
        if (_selectedGroup == null || _guests.Count == 0)
        {
            ShowUnavailable("Export CSV", "Chưa có đoàn hoặc danh sách khách thật để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "CSV UTF-8 (*.csv)|*.csv",
            FileName = $"{_selectedGroup.Code}_guests.csv",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        static string Csv(string value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";
        var csv = new StringBuilder();
        csv.AppendLine("GuestId,HoTen,QuocTich,Passport,Phone,Email,Phong,HangPhong,NgayDen,NgayDi,TrangThai,VIP,Leader");
        foreach (var guest in _guests)
        {
            csv.AppendLine(string.Join(",", new[]
            {
                Csv(guest.GuestGuid == Guid.Empty ? guest.Id.ToString() : guest.GuestGuid.ToString()),
                Csv(guest.Name), Csv(guest.Nationality), Csv(guest.Passport), Csv(guest.Phone), Csv(guest.Email),
                Csv(guest.RoomNumber), Csv(guest.RoomType), Csv(guest.Arrival.ToString("yyyy-MM-dd")),
                Csv(guest.Departure.ToString("yyyy-MM-dd")), Csv(guest.Status), Csv(guest.Vip ? "1" : "0"), Csv(guest.Leader ? "1" : "0")
            }));
        }

        try
        {
            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            FooterLabel.Text = $"Đã xuất {_guests.Count} khách thật ra CSV.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Không thể xuất CSV: " + ex.Message, "Export CSV", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrintGuests()
    {
        if (_selectedGroup == null || _guests.Count == 0)
        {
            ShowUnavailable("In danh sách", "Chưa có đoàn hoặc danh sách khách thật để in.");
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;
        dialog.PrintVisual(ContentPanel, $"Danh sách khách đoàn {_selectedGroup.Code}");
    }

    private void ShowUnavailable(string title, string message) => MessageBox.Show(this, message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    private bool CanConfirm() => _confirmProfile && _confirmDocs && _confirmStay && _confirmAll;
    private List<string> GetWarnings()
    {
        var w = new List<string>();
        if (_selectedGuest != null && string.IsNullOrEmpty(_gmPassport)) w.Add("Thiếu Passport");
        if (_selectedGuest != null && string.IsNullOrEmpty(_gmPhone)) w.Add("Thiếu SĐT");
        return w;
    }

    // ═══ HELPERS ═══
    private FrameworkElement Field(string label, string value, Action<string> setter) { var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) }; sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") }); var tb = new TextBox { Text = value, Style = (Style)FindResource("ModTextBox") }; tb.TextChanged += (_, _) => setter(tb.Text); sp.Children.Add(tb); return sp; }
    private FrameworkElement ReadOnlyField(string label, string value) { var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 6) }; sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") }); sp.Children.Add(new TextBox { Text = value, Style = (Style)FindResource("ModTextBox"), IsReadOnly = true, Background = Cb("#F8FAFC") }); return sp; }
    private static FrameworkElement Grid2(FrameworkElement a, FrameworkElement b) => new Grid { Margin = new Thickness(0, 0, 0, 6), ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(8) }, new ColumnDefinition() }, Children = { a, b } };
    private static FrameworkElement Chk(string label, bool value, Action<bool> setter) { var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), Margin = new Thickness(0, 0, 12, 4), VerticalAlignment = VerticalAlignment.Center }; cb.Checked += (_, _) => setter(true); cb.Unchecked += (_, _) => setter(false); return cb; }
    private static FrameworkElement ReadOnlyCheck(string label, bool value) => new CheckBox { Content = label, IsChecked = value, IsEnabled = false, FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#64748B"), Margin = new Thickness(0, 0, 12, 4), VerticalAlignment = VerticalAlignment.Center };
    private static FrameworkElement SecTitle(string t) => new TextBlock { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#334155"), Margin = new Thickness(0, 12, 0, 6) };
    private static FrameworkElement Sec(string t) => new TextBlock { Text = t, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(0, 8, 0, 4) };
    private static FrameworkElement Row(string l, string v) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), Width = 85 }, new TextBlock { Text = v, FontSize = 11, Foreground = C("#334155") } } };
    private static FrameworkElement ValRow(string l, bool ok) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "✗ ", FontSize = 12, Foreground = ok ? C("#16A34A") : C("#DC2626"), FontWeight = FontWeights.Bold }, new TextBlock { Text = l, FontSize = 11, Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center } } };
    private static FrameworkElement Badge(string t, string fg, string bg) => new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(6, 3, 6, 3), Margin = new Thickness(0, 0, 6, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bg)), Child = new TextBlock { Text = t, FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fg)), FontWeight = FontWeights.SemiBold } };
    private Button ActionBtn(string text, Action action) { var btn = new Button { Content = text, Height = 30, FontSize = 11, Margin = new Thickness(0, 0, 6, 4), Padding = new Thickness(8, 0, 8, 0), Background = Cb("#EFF6FF"), Foreground = C("#1E40AF"), BorderBrush = Cb("#BFDBFE"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand }; btn.Click += (_, _) => action(); return btn; }
    private static Border Kpi(string l, string v, string c) => new() { Width = 110, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}14")), CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 6, 6), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}33")), BorderThickness = new Thickness(1), Child = new StackPanel { Children = { new TextBlock { Text = l, FontSize = 10, Foreground = C("#64748B") }, new TextBlock { Text = v, FontSize = 18, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c)) } } } };
    private static FrameworkElement MakeCfm(string label, bool value, Action<bool> setter) { var border = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0), Background = Cb("#FAFBFD"), BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1) }; var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalContentAlignment = VerticalAlignment.Center }; cb.Checked += (_, _) => { setter(true); border.Background = Cb("#F0FDF4"); border.BorderBrush = Cb("#86EFAC"); }; cb.Unchecked += (_, _) => { setter(false); border.Background = Cb("#FAFBFD"); border.BorderBrush = Cb("#E2E8F0"); }; border.Child = cb; return border; }
    private static FrameworkElement STitle(string t) => new TextBlock { Text = t, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 4) };
    private static FrameworkElement Prev(string l, string v) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 12, Foreground = C("#94A3B8"), Width = 120 }, new TextBlock { Text = v, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") } } };
    private static FrameworkElement Sep() => new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══ DATA ═══
public class GmGuest { public int Id; public Guid GuestGuid { get; set; } public string Name { get; set; } = ""; public string Gender { get; set; } = ""; public DateTime Birthday; public string Nationality { get; set; } = ""; public string Passport { get; set; } = ""; public string Visa { get; set; } = ""; public string Phone { get; set; } = ""; public string Email { get; set; } = ""; public string Company { get; set; } = ""; public string RoomNumber { get; set; } = ""; public string RoomType { get; set; } = ""; public DateTime Arrival, Departure; public bool Vip, Leader, CheckedIn; public string Status { get; set; } = ""; public string MealPlan { get; set; } = "BB"; public string Allergy { get; set; } = ""; public ObservableCollection<GmTimelineEntry> Timeline { get; set; } = new(); public string ArrivalDisplay => Arrival.ToString("dd/MM"); public string DepartureDisplay => Departure.ToString("dd/MM"); }
public class GmGroup
{
    public int Id { get; set; }
    public Guid GroupGuid { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Company { get; set; } = "";
    public DateTime Arrival { get; set; }
    public DateTime Departure { get; set; }
    public int TotalGuests { get; set; }
    public string Status { get; set; } = "";
}

public class GmTimelineEntry { public DateTime Date; public string Event { get; set; } = ""; public string User { get; set; } = ""; public string Detail { get; set; } = ""; }
