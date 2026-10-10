using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.FrontOffice.Cashier;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.NightAudit;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Application.FrontOffice.Stay;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.NightAudit;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

/// <summary>Small in-app command center for the three Night Audit-only steps.</summary>
public partial class NightAuditCenterWindow : Window
{
    private readonly ObservableCollection<AuditCheckItem> checks = new()
    {
        new("Folio đang mở", "Folio", "Chưa kiểm tra", "Chưa tải dữ liệu", "Đang chờ tải dữ liệu Folio từ cơ sở dữ liệu."),
        new("Chênh lệch thanh toán", "Đối chiếu", "Chưa kiểm tra", "Chưa tải dữ liệu", "Đang chờ tải settlement thật từ cơ sở dữ liệu."),
        new("Doanh thu trong ngày", "Doanh thu", "Sẽ tạo", "Chưa có snapshot doanh thu", "Snapshot doanh thu sẽ được tạo bằng dữ liệu thật ở bước GenerateRevenueSnapshot.", blocksRun: false),
        new("Khách chưa check-out", "Lễ tân", "Chưa kiểm tra", "Chưa tải dữ liệu", "Đang chờ tải danh sách lưu trú từ cơ sở dữ liệu."),
        new("Đối soát thu ngân chờ xử lý", "Thu ngân", "Chưa kiểm tra", "Chưa tải dữ liệu", "Đang chờ tải các ca thu ngân/đối soát pending từ cơ sở dữ liệu."),
        new("Đồng bộ hệ thống", "Hệ thống", "Chưa kiểm tra", "Chưa tải dữ liệu", "Đang chờ chạy health-check database và migration thật.")
    };

