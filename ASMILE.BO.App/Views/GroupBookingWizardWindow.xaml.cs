using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.App.Controls;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Application.FrontOffice.Rate;
using ASMILE.BO.Core.FrontOffice.Rate;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class GroupBookingWizardWindow : Window
{
    private int _step;
    private bool _hasChanges;

    // Step A — Group selection
    private GbGroup? _selectedGroup;
    private readonly List<GbGroup> _groups = new();
    private string _groupSearch = "", _groupFilterStatus = "";
    private readonly TablePagerViewModel<GbGroup> _groupPager = new() { ItemName = "đoàn" };
    private bool _buildingGroupPage;

    // Real application services. The wizard must fail closed when the
    // execution context or persistence services are unavailable.
    private readonly IGroupReservationQueryService? _groupQuery;
    private readonly IGroupReservationCommandService? _groupCommands;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IHotelRoomMasterDataRepository? _roomTypes;
    private readonly IRateRepository? _rates;
    private readonly IServiceScope? _serviceScope;
    private readonly Dictionary<string, Guid> _roomTypeIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<RatePlan> _availableRatePlans = new();
    private bool _loadingRealData;
    private string? _realDataError;

    // Step B — Room Block
    private readonly ObservableCollection<GbRoomBlock> _roomBlocks = new();

    // Step C — Rate
    private string _rateCurrency = "VND";
    private readonly ObservableCollection<GbRateLine> _rateLines = new();
    private decimal _taxPct = 8, _serviceChargePct = 5;
    private bool _taxIncluded;

    // Step D — Deposit
    private bool _depositRequired = true;
    private readonly ObservableCollection<GbDepositSchedule> _depositSchedules = new();
    private DateTime? _cutoffDate, _releaseDate;
    private string _guaranteeType = "";

    // Step F — Policies
    private bool _lateCheckout, _earlyCheckin, _freeUpgrade, _complimentaryFoc;
    private int _tourLeaderRooms, _driverRooms, _guideRooms;
    private bool _breakfast, _lunch, _dinner, _airportPickup, _welcomeDrink, _meetingRoom, _laundrySvc, _spaSvc;
    private string _specialRequest = "";

    // Step G/H
    private bool _confirmBlock, _confirmRate, _confirmDeposit, _confirmRooming, _confirmPolicies, _confirmAll;

    // UI
    private Border[] _stepBoxes = new Border[8];
    private TextBlock[] _stepLbls = new TextBlock[8];
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F", "G", "H" };
    private static readonly string[] StepTitles = { "Chọn đoàn", "Nhu cầu phòng", "Giá đoàn", "Đặt cọc & Bảo đảm", "Rooming List", "Chính sách đoàn", "Kiểm tra", "Xác nhận" };

    public GroupBookingWizardWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _groupQuery = scopedServices?.GetService<IGroupReservationQueryService>();
        _groupCommands = scopedServices?.GetService<IGroupReservationCommandService>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _roomTypes = scopedServices?.GetService<IHotelRoomMasterDataRepository>();
        _rates = scopedServices?.GetService<IRateRepository>();
        InitializeRealDataState();
        _groupPager.PropertyChanged += OnGroupPagerChanged;
        BuildStepBar();
        GoTo(0);
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _serviceScope?.Dispose();
    }

    private void InitializeRealDataState()
    {
        _groups.Clear();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_loadingRealData) return;
        _loadingRealData = true;
        try
        {
            await LoadGroupsAsync();
        }
        finally
        {
            _loadingRealData = false;
        }
    }

    private async Task LoadGroupsAsync()
    {
        _realDataError = null;
        _groups.Clear();
        var propertyId = _contextAccessor?.Current?.Property.PropertyId.Value;
        if (propertyId is null || _groupQuery is null)
        {
            _realDataError = "Chưa có execution context hoặc query đoàn thật.";
            BuildA();
            return;
        }

        try
        {
            var groups = await _groupQuery.GetGroupsAsync(propertyId.Value, CancellationToken.None);
            _groups.AddRange(groups.Select(MapGroup));
        }
        catch (Exception ex)
        {
            _realDataError = ex.Message;
        }

        ResetGroupPage();
        BuildA();
        UpdateFooter();
    }

    private static GbGroup MapGroup(GroupReservationSummaryDto group) => new()
    {
        Id = group.GroupReservationId,
        Code = group.GroupCode,
        Name = group.GroupName,
        Company = group.CompanyName,
        Agent = group.AgencyName,
        Arrival = group.ArrivalDate.ToDateTime(TimeOnly.MinValue),
        Departure = group.DepartureDate.ToDateTime(TimeOnly.MinValue),
        TotalGuests = group.TotalAdults + group.TotalChildren,
        ExpectedRooms = group.ExpectedRooms,
        Leader = group.ContactPerson,
        Status = group.Status.ToString(),
        Market = group.MarketSegment,
        Vip = group.IsVip ? "VIP" : null,
        SpecialRequest = null
    };

    // ═══ STEP BAR ═══
    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        for (int i = 0; i < 8; i++)
        {
            var idx = i;
            var lbl = new TextBlock { Text = "Chưa bắt đầu", FontSize = 9, Foreground = C("#64748B") };
            _stepLbls[i] = lbl;
            var letter = new Border { Width = 28, Height = 28, CornerRadius = new CornerRadius(7), Background = Cb("#F1F5F9"), Child = new TextBlock { Text = StepLetters[i], FontSize = 13, FontWeight = FontWeights.Bold, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
            var box = new Border { Width = 142, Height = 60, CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 2, 0), Background = Brushes.White, BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand, Padding = new Thickness(10, 7, 10, 7), Child = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition() }, Children = { letter, new StackPanel { Margin = new Thickness(8, 0, 0, 0), Children = { new TextBlock { Text = StepTitles[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#64748B") }, lbl } } } } };
            Grid.SetColumn(letter, 0); Grid.SetColumn((StackPanel)((Grid)box.Child).Children[1], 1);
            box.MouseLeftButtonDown += (_, _) => { if (idx <= _step || CanEnter(idx)) GoTo(idx); };
            _stepBoxes[i] = box;
            StepBar.Children.Add(box);
            if (i < 7) StepBar.Children.Add(new TextBlock { Text = "▸", FontSize = 14, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });
        }
    }

    private void RefreshStepper()
    {
        for (int i = 0; i < 8; i++)
        {
            if (i < _step) { _stepBoxes[i].Background = Cb("#F0FDF4"); _stepBoxes[i].BorderBrush = Cb("#86EFAC"); _stepBoxes[i].BorderThickness = new Thickness(1); _stepLbls[i].Text = "Hoàn thành"; _stepLbls[i].Foreground = C("#166534"); }
            else if (i == _step) { _stepBoxes[i].Background = Cb("#FFF7ED"); _stepBoxes[i].BorderBrush = Cb("#ED7900"); _stepBoxes[i].BorderThickness = new Thickness(1.5); _stepLbls[i].Text = "Đang nhập"; _stepLbls[i].Foreground = C("#C2410C"); }
            else { _stepBoxes[i].Background = Brushes.White; _stepBoxes[i].BorderBrush = Cb("#E2E8F0"); _stepBoxes[i].BorderThickness = new Thickness(1); _stepLbls[i].Text = "Chưa bắt đầu"; _stepLbls[i].Foreground = C("#64748B"); }
        }
    }

    // ═══ NAVIGATION ═══
    private bool CanEnter(int s) => s switch
    {
        0 => true,
        1 => _selectedGroup != null,
        2 => _selectedGroup != null,
        3 => _selectedGroup != null,
        4 => true,
        5 => true,
        6 => true,
        7 => !HasBlocking(),
        _ => false
    };

    private bool CanProceed() => _step switch
    {
        0 => _selectedGroup != null,
        1 => _roomBlocks.Count > 0 && _roomBlocks.Sum(r => r.Qty) > 0,
        // A rate table may legitimately be empty for a newly selected group;
        // allow the operator to continue to deposit/guarantee setup and fill
        // the rates later in the workflow.
        2 => _selectedGroup != null && !string.IsNullOrEmpty(_rateCurrency),
        3 => true,
        4 => true,
        5 => true,
        6 => !HasBlocking(),
        7 => false,
        _ => false
    };

    private void GoTo(int s)
    {
        _step = s; RefreshStepper(); BuildContent(s); UpdateSidebar(s); UpdateFooter(); UpdateStatus();
    }

    private void OnNext(object s, RoutedEventArgs e)
    {
        if (_step < 7 && CanProceed())
        {
            _hasChanges = true;
            if (_step == 1) _ = LoadRatesAsync();
            GoTo(_step + 1);
        }
    }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 0) GoTo(_step - 1); }
    private void OnCancel(object s, RoutedEventArgs e) { if (_hasChanges) { if (MessageBox.Show("Huỷ đặt phòng đoàn đang thực hiện?", "Xác nhận", MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return; } Close(); }
    private void OnClose(object s, RoutedEventArgs e) => OnCancel(s, e);
    private void OnRefresh(object s, RoutedEventArgs e)
    {
        _hasChanges = false;
        _selectedGroup = null;
        _roomBlocks.Clear();
        _rateLines.Clear();
        _availableRatePlans.Clear();
        ResetGroupPage();
        GoTo(0);
        _ = LoadGroupsAsync();
    }

    private async void OnSaveDraft(object s, RoutedEventArgs e)
    {
        await SaveBookingAsync(confirm: false);
    }
    private void OnWindowKeyDown(object s, KeyEventArgs e) { if (e.Key == Key.Escape) OnCancel(s, e); else if (e.Key == Key.Enter && CanProceed() && _step < 7) OnNext(s, e); else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) OnSaveDraft(s, e); }

    private void OnGroupPagerChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_buildingGroupPage || _step != 0 || (e.PropertyName != nameof(TablePagerViewModel<GbGroup>.CurrentPage) && e.PropertyName != nameof(TablePagerViewModel<GbGroup>.SelectedPageSize)))
        {
            return;
        }

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
        FooterLabel.Text = $"Bước {_step + 1}/8 · {StepTitles[_step]}";
        BtnBack.IsEnabled = _step > 0;
        var warn = GetWarnings(); var block = GetBlocking();
        FooterWarn.Visibility = warn.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        FooterWarn.Text = $"⚠ {warn.Count} cảnh báo";
        FooterBlock.Visibility = block.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        FooterBlock.Text = $"⛔ {block.Count} chặn";
        if (_step == 7) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = CanConfirm(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; BtnNext.IsEnabled = CanProceed(); }
        BtnSaveDraft.IsEnabled = _hasChanges;
    }

    private void UpdateStatus()
    {
        StatusPanel.Children.Clear();
        if (_selectedGroup != null)
        {
            StatusPanel.Children.Add(StatusChip("Đoàn", _selectedGroup.Name, "#ED7900"));
            StatusPanel.Children.Add(StatusChip("Đến", _selectedGroup.Arrival.ToString("dd/MM"), "#2563EB"));
            StatusPanel.Children.Add(StatusChip("Đi", _selectedGroup.Departure.ToString("dd/MM"), "#7C3AED"));
            StatusPanel.Children.Add(StatusChip("Khách", _selectedGroup.TotalGuests.ToString(), "#0891B2"));
        }
        if (_roomBlocks.Count > 0)
        {
            var totalRooms = _roomBlocks.Sum(r => r.Qty);
            StatusPanel.Children.Add(StatusChip("Phòng", totalRooms.ToString(), "#16A34A"));
            if (_rateLines.Count > 0)
            {
                var est = _roomBlocks.Sum(rb => { var rate = _rateLines.FirstOrDefault(rl => rl.RoomType == rb.RoomType); return rb.Qty * (rate?.FinalRate ?? 0) * (int)(_selectedGroup?.Departure.Subtract(_selectedGroup?.Arrival ?? DateTime.Today).TotalDays ?? 1); });
                StatusPanel.Children.Add(StatusChip("Doanh thu", $"{est:N0}", "#D97706"));
            }
        }
        if (_selectedGroup != null) StatusPanel.Children.Add(StatusChip("Tiền tệ", _rateCurrency, "#64748B"));
        if (_depositRequired) StatusPanel.Children.Add(StatusChip("Đặt cọc", "Bắt buộc", "#DC2626"));
        if (StatusPanel.Children.Count == 0) StatusPanel.Children.Add(new TextBlock { Text = "Chọn đoàn để bắt đầu đặt phòng", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center });
    }

    private static Border StatusChip(string label, string value, string color)
    {
        return new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}1A")), CornerRadius = new CornerRadius(7), Padding = new Thickness(9, 4, 9, 4), Margin = new Thickness(0, 0, 7, 0), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}33")), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = label + ": ", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), FontWeight = FontWeights.SemiBold }, new TextBlock { Text = value, FontSize = 11, Foreground = C("#334155") } } } };
    }

    // ═══ CONTENT BUILDERS ═══
    private void BuildContent(int s)
    {
        ContentPanel.Children.Clear();
        GroupStepHeader.Children.Clear();
        GroupStepHeader.Visibility = Visibility.Collapsed;
        GroupPagerFooter.Visibility = Visibility.Collapsed;
        GroupPagerHost.Content = null;
        switch (s) { case 0: BuildA(); break; case 1: BuildB(); break; case 2: BuildC(); break; case 3: BuildD(); break; case 4: BuildE(); break; case 5: BuildF(); break; case 6: BuildG(); break; case 7: BuildH(); break; }
    }

    private void BuildA()
    {
        // BuildA can be called directly by the search/filter/selection handlers.
        // Always reset the host first so a refresh does not append a second copy
        // of the group list below the current one.
        ContentPanel.Children.Clear();
        ContentScroll.ScrollToTop();
        GroupStepHeader.Children.Clear();
        GroupStepHeader.Visibility = Visibility.Visible;

        GroupStepHeader.Children.Add(STitle("Chọn đoàn"));
        GroupStepHeader.Children.Add(SText("Chọn hồ sơ đoàn đã tạo để tiến hành đặt phòng. Không tạo đoàn mới ở bước này."));
        // Search
        var fBar = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        var searchTb = new TextBox { Text = _groupSearch, Width = 220, Height = 34, Margin = new Thickness(0, 0, 8, 4) };
        searchTb.TextChanged += (_, _) => { _groupSearch = searchTb.Text; ResetGroupPage(); BuildA(); };
        fBar.Children.Add(searchTb);
        var filterCb = new ComboBox { Width = 130, Height = 34, Margin = new Thickness(0, 0, 8, 4), ItemsSource = new[] { "Tất cả", "Confirmed", "Tentative", "Arrived" }, SelectedItem = _groupFilterStatus };
        filterCb.SelectionChanged += (_, _) => { _groupFilterStatus = filterCb.SelectedItem?.ToString() ?? ""; ResetGroupPage(); BuildA(); };
        fBar.Children.Add(filterCb);
        GroupStepHeader.Children.Add(fBar);

        var filtered = _groups.Where(g => (string.IsNullOrEmpty(_groupSearch) || g.Name.ToLower().Contains(_groupSearch.ToLower()) || g.Code.ToLower().Contains(_groupSearch.ToLower()) || (g.Company?.ToLower().Contains(_groupSearch.ToLower()) ?? false)) && (string.IsNullOrEmpty(_groupFilterStatus) || _groupFilterStatus == "Tất cả" || g.Status == _groupFilterStatus)).ToList();
        _buildingGroupPage = true;
        _groupPager.SetSource(filtered, resetPage: false);
        _buildingGroupPage = false;

        var list = new StackPanel();
        foreach (var grp in _groupPager.Items)
        {
            var g = grp;
            var b = new Border { Style = (Style)FindResource("GroupCard"), Margin = new Thickness(0, 0, 0, 6) };
            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }, new ColumnDefinition { Width = GridLength.Auto } } };
            var sp = new StackPanel();
            sp.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = g.Code, FontWeight = FontWeights.SemiBold, FontSize = 12, Foreground = C("#ED7900") }, new TextBlock { Text = " · " + g.Name, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D"), Margin = new Thickness(8, 0, 0, 0) } } });
            sp.Children.Add(new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = g.Company ?? "—", FontSize = 11, Foreground = C("#334155") }, new TextBlock { Text = " | " + (g.Agent ?? "—"), FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(4, 0, 0, 0) } } });
            sp.Children.Add(new TextBlock { FontSize = 11, Foreground = C("#64748B"), Margin = new Thickness(0, 3, 0, 0), Text = $"{g.Arrival:dd/MM} → {g.Departure:dd/MM} · {g.TotalGuests} khách · {g.ExpectedRooms} phòng" });
            Grid.SetColumn(sp, 0); grid.Children.Add(sp);
            var badge = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 4, 8, 4), VerticalAlignment = VerticalAlignment.Center, Background = Cb("#DCFCE7"), Child = new TextBlock { Text = g.Status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } };
            Grid.SetColumn(badge, 1); grid.Children.Add(badge);
            b.Child = grid;
            if (g == _selectedGroup) { b.Background = Cb("#FFF7ED"); b.BorderBrush = Cb("#FCD34D"); b.BorderThickness = new Thickness(2); }
            b.MouseLeftButtonDown += (_, _) =>
            {
                _selectedGroup = g;
                _roomBlocks.Clear();
                _rateLines.Clear();
                _availableRatePlans.Clear();
                _ = LoadRatesAsync();
                BuildA();
                UpdateSidebar(_step);
                UpdateFooter();
                UpdateStatus();
            };
            list.Children.Add(b);
        }
        ContentPanel.Children.Add(list);

        GroupPagerSummary.Text = filtered.Count == 0 ? "Không có đoàn phù hợp" : _groupPager.PageText;
        GroupPagerHost.Content = new ServerPagerControl { DataContext = _groupPager };
        GroupPagerFooter.Visibility = Visibility.Visible;
    }

    private void BuildB()
    {
        ContentPanel.Children.Clear();
        ContentScroll.ScrollToTop();
        ContentPanel.Children.Add(STitle("Nhu cầu phòng — Room Block"));
        ContentPanel.Children.Add(SText("Nhập số lượng phòng theo từng hạng. Đây là room block, không phải từng phòng riêng lẻ."));

        // KPI
        var totalRooms = _roomBlocks.Sum(r => r.Qty);
        var totalPax = _roomBlocks.Sum(r => r.Qty * (r.Adults + r.Children));
        ContentPanel.Children.Add(new WrapPanel { Margin = new Thickness(0, 0, 0, 10), Children = { Kpi("Tổng phòng", totalRooms.ToString(), "#ED7900"), Kpi("Tổng khách", totalPax.ToString(), "#2563EB"), Kpi("Người lớn", _roomBlocks.Sum(r => r.Qty * r.Adults).ToString(), "#16A34A"), Kpi("Trẻ em", _roomBlocks.Sum(r => r.Qty * r.Children).ToString(), "#0891B2"), Kpi("Extra Bed", _roomBlocks.Sum(r => r.ExtraBed).ToString(), "#7C3AED") } });

        // Actions
        var acts = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        acts.Children.Add(Btn("+ Thêm hạng phòng", () => { _roomBlocks.Add(new GbRoomBlock()); BuildB(); UpdateFooter(); UpdateStatus(); }));
        acts.Children.Add(Btn("Sao chép dòng", () => { if (_roomBlocks.Count > 0) { var last = _roomBlocks[^1]; _roomBlocks.Add(new GbRoomBlock { RoomType = last.RoomType, Qty = last.Qty, Adults = last.Adults, RatePlan = last.RatePlan }); BuildB(); } }));
        acts.Children.Add(Btn("Xóa tất cả", () => { _roomBlocks.Clear(); BuildB(); UpdateFooter(); }));
        ContentPanel.Children.Add(acts);

        // Room block grid
        var dg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = _roomBlocks, MaxHeight = 280 };
        dg.Columns.Add(new DataGridTextColumn { Header = "Hạng phòng", Binding = new System.Windows.Data.Binding("RoomType"), Width = 130 });
        dg.Columns.Add(new DataGridTextColumn { Header = "SL", Binding = new System.Windows.Data.Binding("Qty"), Width = 60 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Người lớn", Binding = new System.Windows.Data.Binding("Adults"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Trẻ em", Binding = new System.Windows.Data.Binding("Children"), Width = 60 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Rate Plan", Binding = new System.Windows.Data.Binding("RatePlan"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Extra Bed", Binding = new System.Windows.Data.Binding("ExtraBed"), Width = 70 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Smoking", Binding = new System.Windows.Data.Binding("Smoking"), Width = 90 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Ghi chú", Binding = new System.Windows.Data.Binding("Note"), Width = 140 });
        ContentPanel.Children.Add(dg);

        ContentPanel.Children.Add(new TextBlock { Text = "Double-click để sửa giá trị trong bảng.", FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 4, 0, 0) });
    }

    private void BuildC()
    {
        ContentPanel.Children.Add(STitle("Giá đoàn — Group Rate"));
        ContentPanel.Children.Add(SText("Thiết lập giá cho từng hạng phòng. Giá đoàn khác với giá FIT thông thường."));

        var g1 = Grid2();
        AddGridChild(g1, MakeCombo("Currency", _rateCurrency, v => { _rateCurrency = v; OnChanged(); }, new[] { "VND", "USD", "EUR" }));
        AddGridChild(g1, MakeCombo("Rate Plan", "GROUP", _ => { }, new[] { "GROUP", "CORP", "NEGOTIATED", "MANUAL" }));
        ContentPanel.Children.Add(g1);

        ContentPanel.Children.Add(SubTitle("Bảng giá theo hạng phòng"));
        SyncRateLines();
        var dg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = _rateLines };
        dg.Columns.Add(new DataGridTextColumn { Header = "Hạng phòng", Binding = new System.Windows.Data.Binding("RoomType"), Width = 120 });
        dg.Columns.Add(new DataGridTextColumn { Header = "BAR", Binding = new System.Windows.Data.Binding("BarDisplay"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Contract", Binding = new System.Windows.Data.Binding("ContractDisplay"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Selling", Binding = new System.Windows.Data.Binding("SellingDisplay"), Width = 100 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Discount %", Binding = new System.Windows.Data.Binding("DiscountPctDisplay"), Width = 90 });
        dg.Columns.Add(new DataGridTextColumn { Header = "Giá cuối", Binding = new System.Windows.Data.Binding("FinalDisplay"), Width = 110 });
        ContentPanel.Children.Add(dg);

        ContentPanel.Children.Add(Sep());
        var g2 = Grid3();
        AddGridChild(g2, MakeDecField("VAT %", _taxPct, v => _taxPct = v));
        AddGridChild(g2, MakeDecField("Phí DV %", _serviceChargePct, v => _serviceChargePct = v));
        AddGridChild(g2, MakeCheck("Đã gồm thuế", _taxIncluded, v => _taxIncluded = v));
        ContentPanel.Children.Add(g2);

        if (_roomBlocks.Count > 0 && _rateLines.Count > 0)
        {
            var estRev = _roomBlocks.Sum(rb => { var rl = _rateLines.FirstOrDefault(r => r.RoomType == rb.RoomType); return rb.Qty * (rl?.FinalRate ?? 0) * Math.Max(1, (int)(_selectedGroup?.Departure.Subtract(_selectedGroup?.Arrival ?? DateTime.Today).TotalDays ?? 1)); });
            ContentPanel.Children.Add(new Border { Background = Cb("#FFF7ED"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Child = new StackPanel { Children = { new TextBlock { Text = "Doanh thu dự kiến", FontSize = 12, Foreground = C("#92400E") }, new TextBlock { Text = $"{estRev:N0} {_rateCurrency}", FontSize = 22, FontWeight = FontWeights.Bold, Foreground = C("#C2410C") } } } });
        }
    }

    private void BuildD()
    {
        ContentPanel.Children.Clear();
        ContentScroll.ScrollToTop();
        ContentPanel.Children.Add(STitle("Đặt cọc & Bảo đảm"));
        ContentPanel.Children.Add(SText("Thiết lập chính sách đặt cọc và lịch thanh toán."));

        var g1 = Grid2();
        AddGridChild(g1, MakeCheck("Yêu cầu đặt cọc", _depositRequired, v => { _depositRequired = v; BuildD(); }));
        AddGridChild(g1, MakeCombo("Loại bảo đảm", _guaranteeType, v => _guaranteeType = v, new[] { "Credit Card", "Bank Transfer", "Cash", "Guarantee Letter" }));
        ContentPanel.Children.Add(g1);

        var g2 = Grid2();
        AddGridChild(g2, MakeDate("Ngày Cut-off", _cutoffDate, v => _cutoffDate = v));
        AddGridChild(g2, MakeDate("Ngày Release", _releaseDate, v => _releaseDate = v));
        ContentPanel.Children.Add(g2);

        if (_depositRequired)
        {
            ContentPanel.Children.Add(SubTitle("Lịch thanh toán"));
            var estimatedRevenue = GetEstimatedRevenue();
            foreach (var schedule in _depositSchedules)
                schedule.EstimatedAmount = estimatedRevenue * schedule.Percent / 100m;

            var add = new Button { Content = "+ Thêm đợt", Style = (Style)FindResource("ActionBtn"), Height = 30, FontSize = 11, Margin = new Thickness(0, 0, 0, 6) };
            add.Click += (_, _) => { _depositSchedules.Add(new GbDepositSchedule { DueDate = DateTime.Today.AddDays(_depositSchedules.Count * 10 + 1), Percent = _depositSchedules.Count == 0 ? 30 : _depositSchedules.Count == 1 ? 40 : 30 }); BuildD(); };
            ContentPanel.Children.Add(add);

            var dg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), ItemsSource = _depositSchedules };
            dg.Columns.Add(new DataGridTextColumn { Header = "Ngày đến hạn", Binding = new System.Windows.Data.Binding("DueDateDisplay"), Width = 120 });
            dg.Columns.Add(new DataGridTextColumn { Header = "%", Binding = new System.Windows.Data.Binding("PercentDisplay"), Width = 70 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Số tiền", Binding = new System.Windows.Data.Binding("AmountDisplay"), Width = 140 });
            dg.Columns.Add(new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("Status"), Width = 110 });
            ContentPanel.Children.Add(dg);

            if (_depositSchedules.Count > 0)
            {
                var totalDep = _depositSchedules.Sum(d => d.EstimatedAmount);
                ContentPanel.Children.Add(new Border { Background = Cb("#F0FDF4"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 8, 0, 0), Child = new TextBlock { Text = $"Tổng đặt cọc: {totalDep:N0} {_rateCurrency} ({_depositSchedules.Sum(d => d.Percent)}%)", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } });
            }
        }
    }

    private void BuildE()
    {
        ContentPanel.Children.Add(STitle("Rooming List"));
        ContentPanel.Children.Add(SText("Rooming List được quản lý ở workspace dữ liệu thật sau khi booking đoàn được lưu. Wizard không tạo khách giả hoặc dữ liệu tạm trong bộ nhớ."));

        var actionBar = new WrapPanel { Margin = new Thickness(0, 8, 0, 12) };
        actionBar.Children.Add(Btn("Mở Rooming List thật", OpenPersistedRoomingList));
        ContentPanel.Children.Add(actionBar);

        var summary = new Border
        {
            Background = Cb("#EFF6FF"),
            BorderBrush = Cb("#BFDBFE"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 12, 14, 12),
            Child = new StackPanel()
        };
        var stack = (StackPanel)summary.Child;
        stack.Children.Add(new TextBlock { Text = "Dữ liệu sẽ được nhập ở Rooming List chính thức", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#1D4ED8") });
        stack.Children.Add(new TextBlock { Text = _selectedGroup is null ? "Chưa chọn đoàn." : $"Đoàn {_selectedGroup.Code} · {_selectedGroup.Name}", FontSize = 11, Foreground = C("#334155"), Margin = new Thickness(0, 5, 0, 0) });
        stack.Children.Add(new TextBlock { Text = "Sau khi lưu booking, chọn khách thật từ hồ sơ khách và thực hiện phân phòng; mọi thay đổi sẽ ghi vào DB.", FontSize = 11, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 0) });
        ContentPanel.Children.Add(summary);
    }

    private void OpenPersistedRoomingList()
    {
        var existing = System.Windows.Application.Current.Windows
            .OfType<GroupRoomingListWindow>()
            .FirstOrDefault(window => window.IsLoaded);
        if (existing is not null)
        {
            if (existing.WindowState == WindowState.Minimized)
                existing.WindowState = WindowState.Normal;
            existing.Activate();
            existing.Focus();
            return;
        }

        new GroupRoomingListWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        }.Show();
    }

    private void BuildF()
    {
        ContentPanel.Children.Add(STitle("Chính sách đoàn"));
        ContentPanel.Children.Add(SText("Thiết lập các chính sách ưu đãi và dịch vụ cho đoàn."));

        ContentPanel.Children.Add(SubTitle("Chính sách phòng"));
        var g1 = Grid4();
        AddGridChild(g1, MakeCheck("Late Check-out", _lateCheckout, v => _lateCheckout = v));
        AddGridChild(g1, MakeCheck("Early Check-in", _earlyCheckin, v => _earlyCheckin = v));
        AddGridChild(g1, MakeCheck("Free Upgrade", _freeUpgrade, v => _freeUpgrade = v));
        AddGridChild(g1, MakeCheck("FOC", _complimentaryFoc, v => _complimentaryFoc = v));
        ContentPanel.Children.Add(g1);

        ContentPanel.Children.Add(SubTitle("Phòng miễn phí"));
        var g2 = Grid3();
        AddGridChild(g2, MakeIntField("Tour Leader", _tourLeaderRooms, v => _tourLeaderRooms = v));
        AddGridChild(g2, MakeIntField("Driver", _driverRooms, v => _driverRooms = v));
        AddGridChild(g2, MakeIntField("Guide", _guideRooms, v => _guideRooms = v));
        ContentPanel.Children.Add(g2);

        ContentPanel.Children.Add(SubTitle("Dịch vụ & Meal Plan"));
        var sGrid = Grid4();
        AddGridChild(sGrid, MakeCheck("Breakfast", _breakfast, v => _breakfast = v));
        AddGridChild(sGrid, MakeCheck("Lunch", _lunch, v => _lunch = v));
        AddGridChild(sGrid, MakeCheck("Dinner", _dinner, v => _dinner = v));
        AddGridChild(sGrid, MakeCheck("Airport Transfer", _airportPickup, v => _airportPickup = v));
        ContentPanel.Children.Add(sGrid);
        var sGrid2 = Grid4();
        AddGridChild(sGrid2, MakeCheck("Welcome Drink", _welcomeDrink, v => _welcomeDrink = v));
        AddGridChild(sGrid2, MakeCheck("Meeting Room", _meetingRoom, v => _meetingRoom = v));
        AddGridChild(sGrid2, MakeCheck("Laundry", _laundrySvc, v => _laundrySvc = v));
        AddGridChild(sGrid2, MakeCheck("Spa", _spaSvc, v => _spaSvc = v));
        ContentPanel.Children.Add(sGrid2);

        ContentPanel.Children.Add(SubTitle("Yêu cầu đặc biệt"));
        ContentPanel.Children.Add(MakeTextArea("Special Request", _specialRequest, v => _specialRequest = v, "Nhập yêu cầu đặc biệt của đoàn..."));
    }

    private void BuildG()
    {
        ContentPanel.Children.Add(STitle("Kiểm tra"));
        ContentPanel.Children.Add(SText("Kiểm tra toàn bộ thông tin booking trước khi xác nhận."));

        var checks = new (string, bool)[]
        {
            ("Đã chọn đoàn", _selectedGroup != null),
            ("Room Block đủ phòng", _roomBlocks.Count > 0 && _roomBlocks.Sum(r => r.Qty) > 0),
            ("Giá đầy đủ", _rateLines.Count > 0 && _rateLines.All(r => r.FinalRate > 0)),
            ("Đặt cọc hợp lệ", !_depositRequired || _depositSchedules.Count > 0),
            ("Rooming List sẽ nhập sau khi phân phòng", true),
            ("Chính sách hợp lệ", true),
            ("Không overbooking", _selectedGroup != null && _roomBlocks.Sum(r => r.Qty) <= _selectedGroup.ExpectedRooms),
            ("Ngày hợp lệ", _selectedGroup != null),
        };
        foreach (var (label, ok) in checks)
        {
            ContentPanel.Children.Add(new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Background = ok ? Cb("#F0FDF4") : Cb("#FEF2F2"), BorderBrush = ok ? Cb("#BBF7D0") : Cb("#FECACA"), BorderThickness = new Thickness(1), Child = new StackPanel { Orientation = Orientation.Horizontal, Children = { new TextBlock { Text = ok ? "✓ " : "✗ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626"), FontWeight = FontWeights.Bold }, new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center } } } });
        }

        var block = GetBlocking(); var warn = GetWarnings();
        if (block.Count > 0) { ContentPanel.Children.Add(Sep()); ContentPanel.Children.Add(SubTitle("Blocking")); foreach (var b in block) ContentPanel.Children.Add(new TextBlock { Text = $"⛔ {b}", FontSize = 12, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) }); }
        if (warn.Count > 0) { ContentPanel.Children.Add(Sep()); ContentPanel.Children.Add(SubTitle("Cảnh báo")); foreach (var w in warn) ContentPanel.Children.Add(new TextBlock { Text = $"⚠ {w}", FontSize = 12, Foreground = C("#D97706"), Margin = new Thickness(0, 2, 0, 0) }); }

        var allOk = block.Count == 0 && checks.All(c => c.Item2);
        ContentPanel.Children.Add(new Border { Background = allOk ? Cb("#DCFCE7") : Cb("#FEE2E2"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 10, 0, 0), Child = new TextBlock { Text = allOk ? "✓ Sẵn sàng xác nhận booking" : "⛔ Cần khắc phục các vấn đề trên", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = allOk ? C("#16A34A") : C("#DC2626"), TextAlignment = TextAlignment.Center } });
    }

    private void BuildH()
    {
        ContentPanel.Children.Add(STitle("Xác nhận Booking"));
        ContentPanel.Children.Add(new Border { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Cb("#BFDBFE"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = "Lưu thật tại bước này: đoàn + room block + giá negotiated. Rooming List sẽ nhập bằng dữ liệu assignment thật sau khi phân phòng; lịch đặt cọc và chính sách dịch vụ chưa thuộc command cấu hình booking này.", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#1E40AF"), TextWrapping = TextWrapping.Wrap } });

        ContentPanel.Children.Add(SubTitle("Tóm tắt Booking"));
        var pp = new StackPanel();
        pp.Children.Add(Prev("Đoàn", _selectedGroup?.Name ?? "—"));
        pp.Children.Add(Prev("Mã đoàn", _selectedGroup?.Code ?? "—"));
        pp.Children.Add(Prev("Công ty", _selectedGroup?.Company ?? "—"));
        pp.Children.Add(Prev("Đại lý", _selectedGroup?.Agent ?? "—"));
        pp.Children.Add(Prev("Lưu trú", _selectedGroup != null ? $"{_selectedGroup.Arrival:dd/MM/yyyy} → {_selectedGroup.Departure:dd/MM/yyyy}" : "—"));
        pp.Children.Add(Prev("Tổng phòng", $"{_roomBlocks.Sum(r => r.Qty)} phòng / {_roomBlocks.Count} hạng"));
        pp.Children.Add(Prev("Rooming List", "Nhập ở workspace dữ liệu thật sau khi booking"));
        pp.Children.Add(Prev("Doanh thu dự kiến", $"{GetEstimatedRevenue():N0} {_rateCurrency}"));
        pp.Children.Add(Prev("Đặt cọc", _depositRequired ? $"{_depositSchedules.Sum(d => d.Percent)}% qua {_depositSchedules.Count} đợt" : "Không"));
        ContentPanel.Children.Add(pp);

        ContentPanel.Children.Add(Sep());
        ContentPanel.Children.Add(SubTitle("Room Block"));
        foreach (var rb in _roomBlocks) ContentPanel.Children.Add(new TextBlock { Text = $"• {rb.RoomType}: {rb.Qty} phòng × {rb.Adults + rb.Children} khách", FontSize = 12, Foreground = C("#334155"), Margin = new Thickness(0, 1, 0, 0) });

        ContentPanel.Children.Add(Sep());
        ContentPanel.Children.Add(SubTitle("Xác nhận"));
        ContentPanel.Children.Add(MakeCfmCheck("Tôi đã kiểm tra Room Block", _confirmBlock, v => { _confirmBlock = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("Tôi đã kiểm tra Giá đoàn", _confirmRate, v => { _confirmRate = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("Tôi đã kiểm tra Đặt cọc", _confirmDeposit, v => { _confirmDeposit = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("Tôi đã hiểu Rooming List sẽ nhập sau khi phân phòng", _confirmRooming, v => { _confirmRooming = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("Tôi đã kiểm tra Chính sách", _confirmPolicies, v => { _confirmPolicies = v; UpdateFooter(); }));
        ContentPanel.Children.Add(MakeCfmCheck("Tôi xác nhận thông tin đúng", _confirmAll, v => { _confirmAll = v; UpdateFooter(); }));
    }

    private async void OnConfirmBooking(object s, RoutedEventArgs e)
    {
        if (!CanConfirm()) return;
        await SaveBookingAsync(confirm: true);
    }

    private bool CanConfirm() => !HasBlocking() && _confirmBlock && _confirmRate && _confirmDeposit && _confirmRooming && _confirmPolicies && _confirmAll;
    private List<string> GetBlocking()
    {
        var b = new List<string>();
        if (_selectedGroup == null) b.Add("Chưa chọn đoàn");
        if (_roomBlocks.Count == 0 || _roomBlocks.Sum(r => r.Qty) == 0) b.Add("Chưa có Room Block");
        if (_rateLines.Count == 0 || _rateLines.Any(r => r.FinalRate <= 0)) b.Add("Chưa thiết lập giá");
        if (_selectedGroup != null && _roomBlocks.Sum(r => r.Qty) != _selectedGroup.ExpectedRooms)
            b.Add($"Tổng room block phải bằng {_selectedGroup.ExpectedRooms} phòng");
        if (_selectedGroup != null && _selectedGroup.Status is not ("Draft" or "Tentative"))
            b.Add("Chỉ được cấu hình đoàn ở trạng thái Draft hoặc Tentative");
        if (_realDataError is not null) b.Add("Không tải được dữ liệu thật của module");
        return b;
    }
    private List<string> GetWarnings()
    {
        var w = new List<string>();
        if (_depositRequired && _depositSchedules.Count == 0) w.Add("Yêu cầu đặt cọc nhưng chưa có lịch thanh toán");
        if (_depositRequired && _depositSchedules.Count > 0 && _depositSchedules.Sum(x => x.Percent) != 100m) w.Add("Tổng tỷ lệ lịch đặt cọc phải bằng 100%");
        if (_depositRequired && string.IsNullOrWhiteSpace(_guaranteeType)) w.Add("Yêu cầu đặt cọc nhưng chưa chọn loại bảo đảm");
        if (_roomBlocks.Count > 0 && _rateLines.Count == 0) w.Add("Có Room Block nhưng chưa có giá");
        return w;
    }
    private bool HasBlocking() => GetBlocking().Count > 0;
    private decimal GetEstimatedRevenue()
    {
        var nights = Math.Max(1, (int)(_selectedGroup?.Departure.Subtract(_selectedGroup?.Arrival ?? DateTime.Today).TotalDays ?? 1));
        return _roomBlocks.Sum(rb => { var rl = _rateLines.FirstOrDefault(r => r.RoomType == rb.RoomType); return rb.Qty * (rl?.FinalRate ?? 0) * nights; });
    }

    private async Task LoadRatesAsync()
    {
        var group = _selectedGroup;
        var propertyId = _contextAccessor?.Current?.Property.PropertyId.Value;
        if (group is null || propertyId is null || _rates is null || _roomTypes is null)
            return;

        try
        {
            var plans = await _rates.GetAllAsync(
                propertyId.Value,
                new RatePlanSearchFilter(propertyId.Value, RatePlanStatus.Active, null, null),
                CancellationToken.None);
            _availableRatePlans.Clear();
            _availableRatePlans.AddRange(plans);
            _roomTypeIds.Clear();
            foreach (var roomBlock in _roomBlocks)
            {
                var roomType = await _roomTypes.GetRoomTypeByCodeAsync(
                    propertyId.Value,
                    roomBlock.RoomType.Trim(),
                    CancellationToken.None);
                if (roomType is not null && roomType.IsActive)
                    _roomTypeIds[roomBlock.RoomType.Trim()] = roomType.HotelRoomTypeId;
            }

            _rateLines.Clear();
            var arrival = DateOnly.FromDateTime(group.Arrival);
            var nights = Math.Max(1, (int)(group.Departure - group.Arrival).TotalDays);
            foreach (var roomBlock in _roomBlocks)
            {
                var code = roomBlock.RoomType.Trim();
                _roomTypeIds.TryGetValue(code, out var roomTypeId);
                var candidates = _availableRatePlans
                    .Where(x => x.RoomTypeId == roomTypeId
                        && x.EffectiveFrom.Date <= group.Arrival.Date
                        && x.EffectiveTo.Date >= group.Arrival.Date)
                    .ToList();
                var bar = candidates.FirstOrDefault(x => x.Hierarchy == RateHierarchyLevel.BAR);
                var contract = candidates.FirstOrDefault(x =>
                    x.Hierarchy is RateHierarchyLevel.Negotiated or RateHierarchyLevel.Corporate);
                var selected = candidates
                    .OrderBy(x => RatePriority(x.Hierarchy))
                    .ThenByDescending(x => x.EffectiveFrom)
                    .FirstOrDefault();
                var barRate = bar?.CalculatePrice(arrival, roomBlock.Adults, nights, roomBlock.Qty, 0).finalPrice ?? 0;
                var contractRate = contract?.CalculatePrice(arrival, roomBlock.Adults, nights, roomBlock.Qty, 0).finalPrice ?? 0;
                var sellingRate = selected?.CalculatePrice(arrival, roomBlock.Adults, nights, roomBlock.Qty, 0).finalPrice ?? 0;
                _rateLines.Add(new GbRateLine
                {
                    RoomType = roomBlock.RoomType,
                    BarRate = barRate,
                    ContractRate = contractRate,
                    SellingRate = sellingRate,
                    DiscountPct = 0
                });
            }

            if (_step == 2 && ReferenceEquals(_selectedGroup, group))
            {
                BuildC();
                UpdateFooter();
                UpdateStatus();
            }
        }
        catch (Exception ex)
        {
            _realDataError = ex.Message;
            if (_step == 2) { BuildC(); UpdateFooter(); }
        }
    }

    private static int RatePriority(RateHierarchyLevel hierarchy) => hierarchy switch
    {
        RateHierarchyLevel.Group => 0,
        RateHierarchyLevel.Negotiated => 1,
        RateHierarchyLevel.Corporate => 2,
        RateHierarchyLevel.Package => 3,
        RateHierarchyLevel.BAR => 4,
        _ => 5
    };

    private async Task<IReadOnlyCollection<CreateGroupRoomBlockDraft>> BuildRoomBlockDraftsAsync(bool requireRates)
    {
        if (_selectedGroup is null)
            throw new InvalidOperationException("Chưa chọn đoàn.");
        if (_roomTypes is null || _contextAccessor?.Current is null)
            throw new InvalidOperationException("Chưa có dịch vụ master data hoặc execution context thật.");

        var propertyId = _contextAccessor.Current.Property.PropertyId.Value;
        var drafts = new List<CreateGroupRoomBlockDraft>(_roomBlocks.Count);
        foreach (var roomBlock in _roomBlocks)
        {
            var code = roomBlock.RoomType.Trim();
            if (string.IsNullOrWhiteSpace(code) || roomBlock.Qty <= 0)
                throw new ArgumentException("Room block phải có hạng phòng và số lượng hợp lệ.");

            if (!_roomTypeIds.TryGetValue(code, out var roomTypeId))
            {
                var roomType = await _roomTypes.GetRoomTypeByCodeAsync(propertyId, code, CancellationToken.None);
                if (roomType is null || !roomType.IsActive)
                    throw new KeyNotFoundException($"Không tìm thấy hạng phòng đang hoạt động: {code}.");
                roomTypeId = roomType.HotelRoomTypeId;
                _roomTypeIds[code] = roomTypeId;
            }

            var rate = _rateLines.FirstOrDefault(x => string.Equals(x.RoomType, roomBlock.RoomType, StringComparison.OrdinalIgnoreCase));
            if (requireRates && (rate is null || rate.FinalRate <= 0))
                throw new InvalidOperationException($"Chưa có giá thật hợp lệ cho hạng phòng {code}.");

            drafts.Add(new CreateGroupRoomBlockDraft(
                roomTypeId,
                roomBlock.Qty,
                DateOnly.FromDateTime(_selectedGroup.Arrival),
                DateOnly.FromDateTime(_selectedGroup.Departure),
                _cutoffDate is null ? null : DateOnly.FromDateTime(_cutoffDate.Value),
                rate is { FinalRate: > 0 } ? rate.FinalRate : null));
        }

        return drafts;
    }

    private async Task SaveBookingAsync(bool confirm)
    {
        try
        {
            var drafts = await BuildRoomBlockDraftsAsync(confirm);
            if (_groupCommands is null)
                throw new InvalidOperationException("Chưa có group booking command thật trong DI.");

            var result = await _groupCommands.ConfigureBookingAsync(
                new ConfigureGroupBookingRequest(_selectedGroup!.Id, drafts, confirm, BuildDepositPolicyDraft()),
                CancellationToken.None);
            _hasChanges = false;
            MessageBox.Show(
                confirm
                    ? $"Đã xác nhận booking đoàn {result.GroupCode} và lưu {result.RoomBlockCount} room block."
                    : $"Đã lưu nháp booking đoàn {result.GroupCode} với {result.RoomBlockCount} room block.",
                "Đặt phòng đoàn",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Không thể {(confirm ? "xác nhận" : "lưu nháp")} booking đoàn.\n\n{ex.Message}",
                "Đặt phòng đoàn",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private GroupDepositPolicyDraft BuildDepositPolicyDraft()
    {
        var schedules = _depositRequired
            ? _depositSchedules
                .Select(x => new GroupDepositScheduleDraft(DateOnly.FromDateTime(x.DueDate), x.Percent))
                .ToArray()
            : Array.Empty<GroupDepositScheduleDraft>();

        return new GroupDepositPolicyDraft(
            _depositRequired,
            _depositRequired ? _guaranteeType : null,
            _cutoffDate is null ? null : DateOnly.FromDateTime(_cutoffDate.Value),
            _releaseDate is null ? null : DateOnly.FromDateTime(_releaseDate.Value),
            schedules);
    }

    private void SyncRateLines()
    {
        // This collection is populated only by LoadRatesAsync from persisted
        // rate plans. Never synthesize a price in the wizard.
    }

    // ═══ SIDEBAR ═══
    private void UpdateSidebar(int s)
    {
        SidebarContent.Children.Clear();
        if (_selectedGroup == null) { SidebarContent.Children.Add(new TextBlock { Text = "Chọn một đoàn để bắt đầu.", FontSize = 12, Foreground = C("#94A3B8") }); return; }
        SidebarContent.Children.Add(SideSec("Thông tin đoàn"));
        SidebarContent.Children.Add(SideRow("Tên", _selectedGroup.Name));
        SidebarContent.Children.Add(SideRow("Mã", _selectedGroup.Code));
        SidebarContent.Children.Add(SideRow("Công ty", _selectedGroup.Company ?? "—"));
        SidebarContent.Children.Add(SideRow("Đại lý", _selectedGroup.Agent ?? "—"));
        SidebarContent.Children.Add(SideRow("Trưởng đoàn", _selectedGroup.Leader ?? "—"));
        SidebarContent.Children.Add(SideRow("Khách", _selectedGroup.TotalGuests.ToString()));
        SidebarContent.Children.Add(SideSep());

        if (_roomBlocks.Count > 0)
        {
            SidebarContent.Children.Add(SideSec("Room Block"));
            foreach (var rb in _roomBlocks) SidebarContent.Children.Add(SideRow(rb.RoomType, $"{rb.Qty} phòng"));
            SidebarContent.Children.Add(SideSep());
        }

        var estRev = GetEstimatedRevenue();
        if (estRev > 0)
        {
            SidebarContent.Children.Add(SideSec("Tài chính"));
            SidebarContent.Children.Add(SideRow("Doanh thu DK", $"{estRev:N0} {_rateCurrency}"));
            if (_depositRequired && _depositSchedules.Count > 0)
                SidebarContent.Children.Add(SideRow("Cọc dự kiến", $"{_depositSchedules.Sum(d => estRev * d.Percent / 100):N0} {_rateCurrency}"));
            SidebarContent.Children.Add(SideSep());
        }

        var block = GetBlocking(); var warn = GetWarnings();
        if (block.Count > 0) { SidebarContent.Children.Add(SideSec($"⛔ Blocking ({block.Count})")); foreach (var b in block.Take(3)) SidebarContent.Children.Add(new TextBlock { Text = b, FontSize = 10, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) }); }
        if (warn.Count > 0) { SidebarContent.Children.Add(SideSec($"⚠ Warning ({warn.Count})")); foreach (var w in warn.Take(3)) SidebarContent.Children.Add(new TextBlock { Text = w, FontSize = 10, Foreground = C("#D97706"), Margin = new Thickness(0, 2, 0, 0) }); }
    }

    // ═══ HELPERS ═══
    private void OnChanged() { _hasChanges = true; UpdateFooter(); UpdateStatus(); }
    private static FrameworkElement STitle(string t) => new TextBlock { Text = t, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 4) };
    private static FrameworkElement SText(string t) => new TextBlock { Text = t, FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 0, 0, 10) };
    private static FrameworkElement SubTitle(string t) => new TextBlock { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#334155"), Margin = new Thickness(0, 10, 0, 6) };
    private static FrameworkElement Sep() => new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) };
    private static FrameworkElement Prev(string l, string v) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 12, Foreground = C("#94A3B8"), Width = 140 }, new TextBlock { Text = v, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") } } };
    private static FrameworkElement SideSec(string t) => new TextBlock { Text = t, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(0, 0, 0, 6) };
    private static FrameworkElement SideRow(string l, string v) => new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0), Children = { new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), Width = 85 }, new TextBlock { Text = v, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#334155") } } };
    private static FrameworkElement SideSep() => new Border { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 6, 0, 6) };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static Button Btn(string text, Action action) { var btn = new Button { Content = text, Height = 32, FontSize = 11, Margin = new Thickness(0, 0, 6, 0), Padding = new Thickness(10, 0, 10, 0), Background = Cb("#FFF7ED"), Foreground = C("#D97706"), BorderBrush = Cb("#FCD34D"), BorderThickness = new Thickness(1), Cursor = Cursors.Hand }; btn.Click += (_, _) => action(); return btn; }
    private static Border Kpi(string label, string value, string color) => new Border { Width = 120, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}14")), CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 8, 6), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}33")), BorderThickness = new Thickness(1), Child = new StackPanel { Children = { new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B") }, new TextBlock { Text = value, FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) } } } };

    // Input helpers
    private static void AddGridChild(Grid grid, FrameworkElement child)
    {
        var column = grid.Children.Count * 2;
        Grid.SetColumn(child, Math.Min(column, grid.ColumnDefinitions.Count - 1));
        grid.Children.Add(child);
    }

    private static Grid Grid2() => new() { Margin = new Thickness(0, 0, 0, 8), ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(12) }, new ColumnDefinition() } };
    private static Grid Grid3() => new() { Margin = new Thickness(0, 0, 0, 8), ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(8) }, new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(8) }, new ColumnDefinition() } };
    private static Grid Grid4() => new() { Margin = new Thickness(0, 0, 0, 8), ColumnDefinitions = { new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(6) }, new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(6) }, new ColumnDefinition(), new ColumnDefinition { Width = new GridLength(6) }, new ColumnDefinition() } };

    private FrameworkElement MakeField(string label, string value, Action<string> setter, string ph = "")
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value, Style = (Style)FindResource("ModTextBox") };
        tb.TextChanged += (_, _) => { setter(tb.Text); OnChanged(); };
        sp.Children.Add(tb); return sp;
    }

    private FrameworkElement MakeIntField(string label, int value, Action<int> setter)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value.ToString(), Style = (Style)FindResource("ModTextBox") };
        tb.TextChanged += (_, _) => { if (int.TryParse(tb.Text, out var v)) setter(v); };
        sp.Children.Add(tb); return sp;
    }

    private FrameworkElement MakeDecField(string label, decimal value, Action<decimal> setter)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value > 0 ? value.ToString("N0") : "", Style = (Style)FindResource("ModTextBox") };
        tb.TextChanged += (_, _) => { if (decimal.TryParse(tb.Text, out var v)) setter(v); else if (string.IsNullOrEmpty(tb.Text)) setter(0); };
        sp.Children.Add(tb); return sp;
    }

    private FrameworkElement MakeCombo(string label, string value, Action<string> setter, string[] opts)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var cb = new ComboBox { Style = (Style)FindResource("ModCombo"), ItemsSource = opts, SelectedItem = value };
        cb.SelectionChanged += (_, _) => { if (cb.SelectedItem is string s) setter(s); OnChanged(); };
        sp.Children.Add(cb); return sp;
    }

    private FrameworkElement MakeDate(string label, DateTime? value, Action<DateTime?> setter)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var dp = new DatePicker { Style = (Style)FindResource("ModDatePicker"), SelectedDate = value };
        dp.SelectedDateChanged += (_, _) => { setter(dp.SelectedDate); OnChanged(); };
        sp.Children.Add(dp); return sp;
    }

    private static FrameworkElement MakeCheck(string label, bool value, Action<bool> setter)
    {
        var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center };
        cb.Checked += (_, _) => setter(true); cb.Unchecked += (_, _) => setter(false);
        return cb;
    }

    private FrameworkElement MakeTextArea(string label, string value, Action<string> setter, string ph = "")
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value, Height = 70, Padding = new Thickness(12, 8, 12, 8), VerticalContentAlignment = VerticalAlignment.Top, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#17365D"), Background = Brushes.White, BorderBrush = Cb("#D7E2F0"), BorderThickness = new Thickness(1), TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
        tb.TextChanged += (_, _) => setter(tb.Text);
        sp.Children.Add(tb); return sp;
    }

    private static FrameworkElement MakeCfmCheck(string label, bool value, Action<bool> setter)
    {
        var border = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0), Background = Cb("#FAFBFD"), BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1) };
        var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalContentAlignment = VerticalAlignment.Center };
        cb.Checked += (_, _) => { setter(true); border.Background = Cb("#F0FDF4"); border.BorderBrush = Cb("#86EFAC"); };
        cb.Unchecked += (_, _) => { setter(false); border.Background = Cb("#FAFBFD"); border.BorderBrush = Cb("#E2E8F0"); };
        border.Child = cb; return border;
    }
}

