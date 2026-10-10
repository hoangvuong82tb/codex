using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.App.Controls;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Guest;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public partial class GroupRoomingListWindow : Window
{
    private int _step; private bool _hasChanges;

    // Step A
    private RlGroup? _selectedGroup;
    private readonly List<RlGroup> _groups = new();
    private readonly TablePagerViewModel<RlGroup> _groupPager = new() { ItemName = "đoàn" };
    private bool _buildingGroupPage;
    private string _groupSearch = "", _groupFilterStatus = "";
    private string? _groupLoadError;

    // Step B — Guest list
    private readonly ObservableCollection<RlGuest> _guests = new();
    private RlGuest? _selectedGuest;
    private string _guestSearch = "";

    // Step C — Room pairing
    private readonly ObservableCollection<RlRoomPair> _roomPairs = new();
    private RlRoomPair? _selectedRoomPair;

    // Step D — Guest profile
    // Step E/F
    private bool _confirmGuests, _confirmDocs, _confirmRooms, _confirmAll;

    // UI
    private readonly Border[] _stepBoxes = new Border[6];
    private readonly TextBlock[] _stepLbls = new TextBlock[6];
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F" };
    private static readonly string[] StepTitles = { "Chọn đoàn", "Danh sách khách", "Ghép phòng", "Hồ sơ khách", "Kiểm tra", "Xác nhận" };
    private readonly IGroupReservationQueryService? _groupQuery;
    private readonly IRoomingListService? _roomingListService;
    private readonly IGroupRoomAssignmentCommandService? _groupRoomAssignmentCommands;
    private readonly IGuestProfileRepository? _guestProfileRepository;
    private readonly HotelRoomMasterDataQueryHandlers? _hotelQueries;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private IServiceScope? _dataScope;
    private bool _persistedMode;

    public GroupRoomingListWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _dataScope = services?.CreateScope();
        var scopedServices = _dataScope?.ServiceProvider;
        _groupQuery = scopedServices?.GetService<IGroupReservationQueryService>();
        _roomingListService = scopedServices?.GetService<IRoomingListService>();
        _groupRoomAssignmentCommands = scopedServices?.GetService<IGroupRoomAssignmentCommandService>();
        _guestProfileRepository = scopedServices?.GetService<IGuestProfileRepository>();
        _hotelQueries = scopedServices?.GetService<HotelRoomMasterDataQueryHandlers>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _persistedMode = _groupQuery is not null && _roomingListService is not null && _contextAccessor is not null;
        // Do not populate fallback/demo groups. If the real dashboard service is
        // unavailable or returns no data, the production screen must stay empty.
        _groupPager.PropertyChanged += (_, e) =>
        {
            if (!_buildingGroupPage && (e.PropertyName == nameof(TablePagerViewModel<RlGroup>.CurrentPage) || e.PropertyName == nameof(TablePagerViewModel<RlGroup>.SelectedPageSize)))
                BuildA();
        };
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
        if (!_persistedMode || _contextAccessor?.Current is not { } context)
        {
            _groupLoadError = "Không thể tải danh sách đoàn thật vì dịch vụ hoặc property context chưa sẵn sàng.";
            BuildA();
            return;
        }

        try
        {
            // The dashboard exposes a room-block projection and can omit groups
            // that have no confirmed rooms yet. Rooming List must start from the
            // persisted group reservation query so draft/tentative groups remain
            // selectable and no synthetic block row is displayed.
            _groupLoadError = null;
            var groups = await _groupQuery!.GetGroupsAsync(
                context.Property.PropertyId.Value,
                CancellationToken.None);
            _groups.Clear();
            foreach (var group in groups)
            {
                _groups.Add(new RlGroup
                {
                    Id = group.GroupReservationId,
                    Code = group.GroupCode,
                    Name = group.GroupName,
                    Company = group.CompanyName ?? group.AgencyName,
                    Arrival = group.ArrivalDate.ToDateTime(TimeOnly.MinValue),
                    Departure = group.DepartureDate.ToDateTime(TimeOnly.MinValue),
                    TotalGuests = group.TotalAdults + group.TotalChildren,
                    TotalRooms = group.ConfirmedRooms,
                    Status = group.Status.ToString()
                });
            }
            _groupPager.CurrentPage = 1;
            BuildA();
        }
        catch (Exception ex)
        {
            _groups.Clear();
            _groupLoadError = "Không thể tải danh sách đoàn thật: " + ex.Message;
            BuildA();
        }
    }

    private void GenerateGuests()
    {
        if (_selectedGroup == null) return;
        if (_persistedMode)
        {
            _ = LoadPersistedRoomingListAsync(_selectedGroup);
            return;
        }
        _guests.Clear();
        _roomPairs.Clear();
    }

    private async Task LoadPersistedRoomingListAsync(RlGroup group)
    {
        try
        {
            var items = await _roomingListService!.GetRoomingListAsync(group.Id, CancellationToken.None);
            var guestProfiles = await LoadGuestProfilesAsync(items);
            var roomCapacities = await LoadRoomCapacitiesAsync(items);
            _guests.Clear();
            _roomPairs.Clear();
            var roomGroups = items.GroupBy(x => x.HotelRoomId);
            var guestIndex = 1;
            foreach (var item in items)
            {
                _guests.Add(new RlGuest
                {
                    Id = guestIndex++,
                    AssignmentId = item.AssignmentId,
                    GuestId = item.GuestId,
                    Name = item.GuestName ?? (item.GuestId is Guid guestId && guestProfiles.TryGetValue(guestId, out var profile)
                        ? profile.FullName
                        : "Chưa gán khách"),
                    Gender = item.GuestId is Guid genderGuestId && guestProfiles.TryGetValue(genderGuestId, out var genderProfile)
                        ? genderProfile.Gender ?? ""
                        : "",
                    Nationality = item.GuestId is Guid nationalityGuestId && guestProfiles.TryGetValue(nationalityGuestId, out var nationalityProfile)
                        ? nationalityProfile.Nationality ?? ""
                        : "",
                    Phone = item.GuestId is Guid phoneGuestId && guestProfiles.TryGetValue(phoneGuestId, out var phoneProfile)
                        ? phoneProfile.Phone ?? ""
                        : "",
                    Email = item.GuestId is Guid emailGuestId && guestProfiles.TryGetValue(emailGuestId, out var emailProfile)
                        ? emailProfile.Email ?? ""
                        : "",
                    RoomType = item.RoomType,
                    RoomNumber = item.RoomNumber,
                    Arrival = group.Arrival,
                    Departure = group.Departure,
                    Vip = item.IsVip,
                    Child = item.Children > 0,
                    IsCheckedIn = item.IsCheckedIn,
                    IsCheckedOut = item.IsCheckedOut,
                    Passport = item.GuestId is Guid passportGuestId && guestProfiles.TryGetValue(passportGuestId, out var passportProfile)
                        ? passportProfile.Documents.FirstOrDefault(x => x.DocumentType == GuestDocumentType.Passport)?.DocumentNumber ?? ""
                        : "",
                    Visa = item.GuestId is Guid visaGuestId && guestProfiles.TryGetValue(visaGuestId, out var visaProfile)
                        ? visaProfile.Documents.FirstOrDefault(x => x.DocumentType == GuestDocumentType.Visa)?.DocumentNumber ?? ""
                        : "",
                    Note = item.IsCheckedOut ? "Đã check-out" : item.IsCheckedIn ? "Đã check-in" :
                        (item.GuestId is Guid noteGuestId2 && guestProfiles.TryGetValue(noteGuestId2, out var noteProfile2)
                            ? noteProfile2.Notes ?? ""
                            : "")
                });
            }
            foreach (var room in roomGroups)
            {
                var roomItems = room.ToList();
                var guests = roomItems.Where(x => !string.IsNullOrWhiteSpace(x.GuestName)).Select(x => x.GuestName!).ToList();
                var adults = roomItems.Sum(x => x.Adults);
                var children = roomItems.Sum(x => x.Children);
                var hasCapacity = roomCapacities.TryGetValue(room.Key, out var capacity);
                _roomPairs.Add(new RlRoomPair
                {
                    HotelRoomId = room.First().HotelRoomId,
                    RoomNumber = room.First().RoomNumber,
                    GuestA = guests.ElementAtOrDefault(0) ?? "",
                    GuestB = guests.ElementAtOrDefault(1) ?? "",
                    Type = guests.Count > 1 ? "Twin" : "Single",
                    IsCheckedIn = room.Any(x => x.IsCheckedIn),
                    IsCheckedOut = room.Any(x => x.IsCheckedOut),
                    Adults = adults,
                    Children = children,
                    CapacityKnown = hasCapacity,
                    CapacityValid = hasCapacity && adults <= capacity!.MaximumAdultCapacity && children <= capacity.MaximumChildCapacity
                });
            }
            BuildContent(_step);
            UpdateStatus();
            UpdateFooter();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải Rooming List thật: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<IReadOnlyDictionary<Guid, GuestProfile>> LoadGuestProfilesAsync(
        IReadOnlyList<RoomingListItemDto> items)
    {
        if (_guestProfileRepository is null || _contextAccessor?.Current is not { } context)
            return new Dictionary<Guid, GuestProfile>();
        var propertyId = context.Property.PropertyId.Value;

        var guestIds = items
            .Where(x => x.GuestId is not null)
            .Select(x => x.GuestId!.Value)
            .Distinct()
            .ToArray();
        if (guestIds.Length == 0)
            return new Dictionary<Guid, GuestProfile>();

        var profiles = new Dictionary<Guid, GuestProfile>();
        foreach (var guestId in guestIds)
        {
            try
            {
                var profile = await _guestProfileRepository.GetAsync(propertyId, guestId, CancellationToken.None);
                if (profile is not null)
                    profiles[guestId] = profile;
            }
            catch
            {
                // One missing/inaccessible profile must not hide the real Rooming List.
            }
        }

        return profiles;
    }

    private async Task<IReadOnlyDictionary<Guid, RoomCapacity>> LoadRoomCapacitiesAsync(
        IReadOnlyList<RoomingListItemDto> items)
    {
        var capacities = new Dictionary<Guid, RoomCapacity>();
        if (_hotelQueries is null)
            return capacities;

        foreach (var roomId in items.Select(x => x.HotelRoomId).Distinct())
        {
            try
            {
                var roomResult = await _hotelQueries.GetRoom(roomId, CancellationToken.None);
                if (!roomResult.IsSuccess || roomResult.Value is not { } room)
                    continue;

                var typeResult = await _hotelQueries.GetType(room.HotelRoomTypeId, CancellationToken.None);
                if (!typeResult.IsSuccess || typeResult.Value is not { } type)
                    continue;

                capacities[roomId] = new RoomCapacity(type.MaximumAdultCapacity, type.MaximumChildCapacity);
            }
            catch
            {
                // Capacity is a validation signal; a missing master record must remain visible as unknown.
            }
        }

        return capacities;
    }

    // ═══ STEP BAR ═══
    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        for (int i = 0; i < 6; i++)
        {
            var idx = i;
            var lbl = new TextBlock { Text = "Chưa bắt đầu", FontSize = 9, Foreground = C("#64748B") }; _stepLbls[i] = lbl;
            var letter = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(7), Background = Cb("#F1F5F9"), Child = new TextBlock { Text = StepLetters[i], FontSize = 13, FontWeight = FontWeights.Bold, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            var box = new Border { Width = 150, Height = 60, CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 2, 0), Background = Brushes.White, BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand, Padding = new Thickness(10, 7, 10, 7), Child = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition() }, Children = { letter, new StackPanel { Margin = new Thickness(8, 0, 0, 0), Children = { new TextBlock { Text = StepTitles[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#64748B") }, lbl } } } } };
            Grid.SetColumn(letter, 0); Grid.SetColumn((StackPanel)((Grid)box.Child).Children[1], 1);
            box.MouseLeftButtonDown += (_, _) => { if (idx <= _step || CanEnter(idx)) GoTo(idx); };
            _stepBoxes[i] = box; StepBar.Children.Add(box);
            if (i < 5) StepBar.Children.Add(new TextBlock { Text = "▸", FontSize = 14, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });
        }
    }

    private void RefreshStepper()
    {
        for (int i = 0; i < 6; i++)
        {
            if (i < _step) { _stepBoxes[i].Background = Cb("#F0FDF4"); _stepBoxes[i].BorderBrush = Cb("#86EFAC"); _stepBoxes[i].BorderThickness = new Thickness(1); _stepLbls[i].Text = "Hoàn thành"; _stepLbls[i].Foreground = C("#166534"); }
            else if (i == _step) { _stepBoxes[i].Background = Cb("#ECFDF5"); _stepBoxes[i].BorderBrush = Cb("#18B33F"); _stepBoxes[i].BorderThickness = new Thickness(1.5); _stepLbls[i].Text = "Đang nhập"; _stepLbls[i].Foreground = C("#065F46"); }
            else { _stepBoxes[i].Background = Brushes.White; _stepBoxes[i].BorderBrush = Cb("#E2E8F0"); _stepBoxes[i].BorderThickness = new Thickness(1); _stepLbls[i].Text = "Chưa bắt đầu"; _stepLbls[i].Foreground = C("#64748B"); }
        }
    }

    private bool CanEnter(int s) => s switch { 0 => true, 1 => _selectedGroup != null, 2 => _selectedGroup != null, 3 => _selectedGroup != null, 4 => true, 5 => !HasBlocking(), _ => false };
    private bool CanProceed() => _step switch { 0 => _selectedGroup != null, 1 => true, 2 => true, 3 => true, 4 => !HasBlocking(), 5 => false, _ => false };

    private void GoTo(int s)
    {
        if (s == 1 && _selectedGroup != null && _guests.Count == 0) GenerateGuests();
        _step = s; RefreshStepper(); BuildContent(s); UpdateFooter(); UpdateStatus();
    }

    private void OnNext(object s, RoutedEventArgs e) { if (_step < 5 && CanProceed()) { _hasChanges = true; GoTo(_step + 1); } }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 0) GoTo(_step - 1); }
    private void OnCancel(object s, RoutedEventArgs e) { if (_hasChanges && MessageBox.Show("Huỷ Rooming List đang thực hiện?", "Xác nhận", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return; Close(); }
    private void OnClose(object s, RoutedEventArgs e) => OnCancel(s, e);
    private void OnRefresh(object s, RoutedEventArgs e) { _hasChanges = false; GoTo(0); }
    private void OnWindowKeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Escape) OnCancel(s, e); else if (e.Key == Key.Enter && CanProceed() && _step < 5) OnNext(s, e); }

    private void UpdateFooter()
    {
        FooterLabel.Text = $"Bước {_step + 1}/6 · {StepTitles[_step]}"; BtnBack.IsEnabled = _step > 0;
        var warn = GetWarnings(); var block = GetBlocking();
        FooterWarn.Visibility = warn.Count > 0 ? Visibility.Visible : Visibility.Collapsed; FooterWarn.Text = $"⚠ {warn.Count}";
        FooterBlock.Visibility = block.Count > 0 ? Visibility.Visible : Visibility.Collapsed; FooterBlock.Text = $"⛔ {block.Count}";
        if (_step == 5) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = CanConfirm(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; BtnNext.IsEnabled = CanProceed(); }
    }

    private void UpdateStatus()
    {
        StatusPanel.Children.Clear();
        if (_selectedGroup != null)
        {
            StatusPanel.Children.Add(Chip("Đoàn", _selectedGroup.Name, "#18B33F"));
            StatusPanel.Children.Add(Chip("Đến", _selectedGroup.Arrival.ToString("dd/MM"), "#2563EB"));
            StatusPanel.Children.Add(Chip("Đi", _selectedGroup.Departure.ToString("dd/MM"), "#7C3AED"));
            StatusPanel.Children.Add(Chip("Khách", $"{_guests.Count}", "#0891B2"));
            StatusPanel.Children.Add(Chip("VIP", $"{_guests.Count(g => g.Vip)}", "#D97706"));
            StatusPanel.Children.Add(Chip("Passport", $"{_guests.Count(g => !string.IsNullOrEmpty(g.Passport))}/{_guests.Count}", "#16A34A"));
        }
        if (StatusPanel.Children.Count == 0) StatusPanel.Children.Add(new TextBlock { Text = "Chọn đoàn để bắt đầu", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center });
    }

    private static Border Chip(string l, string v, string c) => new() { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}1A")), CornerRadius = new CornerRadius(7), Padding = new Thickness(9, 4, 9, 4), Margin = new Thickness(0, 0, 7, 0), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}33")), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = l + ": ", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c)), FontWeight = FontWeights.SemiBold }, new TextBlock { Text = v, FontSize = 11, Foreground = C("#334155") } } } };

    // ═══ CONTENT ═══
    private void BuildContent(int s)
    {
        ContentPanel.Children.Clear();
        switch (s) { case 0: BuildA(); break; case 1: BuildB(); break; case 2: BuildC(); break; case 3: BuildD(); break; case 4: BuildE(); break; case 5: BuildF(); break; }
        if (s is >= 3 and <= 5)
            WrapStepWithSidebar(StepTitles[s]);
    }

    // ═══ A: CHỌN ĐOÀN ═══
    private void BuildA()
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(STitle("Chọn đoàn"));
        ContentPanel.Children.Add(SText("Chọn đoàn từ danh sách để quản lý Rooming List."));
        var fBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        var stb = new TextBox { Text = _groupSearch, Width = 220, Height = 34, Margin = new Thickness(0, 0, 8, 4) }; stb.TextChanged += (_, _) => { _groupSearch = stb.Text; _groupPager.CurrentPage = 1; BuildA(); }; fBar.Children.Add(stb);
        var fcb = new ComboBox { Width = 130, Height = 34, Margin = new Thickness(0, 0, 8, 4), ItemsSource = new[] { "Tất cả", "Confirmed", "Arrived", "Checked In" }, SelectedItem = _groupFilterStatus }; fcb.SelectionChanged += (_, _) => { _groupFilterStatus = fcb.SelectedItem?.ToString() ?? ""; _groupPager.CurrentPage = 1; BuildA(); }; fBar.Children.Add(fcb);
        ContentPanel.Children.Add(fBar);
        if (!string.IsNullOrWhiteSpace(_groupLoadError))
        {
            ContentPanel.Children.Add(new Border
            {
                Background = Cb("#FEF2F2"),
                BorderBrush = Cb("#FCA5A5"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new TextBlock
                {
                    Text = _groupLoadError,
                    Foreground = C("#B91C1C"),
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap
                }
            });
        }
        var filtered = _groups.Where(g => (string.IsNullOrEmpty(_groupSearch) || g.Name.ToLower().Contains(_groupSearch.ToLower()) || g.Code.ToLower().Contains(_groupSearch.ToLower())) && (string.IsNullOrEmpty(_groupFilterStatus) || _groupFilterStatus == "Tất cả" || g.Status == _groupFilterStatus)).ToList();
        _buildingGroupPage = true;
        _groupPager.SetSource(filtered, resetPage: false);
        _buildingGroupPage = false;
        var list = new StackPanel();
        foreach (var grp in _groupPager.Items)
        {
            var g = grp; var sel = g == _selectedGroup;
            var card = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 6), Background = sel ? Cb("#ECFDF5") : Brushes.White, BorderBrush = sel ? Cb("#18B33F") : Cb("#E2E8F0"), BorderThickness = new Thickness(sel ? 2 : 1), Cursor = Cursors.Hand };
            var cg = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            var csp = new StackPanel();
            csp.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = g.Code, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = C("#18B33F") }, new TextBlock { Text = " · " + g.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), Margin = new Thickness(8, 0, 0, 0) } } });
            csp.Children.Add(new TextBlock { Text = $"{g.Company ?? "—"} | {g.Arrival:dd/MM} → {g.Departure:dd/MM} | {g.TotalGuests} khách | {g.TotalRooms} phòng", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 3, 0, 0) });
            Grid.SetColumn(csp, 0); cg.Children.Add(csp);
            Grid.SetColumn(new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center, Background = Cb("#DCFCE7"), Child = new TextBlock { Text = g.Status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } }, 1); cg.Children.Add(new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center, Background = Cb("#DCFCE7"), Child = new TextBlock { Text = g.Status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } });
            // Keep the status badge in the second grid column. The previous
            // implementation created the badge twice and the second instance
            // defaulted to column 0, covering the group details.
            if (cg.Children.Count > 1) Grid.SetColumn(cg.Children[^1], 1);
            card.Child = cg; card.MouseLeftButtonDown += (_, _) => { _selectedGroup = g; BuildA(); UpdateFooter(); UpdateStatus(); }; list.Children.Add(card);
        }
        var selectionGrid = new Grid
        {
            Height = 500,
            MinHeight = 420,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(10) },
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }
            }
        };
        selectionGrid.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding(nameof(ScrollViewer.ViewportWidth)) { Source = ContentScroll });

        var listCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(10) };
        var listShell = new Grid { RowDefinitions = { new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }, new RowDefinition { Height = GridLength.Auto } } };
        var listScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = list };
        Grid.SetRow(listScroll, 0);
        listShell.Children.Add(listScroll);
        var pagerBar = new Border { BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(0, 8, 0, 0), Margin = new Thickness(0, 8, 0, 0) };
        var pagerLayout = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
        pagerLayout.Children.Add(new TextBlock { Text = _groupPager.PageText, FontSize = 11, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center });
        var pager = new ServerPagerControl { DataContext = _groupPager, HorizontalAlignment = HorizontalAlignment.Right };
        Grid.SetColumn(pager, 1);
        pagerLayout.Children.Add(pager);
        pagerBar.Child = pagerLayout;
        Grid.SetRow(pagerBar, 1);
        listShell.Children.Add(pagerBar);
        listCard.Child = listShell;
        Grid.SetColumn(listCard, 0);
        selectionGrid.Children.Add(listCard);

        var detailCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14) };
        var detailStack = new StackPanel();
        if (_selectedGroup == null)
        {
            detailStack.Children.Add(new TextBlock
            {
                Text = "Chọn một đoàn để xem thông tin chi tiết.",
                FontSize = 13,
                Foreground = C("#94A3B8"),
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            var selected = _selectedGroup;
            detailStack.Children.Add(new TextBlock { Text = "Thông tin đoàn", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 10) });
            detailStack.Children.Add(new TextBlock { Text = selected.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), TextWrapping = TextWrapping.Wrap });
            detailStack.Children.Add(new TextBlock { Text = selected.Code, FontSize = 11, Foreground = C("#18B33F"), Margin = new Thickness(0, 3, 0, 10) });
            detailStack.Children.Add(DetailGrid("Nhận diện", ("Mã đoàn", selected.Code), ("Công ty / đại lý", selected.Company ?? "—"), ("Trạng thái", selected.Status)));
            detailStack.Children.Add(DetailGrid("Lưu trú", ("Ngày đến", selected.Arrival.ToString("dd/MM/yyyy")), ("Ngày đi", selected.Departure.ToString("dd/MM/yyyy")), ("Số đêm", (selected.Departure - selected.Arrival).Days.ToString()), ("Số khách", selected.TotalGuests.ToString()), ("Số phòng", selected.TotalRooms.ToString())));
            detailStack.Children.Add(new Border { Background = Cb("#ECFDF5"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Child = new TextBlock { Text = "Đã chọn đoàn — có thể chuyển sang bước tiếp theo.", FontSize = 12, Foreground = C("#166534"), TextWrapping = TextWrapping.Wrap } });
        }
        detailCard.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = detailStack };
        Grid.SetColumn(detailCard, 2);
        selectionGrid.Children.Add(detailCard);

        ContentPanel.Children.Add(selectionGrid);
    }

    // ═══ B: DANH SÁCH KHÁCH ═══
    private void BuildB()
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(STitle("Danh sách khách"));
        // Dashboard KPI
        var kpis = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        kpis.Children.Add(Kpi("Tổng KH", _guests.Count.ToString(), "#18B33F"));
        kpis.Children.Add(Kpi("VIP", _guests.Count(g => g.Vip).ToString(), "#D97706"));
        kpis.Children.Add(Kpi("Leader", _guests.Count(g => g.Leader).ToString(), "#2563EB"));
        kpis.Children.Add(Kpi("Trẻ em", _guests.Count(g => g.Child).ToString(), "#7C3AED"));
        kpis.Children.Add(Kpi("Có Passport", _guests.Count(g => !string.IsNullOrEmpty(g.Passport)).ToString(), "#16A34A"));
        kpis.Children.Add(Kpi("Thiếu Passport", _guests.Count(g => string.IsNullOrEmpty(g.Passport)).ToString(), "#DC2626"));
        ContentPanel.Children.Add(kpis);

        // Toolbar
        var tbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        tbar.Children.Add(ActionBtn("+ Quản lý khách thật", () => _ = OpenGroupGuestManagementAsync()));
        tbar.Children.Add(ActionBtn("Nhập CSV", () => _ = ImportRoomingListAsync()));
        tbar.Children.Add(ActionBtn("Xuất CSV", () => _ = ExportRoomingListAsync()));
        tbar.Children.Add(ActionBtn("In danh sách", () => _ = PrintRoomingListAsync()));
        ContentPanel.Children.Add(tbar);

        // 3-panel: Guest List | Detail | Validation
        var mainGrid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }, new ColumnDefinition { Width = new GridLength(8) }, new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }, new ColumnDefinition { Width = new GridLength(8) }, new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) } }, MinHeight = 420 };

        // LEFT: Guest list
        var leftCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(8) };
        var leftStack = new StackPanel();
        var gSearch = new TextBox { Text = _guestSearch, Height = 30, FontSize = 11, Margin = new Thickness(0, 0, 0, 6) }; gSearch.TextChanged += (_, _) => { _guestSearch = gSearch.Text; BuildB(); }; leftStack.Children.Add(gSearch);
        var filtered = _guests.Where(g => string.IsNullOrEmpty(_guestSearch) || g.Name.ToLower().Contains(_guestSearch.ToLower())).ToList();
        var guestGrid = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = filtered, MaxHeight = 450, SelectionMode = DataGridSelectionMode.Single };
        guestGrid.SelectionChanged += (_, _) => { if (guestGrid.SelectedItem is RlGuest g) { _selectedGuest = g; BuildB(); } };
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "Khách", Binding = new System.Windows.Data.Binding("Name"), Width = 130 });
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "GT", Binding = new System.Windows.Data.Binding("Gender"), Width = 50 });
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "Quốc tịch", Binding = new System.Windows.Data.Binding("Nationality"), Width = 80 });
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "Passport", Binding = new System.Windows.Data.Binding("Passport"), Width = 100 });
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "Đến", Binding = new System.Windows.Data.Binding("ArrivalDisplay"), Width = 80 });
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "Đi", Binding = new System.Windows.Data.Binding("DepartureDisplay"), Width = 80 });
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "Hạng phòng", Binding = new System.Windows.Data.Binding("RoomType"), Width = 80 });
        guestGrid.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("RoomNumber"), Width = 65 });
        guestGrid.Columns.Add(new DataGridTemplateColumn { Header = "VIP", Width = 50, CellTemplate = MakeBoolTemplate("Vip", "★", "#D97706") });
        guestGrid.Columns.Add(new DataGridTemplateColumn { Header = "Sharing", Width = 60, CellTemplate = MakeBoolTemplate("Sharing", "2P", "#7C3AED") });
        leftStack.Children.Add(guestGrid);
        leftCard.Child = leftStack; Grid.SetColumn(leftCard, 0); mainGrid.Children.Add(leftCard);

        // CENTER: Guest Detail
        var centerCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(12) };
        var centerStack = new StackPanel();
        if (_selectedGuest != null)
        {
            var g = _selectedGuest;
            centerStack.Children.Add(new TextBlock { Text = g.Name, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) });
            centerStack.Children.Add(DetailGrid("Thông tin cá nhân", ("Giới tính", g.Gender), ("Ngày sinh", g.BirthdayDisplay), ("Quốc tịch", g.Nationality), ("Passport", g.Passport), ("Visa", string.IsNullOrEmpty(g.Visa) ? "—" : g.Visa), ("Điện thoại", g.Phone), ("Email", g.Email)));
            centerStack.Children.Add(DetailGrid("Lưu trú", ("Ngày đến", g.ArrivalDisplay), ("Ngày đi", g.DepartureDisplay), ("Hạng phòng", g.RoomType), ("Số phòng", string.IsNullOrEmpty(g.RoomNumber) ? "Chưa phân" : g.RoomNumber), ("Meal Plan", g.MealPlan), ("Sharing", g.Sharing ? "Có" : "Không")));
            centerStack.Children.Add(DetailGrid("Phân loại", ("VIP", g.Vip ? "★ Có" : "Không"), ("Leader", g.Leader ? "Có" : "Không"), ("Trẻ em", g.Child ? "Có" : "Không"), ("Ghi chú", string.IsNullOrEmpty(g.Note) ? "—" : g.Note)));
            if (g.AssignmentId != Guid.Empty && g.GuestId is Guid guestId)
            {
                var lifecycle = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
                if (!g.IsCheckedIn)
                    lifecycle.Children.Add(ActionBtn("Ghi nhận đến", () => _ = MarkGuestArrivalAsync(g)));
                else if (!g.IsCheckedOut)
                    lifecycle.Children.Add(ActionBtn("Ghi nhận đi", () => _ = MarkGuestDepartureAsync(g)));
                centerStack.Children.Add(lifecycle);
            }
        }
        else { centerStack.Children.Add(new TextBlock { Text = "Chọn khách từ danh sách bên trái", FontSize = 13, Foreground = C("#94A3B8"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }); }
        centerCard.Child = centerStack; Grid.SetColumn(centerCard, 2); mainGrid.Children.Add(centerCard);

        // RIGHT: Validation
        var rightCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(10) };
        var rightStack = new StackPanel();
        rightStack.Children.Add(new TextBlock { Text = "⚠ Kiểm tra", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#D97706"), Margin = new Thickness(0, 0, 0, 8) });
        var noPassport = _guests.Count(g => string.IsNullOrEmpty(g.Passport));
        var noVisa = _guests.Count(g => string.IsNullOrEmpty(g.Visa));
        var noRoom = _guests.Count(g => string.IsNullOrEmpty(g.RoomNumber));
        var duplicate = _guests.GroupBy(g => g.Passport).Where(x => x.Key != "" && x.Count() > 1).ToList();
        if (noPassport > 0) rightStack.Children.Add(ValBadge($"Thiếu Passport: {noPassport}", "#DC2626"));
        if (noVisa > 0) rightStack.Children.Add(ValBadge($"Thiếu Visa: {noVisa}", "#D97706"));
        if (noRoom > 0) rightStack.Children.Add(ValBadge($"Chưa có phòng: {noRoom}", "#EA580C"));
        if (duplicate.Count > 0) rightStack.Children.Add(ValBadge($"Trùng Passport: {duplicate.Count}", "#DC2626"));
        if (noPassport == 0 && noRoom == 0 && duplicate.Count == 0)
            rightStack.Children.Add(new Border { Background = Cb("#DCFCE7"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Child = new TextBlock { Text = "✓ Tất cả hợp lệ", FontSize = 13, Foreground = C("#16A34A"), FontWeight = FontWeights.SemiBold } });
        rightCard.Child = rightStack; Grid.SetColumn(rightCard, 4); mainGrid.Children.Add(rightCard);
        ContentPanel.Children.Add(mainGrid);
    }

    // ═══ C: GHÉP PHÒNG ═══
    private void BuildC()
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(STitle("Ghép phòng — Room Pairing"));
        ContentPanel.Children.Add(SText("Ghép khách vào phòng. Hỗ trợ Twin, Double, Triple, Quad, Family, Connecting."));
        var acts = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        acts.Children.Add(ActionBtn("Mở phân phòng thật", () => _ = OpenRoomAssignmentAsync()));
        acts.Children.Add(ActionBtn("Xóa ghép phòng", () => _ = RemoveSelectedRoomPairAsync()));
        acts.Children.Add(ActionBtn("Tải lại từ DB", () => _ = ReloadRoomingListAsync()));
        ContentPanel.Children.Add(acts);

        var kpis = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        kpis.Children.Add(Kpi("Tổng phòng", _roomPairs.Count.ToString(), "#18B33F"));
        kpis.Children.Add(Kpi("Single", _roomPairs.Count(p => p.Type == "Single").ToString(), "#2563EB"));
        kpis.Children.Add(Kpi("Twin/Double", _roomPairs.Count(p => p.Type is "Twin" or "Double").ToString(), "#7C3AED"));
        kpis.Children.Add(Kpi("Triple/Quad", _roomPairs.Count(p => p.Type is "Triple" or "Quad").ToString(), "#D97706"));
        ContentPanel.Children.Add(kpis);

        var pairWp = new WrapPanel { Margin = new Thickness(0, 0, 0, 0) };
        foreach (var p in _roomPairs)
        {
            var color = p.Type switch { "Single" => "#2563EB", "Twin" => "#7C3AED", "Double" => "#16A34A", "Triple" => "#D97706", "Quad" => "#DC2626", _ => "#64748B" };
            var selected = p == _selectedRoomPair;
            var card = new Border { Width = 160, MinHeight = 92, CornerRadius = new CornerRadius(10), Padding = new Thickness(10, 8, 10, 8), Margin = new Thickness(0, 0, 6, 6), Background = selected ? Cb("#ECFDF5") : new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}10")), BorderBrush = selected ? Cb("#18B33F") : new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), BorderThickness = new Thickness(selected ? 2 : 1), Cursor = Cursors.Hand };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = $"🚪 {p.RoomNumber}", FontSize = 14, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) });
            stack.Children.Add(new TextBlock { Text = p.GuestA, FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 4, 0, 0) });
            if (!string.IsNullOrEmpty(p.GuestB)) stack.Children.Add(new TextBlock { Text = "+ " + p.GuestB, FontSize = 11, Foreground = C("#64748B") });
            stack.Children.Add(new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(0, 4, 0, 0), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}20")), Child = new TextBlock { Text = p.Type, FontSize = 10, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), FontWeight = FontWeights.SemiBold }, HorizontalAlignment = HorizontalAlignment.Left });
            card.Child = stack;
            card.MouseLeftButtonDown += (_, _) => { _selectedRoomPair = p; BuildContent(2); };
            pairWp.Children.Add(card);
        }
        ContentPanel.Children.Add(pairWp);
        WrapStepWithSidebar(StepTitles[2]);
    }

    // ═══ D: HỒ SƠ KHÁCH ═══
    private void BuildD()
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(STitle("Hồ sơ khách"));
        ContentPanel.Children.Add(SText("Chỉnh sửa thông tin chi tiết của từng khách trong đoàn."));
        var dg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = _guests, MaxHeight = 500 };
        dg.SelectionMode = DataGridSelectionMode.Single;
        dg.SelectionChanged += (_, _) =>
        {
            if (dg.SelectedItem is RlGuest guest)
            {
                _selectedGuest = guest;
                BuildContent(3);
            }
        };
        dg.Columns.Add(new DataGridTextColumn { Header = "Tên", Binding = new System.Windows.Data.Binding("Name"), Width = 130 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Ngày sinh", Binding = new System.Windows.Data.Binding("BirthdayDisplay"), Width = 90 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Passport", Binding = new System.Windows.Data.Binding("Passport"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Visa", Binding = new System.Windows.Data.Binding("Visa"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Phone", Binding = new System.Windows.Data.Binding("Phone"), Width = 110 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Email", Binding = new System.Windows.Data.Binding("Email"), Width = 150 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Meal", Binding = new System.Windows.Data.Binding("MealPlan"), Width = 55 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Ghi chú", Binding = new System.Windows.Data.Binding("Note"), Width = 150 });
        ContentPanel.Children.Add(dg);
        ContentPanel.Children.Add(new TextBlock { Text = "💡 Double-click để sửa thông tin khách.", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 4, 0, 0) });
    }

    // ═══ E: KIỂM TRA ═══
    private void BuildE()
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(STitle("Kiểm tra"));
        var capacityKnown = _roomPairs.Count > 0 && _roomPairs.All(x => x.CapacityKnown);
        var capacityValid = capacityKnown && _roomPairs.All(x => x.CapacityValid);
        var sharingGroups = _guests
            .Where(x => !string.IsNullOrWhiteSpace(x.RoomNumber))
            .GroupBy(x => x.RoomNumber)
            .Where(x => x.Count() > 1)
            .ToList();
        var sharingHasUnknownGender = sharingGroups.Any(x => x.Any(g => string.IsNullOrWhiteSpace(g.Gender)));
        var sharingHasMixedGender = sharingGroups.Any(x => x
            .Select(g => g.Gender.Trim().ToLowerInvariant())
            .Where(g => g.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Count() > 1);
        var checks = new (string, bool)[] {
            ("Tất cả khách có Passport", !_guests.Any(g => string.IsNullOrEmpty(g.Passport))),
            ("Passport không trùng", !_guests.Where(g => !string.IsNullOrEmpty(g.Passport)).GroupBy(g => g.Passport).Any(x => x.Count() > 1)),
            ("Tất cả đã có phòng", !_guests.Any(g => string.IsNullOrEmpty(g.RoomNumber))),
            ("Room capacity hợp lệ và đã đọc từ Room Master", capacityValid),
            ("Không trùng tên khách", !_guests.GroupBy(g => g.Name).Any(x => x.Count() > 1)),
            ("Thông tin liên hệ đầy đủ", _guests.Count(g => !string.IsNullOrEmpty(g.Phone)) > _guests.Count / 2),
            ("Quốc tịch đầy đủ", !_guests.Any(g => string.IsNullOrEmpty(g.Nationality))),
            ("Sharing không khác giới và đủ dữ liệu giới tính", !sharingHasUnknownGender && !sharingHasMixedGender),
        };
        foreach (var (label, ok) in checks)
            ContentPanel.Children.Add(ValCard(label, ok));
        var allOk = checks.All(c => c.Item2);
        ContentPanel.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "✓ Rooming List sẵn sàng" : "⛔ Cần khắc phục", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626"), TextAlignment = TextAlignment.Center } });
    }

    // ═══ F: XÁC NHẬN ═══
    private void BuildF()
    {
        ContentPanel.Children.Clear();
        ContentPanel.Children.Add(STitle("Xác nhận Rooming List"));
        ContentPanel.Children.Add(new Border { Background = Cb("#ECFDF5"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Cb("#86EFAC"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = "Xác nhận sẽ ghi thông tin khách, VIP và occupancy bằng Rooming List command thật.", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#166534") } });
        var s = new StackPanel();
        s.Children.Add(Prev("Đoàn", _selectedGroup?.Name ?? "—"));
        s.Children.Add(Prev("Tổng khách", $"{_guests.Count} (NL: {_guests.Count(g => !g.Child)}, TE: {_guests.Count(g => g.Child)})"));
        s.Children.Add(Prev("VIP / Leader", $"{_guests.Count(g => g.Vip)} / {_guests.Count(g => g.Leader)}"));
        s.Children.Add(Prev("Tổng phòng", _roomPairs.Count.ToString()));
        s.Children.Add(Prev("Single / Sharing", $"{_roomPairs.Count(p => p.Type == "Single")} / {_roomPairs.Count(p => p.Type != "Single")}"));
        s.Children.Add(Prev("Passport / Visa", $"{_guests.Count(g => !string.IsNullOrEmpty(g.Passport))} / {_guests.Count(g => !string.IsNullOrEmpty(g.Visa))}"));
        s.Children.Add(Prev("Thiếu Passport", _guests.Count(g => string.IsNullOrEmpty(g.Passport)).ToString()));
        ContentPanel.Children.Add(s);
        ContentPanel.Children.Add(Sep());
        ContentPanel.Children.Add(MakeCfm("Tôi đã kiểm tra danh sách khách", _confirmGuests, v => { _confirmGuests = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfm("Tôi đã kiểm tra giấy tờ (Passport/Visa)", _confirmDocs, v => { _confirmDocs = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfm("Tôi đã kiểm tra ghép phòng", _confirmRooms, v => { _confirmRooms = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfm("Tôi xác nhận Rooming List đúng", _confirmAll, v => { _confirmAll = v; UpdateFooter(); }));
    }

    private void WrapStepWithSidebar(string stepLabel)
    {
        var existing = ContentPanel.Children.Cast<UIElement>().ToList();
        ContentPanel.Children.Clear();

        var layout = new Grid
        {
            Height = 500,
            MinHeight = 420,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) },
                new ColumnDefinition { Width = new GridLength(10) },
                new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }
            }
        };
        layout.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding(nameof(ScrollViewer.ViewportWidth)) { Source = ContentScroll });

        var leftStack = new StackPanel();
        foreach (var element in existing)
            leftStack.Children.Add(element);
        var leftCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14) };
        leftCard.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = leftStack };
        Grid.SetColumn(leftCard, 0);
        layout.Children.Add(leftCard);

        var detailCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14) };
        var detail = new StackPanel();
        detail.Children.Add(new TextBlock { Text = stepLabel == StepTitles[2] ? "Chi tiết ghép phòng" : stepLabel == StepTitles[3] ? "Sửa nhanh thông tin khách" : "Thông tin đoàn", FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 10) });
        if (stepLabel == StepTitles[2])
        {
            BuildRoomPairDetails(detail);
        }
        else if (stepLabel == StepTitles[3])
        {
            BuildGuestQuickEdit(detail);
        }
        else if (_selectedGroup == null)
        {
            detail.Children.Add(new TextBlock { Text = "Chọn đoàn ở mục A để xem chi tiết.", FontSize = 13, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
        }
        else
        {
            var g = _selectedGroup;
            detail.Children.Add(new TextBlock { Text = g.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), TextWrapping = TextWrapping.Wrap });
            detail.Children.Add(new TextBlock { Text = g.Code, FontSize = 11, Foreground = C("#18B33F"), Margin = new Thickness(0, 3, 0, 10) });
            detail.Children.Add(DetailGrid("Nhận diện", ("Mã đoàn", g.Code), ("Công ty / đại lý", g.Company ?? "—"), ("Trạng thái", g.Status)));
            detail.Children.Add(DetailGrid("Lưu trú", ("Ngày đến", g.Arrival.ToString("dd/MM/yyyy")), ("Ngày đi", g.Departure.ToString("dd/MM/yyyy")), ("Số đêm", (g.Departure - g.Arrival).Days.ToString()), ("Số khách", g.TotalGuests.ToString()), ("Số phòng", g.TotalRooms.ToString())));
            detail.Children.Add(DetailGrid("Rooming List", ("Bước hiện tại", stepLabel), ("Đã nhập khách", _guests.Count.ToString()), ("Đã ghép phòng", _roomPairs.Count.ToString()), ("Thiếu Passport", _guests.Count(x => string.IsNullOrEmpty(x.Passport)).ToString())));
        }
        detailCard.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = detail };
        Grid.SetColumn(detailCard, 2);
        layout.Children.Add(detailCard);
        ContentPanel.Children.Add(layout);
    }

    private void BuildRoomPairDetails(StackPanel detail)
    {
        if (_selectedRoomPair == null)
        {
            detail.Children.Add(new TextBlock { Text = "Chọn một phòng để xem thông tin ghép phòng.", FontSize = 13, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
            return;
        }

        var pair = _selectedRoomPair;
        detail.Children.Add(DetailGrid("Phòng", ("Số phòng", pair.RoomNumber), ("Loại ghép", pair.Type), ("Khách chính", pair.GuestA), ("Khách ghép", string.IsNullOrEmpty(pair.GuestB) ? "—" : pair.GuestB), ("Occupancy", $"NL {pair.Adults} / TE {pair.Children}"), ("Capacity", !pair.CapacityKnown ? "Chưa đọc Room Master" : pair.CapacityValid ? "Hợp lệ" : "Vượt sức chứa"), ("Trạng thái", pair.IsCheckedOut ? "Đã check-out" : pair.IsCheckedIn ? "Đã check-in" : "Chưa sử dụng")));
        var actions = new WrapPanel { Margin = new Thickness(0, 10, 0, 0) };
        actions.Children.Add(ActionBtn("Đổi / tách trong phân phòng", () => _ = OpenRoomAssignmentAsync()));
        actions.Children.Add(ActionBtn("Xóa ghép thật", () => _ = RemoveSelectedRoomPairAsync()));
        detail.Children.Add(actions);
        detail.Children.Add(new TextBlock
        {
            Text = "Các thao tác ghép, đổi và tách được thực hiện trong màn hình Phân phòng đoàn bằng command thật; không sửa dữ liệu cục bộ.",
            FontSize = 11,
            Foreground = C("#64748B"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 8, 0, 0)
        });
    }

    private async Task OpenRoomAssignmentAsync()
    {
        if (_selectedGroup is null)
        {
            MessageBox.Show("Chưa chọn đoàn để phân phòng.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var window = new GroupRoomAssignmentWindow(_selectedGroup.Id) { Owner = this };
            window.ShowDialog();
            await ReloadRoomingListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể mở màn hình phân phòng thật: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task OpenGroupGuestManagementAsync()
    {
        try
        {
            var window = new GroupGuestManagementWindow { Owner = this };
            window.ShowDialog();
            if (_selectedGroup is not null)
                await LoadPersistedRoomingListAsync(_selectedGroup);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể mở màn hình quản lý khách thật: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RemoveSelectedRoomPairAsync()
    {
        if (_selectedGroup is null || _selectedRoomPair is null || _groupRoomAssignmentCommands is null)
        {
            MessageBox.Show("Chưa chọn phòng hoặc chưa có command phân phòng thật.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_selectedRoomPair.IsCheckedIn || _selectedRoomPair.IsCheckedOut)
        {
            MessageBox.Show("Không thể xóa ghép phòng đã check-in hoặc check-out.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (MessageBox.Show($"Xóa ghép phòng {_selectedRoomPair.RoomNumber} khỏi đoàn?", "Xác nhận xóa ghép", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK)
            return;

        try
        {
            await _groupRoomAssignmentCommands.RemoveAsync(_selectedGroup.Id, _selectedRoomPair.HotelRoomId, CancellationToken.None);
            _selectedRoomPair = null;
            await ReloadRoomingListAsync();
            MessageBox.Show("Đã xóa ghép phòng bằng dữ liệu thật.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xóa ghép phòng thật: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ReloadRoomingListAsync()
    {
        if (_selectedGroup is null || _roomingListService is null)
            return;

        await LoadPersistedRoomingListAsync(_selectedGroup);
        _hasChanges = false;
    }

    private void BuildGuestQuickEdit(StackPanel detail)
    {
        if (_selectedGuest == null)
        {
            detail.Children.Add(new TextBlock { Text = "Chọn một khách trong bảng bên trái để sửa nhanh.", FontSize = 13, Foreground = C("#94A3B8"), TextWrapping = TextWrapping.Wrap });
            return;
        }

        var guest = _selectedGuest;
        detail.Children.Add(new TextBlock { Text = guest.Name, FontSize = 15, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), Margin = new Thickness(0, 0, 0, 10) });

        TextBox Field(string label, string value)
        {
            detail.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 0, 0, 3) });
            var box = new TextBox { Text = value, Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(9, 0, 9, 0), VerticalContentAlignment = VerticalAlignment.Center };
            detail.Children.Add(box);
            return box;
        }

        detail.Children.Add(new TextBlock { Text = "Họ tên: " + guest.Name, FontSize = 12, Foreground = C("#334155"), Margin = new Thickness(0, 0, 0, 8) });
        detail.Children.Add(new TextBlock { Text = "Passport/Visa được lưu trực tiếp vào hồ sơ khách thật.", FontSize = 11, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) });
        var passport = Field("Passport", guest.Passport);
        var visa = Field("Visa", guest.Visa);
        var phone = Field("Điện thoại", guest.Phone);
        var email = Field("Email", guest.Email);
        var note = Field("Ghi chú", guest.Note);
        detail.Children.Add(ActionBtn("Lưu hồ sơ khách thật", () => _ = SaveGuestQuickEditAsync(guest, passport.Text, visa.Text, phone.Text, email.Text, note.Text)));
    }

    private async Task SaveGuestQuickEditAsync(RlGuest guest, string passport, string visa, string phone, string email, string note)
    {
        if (_guestProfileRepository is null || guest.GuestId is not Guid guestId || _contextAccessor?.Current is not { } context)
        {
            MessageBox.Show("Khách chưa có GuestId hoặc chưa cấu hình GuestProfile service thật.", "Hồ sơ khách", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var profile = await _guestProfileRepository.GetAsync(context.Property.PropertyId.Value, guestId, CancellationToken.None);
            if (profile is null)
            {
                MessageBox.Show("Không tìm thấy hồ sơ khách thật trong property hiện tại.", "Hồ sơ khách", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            profile.UpdateProfile(
                string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                profile.Address,
                profile.Nationality,
                profile.Gender,
                profile.DateOfBirth,
                DateTime.UtcNow,
                actorId: null);
            profile.SetNotes(string.IsNullOrWhiteSpace(note) ? null : note.Trim(), DateTime.UtcNow, actorId: null);
            var nowUtc = DateTime.UtcNow;
            profile.SetDocument(GuestDocumentType.Passport, passport, issuingCountry: null, expiry: null, nowUtc: nowUtc, actorId: null);
            profile.SetDocument(GuestDocumentType.Visa, visa, issuingCountry: null, expiry: null, nowUtc: nowUtc, actorId: null);
            await _guestProfileRepository.UpdateAsync(profile, CancellationToken.None);
            await LoadPersistedRoomingListAsync(_selectedGroup!);
            MessageBox.Show("Đã lưu Passport, Visa, điện thoại, email và ghi chú vào hồ sơ khách thật.", "Hồ sơ khách", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể lưu hồ sơ khách thật: " + ex.Message, "Hồ sơ khách", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (!CanConfirm()) return;
        if (_roomingListService is null || _selectedGroup is null)
        {
            MessageBox.Show("Chưa có dịch vụ Rooming List thật hoặc chưa chọn đoàn.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var persisted = await _roomingListService.GetRoomingListAsync(_selectedGroup.Id, CancellationToken.None);
            var localByAssignment = _guests
                .Where(x => x.AssignmentId != Guid.Empty)
                .ToDictionary(x => x.AssignmentId);
            var payload = persisted.Select(item =>
            {
                if (!localByAssignment.TryGetValue(item.AssignmentId, out var guest))
                    return item;

                var untouchedPlaceholder = guest.GuestId is null && string.Equals(guest.Name, "Chưa gán khách", StringComparison.Ordinal);
                return item with
                {
                    GuestId = guest.GuestId,
                    GuestName = untouchedPlaceholder ? item.GuestName : guest.Name,
                    IsVip = guest.Vip,
                    Children = guest.Child ? Math.Max(1, item.Children) : item.Children
                };
            }).ToArray();

            var result = await _roomingListService.ImportAsync(_selectedGroup.Id, payload, CancellationToken.None);
            if (result.Errors > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine, result.ErrorDetails), "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _hasChanges = false;
            await LoadPersistedRoomingListAsync(_selectedGroup);
            MessageBox.Show($"Đã ghi {result.Imported:N0} dòng Rooming List vào dữ liệu thật.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể ghi Rooming List thật: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task MarkGuestArrivalAsync(RlGuest guest)
    {
        if (_roomingListService is null || guest.AssignmentId == Guid.Empty || guest.GuestId is null)
        {
            MessageBox.Show("Thiếu dữ liệu assignment/khách thật; không thể ghi nhận đến.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _roomingListService.AssignGuestAsync(guest.AssignmentId, guest.GuestId.Value, guest.Name, CancellationToken.None);
            await _roomingListService.MarkArrivalAsync(guest.AssignmentId, DateTime.UtcNow, CancellationToken.None);
            await LoadPersistedRoomingListAsync(_selectedGroup!);
            _hasChanges = false;
            MessageBox.Show($"Đã ghi nhận khách {guest.Name} đến bằng dữ liệu thật.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể ghi nhận khách đến: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task MarkGuestDepartureAsync(RlGuest guest)
    {
        if (_roomingListService is null || guest.AssignmentId == Guid.Empty)
        {
            MessageBox.Show("Thiếu assignment thật; không thể ghi nhận đi.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await _roomingListService.MarkDepartureAsync(guest.AssignmentId, DateTime.UtcNow, CancellationToken.None);
            await LoadPersistedRoomingListAsync(_selectedGroup!);
            _hasChanges = false;
            MessageBox.Show($"Đã ghi nhận khách {guest.Name} rời đi bằng dữ liệu thật.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể ghi nhận khách rời đi: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExportRoomingListAsync()
    {
        if (_roomingListService is null || _selectedGroup is null)
        {
            MessageBox.Show("Chưa có đoàn/dịch vụ Rooming List thật để xuất.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var items = await _roomingListService.ExportAsync(_selectedGroup.Id, CancellationToken.None);
            if (items.Count == 0)
            {
                MessageBox.Show("Đoàn chưa có dữ liệu Rooming List thật để xuất.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "CSV UTF-8 (*.csv)|*.csv",
                FileName = $"RoomingList-{_selectedGroup.Code}.csv",
                AddExtension = true,
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != true)
                return;

            var csv = new StringBuilder();
            csv.AppendLine("AssignmentId,RoomNumber,RoomType,GuestId,GuestName,Adults,Children,VIP,CheckedIn,CheckedOut");
            foreach (var item in items)
            {
                csv.AppendLine(string.Join(",", Csv(item.AssignmentId.ToString()), Csv(item.RoomNumber), Csv(item.RoomType),
                    Csv(item.GuestId?.ToString() ?? ""), Csv(item.GuestName ?? ""), item.Adults, item.Children,
                    item.IsVip, item.IsCheckedIn, item.IsCheckedOut));
            }

            await File.WriteAllTextAsync(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            MessageBox.Show($"Đã xuất {items.Count:N0} dòng Rooming List thật.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xuất Rooming List thật: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task PrintRoomingListAsync()
    {
        if (_roomingListService is null || _selectedGroup is null)
        {
            MessageBox.Show("Chưa có đoàn/dịch vụ Rooming List thật để in.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var items = await _roomingListService.ExportAsync(_selectedGroup.Id, CancellationToken.None);
            if (items.Count == 0)
            {
                MessageBox.Show("Đoàn chưa có dữ liệu Rooming List thật để in.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var content = new StringBuilder()
                .AppendLine("ROOMING LIST ĐOÀN")
                .AppendLine($"Đoàn: {_selectedGroup.Code} - {_selectedGroup.Name}")
                .AppendLine($"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .AppendLine(new string('-', 110));
            foreach (var item in items)
            {
                content.AppendLine($"{item.RoomNumber} | {item.RoomType} | {item.GuestName ?? "Chưa gán khách"} | Người lớn: {item.Adults} | Trẻ em: {item.Children} | VIP: {(item.IsVip ? "Có" : "Không")} | In: {(item.IsCheckedIn ? "Có" : "Chưa")} | Out: {(item.IsCheckedOut ? "Có" : "Chưa")}");
            }

            new PrintPreviewDialog("Rooming List đoàn", content.ToString()).ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tạo bản in Rooming List thật: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ImportRoomingListAsync()
    {
        if (_roomingListService is null || _selectedGroup is null)
        {
            MessageBox.Show("Chưa có đoàn/dịch vụ Rooming List thật để nhập.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            var current = await _roomingListService.ExportAsync(_selectedGroup.Id, CancellationToken.None);
            var currentByAssignment = current.ToDictionary(x => x.AssignmentId);
            var text = await File.ReadAllTextAsync(dialog.FileName, CancellationToken.None);
            var rows = ParseCsv(text);
            if (rows.Count < 2)
                throw new InvalidOperationException("Tệp CSV phải có dòng tiêu đề và ít nhất một dòng dữ liệu.");

            var header = rows[0].Select((value, index) => new { Key = value.Trim(), Index = index })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .ToDictionary(x => x.Key, x => x.Index, StringComparer.OrdinalIgnoreCase);
            var requiredHeaders = new[] { "AssignmentId", "RoomNumber", "RoomType", "GuestId", "GuestName", "Adults", "Children", "VIP" };
            var missing = requiredHeaders.Where(x => !header.ContainsKey(x)).ToArray();
            if (missing.Length > 0)
                throw new InvalidOperationException("Thiếu cột bắt buộc: " + string.Join(", ", missing));

            var items = new List<RoomingListItemDto>();
            for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
            {
                var row = rows[rowIndex];
                if (row.All(string.IsNullOrWhiteSpace))
                    continue;

                var assignmentIdText = CsvValue(row, header["AssignmentId"]);
                if (!Guid.TryParse(assignmentIdText, out var assignmentId) || !currentByAssignment.TryGetValue(assignmentId, out var existing))
                    throw new InvalidOperationException($"Dòng {rowIndex + 1}: AssignmentId không tồn tại trong Rooming List của đoàn.");

                var roomNumber = CsvValue(row, header["RoomNumber"]);
                var roomType = CsvValue(row, header["RoomType"]);
                if (!string.Equals(roomNumber, existing.RoomNumber, StringComparison.Ordinal)
                    || !string.Equals(roomType, existing.RoomType, StringComparison.Ordinal))
                    throw new InvalidOperationException($"Dòng {rowIndex + 1}: không được đổi phòng/hạng phòng khi nhập Rooming List; hãy dùng phân phòng.");

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

            var result = await _roomingListService.ImportAsync(_selectedGroup.Id, items, CancellationToken.None);
            if (result.Errors > 0)
            {
                var details = string.Join(Environment.NewLine, result.ErrorDetails.Take(8));
                MessageBox.Show($"Không lưu Rooming List. Có lỗi: {result.Errors}/{result.TotalRows}." + Environment.NewLine + details,
                    "Rooming List", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await LoadPersistedRoomingListAsync(_selectedGroup);
            _hasChanges = false;
            MessageBox.Show($"Đã nhập và lưu {result.Imported} dòng Rooming List vào dữ liệu thật.", "Rooming List", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể nhập Rooming List: " + ex.Message, "Rooming List", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private static string CsvValue(IReadOnlyList<string> row, int index)
        => index < row.Count ? row[index].Trim() : string.Empty;

    private static string? NullIfEmpty(string value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool ParseBoolean(string value)
        => value.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
           || value.Trim() is "1" or "yes" or "y" or "có" or "co";

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    private bool CanConfirm() => !HasBlocking() && _confirmGuests && _confirmDocs && _confirmRooms && _confirmAll;
    private List<string> GetBlocking()
    {
        var b = new List<string>();
        if (_selectedGroup == null) b.Add("Chưa chọn đoàn");
        if (_guests.Count == 0) b.Add("Danh sách khách trống");
        return b;
    }
    private List<string> GetWarnings()
    {
        var w = new List<string>();
        var noPass = _guests.Count(g => string.IsNullOrEmpty(g.Passport));
        if (noPass > 0) w.Add($"Thiếu Passport: {noPass}/{_guests.Count}");
        if (_roomPairs.Count == 0) w.Add("Chưa ghép phòng");
        return w;
    }
    private bool HasBlocking() => GetBlocking().Count > 0;

    // ═══ HELPERS ═══
    private static DataTemplate MakeBoolTemplate(string prop, string trueText, string color)
    {
        var dt = new DataTemplate();
        var factory = new FrameworkElementFactory(typeof(Border));
        factory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
        factory.SetValue(Border.PaddingProperty, new Thickness(4, 2, 4, 2));
        factory.SetValue(Border.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        var tb = new FrameworkElementFactory(typeof(TextBlock));
        tb.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(prop) { Converter = new BoolToVipConverter(trueText) });
        tb.SetValue(TextBlock.FontSizeProperty, 9.0);
        tb.SetValue(TextBlock.FontWeightProperty, FontWeights.SemiBold);
        tb.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)));
        factory.AppendChild(tb);
        dt.VisualTree = factory;
        return dt;
    }

    private static FrameworkElement DetailGrid(string title, params (string, string)[] rows)
    {
        var stack = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        stack.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(0, 0, 0, 4) });
        foreach (var (label, value) in rows) stack.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0), Children = { new TextBlock { Text = label + ": ", FontSize = 11, Foreground = C("#94A3B8"), Width = 90 }, new TextBlock { Text = value, FontSize = 11, Foreground = C("#334155") } } });
        return stack;
    }

    private static FrameworkElement ValBadge(string text, string color) => new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 4), Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}15")), Child = new TextBlock { Text = text, FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), FontWeight = FontWeights.SemiBold } };
    private static FrameworkElement ValCard(string label, bool ok) => new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Background = ok ? Cb("#F0FDF4") : Cb("#FEF2F2"), BorderBrush = ok ? Cb("#BBF7D0") : Cb("#FECACA"), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = ok ? "✓ " : "✗ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626"), FontWeight = FontWeights.Bold }, new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center } } } };
    private static FrameworkElement SuccessIcon() => new Border { Width = 64, Height = 64, CornerRadius = new CornerRadius(32), Background = Cb("#DCFCE7"), HorizontalAlignment = HorizontalAlignment.Center, Child = new TextBlock { Text = "✓", FontSize = 32, FontWeight = FontWeights.Bold, Foreground = C("#16A34A"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
    private static Border Kpi(string label, string value, string color) => new() { Width = 120, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}14")), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 8, 6), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}33")), BorderThickness = new Thickness(1), Child = new StackPanel { Children = { new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B") }, new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) } } } };
    private Button ActionBtn(string text, Action action) { var btn = new Button { Content = text, Height = 32, FontSize = 11, Margin = new Thickness(0, 0, 6, 4), Padding = new Thickness(10, 0, 10, 0), Background = Cb("#ECFDF5"), Foreground = C("#065F46"), BorderBrush = Cb("#A7F3D0"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand }; btn.Click += (_, _) => action(); return btn; }
    private static FrameworkElement MakeCfm(string label, bool value, Action<bool> setter) { var border = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0), Background = Cb("#FAFBFD"), BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1) }; var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalContentAlignment = VerticalAlignment.Center }; cb.Checked += (_, _) => { setter(true); border.Background = Cb("#F0FDF4"); border.BorderBrush = Cb("#86EFAC"); }; cb.Unchecked += (_, _) => { setter(false); border.Background = Cb("#FAFBFD"); border.BorderBrush = Cb("#E2E8F0"); }; border.Child = cb; return border; }
    private static FrameworkElement STitle(string t) => new TextBlock { Text = t, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 4) };
    private static FrameworkElement SText(string t) => new TextBlock { Text = t, FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 0, 0, 10) };
    private static FrameworkElement Sep() => new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) };
    private static FrameworkElement Prev(string l, string v) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 12, Foreground = C("#94A3B8"), Width = 140 }, new TextBlock { Text = v, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") } } };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══ DATA ═══
public class RlGroup { public Guid Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? Company { get; set; } public DateTime Arrival, Departure; public int TotalGuests, TotalRooms; public string Status { get; set; } = ""; }
public class RlGuest { public int Id; public Guid AssignmentId; public Guid? GuestId; public string Name { get; set; } = ""; public string Gender { get; set; } = ""; public string Nationality { get; set; } = ""; public string Passport { get; set; } = ""; public string Visa { get; set; } = ""; public DateTime Birthday; public string Phone { get; set; } = ""; public string Email { get; set; } = ""; public DateTime Arrival, Departure; public string RoomType { get; set; } = ""; public string RoomNumber { get; set; } = ""; public bool Vip, Leader, Child, Sharing, IsCheckedIn, IsCheckedOut; public string MealPlan { get; set; } = "BB"; public string Note { get; set; } = ""; public string ArrivalDisplay => Arrival.ToString("dd/MM"); public string DepartureDisplay => Departure.ToString("dd/MM"); public string BirthdayDisplay => Birthday.ToString("dd/MM/yyyy"); }
public class RlRoomPair { public Guid HotelRoomId; public string RoomNumber { get; set; } = ""; public string GuestA { get; set; } = ""; public string GuestB { get; set; } = ""; public string Type { get; set; } = "Single"; public bool IsCheckedIn; public bool IsCheckedOut; public int Adults; public int Children; public bool CapacityKnown; public bool CapacityValid; }
internal sealed record RoomCapacity(int MaximumAdultCapacity, int MaximumChildCapacity);
public class BoolToVipConverter : System.Windows.Data.IValueConverter
{
    private readonly string _trueText;
    public BoolToVipConverter(string trueText = "✓") => _trueText = trueText;
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => value is true ? _trueText : "";
    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => System.Windows.Data.Binding.DoNothing;
}
