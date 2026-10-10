using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.DoorLock;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.Housekeeping;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Payment;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class GroupCheckoutWindow : Window
{
    private int _step;
    private GpGroup? _group; private bool _groupSelected;
    private int _groupPage, _groupPageSize = 20, _roomPage, _roomPageSize = 20;
    private readonly List<GpGroup> _allGroups;
    private readonly List<GpRoom> _allRooms = new();
    private readonly HashSet<string> _selectedRooms = new(), _blockedRooms = new(), _stayingRooms = new();
    private decimal _masterBalance;
    private readonly IGroupReservationQueryService? _groupQuery;
    private readonly IGroupCheckOutCommandService? _groupCheckOutService;
    private readonly IGroupBillingService? _groupBillingService;
    private readonly IGroupServiceOrderQueryService? _groupServiceOrderQuery;
    private readonly IGroupDepositService? _groupDepositService;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHousekeepingBoardService? _housekeepingBoardService;
    private readonly IMinibarQueryService? _minibarQueryService;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly IKeyCardService? _keyCardService;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);
    private BillingSummaryDto? _billingSummary;
    private IReadOnlyList<GroupServiceOrderDto> _serviceOrders = Array.Empty<GroupServiceOrderDto>();
    private IReadOnlyList<GroupDepositDto> _groupDeposits = Array.Empty<GroupDepositDto>();
    private PaymentReconciliation? _paymentReconciliation;
    private string? _detailLoadWarning;
    private int _detailLoadVersion;

    public GroupCheckoutWindow()
    {
        InitializeComponent(); Owner = System.Windows.Application.Current.MainWindow;
        _allGroups = new();
        var applicationServices = (System.Windows.Application.Current as App)?.Services;
        // A checkout window owns a complete scoped service graph. Resolving
        // EF-backed services from the application scope makes independent
        // windows share AppDbContext and can trigger concurrent-operation
        // failures while one window is loading or posting another workflow.
        _serviceScope = applicationServices?.CreateScope();
        var services = _serviceScope?.ServiceProvider;
        _groupQuery = services?.GetService<IGroupReservationQueryService>();
        _groupCheckOutService = services?.GetService<IGroupCheckOutCommandService>();
        _groupBillingService = services?.GetService<IGroupBillingService>();
        _groupServiceOrderQuery = services?.GetService<IGroupServiceOrderQueryService>();
        _groupDepositService = services?.GetService<IGroupDepositService>();
        _stayRepository = services?.GetService<IHotelStayRepository>();
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _housekeepingBoardService = services?.GetService<IHousekeepingBoardService>();
        _minibarQueryService = services?.GetService<IMinibarQueryService>();
        _paymentRepository = services?.GetService<IHotelPaymentRepository>();
        _keyCardService = services?.GetService<IKeyCardService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        InitFilters(); RefreshGroupPage(true); GoTo(1);
        Loaded += async (_, _) => await LoadPersistedDataAsync();
        Closed += async (_, _) =>
        {
            // Wait for an in-flight EF operation before disposing its scope.
            await _dataOperationGate.WaitAsync();
            _dataOperationGate.Release();
            _dataOperationGate.Dispose();
            _serviceScope?.Dispose();
        };
    }

    private async Task LoadPersistedDataAsync()
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await LoadPersistedDataCoreAsync();
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task LoadPersistedDataCoreAsync()
    {
        _allGroups.Clear();
        _allRooms.Clear();
        if (_groupQuery is null || _contextAccessor?.Current is not { } context)
        {
            RefreshGroupPage(true);
            return;
        }

        try
        {
            var propertyId = context.Property.PropertyId.Value;
            var groups = await _groupQuery.GetGroupsAsync(propertyId, CancellationToken.None);
            var stays = _stayRepository is null
                ? Array.Empty<HotelStayDto>()
                : await _stayRepository.GetCurrentInHouseAsync(propertyId, CancellationToken.None);
            var staysByReservation = stays
                .Where(x => x.ReservationId.HasValue)
                .GroupBy(x => x.ReservationId!.Value)
                .ToDictionary(x => x.Key, x => x.First());
            var housekeepingByRoom = new Dictionary<Guid, HkRoomSnapshot>();
            if (_housekeepingBoardService is not null)
            {
                try
                {
                    housekeepingByRoom = (await _housekeepingBoardService.GetRoomSnapshotAsync(propertyId, CancellationToken.None))
                        .GroupBy(x => x.RoomId)
                        .ToDictionary(x => x.Key, x => x.First());
                }
                catch
                {
                    // Keep the room list usable while exposing that HSK data is unavailable below.
                }
            }
            var minibarByRoom = new Dictionary<Guid, MinibarRoomSnapshot>();
            if (_minibarQueryService is not null)
            {
                try
                {
                    minibarByRoom = (await _minibarQueryService.QueryAsync(propertyId, CancellationToken.None))
                        .GroupBy(x => x.RoomId)
                        .ToDictionary(x => x.Key, x => x.First());
                }
                catch
                {
                    // Keep the room list usable while exposing that minibar data is unavailable below.
                }
            }
            foreach (var group in groups.Where(x => x.Status == GroupReservationStatus.CheckedIn))
            {
                _allGroups.Add(new GpGroup(
                    group.GroupReservationId,
                    group.GroupCode,
                    group.GroupName,
                    group.CompanyName ?? group.AgencyName ?? "—",
                    (group.TotalAdults + group.TotalChildren).ToString(),
                    group.ArrivalDate.ToString("dd/MM/yyyy"),
                    group.DepartureDate.ToString("dd/MM/yyyy"),
                    group.RoomingList.Count,
                    group.TotalAdults + group.TotalChildren,
                    group.TotalRevenue,
                    Math.Max(group.TotalRevenue - group.OutstandingBalance, 0m),
                    group.TotalDeposit,
                    group.OutstandingBalance,
                    group.OutstandingBalance <= 0.01m,
                    group.Status.ToString()));

                foreach (var room in group.RoomingList.Where(x => !x.IsCheckedOut))
                {
                    var stay = room.ReservationId is { } reservationId && staysByReservation.TryGetValue(reservationId, out var matchedStay)
                        ? matchedStay
                        : null;
                    var folio = stay is null || _folioRepository is null
                        ? null
                        : await _folioRepository.GetByStayAsync(propertyId, stay.HotelStayId, CancellationToken.None);
                    var hskStatus = housekeepingByRoom.TryGetValue(room.HotelRoomId, out var hskSnapshot)
                        ? string.IsNullOrWhiteSpace(hskSnapshot.HousekeepingStatus) ? "Chưa cập nhật" : hskSnapshot.HousekeepingStatus
                        : _housekeepingBoardService is null ? "Chưa có service" : "Chưa có snapshot";
                    var minibarStatus = minibarByRoom.TryGetValue(room.HotelRoomId, out var minibarSnapshot)
                        ? $"{minibarSnapshot.Status}{(minibarSnapshot.IsPosted ? " · đã post" : " · chưa post")}"
                        : _minibarQueryService is null ? "Chưa có service" : "Chưa có snapshot";
                    var keyStatus = await LoadKeyStatusAsync(stay);
                    var isStaying = !room.IsCheckedIn;
                    var missingFolio = room.IsCheckedIn && (stay is null || folio is null);
                    _allRooms.Add(new GpRoom(
                        room.AssignmentId,
                        room.RoomNumber,
                        group.GroupCode,
                        stay?.GuestName ?? room.GuestName ?? "—",
                        (room.Adults + room.Children).ToString(),
                        group.DepartureDate.ToString("dd/MM/yyyy"),
                        folio?.OutstandingBalance ?? 0m,
                        folio is not null,
                        stay?.HotelStayId,
                        folio?.HotelFolioId,
                        folio?.FolioNumber,
                        minibarStatus,
                        hskStatus,
                        keyStatus,
                        missingFolio,
                        isStaying,
                        false));
                }
            }

            RefreshGroupPage(true);
        }
        catch (Exception ex)
        {
            _allGroups.Clear();
            _allRooms.Clear();
            MessageBox.Show($"Không thể tải danh sách check-out đoàn thật.\n{ex.Message}", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
            RefreshGroupPage(true);
        }
    }

    private async Task<string> LoadKeyStatusAsync(HotelStayDto? stay)
    {
        if (stay is null || _keyCardService is null)
            return _keyCardService is null ? "Chưa có service" : "Chưa có Stay";

        try
        {
            var keys = await _keyCardService.GetKeysForStayAsync(stay.HotelStayId, CancellationToken.None);
            var active = keys.Count(x => x.Status == KeyStatus.Active);
            return active == 0 ? "Không có thẻ hiệu lực" : $"{active} thẻ hiệu lực";
        }
        catch
        {
            return "Không đọc được";
        }
    }

    private async Task LoadSelectedGroupDetailsAsync(Guid groupId)
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await LoadSelectedGroupDetailsCoreAsync(groupId);
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task LoadSelectedGroupDetailsCoreAsync(Guid groupId)
    {
        var loadVersion = ++_detailLoadVersion;
        _billingSummary = null;
        _serviceOrders = Array.Empty<GroupServiceOrderDto>();
        _groupDeposits = Array.Empty<GroupDepositDto>();
        _paymentReconciliation = null;
        _detailLoadWarning = null;

        if (_contextAccessor?.Current is not { } context)
        {
            _detailLoadWarning = "Chưa có property context để đọc chi tiết đoàn thật.";
            RenderStep();
            return;
        }

        var propertyId = context.Property.PropertyId.Value;
        var warnings = new List<string>();
        if (_groupBillingService is not null)
        {
            try
            {
                var billingSummary = await _groupBillingService.GetBillingSummaryAsync(groupId, CancellationToken.None);
                if (!IsCurrentDetailLoad(groupId, loadVersion)) return;
                _billingSummary = billingSummary;
                if (_billingSummary is not null)
                    _masterBalance = _billingSummary.Outstanding;
            }
            catch (Exception ex)
            {
                warnings.Add($"billing: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("chưa đăng ký GroupBillingService");
        }

        if (_groupServiceOrderQuery is not null)
        {
            try
            {
                var serviceOrders = await _groupServiceOrderQuery.SearchAsync(propertyId, groupId, CancellationToken.None);
                if (!IsCurrentDetailLoad(groupId, loadVersion)) return;
                _serviceOrders = serviceOrders;
            }
            catch (Exception ex)
            {
                warnings.Add($"dịch vụ: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("chưa đăng ký GroupServiceOrderQueryService");
        }

        if (_groupDepositService is not null)
        {
            try
            {
                var groupDeposits = await _groupDepositService.GetAsync(propertyId, groupId, CancellationToken.None);
                if (!IsCurrentDetailLoad(groupId, loadVersion)) return;
                _groupDeposits = groupDeposits;
            }
            catch (Exception ex)
            {
                warnings.Add($"đặt cọc: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("chưa đăng ký GroupDepositService");
        }

        if (_paymentRepository is not null)
        {
            try
            {
                var paymentReconciliation = await LoadPaymentReconciliationAsync(propertyId);
                if (!IsCurrentDetailLoad(groupId, loadVersion)) return;
                _paymentReconciliation = paymentReconciliation;
            }
            catch (Exception ex)
            {
                warnings.Add($"payment allocation: {ex.Message}");
            }
        }
        else
        {
            warnings.Add("chưa đăng ký HotelPaymentRepository");
        }

        if (!IsCurrentDetailLoad(groupId, loadVersion)) return;
        _detailLoadWarning = warnings.Count == 0 ? null : string.Join("; ", warnings);
        RenderStep();
        UpdateSummary();
    }

    private bool IsCurrentDetailLoad(Guid groupId, int loadVersion) =>
        loadVersion == _detailLoadVersion && _group?.GroupReservationId == groupId;

    private async Task<PaymentReconciliation> LoadPaymentReconciliationAsync(Guid propertyId)
    {
        if (_paymentRepository is null)
            return PaymentReconciliation.Empty;

        var groupCode = _group?.Code;
        if (string.IsNullOrWhiteSpace(groupCode))
            return PaymentReconciliation.Empty;

        var folioIds = _allRooms
            .Where(x => x.GroupCode == groupCode && !x.IsBlocked && x.FolioId.HasValue)
            .Select(x => x.FolioId!.Value)
            .Distinct()
            .ToList();
        if (folioIds.Count == 0)
            return PaymentReconciliation.Empty;

        // Payment repository instances share the scoped EF DbContext. Query
        // each Folio sequentially to avoid concurrent operations on it.
        var payments = new List<HotelPaymentDto>();
        foreach (var folioId in folioIds)
            payments.AddRange(await _paymentRepository.GetForFolioAsync(propertyId, folioId, CancellationToken.None));
        var completed = payments.Where(IsCompletedPayment).ToList();
        var netReceived = completed.Sum(x => Math.Max(0m, x.Amount - x.RefundedAmount));
        var allocated = completed.Sum(x => Math.Min(Math.Max(0m, x.AllocatedAmount), Math.Max(0m, x.Amount - x.RefundedAmount)));
        return new PaymentReconciliation(
            folioIds.Count,
            completed.Count,
            netReceived,
            allocated,
            Math.Max(0m, netReceived - allocated));
    }

    private static bool IsCompletedPayment(HotelPaymentDto payment) =>
        payment.Status is HotelPaymentStatus.Completed
            or HotelPaymentStatus.PartiallyRefunded
            or HotelPaymentStatus.Refunded;

    private void InitFilters() { Set(GroupDayFilter, "Tất cả","Hôm nay","Ngày mai"); Set(GroupPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); Set(RoomPageSizeBox, "20 dòng / trang","50 dòng / trang","100 dòng / trang"); }
    private static void Set(ComboBox cb, params string[] v) { cb.ItemsSource = v; cb.SelectedIndex = 0; }

    private void GoTo(int s) { _step = Math.Clamp(s, 1, 6); BuildStepBar(); Step1Content.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed; Step2Content.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed; Step3Content.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed; Step4Content.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed; Step5Content.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed; Step6Content.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; RenderStep(); UpdateNav(); UpdateSummary(); }

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A","Chọn đoàn"),("B","Phòng & khách"),("C","Kiểm tra Folio"),("D","DV & bàn giao"),("E","HĐ & thanh toán"),("F","Xác nhận") };
        for (int i = 0; i < 6; i++) { var idx = i + 1; var a = idx == _step; var b = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(2, 6, 2, 6), Cursor = System.Windows.Input.Cursors.Hand, Background = a ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = a ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1) }; b.MouseLeftButtonDown += (_, _) => GoTo(idx); var sp = new StackPanel { Orientation = Orientation.Horizontal }; sp.Children.Add(new TextBlock { Text = steps[i].Item1, FontSize = 13, FontWeight = FontWeights.Bold, Foreground = a ? C("#2F6BFF") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = " " + steps[i].Item2, FontSize = 12, Foreground = a ? C("#1E40AF") : C("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) }); b.Child = sp; StepBar.Children.Add(b); if (i < 5) StepBar.Children.Add(Sep()); }
    }

    private void RenderStep() { switch (_step) { case 2: BuildStep2(); break; case 3: BuildStep3(); break; case 4: BuildStep4(); break; case 5: BuildStep5(); break; case 6: BuildStep6(); break; } }

    private void UpdateNav()
    {
        BtnBack.IsEnabled = _step > 1;
        if (_step == 6) { BtnNext.Visibility = Visibility.Collapsed; BtnConfirm.Visibility = Visibility.Visible; BtnConfirm.IsEnabled = CanConfirm(); }
        else { BtnNext.Visibility = Visibility.Visible; BtnConfirm.Visibility = Visibility.Collapsed; }
        var blocked = _blockedRooms.Count; var staying = _stayingRooms.Count;
        FooterStepLabel.Text = $"Bước {_step}/6 · {new[] { "Chọn đoàn","Phòng & khách","Kiểm tra Folio","DV & bàn giao","HĐ & thanh toán","Xác nhận" }[_step - 1]} · {_selectedRooms.Count} chọn · {staying} ở lại · {blocked} chặn";
    }

    private void UpdateSummary()
    {
        SummaryStrip.Children.Clear(); var g = _group; var sel = _groupSelected;
        SummaryStrip.Children.Add(Strip("Đoàn", g?.Name ?? "Chưa chọn", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Phòng", sel ? $"{_selectedRooms.Count}/{g?.RoomCount}" : "0/0", sel)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Ở lại", $"{_stayingRooms.Count}", _stayingRooms.Count > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Bị chặn", $"{_blockedRooms.Count}", _blockedRooms.Count > 0)); SummaryStrip.Children.Add(Sep());
        SummaryStrip.Children.Add(Strip("Còn P/Thu", sel ? $"{_masterBalance:N0}" : "—", _masterBalance == 0)); SummaryStrip.Children.Add(Sep());
        var selectedFoliosReady = sel && _allRooms
            .Where(x => x.GroupCode == g?.Code && _selectedRooms.Contains(x.Room))
            .All(x => x.HasFolio);
        var ready = sel && _selectedRooms.Count > 0 && _blockedRooms.Count == 0 && selectedFoliosReady;
        SummaryStrip.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF9C3"), CornerRadius = new CornerRadius(8), Padding = new Thickness(8, 2, 8, 2), Child = new TextBlock { Text = ready ? "Sẵn sàng" : "Chưa SS", FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#A16207") } });
    }

    // STEP 1: Group
    private List<GpGroup> GetFilteredGroups() { var f = _allGroups.AsEnumerable(); var kw = GroupSearchBox.Text?.ToLower(); if (!string.IsNullOrWhiteSpace(kw)) f = f.Where(g => g.Name.ToLower().Contains(kw) || g.Code.ToLower().Contains(kw) || g.Company.ToLower().Contains(kw)); return f.ToList(); }
    private void RefreshGroupPage(bool reset) { if (reset) _groupPage = 0; var f = GetFilteredGroups(); var tp = Math.Max(1, (f.Count + _groupPageSize - 1) / _groupPageSize); if (_groupPage >= tp) _groupPage = tp - 1; GroupGrid.ItemsSource = f.Skip(_groupPage * _groupPageSize).Take(_groupPageSize).ToList(); GroupPagerSummary.Text = $"Hiển thị {_groupPage * _groupPageSize + 1}–{Math.Min((_groupPage + 1) * _groupPageSize, f.Count)} trên tổng {f.Count} đoàn"; GroupPrevBtn.IsEnabled = _groupPage > 0; GroupNextBtn.IsEnabled = _groupPage < tp - 1; BuildGroupBtns(tp); }
    private void BuildGroupBtns(int tp) { GroupPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _groupPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _groupPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _groupPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _groupPage = idx; RefreshGroupPage(false); }; nb.Template = GroupPrevBtn.Template; GroupPageBtns.Children.Add(nb); } }
    private void OnGroupPreviewChanged(object s, SelectionChangedEventArgs e) { if (GroupGrid.SelectedItem is not GpGroup g) return; _group = g; _groupSelected = false; GroupEmptyState.Visibility = Visibility.Collapsed; GroupDetailPanel.Visibility = Visibility.Visible; GDName.Text = g.Name; GDMeta.Text = $"{g.Code} · {g.Company}"; GDInfo1.Text = $"Phòng: {g.RoomCount} · Khách: {g.GuestCount}"; GDInfo2.Text = $"Ngày: {g.Arrival} → {g.Departure}"; GDInfo3.Text = $"Còn phải thu: {g.Balance:N0} VND"; GDInfo4.Text = $"Trạng thái: {g.Status}"; UpdateSummary(); }
    private void OnSelectGroup(object s, RoutedEventArgs e) { if (_group == null) return; _groupSelected = true; _masterBalance = _group.Outstanding; _selectedRooms.Clear(); _blockedRooms.Clear(); _stayingRooms.Clear(); var gr = _allRooms.Where(r => r.GroupCode == _group.Code).ToList(); foreach (var r in gr) { if (r.IsBlocked) _blockedRooms.Add(r.Room); else if (r.IsStaying) _stayingRooms.Add(r.Room); else _selectedRooms.Add(r.Room); } BtnSelectGroup.Content = "✓ Đã chọn"; BtnSelectGroup.IsEnabled = false; UpdateSummary(); UpdateNav(); _ = LoadSelectedGroupDetailsAsync(_group.GroupReservationId); }

    // STEP 2: Rooms
    private void BuildStep2()
    {
        Step2Toolbar.Children.Clear(); if (_group == null) return;
        var selectAll = new Button { Content = "Chọn tất cả đủ ĐK", Style = FindResource("SecondaryButton") as Style, Height = 32, FontSize = 12, Margin = new Thickness(0, 0, 6, 0) };
        selectAll.Click += (_, _) =>
        {
            _selectedRooms.Clear();
            foreach (var room in _allRooms.Where(x => x.GroupCode == _group.Code && !x.IsBlocked && !x.IsStaying))
                _selectedRooms.Add(room.Room);
            RefreshRoomPage(false);
            UpdateSummary();
            UpdateNav();
        };
        Step2Toolbar.Children.Add(selectAll);
        var clearSelection = new Button { Content = "Bỏ chọn", Style = FindResource("SecondaryButton") as Style, Height = 32, FontSize = 12, Margin = new Thickness(0, 0, 6, 0) };
        clearSelection.Click += (_, _) =>
        {
            _selectedRooms.Clear();
            RefreshRoomPage(false);
            UpdateSummary();
            UpdateNav();
        };
        Step2Toolbar.Children.Add(clearSelection);
        var sc = new ComboBox { Width = 120, Style = FindResource("ModernComboBox") as Style, Margin = new Thickness(0, 0, 6, 0) }; Set(sc, "Tất cả","Sẵn sàng","Ở lại","Bị chặn","Trả sớm"); Step2Toolbar.Children.Add(sc);
        Step2Toolbar.Children.Add(new TextBlock { Text = $"✓ {_selectedRooms.Count} | ⛔ {_blockedRooms.Count} | 🏠 {_stayingRooms.Count}", FontSize = 11, Foreground = C("#64748B"), VerticalAlignment = VerticalAlignment.Center });
        var gr = _allRooms.Where(r => r.GroupCode == _group.Code).ToList();
        RoomGrid.Columns.Clear(); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Chọn", Binding = new System.Windows.Data.Binding("SelStatus"), Width = 50 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Phòng", Binding = new System.Windows.Data.Binding("Room"), Width = 55 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Khách", Binding = new System.Windows.Data.Binding("Guest"), Width = 130 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "SK", Binding = new System.Windows.Data.Binding("GuestCount"), Width = 40 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Ngày đi", Binding = new System.Windows.Data.Binding("Departure"), Width = 70 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Folio", Binding = new System.Windows.Data.Binding("BalanceDisplay"), Width = 90 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Minibar", Binding = new System.Windows.Data.Binding("MinibarStatus"), Width = 70 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "HSK", Binding = new System.Windows.Data.Binding("HskStatus"), Width = 60 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Thẻ", Binding = new System.Windows.Data.Binding("KeyStatus"), Width = 60 }); RoomGrid.Columns.Add(new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("FullStatus"), Width = 90 });
        RefreshRoomPage(true);
        RoomDetailPanel.Children.Clear(); RoomDetailPanel.Children.Add(STitle("Chi tiết phòng")); RoomDetailPanel.Children.Add(new TextBlock { Text = "Click dòng để xem chi tiết phòng.", FontSize = 12, Foreground = C("#94A3B8") });
    }
    private void RefreshRoomPage(bool reset)
    {
        if (reset) _roomPage = 0;
        var gr = _allRooms
            .Where(r => _group != null && r.GroupCode == _group.Code)
            .Select(r =>
            {
                dynamic x = new
                {
                    r.Room,
                    r.Guest,
                    r.GuestCount,
                    r.Departure,
                    r.StayId,
                    r.FolioId,
                    r.FolioNumber,
                    r.HasFolio,
                    BalanceDisplay = r.BalanceDisplay,
                    SelStatus = _selectedRooms.Contains(r.Room) ? "✓" : _blockedRooms.Contains(r.Room) ? "⛔" : _stayingRooms.Contains(r.Room) ? "🏠" : "○",
                    MinibarStatus = r.MinibarStatus,
                    HskStatus = r.HskStatus,
                    KeyStatus = r.KeyStatus,
                    FullStatus = _blockedRooms.Contains(r.Room) ? (r.HasFolio ? "Bị chặn" : "Thiếu Folio") : _stayingRooms.Contains(r.Room) ? "Ở lại" : r.HasFolio ? "Sẵn sàng" : "Thiếu Folio"
                };
                return x;
            })
            .ToList();
        var tp = Math.Max(1, (gr.Count + _roomPageSize - 1) / _roomPageSize);
        if (_roomPage >= tp) _roomPage = tp - 1;
        RoomGrid.ItemsSource = gr.Skip(_roomPage * _roomPageSize).Take(_roomPageSize).ToList();
        RoomPagerSummary.Text = $"Hiển thị {_roomPage * _roomPageSize + 1}–{Math.Min((_roomPage + 1) * _roomPageSize, gr.Count)} trên tổng {gr.Count} phòng";
        RoomPrevBtn.IsEnabled = _roomPage > 0;
        RoomNextBtn.IsEnabled = _roomPage < tp - 1;
        BuildRoomBtns(tp);
    }
    private void BuildRoomBtns(int tp) { RoomPageBtns.Children.Clear(); for (int i = 0; i < tp && i < 7; i++) { var idx = i; var nb = new Button { Content = (i + 1).ToString(), Width = 32, Height = 30, FontSize = 11, Margin = new Thickness(2, 0, 2, 0), Background = i == _roomPage ? Cb("#2F6BFF") : Cb("#F1F5F9"), Foreground = i == _roomPage ? Cb("#FFF") : Cb("#475569"), BorderThickness = new Thickness(i == _roomPage ? 0 : 1), BorderBrush = Cb("#E2E8F0"), Cursor = System.Windows.Input.Cursors.Hand }; nb.Click += (_, _) => { _roomPage = idx; RefreshRoomPage(false); }; nb.Template = RoomPrevBtn.Template; RoomPageBtns.Children.Add(nb); } }
    private void OnRoomPreviewChanged(object s, SelectionChangedEventArgs e)
    {
        if (RoomGrid.SelectedItem == null) return;
        dynamic row = RoomGrid.SelectedItem;
        RoomDetailPanel.Children.Clear();
        RoomDetailPanel.Children.Add(STitle("Chi tiết phòng " + row.Room));
        RoomDetailPanel.Children.Add(Row("Phòng", (string)row.Room)); RoomDetailPanel.Children.Add(Row("Khách", (string)row.Guest));
        RoomDetailPanel.Children.Add(Row("Số khách", (string)row.GuestCount)); RoomDetailPanel.Children.Add(Row("Ngày đi", (string)row.Departure));
        RoomDetailPanel.Children.Add(Row("Folio", (string)row.BalanceDisplay));
        if (row.HasFolio is true && row.StayId is Guid stayId)
        {
            var openFolio = new Button
            {
                Content = string.IsNullOrWhiteSpace((string?)row.FolioNumber) ? "Mở Folio thật" : $"Mở {row.FolioNumber}",
                Style = FindResource("SecondaryButton") as Style,
                Height = 32,
                Margin = new Thickness(0, 8, 0, 0)
            };
            openFolio.Click += (_, _) => new FolioWorkspaceWindow(stayId).ShowDialog();
            RoomDetailPanel.Children.Add(openFolio);
        }
        RoomDetailPanel.Children.Add(Row("Minibar", (string)row.MinibarStatus)); RoomDetailPanel.Children.Add(Row("HSK", (string)row.HskStatus));
        RoomDetailPanel.Children.Add(Row("Thẻ", (string)row.KeyStatus)); RoomDetailPanel.Children.Add(Row("Trạng thái", (string)row.FullStatus));
        var sel = (string)row.SelStatus;
        var blocked = sel == "⛔"; var staying = sel == "🏠";
        if (blocked) RoomDetailPanel.Children.Add(new Border { Background = Cb("#FEE2E2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), Child = new TextBlock { Text = "⛔ Phòng bị chặn — không thể chọn", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#DC2626") } });
        else if (staying) RoomDetailPanel.Children.Add(new Border { Background = Cb("#DBEAFE"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10), Margin = new Thickness(0, 8, 0, 0), Child = new TextBlock { Text = "🏠 Phòng ở lại — không tự động chọn", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#2563EB") } });
    }
    private void OnRoomPrevPage(object s, RoutedEventArgs e) { if (_roomPage > 0) { _roomPage--; RefreshRoomPage(false); } }
    private void OnRoomNextPage(object s, RoutedEventArgs e) { _roomPage++; RefreshRoomPage(false); }
    private void OnRoomPageSizeChanged(object s, SelectionChangedEventArgs e) { _roomPageSize = RoomPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshRoomPage(true); }

    // STEP 3: Folio
    private void BuildUnavailableStep(Panel left, Panel right, string title, string reason)
    {
        left.Children.Clear();
        right.Children.Clear();
        left.Children.Add(STitle(title));
        left.Children.Add(new Border
        {
            Background = Cb("#FEF3C7"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(12),
            Child = new TextBlock
            {
                Text = reason,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Foreground = C("#92400E")
            }
        });
        right.Children.Add(STitle("Trạng thái"));
        right.Children.Add(new TextBlock
        {
            Text = "Thao tác bị chặn để không hiển thị hoặc ghi số liệu giả.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = C("#64748B")
        });
    }

    private void BuildStep3()
    {
        Step3Left.Children.Clear();
        Step3Right.Children.Clear();
        Step3Left.RowDefinitions.Clear();
        Step3Left.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Step3Left.RowDefinitions.Add(new RowDefinition());
        Step3Right.RowDefinitions.Clear();
        Step3Right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Step3Right.RowDefinitions.Add(new RowDefinition());
        if (_group is null)
        {
            BuildUnavailableStep(Step3Left, Step3Right, "Kiểm tra Folio", "Chưa chọn đoàn.");
            return;
        }

        var billing = _billingSummary;
        var totalCharges = billing?.TotalCharges ?? _group.TotalCharges;
        var totalPayments = billing?.TotalPayments ?? _group.TotalPayments;
        var totalDeposits = billing?.TotalDeposits ?? _group.TotalDeposits;
        var outstanding = billing?.Outstanding ?? _group.Outstanding;
        var isBalanced = billing?.IsBalanced ?? _group.IsBalanced;
        var summary = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
        summary.Children.Add(Metric("Tổng doanh thu", $"{totalCharges:N0}"));
        summary.Children.Add(Metric("Đã thanh toán", $"{totalPayments:N0}"));
        summary.Children.Add(Metric("Đặt cọc", $"{totalDeposits:N0}"));
        summary.Children.Add(Metric("Còn phải thu", $"{outstanding:N0}"));
        Grid.SetRow(summary, 0);
        Step3Left.Children.Add(summary);

        var detail = new StackPanel();
        detail.Children.Add(STitle("Đối chiếu số dư đoàn"));
        detail.Children.Add(Row("Đoàn", _group.Name));
        detail.Children.Add(Row("Mã đoàn", _group.Code));
        detail.Children.Add(Row("Nguồn", billing is null ? "GroupReservation totals / EF" : "EfGroupBillingService + EF ledger"));
        detail.Children.Add(Row("Folio", billing is null ? "Chưa đọc được summary billing" : $"{billing.MasterFolioCount + billing.IndividualFolioCount:N0} Folio"));
        detail.Children.Add(Row("Trạng thái", isBalanced ? "Đã cân" : "Còn phải thu"));
        detail.Children.Add(new Border
        {
            Background = Cb(isBalanced ? "#DCFCE7" : "#FEF3C7"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 10, 0, 0),
            Child = new TextBlock
            {
                Text = isBalanced
                    ? "Tổng số dư nhóm đã cân theo các ledger thật hiện có."
                    : "Còn phải thu theo dữ liệu thật; khi xác nhận sẽ phát hành HĐ cho Folio chưa có HĐ, không tự tạo payment."
            }
        });
        Grid.SetRow(detail, 1);
        Step3Left.Children.Add(detail);

        Step3Right.Children.Add(STitle("Phạm vi kiểm tra"));
        var scope = new StackPanel();
        scope.Children.Add(Row("Đã đọc", $"Billing summary, Folio ledger, đặt cọc ({_groupDeposits.Count:N0} dòng)"));
        scope.Children.Add(Row("Dịch vụ", $"{_serviceOrders.Count:N0} service order thật"));
        scope.Children.Add(Row("Payment", _paymentReconciliation is { } payment
            ? $"{payment.PaymentCount:N0} payment thật · chưa phân bổ {payment.UnallocatedAmount:N0}"
            : "Chưa đọc được payment thật"));
        scope.Children.Add(Row("Có thật", "Phát hành HĐ theo từng Folio khi xác nhận check-out"));
        scope.Children.Add(Row("Giữ nguyên", "Payment allocation chỉ đọc/đối chiếu; thao tác phân bổ vẫn dùng Cashier"));
        if (!string.IsNullOrWhiteSpace(_detailLoadWarning))
            scope.Children.Add(new TextBlock { Text = "Cảnh báo tải chi tiết: " + _detailLoadWarning, TextWrapping = TextWrapping.Wrap, Foreground = C("#92400E"), Margin = new Thickness(0, 8, 0, 0) });
        Grid.SetRow(scope, 1);
        Step3Right.Children.Add(scope);
    }
    // STEP 4: Services & Handover
    private void BuildStep4()
    {
        Step4Left.Children.Clear();
        Step4Right.Children.Clear();
        if (_group is null)
        {
            BuildUnavailableStep(Step4Left, Step4Right, "Dịch vụ & bàn giao", "Chưa chọn đoàn.");
            return;
        }

        Step4Left.Children.Add(STitle("Dịch vụ đoàn đã ghi nhận"));
        if (_serviceOrders.Count == 0)
        {
            Step4Left.Children.Add(new TextBlock { Text = "Không có service order thật cho đoàn này.", Foreground = C("#64748B"), Margin = new Thickness(0, 6, 0, 0) });
        }
        else
        {
            foreach (var service in _serviceOrders.Take(20))
            {
                Step4Left.Children.Add(Row(
                    $"{service.ServiceDate:dd/MM} · {service.ServiceName}",
                    $"SL {service.Quantity:N0} · {service.TotalAmount:N0} · {service.Status}"));
            }
            if (_serviceOrders.Count > 20)
                Step4Left.Children.Add(new TextBlock { Text = $"… và {_serviceOrders.Count - 20:N0} dòng khác", Foreground = C("#64748B"), Margin = new Thickness(0, 6, 0, 0) });
        }

        Step4Right.Children.Add(STitle("Bàn giao phòng"));
        Step4Right.Children.Add(new Border
        {
            Background = Cb("#FEF3C7"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Child = new TextBlock
            {
                Text = "Minibar vẫn phải được kiểm tra/post bằng giao dịch thật. Khi xác nhận check-out, hệ thống tự tạo DepartureClean task Housekeeping thật cho từng phòng chưa có task đang hoạt động.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = C("#92400E")
            }
        });
        Step4Right.Children.Add(Row("Nguồn dịch vụ", "IGroupServiceOrderQueryService / EF"));
    }

    // STEP 5: Invoice & Payment
    private void BuildStep5()
    {
        Step5Left.Children.Clear();
        Step5Right.Children.Clear();
        if (_group is null)
        {
            BuildUnavailableStep(Step5Left, Step5Right, "Hóa đơn & thanh toán", "Chưa chọn đoàn.");
            return;
        }

        Step5Left.Children.Add(STitle("Đối chiếu thanh toán thật"));
        if (_billingSummary is null)
        {
            Step5Left.Children.Add(new TextBlock { Text = "Chưa đọc được billing summary thật; không thể hiển thị số liệu thay thế.", TextWrapping = TextWrapping.Wrap, Foreground = C("#92400E") });
        }
        else
        {
            Step5Left.Children.Add(Row("Tổng charge", $"{_billingSummary.TotalCharges:N0}"));
            Step5Left.Children.Add(Row("Đã thanh toán", $"{_billingSummary.TotalPayments:N0}"));
            Step5Left.Children.Add(Row("Đặt cọc", $"{_billingSummary.TotalDeposits:N0}"));
            Step5Left.Children.Add(Row("Còn phải thu", $"{_billingSummary.Outstanding:N0}"));
            Step5Left.Children.Add(Row("Folio", $"Master {_billingSummary.MasterFolioCount:N0} · Cá nhân {_billingSummary.IndividualFolioCount:N0}"));
            Step5Left.Children.Add(Row("Trạng thái", _billingSummary.IsBalanced ? "Đã cân" : "Còn phải thu"));
        }

        if (_paymentReconciliation is { } paymentReconciliation)
        {
            Step5Left.Children.Add(Row("Payment thật", $"{paymentReconciliation.PaymentCount:N0} payment / {paymentReconciliation.FolioCount:N0} Folio"));
            Step5Left.Children.Add(Row("Đã nhận ròng", $"{paymentReconciliation.NetReceivedAmount:N0}"));
            Step5Left.Children.Add(Row("Đã phân bổ", $"{paymentReconciliation.AllocatedAmount:N0}"));
            Step5Left.Children.Add(Row("Chưa phân bổ", $"{paymentReconciliation.UnallocatedAmount:N0}"));
        }
        else
        {
            Step5Left.Children.Add(Row("Payment thật", "Chưa đọc được từ repository"));
        }

        Step5Right.Children.Add(STitle("Giới hạn nghiệp vụ"));
        Step5Right.Children.Add(Row("Đặt cọc", $"{_groupDeposits.Count:N0} dòng thật"));
        Step5Right.Children.Add(Row("Hóa đơn", "Phát hành theo Folio khi xác nhận check-out"));
        Step5Right.Children.Add(Row("Payment allocation", "Không tự tạo; chỉ phân bổ khi có payment thật"));
        if (_paymentReconciliation is null)
        {
            Step5Right.Children.Add(new Border
            {
                Background = Cb("#FEE2E2"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 0),
                Child = new TextBlock
                {
                    Text = "Chưa đọc được payment thật; không dùng số 0 thay thế và cần kiểm tra lại repository/context.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = C("#991B1B")
                }
            });
        }
        else if (_paymentReconciliation.UnallocatedAmount > 0.01m)
        {
            Step5Right.Children.Add(new Border
            {
                Background = Cb("#FEF3C7"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 10, 0, 0),
                Child = new TextBlock
                {
                    Text = $"Có {_paymentReconciliation.UnallocatedAmount:N0} payment thật chưa phân bổ vào khoản mục. Hãy xử lý tại Cashier trước khi cần đối chiếu chi tiết; màn hình này không tự gán khoản mục.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = C("#92400E")
                }
            });
        }
        Step5Right.Children.Add(new Border
        {
            Background = Cb("#FEF3C7"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 10, 0, 0),
            Child = new TextBlock
            {
                Text = "Hóa đơn được phát hành thật cho Folio chưa có hóa đơn trong cùng giao dịch check-out. Payment allocation được đọc từ bảng nghiệp vụ thật; payment không tự sinh và không bị phân bổ giả.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = C("#92400E")
            }
        });
    }

    // STEP 6: Confirm
    private void BuildStep6()
    {
        Step6Left.Children.Clear();
        Step6Right.Children.Clear();
        if (_group is null)
        {
            BuildUnavailableStep(Step6Left, Step6Right, "Xác nhận Check-out", "Chưa chọn đoàn.");
            return;
        }

        Step6Left.Children.Add(STitle("Tóm tắt thao tác sẽ ghi"));
        Step6Left.Children.Add(Row("Đoàn", $"{_group.Code} · {_group.Name}"));
        Step6Left.Children.Add(Row("Phòng trả", $"{_selectedRooms.Count:N0}"));
        Step6Left.Children.Add(Row("Phòng ở lại", $"{_stayingRooms.Count:N0}"));
        Step6Left.Children.Add(Row("Phòng bị chặn", $"{_blockedRooms.Count:N0}"));
        Step6Left.Children.Add(Row("Command", "EfGroupCheckOutCommandService + HĐ thật"));

        Step6Right.Children.Add(STitle("Điều kiện và giới hạn"));
        Step6Right.Children.Add(Row("Điều kiện", _blockedRooms.Count == 0 && _selectedRooms.Count > 0 ? "Đủ phòng hợp lệ" : "Chưa đủ điều kiện"));
        Step6Right.Children.Add(Row("Thanh toán", _billingSummary?.IsBalanced == true ? "Đã cân" : "Còn phải thu / chưa đọc đủ"));
        Step6Right.Children.Add(new Border
        {
            Background = Cb("#DBEAFE"),
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 10, 0, 0),
            Child = new TextBlock
            {
                Text = "Xác nhận check-out assignment/group thật và phát hành HĐ cho Folio chưa có HĐ trong cùng transaction. Không tự tạo payment, minibar hoặc bàn giao giả.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = C("#1E40AF")
            }
        });
    }

    private FrameworkElement HOSection(string title, (string, string, string)[] items)
    {
        var sp = new StackPanel(); sp.Children.Add(new TextBlock { Text = title, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#0F172A"), Margin = new Thickness(0, 6, 0, 4) });
        foreach (var (label, status, who) in items) { var row = new Grid { Margin = new Thickness(0, 2, 0, 0) }; row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) }); row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); row.ColumnDefinitions.Add(new ColumnDefinition()); row.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B") }); row.Children.Add(new TextBlock { Text = status, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = status == "✓" ? C("#16A34A") : C("#EA580C") }); Grid.SetColumn(row.Children[^1], 1); row.Children.Add(new TextBlock { Text = who, FontSize = 11, Foreground = C("#94A3B8") }); Grid.SetColumn(row.Children[^1], 2); sp.Children.Add(row); }
        return new Border { Background = Cb("#FFFFFF"), CornerRadius = new CornerRadius(10), BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 6), Child = sp };
    }
    private static FrameworkElement Metric(string label, string value) => new Border { Background = Cb("#FAFBFD"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 6, 4), Child = new StackPanel { Children = { new TextBlock { Text = value, FontSize = 16, FontWeight = FontWeights.Bold, Foreground = C("#17365D") }, new TextBlock { Text = label, FontSize = 10, Foreground = C("#94A3B8") } } } };

    // Group pager
    private void OnApplyGroupFilters(object s, RoutedEventArgs e) => RefreshGroupPage(true);
    private void OnClearGroupFilters(object s, RoutedEventArgs e) { GroupSearchBox.Text = ""; GroupDayFilter.SelectedIndex = 0; RefreshGroupPage(true); }
    private void OnGroupPrevPage(object s, RoutedEventArgs e) { if (_groupPage > 0) { _groupPage--; RefreshGroupPage(false); } }
    private void OnGroupNextPage(object s, RoutedEventArgs e) { _groupPage++; RefreshGroupPage(false); }
    private void OnGroupPageSizeChanged(object s, SelectionChangedEventArgs e) { _groupPageSize = GroupPageSizeBox.SelectedIndex switch { 0 => 20, 1 => 50, 2 => 100, _ => 20 }; RefreshGroupPage(true); }

    // Nav
    private void OnNext(object s, RoutedEventArgs e) { if (_step < 6) GoTo(_step + 1); }
    private void OnBack(object s, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private async void OnConfirm(object s, RoutedEventArgs e)
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await ConfirmCoreAsync();
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task ConfirmCoreAsync()
    {
        if (!CanConfirm())
        {
            MessageBox.Show("Chưa đủ điều kiện: cần chọn đoàn, có phòng hợp lệ, Folio thật cho từng phòng và không có phòng bị chặn.", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!_groupSelected || _group is null)
        {
            MessageBox.Show("Hãy chọn một đoàn trước khi xác nhận.", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_groupCheckOutService is null)
        {
            MessageBox.Show("Chưa đăng ký command check-out đoàn thật. Dữ liệu chưa được ghi.", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var rooms = _allRooms
            .Where(x => x.GroupCode == _group.Code && _selectedRooms.Contains(x.Room) && !x.IsBlocked && !x.IsStaying)
            .ToList();
        if (rooms.Any(x => !x.HasFolio))
        {
            MessageBox.Show("Có phòng được chọn nhưng chưa có Folio thật. Hãy mở Folio của phòng để xử lý trước khi check-out.", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (rooms.Count == 0)
        {
            MessageBox.Show("Không có phòng đã check-in hợp lệ để trả.", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var result = await _groupCheckOutService.CheckOutAndIssueInvoicesAsync(
                _group.GroupReservationId,
                rooms.Select(room => room.AssignmentId).ToList(),
                DateTime.UtcNow,
                CancellationToken.None);

            MessageBox.Show($"Đã check-out thật {result.CheckedOutRooms} phòng của đoàn {result.GroupCode}; phát hành {result.InvoicesGenerated} hóa đơn, tạo {result.HousekeepingTasksCreated} task Housekeeping. Trạng thái đoàn: {result.Status}.", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Information);
            _groupSelected = false;
            _group = null;
            _selectedRooms.Clear();
            _blockedRooms.Clear();
            _stayingRooms.Clear();
            await LoadPersistedDataCoreAsync();
            GoTo(1);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể check-out đoàn. Giao dịch đã rollback; trạng thái chưa được ghi dở dang.\n{ex.Message}", "Check-out đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
            await LoadPersistedDataCoreAsync();
        }
    }

    private bool CanConfirm() =>
        _groupSelected
        && _group is not null
        && _selectedRooms.Count > 0
        && _blockedRooms.Count == 0
        && _allRooms.Where(x => x.GroupCode == _group.Code && _selectedRooms.Contains(x.Room)).All(x => x.HasFolio)
        && _groupCheckOutService is not null;

    private void OnRefresh(object s, RoutedEventArgs e) => GoTo(1);
    private void OnClose(object s, RoutedEventArgs e) => Close();

    // Helpers
    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 8) };
    private static TextBlock Row(string l, string v) => new TextBlock { FontSize = 12, Foreground = C("#6F8198"), Margin = new Thickness(0, 2, 0, 0) }.WithRuns5((C("#94A3B8"), l + ": ", false), (C("#17365D"), v, true));
    private static FrameworkElement Strip(string l, string v, bool ok) { var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 0, 4, 0) }; sp.Children.Add(new TextBlock { Text = l + ": ", FontSize = 11, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); sp.Children.Add(new TextBlock { Text = v, FontSize = 11, FontWeight = ok ? FontWeights.SemiBold : FontWeights.Normal, Foreground = ok ? C("#17365D") : C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center }); return sp; }
    private static TextBlock Sep() => new() { Text = " │ ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center };
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));
}

internal record GpGroup(Guid GroupReservationId, string Code, string Name, string Company, string GuestCount, string Arrival, string Departure, int RoomCount, int TotalGuests, decimal TotalCharges, decimal TotalPayments, decimal TotalDeposits, decimal Outstanding, bool IsBalanced, string Status)
{
    public decimal Balance => Outstanding;
    public string BalanceDisplay => $"{Balance:N0}";
}
internal record GpRoom(Guid AssignmentId, string Room, string GroupCode, string Guest, string GuestCount, string Departure, decimal Balance, bool HasFolio, Guid? StayId, Guid? FolioId, string? FolioNumber, string MinibarStatus, string HskStatus, string KeyStatus, bool IsBlocked, bool IsStaying, bool IsEarly)
{
    public string BalanceDisplay => HasFolio ? $"{Balance:N0}" : "Chưa có Folio";
}
internal sealed record PaymentReconciliation(int FolioCount, int PaymentCount, decimal NetReceivedAmount, decimal AllocatedAmount, decimal UnallocatedAmount)
{
    public static PaymentReconciliation Empty { get; } = new(0, 0, 0m, 0m, 0m);
}
internal static class GpExt { public static TextBlock WithRuns5(this TextBlock tb, params (SolidColorBrush, string, bool)[] runs) { foreach (var (b, t, w) in runs) tb.Inlines.Add(new System.Windows.Documents.Run(t) { Foreground = b, FontWeight = w ? FontWeights.SemiBold : FontWeights.Normal }); return tb; } }