// ═══ DATA CLASSES ═══
public class GbGroup
{
    public Guid Id { get; set; }
    public string Code { get; set; } = ""; public string Name { get; set; } = "";
    public string? Company { get; set; } public string? Agent { get; set; }
    public DateTime Arrival { get; set; } public DateTime Departure { get; set; }
    public int TotalGuests { get; set; } public int ExpectedRooms { get; set; }
    public string? Leader { get; set; } public string Status { get; set; } = "";
    public string? Market { get; set; } public string Currency { get; set; } = "VND";
    public string? Vip { get; set; } public string? SpecialRequest { get; set; }
}

public class GbRoomBlock
{
    public string RoomType { get; set; } = "STD";
    public int Qty { get; set; } = 1; public int Adults { get; set; } = 2; public int Children { get; set; }
    public int ExtraBed { get; set; } public string RatePlan { get; set; } = "GROUP";
    public string Smoking { get; set; } = "Non-smoking"; public string Note { get; set; } = "";
}

public class GbRateLine
{
    public string RoomType { get; set; } = "";
    public decimal BarRate { get; set; } public decimal ContractRate { get; set; } public decimal SellingRate { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal FinalRate => SellingRate * (100 - DiscountPct) / 100;
    public string BarDisplay => $"{BarRate:N0}"; public string ContractDisplay => $"{ContractRate:N0}";
    public string SellingDisplay => $"{SellingRate:N0}"; public string DiscountPctDisplay => $"{DiscountPct}%";
    public string FinalDisplay => $"{FinalRate:N0}";
}

public class GbDepositSchedule
{
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(10);
    public decimal Percent { get; set; } = 30;
    public decimal EstimatedAmount { get; set; }
    public string Status { get; set; } = "Chưa thanh toán";
    public string DueDateDisplay => DueDate.ToString("dd/MM/yyyy");
    public string PercentDisplay => $"{Percent}%";
    public string AmountDisplay => EstimatedAmount > 0 ? $"{EstimatedAmount:N0}" : "Chưa tính";
}
