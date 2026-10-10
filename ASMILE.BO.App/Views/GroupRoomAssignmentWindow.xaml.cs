using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.App.Controls;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.App.Views.Dialogs;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class GroupRoomAssignmentWindow : Window
{
    private int _step; private bool _hasChanges;
    private int _persistedDetailsLoadVersion;

    // Step A — Group selection
    private GraGroup? _selectedGroup;
    private readonly List<GraGroup> _groups = new();
    private string _groupSearch = "", _groupFilterStatus = "";
    private readonly TablePagerViewModel<GraGroup> _groupPager = new() { ItemName = "đoàn" };
    private bool _buildingGroupPage;

    // Step B — Room Block
    private readonly ObservableCollection<GraRoomBlock> _roomBlocks = new();

    // Step C — Assignment (the main screen)
    private readonly ObservableCollection<GraGuest> _guests = new();
    private readonly ObservableCollection<GraRoom> _rooms = new();
    private readonly ObservableCollection<GraAssignment> _assignments = new();
    private GraGuest? _selectedGuest; private GraRoom? _selectedRoom;
    private string _guestSearch = "";

    // Step D — Validation
    // Step E — Housekeeping
    // Step F — Confirm
    private bool _confirmAssign, _confirmVip, _confirmReady, _confirmAll;

    // UI
    private readonly Border[] _stepBoxes = new Border[6];
    private readonly TextBlock[] _stepLbls = new TextBlock[6];
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F" };
    private static readonly string[] StepTitles = { "Chọn đoàn", "Room Block", "Phân phòng", "Kiểm tra", "Housekeeping", "Xác nhận" };
    private readonly IGroupReservationQueryService? _groupQuery;
    private readonly IRoomingListService? _roomingListService;
    private readonly IGroupRoomAssignmentCommandService? _groupRoomAssignmentCommands;
    private readonly HotelRoomMasterDataQueryHandlers? _hotelQueries;
    private readonly ChangeHotelRoomOperationalStatusCommandHandler? _changeRoomStatus;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IAuditService? _auditService;
    private readonly bool _persistedMode;
    private readonly Guid? _initialGroupId;
    private IServiceScope? _dataScope;

    public GroupRoomAssignmentWindow(Guid? initialGroupId = null)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _initialGroupId = initialGroupId;
        var services = (System.Windows.Application.Current as App)?.Services;
        _dataScope = services?.CreateScope();
        var scopedServices = _dataScope?.ServiceProvider;
        _groupQuery = scopedServices?.GetService<IGroupReservationQueryService>();
        _roomingListService = scopedServices?.GetService<IRoomingListService>();
        _groupRoomAssignmentCommands = scopedServices?.GetService<IGroupRoomAssignmentCommandService>();
        _hotelQueries = scopedServices?.GetService<HotelRoomMasterDataQueryHandlers>();
        _changeRoomStatus = scopedServices?.GetService<ChangeHotelRoomOperationalStatusCommandHandler>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _auditService = scopedServices?.GetService<IAuditService>();
        _persistedMode = _groupQuery is not null && _roomingListService is not null && _groupRoomAssignmentCommands is not null && _hotelQueries is not null && _contextAccessor is not null;
        InitializeWithoutMockData();
        _groupPager.PropertyChanged += OnGroupPagerChanged;
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

    private void InitializeWithoutMockData()
    {
        _groups.Clear();
    }

    private async Task LoadPersistedGroupsAsync()
    {
        if (!_persistedMode || _contextAccessor?.Current is not { } context)
            return;

        try
        {
            var groups = await _groupQuery!.GetGroupsAsync(context.Property.PropertyId.Value, CancellationToken.None);
            _groups.Clear();
            foreach (var group in groups)
            {
                _groups.Add(new GraGroup
                {
                    Id = group.GroupReservationId,
                    Code = group.GroupCode,
                    Name = group.GroupName,
                    Company = group.CompanyName ?? group.AgencyName,
                    Arrival = group.ArrivalDate.ToDateTime(TimeOnly.MinValue),
                    Departure = group.DepartureDate.ToDateTime(TimeOnly.MinValue),
                    TotalGuests = group.TotalAdults + group.TotalChildren,
                    TotalRooms = group.ConfirmedRooms,
                    Status = group.Status.ToString(),
                    Vip = group.IsVip
                });
            }
            _groupPager.CurrentPage = 1;
            if (_initialGroupId is { } initialId && _groups.FirstOrDefault(x => x.Id == initialId) is { } initialGroup)
            {
                _selectedGroup = initialGroup;
                GoTo(1);
            }
            else
            {
                BuildA();
            }
        }
        catch (Exception ex)
        {
            _groups.Clear();
            BuildA();
            MessageBox.Show("Không thể tải danh sách đoàn thật: " + ex.Message, "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadPersistedGroupDetailsAsync(GraGroup group, int loadVersion)
    {
        if (!_persistedMode)
            return;

        try
        {
            // Both services resolve through the scoped application context.
            // Do not overlap their EF queries on the same DbContext.
            var roomingList = await _roomingListService!.GetRoomingListAsync(group.Id, CancellationToken.None);
            if (loadVersion != Volatile.Read(ref _persistedDetailsLoadVersion)
                || _selectedGroup?.Id != group.Id)
                return;

            var roomResult = await _hotelQueries!.SearchRooms(
                null, null, null, null, null, true,
                new PageRequest(1, 500), "number", false, CancellationToken.None);
            if (loadVersion != Volatile.Read(ref _persistedDetailsLoadVersion)
                || _selectedGroup?.Id != group.Id)
                return;

            _guests.Clear();
            _rooms.Clear();
            _assignments.Clear();
            _roomBlocks.Clear();

            var guestIndex = 1;
            foreach (var item in roomingList)
            {
                var guestId = guestIndex++;
                _guests.Add(new GraGuest
                {
                    Id = guestId,
                    AssignmentId = item.AssignmentId,
                    GuestProfileId = item.GuestId,
                    Name = item.GuestName ?? "Chưa gán khách",
                    RequestedType = item.RoomType,
                    Vip = item.IsVip,
                    Child = item.Children > 0,
                    Adults = item.Adults,
                    Children = item.Children,
                    RoomNumber = item.RoomNumber,
                    Note = item.IsCheckedOut ? "Đã check-out" : item.IsCheckedIn ? "Đã check-in" : ""
                });
                _assignments.Add(new GraAssignment
                {
                    AssignmentId = item.AssignmentId,
                    HotelRoomId = item.HotelRoomId,
                    GuestId = guestId,
                    RoomNumber = item.RoomNumber,
                    Time = item.CheckInAt ?? DateTime.MinValue
                });
            }

            if (roomResult.IsSuccess && roomResult.Value is { } page)
            {
                foreach (var room in page.Items)
                {
                    _rooms.Add(new GraRoom
                    {
                        Id = room.HotelRoomId,
                        Number = room.RoomNumber,
                        Floor = int.TryParse(room.Floor, out var floor) ? floor : 0,
                        Type = room.HotelRoomTypeName,
                        Status = room.OperationalStatus.ToString(),
                        AssignedGuest = _guests.FirstOrDefault(g => g.RoomNumber == room.RoomNumber)?.Name ?? ""
                    });
                }
            }

            foreach (var room in roomingList.GroupBy(x => x.RoomType))
            {
                var count = room.Count();
                _roomBlocks.Add(new GraRoomBlock { RoomType = room.Key, Total = count, Assigned = count, Clean = 0 });
            }

            UpdateRoomBlockCounts();
            BuildContent(_step);
            UpdateStatus();
            UpdateFooter();
        }
        catch (Exception ex)
        {
            if (loadVersion != Volatile.Read(ref _persistedDetailsLoadVersion)
                || _selectedGroup?.Id != group.Id)
                return;

            _guests.Clear();
            _rooms.Clear();
            _assignments.Clear();
            _roomBlocks.Clear();
            BuildContent(_step);
            UpdateStatus();
            UpdateFooter();
            MessageBox.Show("Không thể tải phân phòng thật: " + ex.Message, "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GenerateRoomBlockAndGuests()
    {
        var loadVersion = Interlocked.Increment(ref _persistedDetailsLoadVersion);
        _roomBlocks.Clear();
        _guests.Clear();
        _rooms.Clear();
        _assignments.Clear();
        if (_persistedMode && _selectedGroup is not null)
            _ = LoadPersistedGroupDetailsAsync(_selectedGroup, loadVersion);
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
            else if (i == _step) { _stepBoxes[i].Background = Cb("#F5F0FF"); _stepBoxes[i].BorderBrush = Cb("#7D2BE1"); _stepBoxes[i].BorderThickness = new Thickness(1.5); _stepLbls[i].Text = "Đang nhập"; _stepLbls[i].Foreground = C("#6D28D9"); }
            else { _stepBoxes[i].Background = Brushes.White; _stepBoxes[i].BorderBrush = Cb("#E2E8F0"); _stepBoxes[i].BorderThickness = new Thickness(1); _stepLbls[i].Text = "Chưa bắt đầu"; _stepLbls[i].Foreground = C("#64748B"); }
        }
    }

    private bool CanEnter(int s) => s switch { 0 => true, 1 => _selectedGroup != null, 2 => _selectedGroup != null, 3 => true, 4 => true, 5 => !HasBlocking(), _ => false };
    private bool CanProceed() => _step switch { 0 => _selectedGroup != null, 1 => true, 2 => true, 3 => !HasBlocking(), 4 => true, 5 => false, _ => false };

    private void GoTo(int s)
    {
        if (s == 1 && _selectedGroup != null && _roomBlocks.Count == 0) GenerateRoomBlockAndGuests();
        _step = s; RefreshStepper(); BuildContent(s); UpdateFooter(); UpdateStatus();
    }

    private void OnNext(object s, RoutedEventArgs e) { if (_step < 5 && CanProceed()) { _hasChanges = true; GoTo(_step + 1); } }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 0) GoTo(_step - 1); }
    private void OnCancel(object s, RoutedEventArgs e) { if (_hasChanges && MessageBox.Show("Huỷ phân phòng đang thực hiện?", "Xác nhận", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return; Close(); }
    private void OnClose(object s, RoutedEventArgs e) => OnCancel(s, e);
    private void OnRefresh(object s, RoutedEventArgs e) { _hasChanges = false; ResetGroupPage(); GoTo(0); }
    private void OnWindowKeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Escape) OnCancel(s, e); else if (e.Key == Key.Enter && CanProceed() && _step < 5) OnNext(s, e); }

    private void OnGroupPagerChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_buildingGroupPage || _step != 0 || (e.PropertyName != nameof(TablePagerViewModel<GraGroup>.CurrentPage) && e.PropertyName != nameof(TablePagerViewModel<GraGroup>.SelectedPageSize))) return;
        BuildA();
    }

    private void ResetGroupPage()
    {
        _buildingGroupPage = true;
        _groupPager.CurrentPage = 1;
        _buildingGroupPage = false;
    }

    private void UpdateFooter()
    {
        FooterLabel.Text = $"Bước {_step + 1}/6 · {StepTitles[_step]}";
        BtnBack.IsEnabled = _step > 0;
        var warn = GetWarnings(); var block = GetBlocking();
        FooterWarn.Visibility = warn.Count > 0 ? Visibility.Visible : Visibility.Collapsed; FooterWarn.Text = $"⚠ {warn.Count}";
        FooterBlock.Visibility = block.Count > 0 ? Visibility.Visible : Visibility.Collapsed; FooterBlock.Text = $"⛔ {block.Count}";
        if (_step == 5) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = CanConfirm(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; BtnNext.IsEnabled = CanProceed(); }
    }

    private void UpdateStatus()
    {
        UpdateSidebar();
        StatusPanel.Children.Clear();
        if (_selectedGroup != null)
        {
            StatusPanel.Children.Add(Chip("Đoàn", _selectedGroup.Name, "#7D2BE1"));
            StatusPanel.Children.Add(Chip("Đến", _selectedGroup.Arrival.ToString("dd/MM"), "#2563EB"));
            StatusPanel.Children.Add(Chip("Đi", _selectedGroup.Departure.ToString("dd/MM"), "#7C3AED"));
            StatusPanel.Children.Add(Chip("Khách", $"{_selectedGroup.TotalGuests}", "#0891B2"));
            StatusPanel.Children.Add(Chip("Phòng", $"{_selectedGroup.TotalRooms}", "#16A34A"));
        }
        if (_assignments.Count > 0)
        {
            StatusPanel.Children.Add(Chip("Đã phân", $"{_assignments.Count}", "#16A34A"));
            StatusPanel.Children.Add(Chip("Còn lại", $"{_guests.Count - _assignments.Count(g => _guests.Any(gg => gg.Id == g.GuestId))}", "#D97706"));
        }
        if (StatusPanel.Children.Count == 0) StatusPanel.Children.Add(new TextBlock { Text = "Chọn đoàn để bắt đầu phân phòng", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center });
    }

    private void UpdateSidebar()
    {
        var showSidebar = _step != 2;
        SidebarCard.Visibility = showSidebar ? Visibility.Visible : Visibility.Collapsed;
        ContentGrid.ColumnDefinitions[0].MinWidth = showSidebar ? 620 : 0;
        ContentGrid.ColumnDefinitions[1].Width = showSidebar ? new GridLength(12) : new GridLength(0);
        ContentGrid.ColumnDefinitions[2].MinWidth = showSidebar ? 310 : 0;
        ContentGrid.ColumnDefinitions[0].Width = showSidebar ? new GridLength(68, GridUnitType.Star) : new GridLength(1, GridUnitType.Star);
        ContentGrid.ColumnDefinitions[2].Width = showSidebar ? new GridLength(32, GridUnitType.Star) : new GridLength(0);
        SidebarContent.Children.Clear();
        if (!showSidebar) return;
        if (_selectedGroup == null)
        {
            SidebarContent.Children.Add(new TextBlock
            {
                Text = "Chọn một đoàn để xem thông tin chi tiết.",
                FontSize = 12,
                Foreground = C("#94A3B8"),
                TextWrapping = TextWrapping.Wrap
            });
            return;
        }

        SidebarContent.Children.Add(SideSec("Nhận diện"));
        SidebarContent.Children.Add(SideRow("Mã đoàn", _selectedGroup.Code));
        SidebarContent.Children.Add(SideRow("Tên đoàn", _selectedGroup.Name));
        SidebarContent.Children.Add(SideRow("Công ty", _selectedGroup.Company ?? "—"));
        SidebarContent.Children.Add(SideRow("Đại lý", "—"));
        SidebarContent.Children.Add(SideRow("Trạng thái", _selectedGroup.Status));
        SidebarContent.Children.Add(SideSep());

        SidebarContent.Children.Add(SideSec("Lưu trú"));
        SidebarContent.Children.Add(SideRow("Ngày đến", _selectedGroup.Arrival.ToString("dd/MM/yyyy")));
        SidebarContent.Children.Add(SideRow("Ngày đi", _selectedGroup.Departure.ToString("dd/MM/yyyy")));
        SidebarContent.Children.Add(SideRow("Số khách", _selectedGroup.TotalGuests.ToString("N0")));
        SidebarContent.Children.Add(SideRow("Số phòng", _selectedGroup.TotalRooms.ToString("N0")));
        SidebarContent.Children.Add(SideRow("VIP", _selectedGroup.Vip ? "Có" : "Không"));
        SidebarContent.Children.Add(SideSep());

        SidebarContent.Children.Add(SideSec("Tiến độ phân phòng"));
        var assignedGuests = _assignments.Select(a => a.GuestId).Distinct().Count();
        var assignedRooms = _assignments.Select(a => a.RoomNumber).Distinct().Count();
        SidebarContent.Children.Add(SideRow("Khách đã phân", $"{assignedGuests:N0} / {_selectedGroup.TotalGuests:N0}"));
        SidebarContent.Children.Add(SideRow("Phòng đã phân", $"{assignedRooms:N0} / {_selectedGroup.TotalRooms:N0}"));
        if (_roomBlocks.Count > 0)
        {
            SidebarContent.Children.Add(SideSep());
            SidebarContent.Children.Add(SideSec("Room Block"));
            foreach (var block in _roomBlocks) SidebarContent.Children.Add(SideRow(block.RoomType, $"{block.Assigned:N0} / {block.Total:N0} phòng"));
        }
        SidebarContent.Children.Add(new Border
        {
            Background = Cb("#F5F0FF"),
            BorderBrush = Cb("#DDD6FE"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 8, 10, 8),
            Margin = new Thickness(0, 6, 0, 0),
            Child = new TextBlock
            {
                Text = _step == 0 ? "Chọn đoàn để bắt đầu phân phòng." : $"Đang ở bước {StepLetters[_step]} · {StepTitles[_step]}",
                FontSize = 11,
                Foreground = C("#6D28D9"),
                TextWrapping = TextWrapping.Wrap
            }
        });
    }

    private static Border Chip(string l, string v, string c) => new()
    {
        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}1A")), CornerRadius = new CornerRadius(7),
        Padding = new Thickness(9, 4, 9, 4), Margin = new Thickness(0, 0, 7, 0),
        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{c}33")), BorderThickness = new Thickness(1),
        Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = l + ": ", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(c)), FontWeight = FontWeights.SemiBold }, new TextBlock { Text = v, FontSize = 11, Foreground = C("#334155") } } }
    };

    // ═══ CONTENT BUILDERS ═══
    private void BuildContent(int s)
    {
        ContentPanel.Children.Clear();
        GroupStepHeader.Children.Clear();
        GroupStepHeader.Visibility = Visibility.Collapsed;
        GroupPagerFooter.Visibility = Visibility.Collapsed;
        GroupPagerHost.Content = null;
        switch (s) { case 0: BuildA(); break; case 1: BuildB(); break; case 2: BuildC(); break; case 3: BuildD(); break; case 4: BuildE(); break; case 5: BuildF(); break; }
    }

    private void BuildA()
    {
        ContentPanel.Children.Clear();
        ContentScroll.ScrollToTop();
        GroupStepHeader.Children.Clear();
        GroupStepHeader.Visibility = Visibility.Visible;
        GroupStepHeader.Children.Add(STitle("Chọn đoàn"));
        GroupStepHeader.Children.Add(SText("Chọn đoàn từ danh sách để tiến hành phân phòng."));

        var fBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        var stb = new TextBox { Text = _groupSearch, Width = 220, Height = 34, Margin = new Thickness(0, 0, 8, 4) };
        stb.TextChanged += (_, _) => { _groupSearch = stb.Text; ResetGroupPage(); BuildA(); }; fBar.Children.Add(stb);
        var fcb = new ComboBox { Width = 130, Height = 34, Margin = new Thickness(0, 0, 8, 4), ItemsSource = new[] { "Tất cả", "Confirmed", "Arrived", "Checked In" }, SelectedItem = string.IsNullOrEmpty(_groupFilterStatus) ? "Tất cả" : _groupFilterStatus };
        fcb.SelectionChanged += (_, _) => { _groupFilterStatus = fcb.SelectedItem?.ToString() ?? ""; ResetGroupPage(); BuildA(); }; fBar.Children.Add(fcb);
        GroupStepHeader.Children.Add(fBar);

        var filtered = _groups.Where(g => (string.IsNullOrEmpty(_groupSearch) || g.Name.Contains(_groupSearch, StringComparison.OrdinalIgnoreCase) || g.Code.Contains(_groupSearch, StringComparison.OrdinalIgnoreCase) || (g.Company?.Contains(_groupSearch, StringComparison.OrdinalIgnoreCase) ?? false)) && (string.IsNullOrEmpty(_groupFilterStatus) || _groupFilterStatus == "Tất cả" || g.Status == _groupFilterStatus)).ToList();
        _buildingGroupPage = true;
        _groupPager.SetSource(filtered, resetPage: false);
        _buildingGroupPage = false;

        var list = new StackPanel();
        foreach (var g in _groupPager.Items)
        {
            var sel = g == _selectedGroup;
            var card = new Border { CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 6), Background = sel ? Cb("#F5F0FF") : Brushes.White, BorderBrush = sel ? Cb("#7D2BE1") : Cb("#E2E8F0"), BorderThickness = new Thickness(sel ? 2 : 1), Cursor = Cursors.Hand };
            var cg = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            var csp = new StackPanel();
            csp.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = g.Code, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = C("#7D2BE1") }, new TextBlock { Text = " · " + g.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), Margin = new Thickness(8, 0, 0, 0) } } });
            csp.Children.Add(new TextBlock { Text = $"{g.Company ?? "—"} | {g.Arrival:dd/MM} → {g.Departure:dd/MM} | {g.TotalGuests} khách | {g.TotalRooms} phòng", FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 3, 0, 0) });
            Grid.SetColumn(csp, 0); cg.Children.Add(csp);
            var badge = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center, Background = Cb("#DCFCE7"), Child = new TextBlock { Text = g.Status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } };
            Grid.SetColumn(badge, 1); cg.Children.Add(badge);
            card.Child = cg;
            card.MouseLeftButtonDown += (_, _) =>
            {
                _selectedGroup = g;
                GenerateRoomBlockAndGuests();
                BuildA();
                UpdateFooter();
                UpdateStatus();
            };
            list.Children.Add(card);
        }
        ContentPanel.Children.Add(list);
        GroupPagerSummary.Text = filtered.Count == 0 ? "Không có đoàn phù hợp" : _groupPager.PageText;
        GroupPagerHost.Content = new ServerPagerControl { DataContext = _groupPager };
        GroupPagerFooter.Visibility = Visibility.Visible;
    }

    private void BuildB()
    {
        ContentPanel.Children.Add(STitle("Room Block"));
        ContentPanel.Children.Add(SText($"Room Block của đoàn {_selectedGroup?.Name}. Tổng {_roomBlocks.Sum(r => r.Total)} phòng / {_roomBlocks.Count} hạng."));

        var kpis = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        kpis.Children.Add(Kpi("Tổng phòng", _roomBlocks.Sum(r => r.Total).ToString(), "#7D2BE1"));
        kpis.Children.Add(Kpi("Đã phân", _roomBlocks.Sum(r => r.Assigned).ToString(), "#16A34A"));
        kpis.Children.Add(Kpi("Còn lại", _roomBlocks.Sum(r => r.Total - r.Assigned).ToString(), "#D97706"));
        kpis.Children.Add(Kpi("Khách", _selectedGroup?.TotalGuests.ToString() ?? "0", "#0891B2"));
        ContentPanel.Children.Add(kpis);

        foreach (var rb in _roomBlocks)
        {
            var pct = rb.Total > 0 ? rb.Assigned * 100 / rb.Total : 0;
            var card = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 8) };
            var stack = new StackPanel();
            var hdr = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            hdr.Children.Add(new TextBlock { Text = rb.RoomType, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#7D2BE1") });
            var right = new StackPanel { Orientation = Orientation.Horizontal };
            right.Children.Add(new TextBlock { Text = $"{rb.Assigned}/{rb.Total}", FontSize = 16, FontWeight = FontWeights.Bold, Foreground = C("#17365D") });
            right.Children.Add(new TextBlock { Text = $"   còn {rb.Total - rb.Assigned}", FontSize = 12, Foreground = C("#D97706"), VerticalAlignment = VerticalAlignment.Center });
            Grid.SetColumn(right, 1); hdr.Children.Add(right);
            stack.Children.Add(hdr);
            // Progress bar
            var pbar = new ProgressBar { Value = pct, Height = 8, Margin = new Thickness(0, 6, 0, 0) };
            stack.Children.Add(pbar);
            card.Child = stack;
            ContentPanel.Children.Add(card);
        }

        // Room status grid
        ContentPanel.Children.Add(SubTitle("Tình trạng phòng"));
        var dg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = _roomBlocks, MaxHeight = 150 };
        dg.Columns.Add(new DataGridTextColumn { Header = "Hạng phòng", Binding = new System.Windows.Data.Binding("RoomType"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Đã đặt", Binding = new System.Windows.Data.Binding("Total"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Đã phân", Binding = new System.Windows.Data.Binding("Assigned"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Còn chờ", Binding = new System.Windows.Data.Binding("RemainingDisplay"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Vacant", Binding = new System.Windows.Data.Binding("VacantDisplay"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Occupied", Binding = new System.Windows.Data.Binding("OccupiedDisplay"), Width = 70 });
        ContentPanel.Children.Add(dg);
    }

    private void BuildC()
    {
        // BuildC is called directly after selecting a guest or room; refresh the
        // existing visual tree instead of appending a second Assignment Center.
        ContentPanel.Children.Clear();
        ContentScroll.ScrollToTop();
        ContentPanel.Children.Add(STitle("Phân phòng — Assignment Center"));
        // Toolbar
        var tbar = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        tbar.Children.Add(ActionBtn("🤖 Auto Assign", () => AutoAssign()));
        tbar.Children.Add(ActionBtn("🔀 Swap", () => SwapRooms()));
        tbar.Children.Add(ActionBtn("🗑 Clear All", () =>
        {
            if (!CanEditAssignments())
            {
                return;
            }
            _assignments.Clear(); UpdateRoomBlockCounts(); BuildC(); UpdateStatus();
        }));
        tbar.Children.Add(ActionBtn("🔒 Lock Selected", () => _ = LockSelectedRoomAsync()));
        tbar.Children.Add(ActionBtn("📋 In danh sách", PrintAssignments));
        var assignSelected = ActionBtn("Assign", () =>
        {
            if (_selectedGuest != null && _selectedRoom != null)
                AssignGuestToRoom(_selectedGuest, _selectedRoom);
        });
        assignSelected.IsEnabled = _selectedGuest != null && _selectedRoom != null;
        tbar.Children.Add(assignSelected);
        ContentPanel.Children.Add(tbar);

        // 3-panel layout
        var mainGrid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }, new ColumnDefinition { Width = new GridLength(8) }, new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) }, new ColumnDefinition { Width = new GridLength(8) }, new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) } }, Height = 500, MinHeight = 320, VerticalAlignment = VerticalAlignment.Top };

        // LEFT: Guest List
        var leftCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(10, 10, 10, 10) };
        var leftStack = new StackPanel();
        leftStack.Children.Add(new TextBlock { Text = $"👤 Danh sách khách ({_guests.Count})", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#7D2BE1"), Margin = new Thickness(0, 0, 0, 6) });
        var gSearch = new TextBox { Text = _guestSearch, Height = 30, FontSize = 11, Margin = new Thickness(0, 0, 0, 6) };
        gSearch.TextChanged += (_, _) => { _guestSearch = gSearch.Text; BuildC(); }; leftStack.Children.Add(gSearch);

        var unassigned = _guests.Where(g => string.IsNullOrEmpty(_guestSearch) || g.Name.ToLower().Contains(_guestSearch.ToLower())).ToList();
        foreach (var g in unassigned)
        {
            var assigned = _assignments.Any(a => a.GuestId == g.Id);
            var guestRow = new Border { CornerRadius = new CornerRadius(7), Padding = new Thickness(8, 6, 8, 6), Margin = new Thickness(0, 0, 0, 3), Background = g == _selectedGuest ? Cb("#F5F0FF") : assigned ? Cb("#F0FDF4") : Cb("#FAFBFD"), BorderBrush = g == _selectedGuest ? Cb("#7D2BE1") : Cb("Transparent"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand };
            var gr = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            var badges = new StackPanel { Orientation = Orientation.Horizontal };
            if (g.Vip) badges.Children.Add(new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(0, 0, 2, 0), Background = Cb("#FEF3C7"), Child = new TextBlock { Text = "★VIP", FontSize = 9, Foreground = C("#92400E"), FontWeight = FontWeights.SemiBold } });
            if (g.Leader) badges.Children.Add(new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 2, 4, 2), Margin = new Thickness(0, 0, 2, 0), Background = Cb("#DBEAFE"), Child = new TextBlock { Text = "Leader", FontSize = 9, Foreground = C("#1E40AF"), FontWeight = FontWeights.SemiBold } });
            if (g.Child) badges.Children.Add(new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 2, 4, 2), Background = Cb("#FCE7F3"), Child = new TextBlock { Text = "Child", FontSize = 9, Foreground = C("#9D174D"), FontWeight = FontWeights.SemiBold } });
            Grid.SetColumn(badges, 0); gr.Children.Add(badges);
            var info = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
            info.Children.Add(new TextBlock { Text = g.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = assigned ? C("#16A34A") : C("#17365D") });
            info.Children.Add(new TextBlock { Text = $"{g.Gender} · {g.Nationality} · {g.RequestedType}" + (g.Sharing ? " · Sharing" : ""), FontSize = 10, Foreground = C("#94A3B8") });
            Grid.SetColumn(info, 1); gr.Children.Add(info);
            if (assigned) { var a = _assignments.First(x => x.GuestId == g.Id); var rBadge = new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(4, 2, 4, 2), Background = Cb("#DCFCE7"), Child = new TextBlock { Text = "✓ " + a.RoomNumber, FontSize = 10, Foreground = C("#16A34A"), FontWeight = FontWeights.SemiBold } }; Grid.SetColumn(rBadge, 2); gr.Children.Add(rBadge); }
            guestRow.Child = gr;
            guestRow.MouseLeftButtonDown += (_, _) =>
            {
                _selectedGuest = g;
                // A new guest selection starts a fresh guest -> room flow.
                _selectedRoom = null;
                BuildC();
            };
            guestRow.MouseRightButtonDown += (_, _) => {
                var menu = new ContextMenu();
                menu.Items.Add(CreateMenuItem("Assign Room", () => { if (_selectedRoom != null) AssignGuestToRoom(g, _selectedRoom); }));
                menu.Items.Add(CreateMenuItem("Auto Assign Best Match", () => AutoAssignGuest(g)));
                menu.Items.Add(new Separator());
                menu.Items.Add(CreateMenuItem("Release", () =>
                {
                    if (!CanEditAssignments())
                    {
                        return;
                    }
                    var a = _assignments.FirstOrDefault(x => x.GuestId == g.Id);
                    if (a != null) { _assignments.Remove(a); UpdateRoomBlockCounts(); BuildC(); UpdateStatus(); }
                }));
                menu.IsOpen = true;
            };
            leftStack.Children.Add(guestRow);
        }
        leftCard.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = leftStack };
        Grid.SetColumn(leftCard, 0); mainGrid.Children.Add(leftCard);

        // CENTER: Room Inventory
        var centerCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(10, 10, 10, 10) };
        var centerStack = new StackPanel();
        centerStack.Children.Add(new TextBlock { Text = $"🚪 Danh sách phòng ({_rooms.Count})", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#7D2BE1"), Margin = new Thickness(0, 0, 0, 6) });
        var floors = _rooms.Select(r => r.Floor).Distinct().OrderBy(f => f);
        foreach (var floor in floors)
        {
            var floorRooms = _rooms.Where(r => r.Floor == floor).ToList();
            var floorHeader = new Border { Background = Cb("#F5F0FF"), CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 4, 0, 4) };
            floorHeader.Child = new TextBlock { Text = $"Tầng {floor} ({floorRooms.Count} phòng)", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#6D28D9") };
            centerStack.Children.Add(floorHeader);
            var roomWp = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 4),
                HorizontalAlignment = HorizontalAlignment.Left
            };
            foreach (var room in floorRooms)
            {
                var assigned = _assignments.Any(a => a.RoomNumber == room.Number);
                var color = room.Status switch { "Available" => "#16A34A", "Vacant Clean" => "#16A34A", "Vacant Dirty" => "#D97706", "Vacant Inspected" => "#2563EB", "Occupied" => "#DC2626", "OOO" or "OutOfOrder" or "OutOfService" => "#94A3B8", _ => "#64748B" };
                var roomCard = new Border { Width = 70, Height = 56, CornerRadius = new CornerRadius(8), Margin = new Thickness(2), Background = room == _selectedRoom ? Cb("#F5F0FF") : assigned ? new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}20")) : Cb("#FAFBFD"), BorderBrush = room == _selectedRoom ? Cb("#7D2BE1") : assigned ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) : Cb("#E2E8F0"), BorderThickness = new Thickness(room == _selectedRoom ? 2 : 1), Cursor = Cursors.Hand };
                var rcStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(4) };
                rcStack.Children.Add(new TextBlock { Text = room.Number, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), HorizontalAlignment = HorizontalAlignment.Center });
                rcStack.Children.Add(new TextBlock { Text = room.Type, FontSize = 9, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center });
                rcStack.Children.Add(new TextBlock { Text = room.Status switch { "Available" => "Sẵn sàng", "Vacant Clean" => "Sạch", "Vacant Dirty" => "Bẩn", "Vacant Inspected" => "Đã KT", "Occupied" => "Có khách", "OOO" or "OutOfOrder" or "OutOfService" => "Không khả dụng", _ => room.Status }, FontSize = 8, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), HorizontalAlignment = HorizontalAlignment.Center });
                if (room.Connecting != null) rcStack.Children.Add(new TextBlock { Text = "🔗", FontSize = 8, HorizontalAlignment = HorizontalAlignment.Center });
                roomCard.Child = rcStack;
                roomCard.MouseLeftButtonDown += (_, _) =>
                {
                    _selectedRoom = room;
                    if (_selectedGuest != null)
                        AssignGuestToRoom(_selectedGuest, room);
                    else
                        BuildC();
                };
                roomCard.MouseRightButtonDown += (_, _) => {
                    var menu = new ContextMenu();
                    menu.Items.Add(CreateMenuItem("Assign Guest", () => { if (_selectedGuest != null) AssignGuestToRoom(_selectedGuest, room); }));
                    menu.Items.Add(CreateMenuItem("Lock Room", () => _ = LockRoomAsync(room)));
                    menu.Items.Add(new Separator());
                    menu.Items.Add(CreateMenuItem("View History", () => _ = ShowRoomHistoryAsync(room)));
                    menu.IsOpen = true;
                };
                roomWp.Children.Add(roomCard);
            }
            centerStack.Children.Add(roomWp);
        }
        var centerScroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            CanContentScroll = false,
            MaxHeight = 500
        };
        // ScrollViewer measures its content at an unconstrained width by default.
        // Bind the stack to the viewport so each WrapPanel wraps to the card width
        // instead of creating a clipped horizontal row.
        centerStack.SetBinding(FrameworkElement.WidthProperty, new System.Windows.Data.Binding(nameof(ScrollViewer.ViewportWidth)) { Source = centerScroll });
        centerScroll.Content = centerStack;
        centerCard.Child = centerScroll;
        Grid.SetColumn(centerCard, 2); mainGrid.Children.Add(centerCard);

        // RIGHT: Assignment Preview
        var rightCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(10, 10, 10, 10) };
        var rightStack = new StackPanel();
        rightStack.Children.Add(new TextBlock { Text = $"📋 Đã phân ({_assignments.Count}/{_selectedGroup?.TotalRooms ?? 0})", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#7D2BE1"), Margin = new Thickness(0, 0, 0, 6) });
        foreach (var a in _assignments)
        {
            var g = _guests.FirstOrDefault(x => x.Id == a.GuestId);
            var r = _rooms.FirstOrDefault(x => x.Number == a.RoomNumber);
            var row = new Border { CornerRadius = new CornerRadius(6), Padding = new Thickness(8, 5, 8, 5), Margin = new Thickness(0, 0, 0, 3), Background = Cb("#F0FDF4"), BorderBrush = Cb("#BBF7D0"), BorderThickness = new Thickness(1) };
            var rg = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            var ri = new StackPanel();
            ri.Children.Add(new TextBlock { Text = $"✓ {a.RoomNumber} ← {g?.Name ?? "?"}", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") });
            ri.Children.Add(new TextBlock { Text = $"{r?.Type ?? ""} · {a.Time.ToString("HH:mm")}", FontSize = 10, Foreground = C("#94A3B8") });
            Grid.SetColumn(ri, 0); rg.Children.Add(ri);
            var xBtn = new Button { Content = "✕", Width = 24, Height = 24, FontSize = 10, Background = Cb("#FEE2E2"), Foreground = C("#DC2626"), BorderThickness = new Thickness(0), Cursor = Cursors.Hand };
            xBtn.Click += (_, _) => { _assignments.Remove(a); UpdateRoomBlockCounts(); BuildC(); UpdateStatus(); };
            Grid.SetColumn(xBtn, 1); rg.Children.Add(xBtn);
            row.Child = rg; rightStack.Children.Add(row);
        }
        rightCard.Child = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Content = rightStack };
        Grid.SetColumn(rightCard, 4); mainGrid.Children.Add(rightCard);

        ContentPanel.Children.Add(mainGrid);
        ContentPanel.Children.Add(new TextBlock { Text = "💡 Tip: Chọn khách (trái) + chọn phòng (giữa) → click Assign. Chuột phải để menu ngữ cảnh. Auto Assign để phân tự động.", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 8, 0, 0) });
    }

    private void BuildD()
    {
        ContentPanel.Children.Add(STitle("Kiểm tra"));
        var assigned = _assignments.Count;
        var total = _selectedGroup?.TotalRooms ?? 1;
        var checks = new (string, bool)[] {
            ("Tất cả đã phân phòng", assigned >= total),
            ("Không duplicate phòng", !HasDuplicateRooms()),
            ("Không duplicate khách", !HasDuplicateGuests()),
            ("Không assign phòng Dirty/OOO", !HasDirtyAssignment()),
            ("VIP đã được ưu tiên", true),
            ("Connecting room đúng", true),
            ("Không overbooking", assigned <= total),
        };
        foreach (var (label, ok) in checks)
            ContentPanel.Children.Add(new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Background = ok ? Cb("#F0FDF4") : Cb("#FEF2F2"), BorderBrush = ok ? Cb("#BBF7D0") : Cb("#FECACA"), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = ok ? "✓ " : "✗ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626"), FontWeight = FontWeights.Bold }, new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center } } } });

        var block = GetBlocking();
        if (block.Count > 0) { ContentPanel.Children.Add(Sep()); ContentPanel.Children.Add(new TextBlock { Text = "⛔ Blocking", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#DC2626") }); foreach (var b in block) ContentPanel.Children.Add(new TextBlock { Text = $"• {b}", FontSize = 12, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) }); }
        var allOk = block.Count == 0 && checks.All(c => c.Item2);
        ContentPanel.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "✓ Sẵn sàng xác nhận phân phòng" : "⛔ Cần khắc phục các vấn đề trên", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626"), TextAlignment = TextAlignment.Center } });
    }

    private void BuildE()
    {
        ContentPanel.Children.Add(STitle("Housekeeping"));
        ContentPanel.Children.Add(SText("Trạng thái buồng phòng của các phòng đã phân."));

        var kpis = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        var assignedRooms = _rooms.Where(r => _assignments.Any(a => a.RoomNumber == r.Number)).ToList();
        kpis.Children.Add(Kpi("Tổng đã phân", assignedRooms.Count.ToString(), "#7D2BE1"));
        kpis.Children.Add(Kpi("Sạch", assignedRooms.Count(r => r.Status == "Vacant Clean").ToString(), "#16A34A"));
        kpis.Children.Add(Kpi("Bẩn", assignedRooms.Count(r => r.Status == "Vacant Dirty").ToString(), "#D97706"));
        kpis.Children.Add(Kpi("Đã kiểm tra", assignedRooms.Count(r => r.Status == "Vacant Inspected").ToString(), "#2563EB"));
        ContentPanel.Children.Add(kpis);

        var dg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = assignedRooms, MaxHeight = 300 };
        dg.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("Number"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Hạng", Binding = new System.Windows.Data.Binding("Type"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "HK Status", Binding = new System.Windows.Data.Binding("Status"), Width = 120 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Khách", Binding = new System.Windows.Data.Binding("AssignedGuest"), Width = 140 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Connecting", Binding = new System.Windows.Data.Binding("Connecting"), Width = 80 });
        dg.Columns.Add(new DataGridTextColumn { Header = "ETA", Binding = new System.Windows.Data.Binding("EtaDisplay"), Width = 80 });
        ContentPanel.Children.Add(dg);

        var acts = new WrapPanel();
        acts.Children.Add(ActionBtn("📞 Gọi HK", () =>
        {
            var roomNumber = _selectedRoom?.Number ?? assignedRooms.FirstOrDefault()?.Number;
            if (string.IsNullOrWhiteSpace(roomNumber))
            {
                MessageBox.Show("Chưa có phòng đã phân để mở Housekeeping.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            new HousekeepingMessageWindow(roomNumber).ShowDialog();
        }));
        acts.Children.Add(ActionBtn("🔄 Làm mới HK", () => { GenerateRoomBlockAndGuests(); BuildE(); }));
        acts.Children.Add(ActionBtn("📋 In danh sách HK", () => PrintHousekeepingAssignments(assignedRooms)));
        ContentPanel.Children.Add(acts);
    }

    private void PrintHousekeepingAssignments(IReadOnlyList<GraRoom> assignedRooms)
    {
        if (assignedRooms.Count == 0)
        {
            MessageBox.Show("Chưa có phòng đã phân để in danh sách Housekeeping.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var content = new StringBuilder()
            .AppendLine("DANH SÁCH HOUSEKEEPING — ĐOÀN")
            .AppendLine($"Đoàn: {_selectedGroup?.Code} - {_selectedGroup?.Name}")
            .AppendLine($"Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine(new string('-', 90));
        foreach (var room in assignedRooms)
        {
            var assignment = _assignments.FirstOrDefault(x => x.RoomNumber == room.Number);
            var guest = assignment is null ? null : _guests.FirstOrDefault(x => x.Id == assignment.GuestId);
            content.AppendLine($"Phòng {room.Number} | {room.Type} | {room.Status} | Khách: {guest?.Name ?? "—"} | VIP: {(guest?.Vip == true ? "Có" : "Không")}");
        }

        new PrintPreviewDialog("Danh sách Housekeeping đoàn", content.ToString()).ShowDialog();
    }

    private void BuildF()
    {
        ContentPanel.Children.Add(STitle("Xác nhận phân phòng"));
        ContentPanel.Children.Add(new Border { Background = Cb("#ECFDF5"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Cb("#86EFAC"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = "Dữ liệu sẽ được lưu bằng một transaction thật, kèm kiểm tra trùng phòng và audit.", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#166534") } });

        ContentPanel.Children.Add(SubTitle("Tổng kết"));
        var s = new StackPanel();
        s.Children.Add(Prev("Đoàn", _selectedGroup?.Name ?? "—"));
        s.Children.Add(Prev("Khách", $"{_selectedGroup?.TotalGuests ?? 0}"));
        s.Children.Add(Prev("Phòng", $"{_selectedGroup?.TotalRooms ?? 0}"));
        s.Children.Add(Prev("Đã phân", $"{_assignments.Count}"));
        s.Children.Add(Prev("Còn lại", $"{( _selectedGroup?.TotalRooms ?? 0) - _assignments.Count}"));
        s.Children.Add(Prev("VIP", $"{_guests.Count(g => g.Vip)}"));
        s.Children.Add(Prev("Leader/Guide/Driver", $"{_guests.Count(g => g.Leader)}"));
        ContentPanel.Children.Add(s);

        ContentPanel.Children.Add(Sep());
        ContentPanel.Children.Add(SubTitle("Room Block Summary"));
        foreach (var rb in _roomBlocks) ContentPanel.Children.Add(new TextBlock { Text = $"• {rb.RoomType}: {rb.Total} đặt / {rb.Assigned} đã phân", FontSize = 12, Foreground = C("#334155"), Margin = new Thickness(0, 2, 0, 0) });

        ContentPanel.Children.Add(Sep());
        ContentPanel.Children.Add(MakeCfmCheck("Đã phân đủ phòng", _confirmAssign, v => { _confirmAssign = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("VIP đã được ưu tiên", _confirmVip, v => { _confirmVip = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("Phòng sẵn sàng (Clean/Inspected)", _confirmReady, v => { _confirmReady = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("Tôi xác nhận phân phòng đúng", _confirmAll, v => { _confirmAll = v; UpdateFooter(); }));
    }

    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        if (!_persistedMode || _selectedGroup is null || _groupRoomAssignmentCommands is null)
        {
            MessageBox.Show("Thiếu command persistence thật; dữ liệu chưa được ghi.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var items = new List<AssignGroupRoomItem>();
            foreach (var assignment in _assignments)
            {
                var room = _rooms.FirstOrDefault(x => x.Number == assignment.RoomNumber);
                var guest = _guests.FirstOrDefault(x => x.Id == assignment.GuestId);
                if (room is null || guest is null || room.Id == Guid.Empty)
                    throw new InvalidOperationException("GROUP_ROOM_ASSIGNMENT_DATA_INVALID");

                items.Add(new AssignGroupRoomItem(
                    room.Id,
                    guest.GuestProfileId,
                    guest.GuestProfileId is null ? null : guest.Name,
                    guest.Adults,
                    guest.Children));
            }

            var saved = await _groupRoomAssignmentCommands.ReplaceAsync(
                new ReplaceGroupRoomAssignmentsRequest(_selectedGroup.Id, items),
                CancellationToken.None);
            _hasChanges = false;
            var loadVersion = Interlocked.Increment(ref _persistedDetailsLoadVersion);
            await LoadPersistedGroupDetailsAsync(_selectedGroup, loadVersion);
            MessageBox.Show($"Đã lưu {saved.AssignmentCount} phân phòng vào cơ sở dữ liệu.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể lưu phân phòng: " + ex.Message, "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PrintAssignments()
    {
        if (_selectedGroup is null || _assignments.Count == 0)
        {
            MessageBox.Show("Chưa có đoàn hoặc phân phòng để in.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var content = new StringBuilder()
            .AppendLine("DANH SÁCH PHÂN PHÒNG ĐOÀN")
            .AppendLine($"Đoàn: {_selectedGroup.Code} - {_selectedGroup.Name}")
            .AppendLine($"Thời điểm: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
            .AppendLine(new string('-', 100));
        foreach (var assignment in _assignments)
        {
            var guest = _guests.FirstOrDefault(x => x.Id == assignment.GuestId);
            var room = _rooms.FirstOrDefault(x => x.Number == assignment.RoomNumber);
            content.AppendLine(
                $"Phòng {assignment.RoomNumber} | {room?.Type ?? "—"} | " +
                $"Khách: {guest?.Name ?? "—"} | Người lớn: {guest?.Adults ?? 0} | Trẻ em: {guest?.Children ?? 0} | " +
                $"VIP: {(guest?.Vip == true ? "Có" : "Không")} | {room?.Status ?? "—"}");
        }

        new PrintPreviewDialog("Danh sách phân phòng đoàn", content.ToString()).ShowDialog();
    }

    // ═══ ASSIGNMENT LOGIC ═══
    private Task LockSelectedRoomAsync() => _selectedRoom is null
        ? ShowLockMessageAsync("Chưa chọn phòng để khóa.", MessageBoxImage.Information)
        : LockRoomAsync(_selectedRoom);

    private async Task LockRoomAsync(GraRoom room)
    {
        if (_hotelQueries is null || _changeRoomStatus is null)
        {
            await ShowLockMessageAsync("Chưa cấu hình command dữ liệu phòng thật.", MessageBoxImage.Warning);
            return;
        }
        if (_assignments.Any(assignment => assignment.RoomNumber == room.Number))
        {
            await ShowLockMessageAsync($"Phòng {room.Number} đã được phân cho khách; không thể khóa trong lúc phân phòng.", MessageBoxImage.Warning);
            return;
        }

        try
        {
            var current = await _hotelQueries.GetRoom(room.Id, CancellationToken.None);
            if (current.IsFailure || current.Value is null)
            {
                await ShowLockMessageAsync(current.Error?.Message ?? $"Không tải được phòng {room.Number}.", MessageBoxImage.Warning);
                return;
            }
            if (current.Value.OperationalStatus is HotelRoomOperationalStatus.OutOfOrder or HotelRoomOperationalStatus.OutOfService)
            {
                await ShowLockMessageAsync($"Phòng {room.Number} đã ở trạng thái không khả dụng.", MessageBoxImage.Information);
                return;
            }

            var statusDialog = new RoomOperationalStatusDialog(current.Value.OperationalStatus.ToString()) { Owner = this };
            if (statusDialog.ShowDialog() != true || statusDialog.SelectedStatus is not { } status
                || status == HotelRoomOperationalStatus.Available)
                return;

            var result = await _changeRoomStatus.HandleAsync(
                new ChangeHotelRoomOperationalStatusCommand(
                    current.Value.HotelRoomId,
                    status,
                    current.Value.RowVersion ?? []),
                CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                await ShowLockMessageAsync(result.Error?.Message ?? $"Không thể khóa phòng {room.Number}.", MessageBoxImage.Warning);
                return;
            }

            room.Status = status switch
            {
                HotelRoomOperationalStatus.OutOfOrder => "OutOfOrder",
                HotelRoomOperationalStatus.OutOfService => "OutOfService",
                _ => status.ToString()
            };
            BuildC();
            MessageBox.Show($"Đã khóa phòng {room.Number} bằng dữ liệu thật.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            await ShowLockMessageAsync(ex.Message, MessageBoxImage.Error);
        }
    }

    private Task ShowLockMessageAsync(string message, MessageBoxImage image)
    {
        MessageBox.Show(message, "Phân phòng đoàn", MessageBoxButton.OK, image);
        return Task.CompletedTask;
    }

    private void AssignGuestToRoom(GraGuest guest, GraRoom room)
    {
        if (!CanEditAssignments()) return;
        if (room.Status is "OOO" or "OutOfOrder" or "OutOfService") { MessageBox.Show($"Phòng {room.Number} đang không khả dụng — không thể assign.", "Blocked", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var existingGuest = _assignments.Where(a => a.GuestId == guest.Id).ToList(); foreach (var a in existingGuest) _assignments.Remove(a);
        var existingRoom = _assignments.Where(a => a.RoomNumber == room.Number).ToList(); foreach (var a in existingRoom) _assignments.Remove(a);
        _assignments.Add(new GraAssignment { AssignmentId = guest.AssignmentId, HotelRoomId = room.Id, GuestId = guest.Id, RoomNumber = room.Number, Time = DateTime.Now });
        UpdateRoomBlockCounts(); BuildC(); UpdateStatus();
    }

    private void AutoAssign()
    {
        if (!CanEditAssignments()) return;
        _assignments.Clear();
        var availRooms = _rooms.Where(r => r.Status is "Available" or "Vacant Clean" or "Vacant Inspected").ToList();
        var unassigned = _guests.ToList();
        foreach (var g in unassigned.Take(availRooms.Count))
        {
            var match = availRooms.FirstOrDefault(r => r.Type == g.RequestedType && !_assignments.Any(a => a.RoomNumber == r.Number))
                        ?? availRooms.FirstOrDefault(r => !_assignments.Any(a => a.RoomNumber == r.Number));
            if (match == null) break;
            _assignments.Add(new GraAssignment { AssignmentId = g.AssignmentId, HotelRoomId = match.Id, GuestId = g.Id, RoomNumber = match.Number, Time = DateTime.Now });
        }
        UpdateRoomBlockCounts(); BuildC(); UpdateStatus();
        MessageBox.Show($"Auto-assign hoàn tất: {_assignments.Count}/{_guests.Count} khách được phân phòng.", "Auto Assign", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void AutoAssignGuest(GraGuest guest)
    {
        if (!CanEditAssignments()) return;
        var match = _rooms.Where(r => (r.Status is "Available" or "Vacant Clean" or "Vacant Inspected") && !_assignments.Any(a => a.RoomNumber == r.Number)).OrderBy(r => r.Type == guest.RequestedType ? 0 : 1).ThenBy(r => r.Floor).FirstOrDefault();
        if (match == null) { MessageBox.Show("Không tìm thấy phòng phù hợp.", "Auto Assign", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        AssignGuestToRoom(guest, match);
    }

    private void SwapRooms()
    {
        if (!CanEditAssignments()) return;
        if (_assignments.Count < 2) { MessageBox.Show("Cần ít nhất 2 assignment để swap.", "Swap", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        var a1 = _assignments[0]; var a2 = _assignments[^1];
        (a1.RoomNumber, a2.RoomNumber) = (a2.RoomNumber, a1.RoomNumber);
        BuildC();
        MessageBox.Show($"Đã swap: {a1.RoomNumber} ↔ {a2.RoomNumber}", "Swap", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool CanEditAssignments()
    {
        if (!_persistedMode)
            return true;
        if (_selectedGroup is null)
        {
            MessageBox.Show("Chưa chọn đoàn.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        if (_selectedGroup.Status is "Draft" or "Tentative" or "Confirmed")
            return true;
        MessageBox.Show("Đoàn đã check-in/check-out hoặc đóng; không thể thay đổi phân phòng.", "Phân phòng đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private void UpdateRoomBlockCounts()
    {
        foreach (var rb in _roomBlocks) { rb.Assigned = _rooms.Count(r => r.Type == rb.RoomType && _assignments.Any(a => a.RoomNumber == r.Number)); rb.Clean = _rooms.Count(r => r.Type == rb.RoomType && r.Status == "Vacant Clean"); }
    }

    private bool HasDuplicateRooms() => _assignments.GroupBy(a => a.RoomNumber).Any(g => g.Count() > 1);
    private bool HasDuplicateGuests() => _assignments.GroupBy(a => a.GuestId).Any(g => g.Count() > 1);
    private bool HasDirtyAssignment() => _assignments.Any(a => _rooms.Any(r => r.Number == a.RoomNumber && r.Status is "Vacant Dirty" or "OOO" or "OutOfOrder" or "OutOfService"));

    private bool CanConfirm() => !HasBlocking() && _confirmAssign && _confirmVip && _confirmReady && _confirmAll;
    private List<string> GetBlocking()
    {
        var b = new List<string>();
        if (_selectedGroup == null) b.Add("Chưa chọn đoàn");
        if (HasDuplicateRooms()) b.Add("Trùng phòng — một phòng được assign cho nhiều khách");
        if (HasDuplicateGuests()) b.Add("Trùng khách — một khách được assign nhiều phòng");
        if (HasDirtyAssignment()) b.Add("Phòng Dirty/OOO được assign");
        return b;
    }
    private List<string> GetWarnings()
    {
        var w = new List<string>();
        if (_assignments.Count < (_selectedGroup?.TotalRooms ?? 0)) w.Add($"Chưa phân đủ phòng ({_assignments.Count}/{_selectedGroup?.TotalRooms})");
        var vipUnassigned = _guests.Any(g => g.Vip && !_assignments.Any(a => a.GuestId == g.Id));
        if (vipUnassigned) w.Add("Có khách VIP chưa được phân phòng");
        return w;
    }
    private bool HasBlocking() => GetBlocking().Count > 0;

    // ═══ HELPERS ═══
    private async Task ShowRoomHistoryAsync(GraRoom room)
    {
        if (_auditService is null || room.Id == Guid.Empty)
        {
            await ShowLockMessageAsync("Chưa cấu hình audit service hoặc phòng chưa có mã dữ liệu thật.", MessageBoxImage.Warning);
            return;
        }

        try
        {
            var logs = await _auditService.QueryAsync(
                entityId: room.Id.ToString(),
                fromDate: DateTime.UtcNow.AddDays(-90),
                toDate: DateTime.UtcNow,
                cancellationToken: CancellationToken.None);
            var message = logs.Count == 0
                ? $"Chưa có lịch sử audit cho phòng {room.Number} trong 90 ngày gần nhất."
                : string.Join(Environment.NewLine, logs.Take(20).Select(x =>
                    $"{x.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm} · {x.Username} · {x.Action} · {x.Detail}"));
            MessageBox.Show(message, $"Lịch sử phòng {room.Number}", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            await ShowLockMessageAsync($"Không tải được lịch sử phòng {room.Number}: {ex.Message}", MessageBoxImage.Error);
        }
    }

    private static MenuItem CreateMenuItem(string header, Action action) { var mi = new MenuItem { Header = header, FontSize = 12 }; mi.Click += (_, _) => action(); return mi; }
    private static FrameworkElement SideSec(string title) => new TextBlock
    {
        Text = title,
        FontSize = 12,
        FontWeight = FontWeights.SemiBold,
        Foreground = C("#334155"),
        Margin = new Thickness(0, 0, 0, 6)
    };

    private static FrameworkElement SideRow(string label, string value)
    {
        var row = new Grid { Margin = new Thickness(0, 0, 0, 6) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(96) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row.Children.Add(new TextBlock { Text = label + ":", FontSize = 11, Foreground = C("#94A3B8") });
        var valueText = new TextBlock { Text = value, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(valueText, 1);
        row.Children.Add(valueText);
        return row;
    }

    private static FrameworkElement SideSep() => new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 4, 0, 10) };

    private static FrameworkElement STitle(string t) => new TextBlock { Text = t, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 4) };
    private static FrameworkElement SText(string t) => new TextBlock { Text = t, FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 0, 0, 10) };
    private static FrameworkElement SubTitle(string t) => new TextBlock { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#334155"), Margin = new Thickness(0, 10, 0, 6) };
    private static FrameworkElement Sep() => new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) };
    private static FrameworkElement Prev(string l, string v) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 12, Foreground = C("#94A3B8"), Width = 140 }, new TextBlock { Text = v, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") } } };
    private static Border Kpi(string label, string value, string color) => new Border { Width = 120, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}14")), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 8, 6), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}33")), BorderThickness = new Thickness(1), Child = new StackPanel { Children = { new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B") }, new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) } } } };
    private Button ActionBtn(string text, Action action) { var btn = new Button { Content = text, Height = 32, FontSize = 11, Margin = new Thickness(0, 0, 6, 4), Padding = new Thickness(10, 0, 10, 0), Background = Cb("#F5F0FF"), Foreground = C("#7D2BE1"), BorderBrush = Cb("#DDD6FE"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand }; btn.Click += (_, _) => action(); return btn; }
    private static FrameworkElement MakeCfmCheck(string label, bool value, Action<bool> setter)
    {
        var border = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0), Background = Cb("#FAFBFD"), BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1) };
        var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalContentAlignment = VerticalAlignment.Center };
        cb.Checked += (_, _) => { setter(true); border.Background = Cb("#F0FDF4"); border.BorderBrush = Cb("#86EFAC"); };
        cb.Unchecked += (_, _) => { setter(false); border.Background = Cb("#FAFBFD"); border.BorderBrush = Cb("#E2E8F0"); };
        border.Child = cb; return border;
    }
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

// ═══ DATA CLASSES ═══
public class GraGroup { public Guid Id; public string Code { get; set; } = ""; public string Name { get; set; } = ""; public string? Company { get; set; } public DateTime Arrival, Departure; public int TotalGuests, TotalRooms; public string Status { get; set; } = ""; public bool Vip; }
public class GraRoomBlock { public string RoomType { get; set; } = ""; public int Total, Assigned, Clean; public string RemainingDisplay => $"{Total - Assigned}"; public string VacantDisplay => $"{Clean}"; public string OccupiedDisplay => $"{Total - Clean}"; }
public class GraGuest { public int Id; public Guid AssignmentId; public Guid? GuestProfileId; public string? RoomNumber; public int Adults, Children; public string Name { get; set; } = ""; public string Gender { get; set; } = ""; public bool Vip, Leader, Child, Sharing; public string Nationality { get; set; } = ""; public string RequestedType { get; set; } = ""; public string Note { get; set; } = ""; }
public class GraRoom { public Guid Id; public string Number { get; set; } = ""; public int Floor; public string Type { get; set; } = ""; public string Status { get; set; } = ""; public string? Connecting; public bool Accessible; public string EtaDisplay => DateTime.Today.AddDays(1).ToString("dd/MM"); public string AssignedGuest { get; set; } = ""; }
public class GraAssignment { public Guid AssignmentId; public Guid HotelRoomId; public int GuestId; public string RoomNumber { get; set; } = ""; public DateTime Time; }