    private string currentSection = "check";
    private readonly NightAuditApplicationService? _auditService;
    private readonly INightAuditRepository? _auditRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IHotelStayRepository? _stayRepository;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly ICashReconciliationService? _cashReconciliationService;
    private readonly IInfrastructureHealthService? _infrastructureHealthService;
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);
    private NightAuditDto? _activeAudit;
    private DateOnly _businessDate;
    private bool _persistedMode;
    private bool _isClosed;

    public NightAuditCenterWindow(string section)
    {
        InitializeComponent();
        CheckGrid.ItemsSource = checks;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _auditService = scopedServices?.GetService<NightAuditApplicationService>();
        _auditRepository = scopedServices?.GetService<INightAuditRepository>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _stayRepository = scopedServices?.GetService<IHotelStayRepository>();
        _folioRepository = scopedServices?.GetService<IHotelFolioRepository>();
        _paymentRepository = scopedServices?.GetService<IHotelPaymentRepository>();
        _cashReconciliationService = scopedServices?.GetService<ICashReconciliationService>();
        _infrastructureHealthService = scopedServices?.GetService<IInfrastructureHealthService>();
        _persistedMode = _auditService is not null && _auditRepository is not null && _contextAccessor is not null;
        RunItems.ItemsSource = Array.Empty<RunItem>();
        BusinessDateText.Text = "Đang tải ngày kinh doanh…";
        ShowSection(section);
        CheckGrid.SelectedItem = checks.FirstOrDefault();
        Loaded += async (_, _) => await LoadPersistedAsync();
        Closed += OnClosed;
    }

    public void ShowSection(string section)
    {
        currentSection = section is "check" or "run" or "close" ? section : "check";
        CheckPanel.Visibility = currentSection == "check" ? Visibility.Visible : Visibility.Collapsed;
        RunPanel.Visibility = currentSection == "run" ? Visibility.Visible : Visibility.Collapsed;
        ClosePanel.Visibility = currentSection == "close" ? Visibility.Visible : Visibility.Collapsed;
        UpdateStepAppearance();
        FooterText.Text = currentSection switch
        {
            "check" => "Bước 1/3 · Kiểm tra các điều kiện trước khi chạy.",
            "run" => "Bước 2/3 · Chạy Night Audit sau khi các điều kiện đã sẵn sàng.",
            _ => "Bước 3/3 · Xác nhận và đóng ngày kinh doanh."
        };
        if (currentSection == "close") UpdateCloseState();
    }

    private void Step_Click(object sender, RoutedEventArgs e) => ShowSection((sender as Button)?.Tag?.ToString() ?? "check");
    private void Next_Click(object sender, RoutedEventArgs e) => ShowSection(currentSection == "check" ? "run" : currentSection == "run" ? "close" : "check");
    private void Back_Click(object sender, RoutedEventArgs e) => ShowSection(currentSection == "close" ? "run" : currentSection == "run" ? "check" : "check");
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadPersistedAsync();
        RunStatus.Text = "Đã làm mới dữ liệu kiểm tra lúc " + DateTime.Now.ToString("HH:mm") + ".";
    }

    private void CheckGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CheckGrid.SelectedItem is not AuditCheckItem item) return;
        SelectedCheckName.Text = item.Name;
        SelectedCheckDetail.Text = item.Source + " · " + item.Detail;
        SelectedCheckHint.Text = item.Hint;
    }

    private void MarkChecked_Click(object sender, RoutedEventArgs e)
    {
        if (CheckGrid.SelectedItem is not AuditCheckItem item) return;
        if (item.Status is "Chưa kiểm tra" or "Chưa có nguồn dữ liệu" or "Chưa có snapshot" or "Sẽ tạo")
        {
            item.Hint = "Chưa có dữ liệu thật để xác nhận. Hãy làm mới hoặc hoàn thiện nguồn dữ liệu tương ứng.";
            CheckGrid.Items.Refresh();
            CheckGrid_SelectionChanged(CheckGrid, null!);
            return;
        }
        item.Status = "Sẵn sàng";
        item.Hint = "Đã được nhân viên Night Audit xác nhận ở phiên làm việc này.";
        CheckGrid.Items.Refresh();
        CheckGrid_SelectionChanged(CheckGrid, null!);
    }

    private void Simulate_Click(object sender, RoutedEventArgs e) => RunStatus.Text = _persistedMode
        ? "Chạy thử chỉ kiểm tra trạng thái hiện tại; chưa ghi nhận thay đổi dữ liệu."
        : "Chạy thử hoàn tất ở chế độ xem trước; chưa có ngữ cảnh dữ liệu để ghi nhận.";

    private async void RunAudit_Click(object sender, RoutedEventArgs e)
    {
        await _dataOperationGate.WaitAsync();
        try { await RunAuditCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task RunAuditCoreAsync()
    {
        if (_isClosed) return;
        var unresolved = checks.Count(item => item.BlocksRun && item.Status != "Sẵn sàng");
        if (unresolved > 0)
        {
            RunStatus.Text = $"Còn {unresolved} hạng mục cần xử lý trước khi chạy Night Audit.";
            return;
        }

        if (!_persistedMode)
        {
            RunStatus.Text = "Chưa có ngữ cảnh Front Office hoặc dịch vụ Night Audit để chạy dữ liệu thật.";
            return;
        }

        try
        {
            await EnsureAuditAsync();
            foreach (var step in Enum.GetValues<NightAuditStep>().Where(x => x <= NightAuditStep.GenerateRevenueSnapshot))
            {
                var result = await _auditService!.ExecuteStepAsync(
                    new ExecuteNightAuditStepCommand(_activeAudit!.NightAuditId, step),
                    CancellationToken.None);
                if (result.IsFailure)
                {
                    RunStatus.Text = $"Night Audit dừng ở {step}: {result.Error?.Message}";
                    await LoadPersistedCoreAsync();
                    return;
                }
                _activeAudit = result.Value;
            }

            RunStatus.Text = "Đã hoàn tất các bước kiểm tra và ghi nhận doanh thu. Chuyển sang Đóng ngày để xác nhận.";
            await LoadPersistedCoreAsync();
            ShowSection("close");
        }
        catch (Exception ex)
        {
            RunStatus.Text = "Không thể chạy Night Audit: " + ex.Message;
        }
    }

    private void CloseCheckChanged(object sender, RoutedEventArgs e) => UpdateCloseState();
    private void UpdateCloseState()
    {
        var ready = CloseCheck1?.IsChecked == true && CloseCheck2?.IsChecked == true && CloseCheck3?.IsChecked == true && CloseCheck4?.IsChecked == true;
        CloseDayButton.IsEnabled = ready;
        CloseHint.Text = ready ? "Tất cả điều kiện đã được xác nhận. Bạn có thể đóng ngày." : "Hoàn tất bốn xác nhận để bật thao tác Đóng ngày.";
    }

    private async void CloseDay_Click(object sender, RoutedEventArgs e)
    {
        await _dataOperationGate.WaitAsync();
        try { await CloseDayCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task CloseDayCoreAsync()
    {
        if (_isClosed) return;
        if (!_persistedMode || _activeAudit is null)
        {
            CloseHint.Text = "Chưa có phiên Night Audit thật để đóng ngày.";
            return;
        }

        try
        {
            CloseDayButton.IsEnabled = false;
            var activeAudit = _activeAudit;
            foreach (var step in new[] { NightAuditStep.RollBusinessDate, NightAuditStep.FinalizeAndUnlock })
            {
                var result = await _auditService!.ExecuteStepAsync(
                    new ExecuteNightAuditStepCommand(activeAudit!.NightAuditId, step),
                    CancellationToken.None);
                if (result.IsFailure)
                {
                    CloseHint.Text = "Không thể đóng ngày: " + result.Error?.Message;
                    CloseDayButton.IsEnabled = true;
                    return;
                }
                _activeAudit = result.Value;
            }

            CloseHint.Text = $"Ngày kinh doanh {_businessDate:dd/MM/yyyy} đã được đóng và phiên Night Audit đã hoàn tất.";
            await LoadPersistedCoreAsync();
        }
        catch (Exception ex)
        {
            CloseHint.Text = "Không thể đóng ngày: " + ex.Message;
            CloseDayButton.IsEnabled = true;
        }
    }

    private async Task LoadPersistedAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { await LoadPersistedCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        await _dataOperationGate.WaitAsync();
        try { _serviceScope?.Dispose(); }
        finally { _dataOperationGate.Release(); _dataOperationGate.Dispose(); }
    }

    private async Task LoadPersistedCoreAsync()
    {
        if (_isClosed) return;
        if (!_persistedMode || _contextAccessor?.Current is not { } context)
        {
            BusinessDateText.Text = "Chế độ xem trước — chưa có ngữ cảnh dữ liệu";
            return;
        }

        try
        {
            _businessDate = context.BusinessDate.BusinessDate;
            var propertyId = context.Property.PropertyId.Value;
            _activeAudit = await _auditRepository!.GetCurrentAsync(propertyId, CancellationToken.None);
            BusinessDateText.Text = $"Ngày kinh doanh: {_businessDate:dd/MM/yyyy}";

            if (_stayRepository is not null)
            {
                var inHouse = await _stayRepository.GetCurrentInHouseAsync(propertyId, CancellationToken.None);
                var overdue = inHouse.Count(stay => stay.ScheduledDepartureDate <= _businessDate);
                UpdateCheck("Khách chưa check-out", overdue == 0 ? "0 khách quá ngày trả" : $"{overdue} khách quá ngày trả", overdue == 0
                    ? "Không có khách quá ngày dự kiến trả cần xử lý."
                    : "Rà soát khách đã quá ngày dự kiến trả trước khi đóng ngày.", overdue == 0 ? "Sẵn sàng" : "Cần xử lý");
            }

            if (_folioRepository is not null)
            {
                var folios = await _folioRepository.SearchAsync(
                    new FolioSearchFilter(propertyId, null, null, HotelFolioStatus.Open, null, null, null, null),
                    new PageRequest(1, 200),
                    CancellationToken.None);
                var openCount = folios.Items.Count;
                UpdateCheck("Folio đang mở", $"{openCount} Folio đang mở", openCount == 0
                    ? "Không còn Folio mở cần xử lý."
                    : "Rà soát Folio mở và quyết toán hoặc ghi nhận lý do treo.", openCount == 0 ? "Sẵn sàng" : "Cần xử lý");
            }

            if (_paymentRepository is not null)
            {
                var settlements = await _paymentRepository.GetSettlementSummaryAsync(
                    propertyId, _businessDate, _businessDate, CancellationToken.None);
                var unsettled = settlements.Count(summary => !summary.IsBalanced);
                var outstanding = settlements.Where(summary => !summary.IsBalanced).Sum(summary => summary.Outstanding);
                UpdateCheck("Chênh lệch thanh toán",
                    unsettled == 0 ? "0 Folio còn chênh lệch" : $"{unsettled} Folio · Dư nợ {outstanding:N0} đ",
                    unsettled == 0
                        ? "Đối soát thật không còn Folio có số dư chưa quyết toán trong ngày."
                        : "Mở Đối chiếu để xử lý các Folio còn Outstanding trước khi đóng ngày.",
                    unsettled == 0 ? "Sẵn sàng" : "Cần xử lý");
            }

            if (_cashReconciliationService is not null)
            {
                var pendingReconciliations = await _cashReconciliationService.GetPendingAsync(
                    propertyId, CancellationToken.None);
                UpdateCheck("Đối soát thu ngân chờ xử lý",
                    pendingReconciliations.Count == 0
                        ? "0 ca thu ngân chờ xử lý"
                        : $"{pendingReconciliations.Count} ca thu ngân chờ xử lý",
                    pendingReconciliations.Count == 0
                        ? "Không còn đối soát thu ngân Pending, VarianceDetected hoặc Escalated."
                        : "Mở Thu ngân để xử lý và phê duyệt các đối soát còn tồn.",
                    pendingReconciliations.Count == 0 ? "Sẵn sàng" : "Cần xử lý");
            }

            var revenue = await _auditRepository!.GetRevenueSnapshotAsync(propertyId, _businessDate, CancellationToken.None);
            UpdateCheck("Doanh thu trong ngày", revenue is null
                ? $"Chưa có snapshot cho ngày {_businessDate:dd/MM/yyyy}"
                : $"{revenue.TotalRevenue:N0} đ · Occupancy {revenue.OccupancyRate:F1}% · ADR {revenue.ADR:N0} đ",
                revenue is null
                    ? "Snapshot doanh thu chỉ xuất hiện sau khi bước GenerateRevenueSnapshot ghi dữ liệu thật."
                    : "Đã tải snapshot doanh thu từ cơ sở dữ liệu.",
                revenue is null ? "Sẽ tạo" : "Sẵn sàng");

            if (_infrastructureHealthService is not null)
            {
                var health = await _infrastructureHealthService.CheckAsync(CancellationToken.None);
                var unhealthyChecks = health.Checks
                    .Where(check => !string.Equals(check.Status, "Healthy", StringComparison.OrdinalIgnoreCase))
                    .Select(check => $"{check.Name}: {check.Message}")
                    .ToArray();
                var isHealthy = string.Equals(health.Status, "Healthy", StringComparison.OrdinalIgnoreCase);
                UpdateCheck("Đồng bộ hệ thống",
                    isHealthy
                        ? $"Health-check đạt · {health.CheckedAt.ToLocalTime():HH:mm}"
                        : $"{health.Status} · {string.Join("; ", unhealthyChecks)}",
                    isHealthy
                        ? "Kết nối database và trạng thái migration đã được kiểm tra thật."
                        : "Xử lý các cảnh báo hạ tầng trước khi đóng ngày.",
                    isHealthy ? "Sẵn sàng" : "Cần xử lý");
            }

            RefreshRunItems();
            CheckGrid.Items.Refresh();
            UpdateCloseState();
        }
        catch (Exception ex)
        {
            RunStatus.Text = "Không thể tải dữ liệu Night Audit: " + ex.Message;
        }
    }

    private async Task EnsureAuditAsync()
    {
        var context = _contextAccessor!.Current!;
        if (_activeAudit is null)
        {
            var started = await _auditService!.BeginAsync(
                new BeginNightAuditCommand(context.Property.PropertyId.Value),
                CancellationToken.None);
            if (started.IsFailure) throw new InvalidOperationException(started.Error?.Message);
            _activeAudit = started.Value;
        }
        else if (_activeAudit.Status == NightAuditStatus.Failed)
        {
            var resumed = await _auditService!.ResumeAsync(
                new ResumeNightAuditCommand(_activeAudit.NightAuditId),
                CancellationToken.None);
            if (resumed.IsFailure) throw new InvalidOperationException(resumed.Error?.Message);
            _activeAudit = resumed.Value;
        }
    }

    private void RefreshRunItems()
    {
        if (_activeAudit is null)
        {
            RunItems.ItemsSource = Array.Empty<RunItem>();
            return;
        }

        var groups = new[]
        {
            ("Khóa và kiểm tra dữ liệu", "Khóa ngày kinh doanh, rà soát Stay và Folio.", NightAuditStep.LockBusinessDate, NightAuditStep.ValidateOpenFolios),
            ("Post doanh thu", "Ghi Room charge, thuế và phí dịch vụ vào Folio.", NightAuditStep.PostRoomCharges, NightAuditStep.ApplyPackages),
            ("Đối soát và snapshot", "Kiểm tra quyết toán, cập nhật dự kiến và tạo snapshot.", NightAuditStep.SettlementValidation, NightAuditStep.GenerateRevenueSnapshot),
            ("Đóng ngày và nhật ký", "Chuyển ngày kinh doanh và hoàn tất phiên audit.", NightAuditStep.RollBusinessDate, NightAuditStep.FinalizeAndUnlock)
        };
        RunItems.ItemsSource = groups.Select(group => new RunItem(
            group.Item1,
            group.Item2,
            _activeAudit.Status == NightAuditStatus.Failed && _activeAudit.CurrentStep >= group.Item3 && _activeAudit.CurrentStep <= group.Item4
                ? "Lỗi"
                : _activeAudit.CompletedSteps >= (int)group.Item4
                    ? "Hoàn tất"
                    : "Chờ chạy")).ToArray();
    }

    private void UpdateCheck(string name, string detail, string hint, string status)
    {
        var item = checks.FirstOrDefault(x => x.Name == name);
        if (item is null) return;
        item.Detail = detail;
        item.Hint = hint;
        item.Status = status;
    }

    private void OpenReconcile_Click(object sender, RoutedEventArgs e) => OpenOrActivate<ArReconcileWindow>();
    private void OpenFolio_Click(object sender, RoutedEventArgs e) => OpenOrActivate<FolioWorkspaceWindow>();
    private void OpenLog_Click(object sender, RoutedEventArgs e) => OpenOrActivate<FrontOfficeLogWindow>();

    private void OpenOrActivate<T>() where T : Window, new()
    {
        var existing = System.Windows.Application.Current.Windows.OfType<T>().FirstOrDefault(window => window.IsLoaded);
        if (existing is not null) { existing.Activate(); existing.Focus(); return; }
        new T { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false }.Show();
    }

    private void UpdateStepAppearance()
    {
        SetStep(CheckStep, currentSection == "check", "#276BE8");
        SetStep(RunStep, currentSection == "run", "#18B33F");
        SetStep(CloseStep, currentSection == "close", "#7C3AED");
    }

    private static void SetStep(Button button, bool active, string color)
    {
        button.Background = active ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2F6FB"));
        button.BorderBrush = active ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7E2F0"));
        button.Foreground = active ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#183455"));
    }

    private sealed class AuditCheckItem
    {
        public AuditCheckItem(string name, string source, string status, string detail, string hint, bool blocksRun = true)
        {
            Name = name;
            Source = source;
            Status = status;
            Detail = detail;
            Hint = hint;
            BlocksRun = blocksRun;
        }
        public string Name { get; }
        public string Source { get; }
        public string Status { get; set; }
        public string Detail { get; set; }
        public string Hint { get; set; }
        public bool BlocksRun { get; }
    }

    private sealed record RunItem(string Name, string Detail, string Status);
}
