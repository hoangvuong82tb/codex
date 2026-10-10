using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Availability;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.Availability;
using ASMILE.BO.Core.FrontOffice.Group;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CreateGroupWindow : Window
{
    // ═══ WIZARD STATE ═══
    private int _currentStep;
    private bool _hasUnsavedChanges;

    // Step A - Group Info
    private string _groupName = "", _groupShortName = "", _externalRef = "", _groupSeries = "";
    private string _groupType = "", _segment = "", _market = "", _source = "", _country = "", _language = "";
    private string _tripPurpose = "", _eventType = "", _groupLeader = "";
    private int _guestCount, _roomCount = 1;
    private string _vipLevel = "", _priority = "", _tags = "", _internalNote = "";
    private readonly ObservableCollection<string> _tagsList = new();

    // Step B - Contact & Organization
    private string _orgType = "None"; // None, Company, Agent, Organizer, GroupLeader, Individual
    private int _stepBTabIndex;
    private string _companyName = "", _companyCode = "", _taxCode = "", _companyAddress = "", _companyCountry = "";
    private string _companyPhone = "", _companyEmail = "", _companyWebsite = "", _representative = "", _repTitle = "";
    private string _creditStatus = "";

    private string _agentName = "", _agentCode = "", _iataCode = "", _agentMarket = "";
    private decimal _commissionPct;
    private string _agentContact = "", _agentEmail = "", _agentPhone = "", _agentPaymentTerms = "";

    private string _primaryContactName = "", _primaryContactRole = "", _primaryContactPhone = "";
    private string _primaryContactEmail = "", _primaryContactLang = "", _primaryContactChannel = "";
    private string _primaryContactTime = "", _primaryContactNote = "";
    private readonly List<SecondaryContact> _secondaryContacts = new();

    // Step C - Stay
    private DateTime? _arrivalDate, _departureDate;
    private string _arrivalTime = "14:00", _departureTime = "12:00";
    private DateTime? _cutoffDate;
    private string _cutoffTime = "18:00";
    private bool _earlyArrival, _lateDeparture;
    private string _arrivalMethod = "", _departureMethod = "", _flightNumber = "", _eta = "", _etd = "";
    private bool _pickupRequired, _dropoffRequired;
    private string _meetingPoint = "", _stayPattern = "SameDay";
    private readonly ObservableCollection<ArrivalWave> _arrivalWaves = new();
    private readonly ObservableCollection<DepartureWave> _departureWaves = new();

    // Step D - Room Requirements
    private readonly ObservableCollection<RoomRequirement> _roomRequirements = new();

    // Step E - Rate & Payment
    private string _ratePlan = "", _groupRate = "";
    private string _currency = "VND";
    private decimal _exchangeRate = 1;
    private decimal _rateBeforeTax, _rateAfterTax;
    private decimal _singleSupplement, _doubleRate, _extraBedRate, _childRate;
    private int _complimentaryRooms;
    private string _freeRoomRatio = "";

    private bool _breakfast, _lunch, _dinner, _meetingPackage, _airportTransfer;
    private bool _welcomeDrink, _galaDinner, _spa, _laundry, _otherPackage;

    private string _payer = "", _paymentMethod = "", _billingInstruction = "", _routingRule = "";
    private bool _creditAllowed;
    private decimal _creditLimit;
    private string _paymentTerms = "";
    private bool _cityLedger, _masterFolio, _individualPay, _mixedPayment;

    private bool _depositRequired;
    private string _depositType = "";
    private decimal _depositAmount;
    private decimal _depositPercent;
    private DateTime? _depositDeadline;
    private int _depositInstallments = 1;
    private bool _depositRefundable;
    private string _guaranteeType = "";

    private bool _agentCommissionEnabled;
    private decimal _agentCommissionPct;
    private string _commissionBase = "", _commissionTax = "", _commissionTiming = "";

    private decimal _vatPct = 8, _serviceChargePct = 5;
    private decimal _cityTax, _tourismFee;
    private bool _taxIncluded, _taxExemption;
    private int _stepETabIndex;

    // Step F - Services
    private readonly ObservableCollection<GroupService> _services = new();
    private readonly ObservableCollection<GroupService> _fnbRequirements = new();
    private readonly ObservableCollection<GroupService> _meetingRequirements = new();
    private readonly ObservableCollection<GroupService> _transportRequirements = new();
    private string _housekeepingNote = "", _vipNote = "", _securityNote = "";
    private readonly ObservableCollection<GroupDocumentRow> _documents = new();
    private string _noteInternal = "", _noteReception = "", _noteHousekeeping = "", _noteCashier = "";
    private string _noteFnb = "", _noteSecurity = "", _noteSales = "", _noteBanquet = "";
    private int _stepFTabIndex;

    // Step H - Confirm
    private bool _confirmName, _confirmOrg, _confirmContact, _confirmStay;
    private bool _confirmRooms, _confirmFinance, _confirmServices, _confirmAll;

    // Success state
    private bool _isSubmitted;
    private Guid _createdGroupId;
    private string _createdGroupCode = "";
    private readonly IGroupReservationCommandService? _groupCommandService;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IHotelRoomMasterDataRepository? _roomMasterDataRepository;
    private readonly IAvailabilityEngine? _availabilityEngine;
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);
    private readonly List<HotelRoomTypeDto> _roomTypes = new();
    private bool _roomAvailabilityChecked;
    private bool _roomAvailabilityAvailable;
    private string _roomAvailabilitySummary = "Chưa kiểm tra";
    private bool _isClosed;

    // ═══ UI REFERENCES ═══
    private readonly Border[] _stepBoxes = new Border[8];
    private readonly TextBlock[] _stepLabels = new TextBlock[8];
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F", "G", "H" };
    private static readonly string[] StepTitles =
    {
        "Thông tin đoàn", "Liên hệ & đơn vị", "Lưu trú dự kiến", "Nhu cầu phòng",
        "Giá & thanh toán", "Dịch vụ & yêu cầu", "Kiểm tra", "Xác nhận"
    };

    public CreateGroupWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _groupCommandService = scopedServices?.GetService<IGroupReservationCommandService>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _roomMasterDataRepository = scopedServices?.GetService<IHotelRoomMasterDataRepository>();
        _availabilityEngine = scopedServices?.GetService<IAvailabilityEngine>();
        _ = LoadRoomTypesAsync();
        BuildStepBar();
        GoToStep(0);
        UpdateSummary();
        Closed += OnClosed;
    }

    // ═══ STEP BAR ═══
    private void BuildStepBar()
    {
        StepBar.Children.Clear();
        for (int i = 0; i < 8; i++)
        {
            var idx = i;
            var isActive = i == _currentStep;

            var label = new TextBlock { Text = "Chưa bắt đầu", FontSize = 9, Foreground = C("#64748B") };
            _stepLabels[i] = label;

            var letterBlock = new Border
            {
                Width = 28, Height = 28, CornerRadius = new CornerRadius(7),
                Background = Cb("#F1F5F9"),
                Child = new TextBlock { Text = StepLetters[i], FontSize = 13, FontWeight = FontWeights.Bold, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
            };

            var box = new Border
            {
                Width = 142, Height = 62, CornerRadius = new CornerRadius(10), Margin = new Thickness(0, 0, 2, 0),
                Background = Brushes.White, BorderBrush = Cb("#E2E8F0"), BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand, Padding = new Thickness(10, 8, 10, 8),
                Child = new Grid
                {
                    ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto }, new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) } },
                    Children =
                    {
                        letterBlock,
                        new StackPanel
                        {
                            Margin = new Thickness(8, 0, 0, 0),
                            Children =
                            {
                                new TextBlock { Text = StepTitles[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#64748B") },
                                label
                            }
                        }
                    }
                }
            };
            Grid.SetColumn(letterBlock, 0);
            var sp = (StackPanel)((Grid)box.Child).Children[1];
            Grid.SetColumn(sp, 1);

            // Allow the step bar to act as direct navigation. Validation remains
            // enforced by the footer's Continue/Create actions, so users can
            // review and move between every step without fields being blocked.
            box.MouseLeftButtonDown += (_, _) => GoToStep(idx);
            _stepBoxes[i] = box;
            StepBar.Children.Add(box);

            if (i < 7)
                StepBar.Children.Add(new TextBlock { Text = "▸", FontSize = 14, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 2, 0) });
        }
    }

    private void RefreshStepBar()
    {
        for (int i = 0; i < 8; i++)
        {
            if (i < _currentStep)
            {
                _stepBoxes[i].Background = Cb("#F0FDF4");
                _stepBoxes[i].BorderBrush = Cb("#86EFAC");
                _stepBoxes[i].BorderThickness = new Thickness(1);
                _stepLabels[i].Text = "Hoàn thành";
                _stepLabels[i].Foreground = C("#166534");
            }
            else if (i == _currentStep)
            {
                _stepBoxes[i].Background = Cb("#F5F3FF");
                _stepBoxes[i].BorderBrush = Cb("#7C3AED");
                _stepBoxes[i].BorderThickness = new Thickness(1.5);
                _stepLabels[i].Text = "Đang nhập";
                _stepLabels[i].Foreground = C("#6D28D9");
            }
            else
            {
                _stepBoxes[i].Background = Brushes.White;
                _stepBoxes[i].BorderBrush = Cb("#E2E8F0");
                _stepBoxes[i].BorderThickness = new Thickness(1);
                _stepLabels[i].Text = "Chưa bắt đầu";
                _stepLabels[i].Foreground = C("#64748B");
            }
        }
    }

    // ═══ NAVIGATION ═══
    private void GoToStep(int step)
    {
        _currentStep = step;
        RefreshStepBar();
        BuildStepContent(step);
        UpdateInspector(step);
        UpdateFooter();
        UpdateSummary();
    }

    private bool CanEnterStep(int step) => step switch
    {
        0 => true,
        1 => true,
        2 => !string.IsNullOrWhiteSpace(_groupName) && !string.IsNullOrEmpty(_groupType),
        3 => !string.IsNullOrWhiteSpace(_groupName),
        4 => _roomRequirements.Count > 0 && _roomRequirements.All(r => r.RoomCount > 0),
        5 => !string.IsNullOrEmpty(_currency) && !string.IsNullOrEmpty(_payer),
        6 => true,
        7 => !HasBlockingIssues(),
        _ => false
    };

    private bool CanProceedFromStep(int step) => step switch
    {
        0 => !string.IsNullOrWhiteSpace(_groupName) && !string.IsNullOrEmpty(_groupType) && _guestCount > 0 && _roomCount > 0,
        1 => !string.IsNullOrWhiteSpace(_primaryContactName) && !string.IsNullOrWhiteSpace(_primaryContactPhone),
        2 => _arrivalDate.HasValue && _departureDate.HasValue && _departureDate > _arrivalDate,
        3 => _roomRequirements.Count > 0 && _roomRequirements.All(r => r.RoomCount > 0 && !string.IsNullOrEmpty(r.RoomType)),
        4 => !string.IsNullOrEmpty(_currency) && !string.IsNullOrEmpty(_payer),
        5 => true,
        6 => !HasBlockingIssues(),
        7 => false,
        _ => false
    };

    private void OnNext(object s, RoutedEventArgs e)
    {
        if (_currentStep >= 7) return;
        if (!CanProceedFromStep(_currentStep)) return;
        _hasUnsavedChanges = true;
        GoToStep(_currentStep + 1);
    }

    private void OnBack(object s, RoutedEventArgs e)
    {
        if (_currentStep <= 0) return;
        GoToStep(_currentStep - 1);
    }

    private void OnCancel(object s, RoutedEventArgs e)
    {
        if (_hasUnsavedChanges && !_isSubmitted)
        {
            var result = MessageBox.Show("Hồ sơ đoàn chưa hoàn tất. Bạn có muốn bỏ các thay đổi?", "Xác nhận", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK) return;
        }
        Close();
    }

    private void OnClose(object s, RoutedEventArgs e) => OnCancel(s, e);

    private void OnRefresh(object s, RoutedEventArgs e)
    {
        if (_hasUnsavedChanges)
        {
            var result = MessageBox.Show("Bạn có muốn làm mới và bỏ tất cả thay đổi?", "Làm mới", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
            if (result != MessageBoxResult.OK) return;
        }
        ResetWizard();
        GoToStep(0);
    }

    private async void OnSaveDraft(object s, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_groupName) || !_arrivalDate.HasValue || !_departureDate.HasValue)
        {
            MessageBox.Show("Cần nhập tên đoàn, ngày đến và ngày đi trước khi lưu nháp.", "Lưu nháp", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var created = await PersistGroupAsync();
            _createdGroupCode = created.GroupCode;
            _hasUnsavedChanges = false;
            MessageBox.Show($"Đã lưu đoàn nháp {created.GroupCode} vào dữ liệu thật.", "Lưu nháp", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể lưu đoàn nháp.\n{ex.Message}", "Lưu nháp", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnWindowKeyDown(object s, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && !_hasUnsavedChanges) Close();
        else if (e.Key == Key.Escape && _hasUnsavedChanges) OnCancel(s, e);
        else if (e.Key == Key.Enter && CanProceedFromStep(_currentStep) && _currentStep < 7) OnNext(s, e);
        else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) OnSaveDraft(s, e);
    }

    private void ResetWizard()
    {
        _groupName = ""; _groupShortName = ""; _externalRef = ""; _groupSeries = "";
        _groupType = ""; _segment = ""; _market = ""; _source = ""; _country = ""; _language = "";
        _tripPurpose = ""; _eventType = ""; _groupLeader = "";
        _guestCount = 0; _roomCount = 0;
        _vipLevel = ""; _priority = ""; _tags = ""; _internalNote = "";
        _tagsList.Clear();

        _orgType = "None";
        _companyName = ""; _companyCode = ""; _taxCode = ""; _companyAddress = ""; _companyCountry = "";
        _companyPhone = ""; _companyEmail = ""; _companyWebsite = ""; _representative = ""; _repTitle = "";
        _creditStatus = "";
        _agentName = ""; _agentCode = ""; _iataCode = ""; _agentMarket = "";
        _commissionPct = 0; _agentContact = ""; _agentEmail = ""; _agentPhone = ""; _agentPaymentTerms = "";
        _primaryContactName = ""; _primaryContactRole = ""; _primaryContactPhone = "";
        _primaryContactEmail = ""; _primaryContactLang = ""; _primaryContactChannel = "";
        _primaryContactTime = ""; _primaryContactNote = "";
        _secondaryContacts.Clear();

        _arrivalDate = null; _departureDate = null;
        _arrivalTime = "14:00"; _departureTime = "12:00";
        _cutoffDate = null; _cutoffTime = "18:00";
        _earlyArrival = false; _lateDeparture = false;
        _arrivalMethod = ""; _departureMethod = "";
        _flightNumber = ""; _eta = ""; _etd = "";
        _pickupRequired = false; _dropoffRequired = false;
        _meetingPoint = ""; _stayPattern = "SameDay";
        _arrivalWaves.Clear(); _departureWaves.Clear();

        _roomRequirements.Clear();
        _ratePlan = ""; _groupRate = "";
        _currency = "VND"; _exchangeRate = 1;
        _rateAfterTax = 0; _rateBeforeTax = 0;
        _singleSupplement = 0; _doubleRate = 0; _extraBedRate = 0; _childRate = 0;
        _complimentaryRooms = 0; _freeRoomRatio = "";
        _breakfast = false; _lunch = false; _dinner = false; _meetingPackage = false;
        _airportTransfer = false; _welcomeDrink = false; _galaDinner = false; _spa = false; _laundry = false; _otherPackage = false;
        _payer = ""; _paymentMethod = ""; _billingInstruction = ""; _routingRule = "";
        _creditAllowed = false; _creditLimit = 0;
        _paymentTerms = "";
        _cityLedger = false; _masterFolio = false; _individualPay = false; _mixedPayment = false;
        _depositRequired = false; _depositType = "";
        _depositAmount = 0; _depositPercent = 0; _depositDeadline = null;
        _depositInstallments = 1; _depositRefundable = true;
        _guaranteeType = "";
        _agentCommissionEnabled = false; _agentCommissionPct = 0;
        _commissionBase = ""; _commissionTax = ""; _commissionTiming = "";
        _vatPct = 8; _serviceChargePct = 5; _cityTax = 0; _tourismFee = 0;
        _taxIncluded = false; _taxExemption = false;
        _services.Clear(); _fnbRequirements.Clear(); _meetingRequirements.Clear(); _transportRequirements.Clear();
        _housekeepingNote = ""; _vipNote = ""; _securityNote = "";
        _documents.Clear();
        _noteInternal = ""; _noteReception = ""; _noteHousekeeping = ""; _noteCashier = "";
        _noteFnb = ""; _noteSecurity = ""; _noteSales = ""; _noteBanquet = "";
        _confirmName = false; _confirmOrg = false; _confirmContact = false; _confirmStay = false;
        _confirmRooms = false; _confirmFinance = false; _confirmServices = false; _confirmAll = false;
        _isSubmitted = false; _createdGroupCode = "";
        _hasUnsavedChanges = false;
    }

    private void UpdateFooter()
    {
        FooterStepLabel.Text = $"Bước {_currentStep + 1}/8 · {StepTitles[_currentStep]}";
        BtnBack.IsEnabled = _currentStep > 0;
        BtnSaveDraft.IsEnabled = _hasUnsavedChanges;

        if (_currentStep == 7)
        {
            BtnNext.Visibility = Visibility.Collapsed;
            BtnCreate.Visibility = Visibility.Visible;
            BtnCreate.IsEnabled = CanCreateGroup();
        }
        else
        {
            BtnNext.Visibility = Visibility.Visible;
            BtnCreate.Visibility = Visibility.Collapsed;
            BtnNext.IsEnabled = CanProceedFromStep(_currentStep);
        }

        // Warning/blocking indicators
        var warnings = GetWarnings();
        var blockings = GetBlockingIssues();
        FooterWarningLabel.Visibility = warnings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        FooterWarningLabel.Text = $"⚠ {warnings.Count} cảnh báo";
        FooterBlockingLabel.Visibility = blockings.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        FooterBlockingLabel.Text = $"⛔ {blockings.Count} chặn";
    }

    // ═══ SUMMARY STRIP ═══
    private void UpdateSummary()
    {
        SummaryPanel.Children.Clear();
        if (!string.IsNullOrWhiteSpace(_groupName))
            SummaryPanel.Children.Add(SummaryChip("Đoàn", _groupName, "#7C3AED"));
        if (!string.IsNullOrEmpty(_companyName) || !string.IsNullOrEmpty(_agentName))
            SummaryPanel.Children.Add(SummaryChip("Đơn vị", string.IsNullOrEmpty(_companyName) ? _agentName : _companyName, "#2563EB"));
        if (_arrivalDate.HasValue && _departureDate.HasValue)
        {
            var nights = (_departureDate.Value - _arrivalDate.Value).Days;
            SummaryPanel.Children.Add(SummaryChip("Lưu trú", $"{_arrivalDate:dd/MM} → {_departureDate:dd/MM} ({nights} đêm)", "#16A34A"));
        }
        if (_roomCount > 0)
            SummaryPanel.Children.Add(SummaryChip("Phòng", $"{_roomCount} phòng", "#D97706"));
        if (_guestCount > 0)
            SummaryPanel.Children.Add(SummaryChip("Khách", $"{_guestCount} khách", "#0891B2"));
        if (!string.IsNullOrEmpty(_currency))
            SummaryPanel.Children.Add(SummaryChip("Tiền tệ", _currency, "#7C3AED"));
        if (string.IsNullOrWhiteSpace(_groupName) && _roomCount == 0 && _guestCount == 0)
            SummaryPanel.Children.Add(new TextBlock { Text = "Nhập thông tin đoàn để xem tóm tắt", FontSize = 12, Foreground = C("#94A3B8"), VerticalAlignment = VerticalAlignment.Center });
    }

    private static Border SummaryChip(string label, string value, string color)
    {
        var b = new Border
        {
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}1A")),
            CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 8, 0), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}33")), BorderThickness = new Thickness(1)
        };
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock { Text = label + ": ", FontSize = 11, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), FontWeight = FontWeights.SemiBold });
        sp.Children.Add(new TextBlock { Text = value, FontSize = 11, Foreground = C("#334155") });
        b.Child = sp;
        return b;
    }

    // ═══ STEP CONTENT BUILDER ═══
    private void BuildStepContent(int step)
    {
        StepContentHost.Children.Clear();
        switch (step)
        {
            case 0: BuildStepACompact(); break;
            case 1: BuildStepBCompact(); break;
            case 2: BuildStepCCompact(); break;
            case 3: BuildStepD(); break;
            case 4: BuildStepE(); break;
            case 5: BuildStepF(); break;
            case 6: BuildStepGCompact(); break;
            case 7: BuildStepHCompact(); break;
        }
    }

    // ═══ STEP A — GROUP INFO ═══
    private void BuildStepACompact()
    {
        StepContentHost.Children.Add(STitle("\u0054h\u00f4ng tin \u0111o\u00e0n"));
        StepContentHost.Children.Add(SText("Thi\u1ebft l\u1eadp nh\u1eadn di\u1ec7n c\u01a1 b\u1ea3n cho \u0111o\u00e0n."));
        StepContentHost.Children.Add(SubTitle("Nh\u1eadn di\u1ec7n \u0111o\u00e0n"));

        var identity = Grid4();
        identity.Children.Add(MakeField("T\u00ean \u0111o\u00e0n *", _groupName, v => { _groupName = v; OnFieldChanged(); }, "Nh\u1eadp t\u00ean \u0111o\u00e0n"));
        identity.Children.Add(MakeField("T\u00ean vi\u1ebft t\u1eaft", _groupShortName, v => _groupShortName = v, "Vi\u1ebft t\u1eaft"));
        identity.Children.Add(MakeCombo("Lo\u1ea1i \u0111o\u00e0n *", _groupType, v => { _groupType = v; OnFieldChanged(); }, GroupTypeOptions(), "Ch\u1ecdn lo\u1ea1i \u0111o\u00e0n"));
        identity.Children.Add(MakeCombo("Segment", _segment, v => _segment = v, SegmentOptions(), "Ch\u1ecdn segment"));
        StepContentHost.Children.Add(identity);

        var source = Grid4();
        source.Children.Add(MakeCombo("Market", _market, v => _market = v, MarketOptions(), "Ch\u1ecdn market"));
        source.Children.Add(MakeCombo("Source", _source, v => _source = v, SourceOptions(), "Ch\u1ecdn ngu\u1ed3n"));
        source.Children.Add(MakeField("M\u00e3 tham chi\u1ebfu ngo\u00e0i", _externalRef, v => _externalRef = v, "M\u00e3 t\u1eeb h\u1ec7 th\u1ed1ng ngo\u00e0i"));
        source.Children.Add(MakeField("Group Series", _groupSeries, v => _groupSeries = v, "Series"));
        StepContentHost.Children.Add(source);

        StepContentHost.Children.Add(SubTitle("Ph\u00e2n lo\u1ea1i & quy m\u00f4"));
        var classification = Grid4();
        classification.Children.Add(MakeCombo("Qu\u1ed1c gia ch\u00ednh", _country, v => _country = v, CountryOptions(), "Ch\u1ecdn qu\u1ed1c gia"));
        classification.Children.Add(MakeCombo("Ng\u00f4n ng\u1eef", _language, v => _language = v, LanguageOptions(), "Ch\u1ecdn ng\u00f4n ng\u1eef"));
        classification.Children.Add(MakeCombo("M\u1ee5c \u0111\u00edch chuy\u1ebfn \u0111i", _tripPurpose, v => _tripPurpose = v, TripPurposeOptions(), "Ch\u1ecdn m\u1ee5c \u0111\u00edch"));
        classification.Children.Add(MakeCombo("Lo\u1ea1i s\u1ef1 ki\u1ec7n", _eventType, v => _eventType = v, EventTypeOptions(), "Ch\u1ecdn lo\u1ea1i s\u1ef1 ki\u1ec7n"));
        StepContentHost.Children.Add(classification);

        var size = Grid4();
        size.Children.Add(MakeIntField("S\u1ed1 kh\u00e1ch d\u1ef1 ki\u1ebfn *", _guestCount, v => { _guestCount = v; OnFieldChanged(); }));
        size.Children.Add(MakeIntField("S\u1ed1 ph\u00f2ng d\u1ef1 ki\u1ebfn *", _roomCount, v => { _roomCount = v; OnFieldChanged(); }));
        size.Children.Add(MakeField("Tr\u01b0\u1edfng \u0111o\u00e0n", _groupLeader, v => _groupLeader = v, "T\u00ean tr\u01b0\u1edfng \u0111o\u00e0n"));
        size.Children.Add(MakeCombo("VIP Level", _vipLevel, v => _vipLevel = v, VipOptions()));
        StepContentHost.Children.Add(size);

        var priority = Grid4();
        priority.Children.Add(MakeCombo("Priority", _priority, v => _priority = v, PriorityOptions()));
        priority.Children.Add(MakeField("Tags", _tags, v => _tags = v, "Tag1, Tag2, ..."));
        StepContentHost.Children.Add(priority);
        StepContentHost.Children.Add(MakeTextArea("Ghi ch\u00fa n\u1ed9i b\u1ed9", _internalNote, v => _internalNote = v, "Ghi ch\u00fa cho nh\u00e2n vi\u00ean kh\u00e1ch s\u1ea1n..."));
    }

    private void BuildStepA()
    {
        StepContentHost.Children.Add(STitle("Thông tin đoàn"));
        StepContentHost.Children.Add(SText("Thiết lập nhận diện cơ bản cho đoàn."));

        StepContentHost.Children.Add(SubTitle("Nhận diện đoàn"));
        var g1 = Grid2();
        g1.Children.Add(MakeField("Tên đoàn *", _groupName, v => { _groupName = v; OnFieldChanged(); }, "Nhập tên đoàn"));
        g1.Children.Add(MakeField("Tên viết tắt", _groupShortName, v => { _groupShortName = v; OnFieldChanged(); }, "Viết tắt"));
        StepContentHost.Children.Add(g1);

        var g1b = Grid2();
        g1b.Children.Add(MakeCombo("Loại đoàn *", _groupType, v => { _groupType = v; OnFieldChanged(); }, GroupTypeOptions(), "Chọn loại đoàn"));
        g1b.Children.Add(MakeCombo("Segment", _segment, v => _segment = v, SegmentOptions(), "Chọn segment"));
        StepContentHost.Children.Add(g1b);

        var g1c = Grid2();
        g1c.Children.Add(MakeCombo("Market", _market, v => _market = v, MarketOptions(), "Chọn market"));
        g1c.Children.Add(MakeCombo("Source", _source, v => _source = v, SourceOptions(), "Chọn nguồn"));
        StepContentHost.Children.Add(g1c);

        var g1d = Grid2();
        g1d.Children.Add(MakeField("Mã tham chiếu ngoài", _externalRef, v => _externalRef = v, "Mã từ hệ thống ngoài"));
        g1d.Children.Add(MakeField("Group Series", _groupSeries, v => _groupSeries = v, "Series"));
        StepContentHost.Children.Add(g1d);

        StepContentHost.Children.Add(SubTitle("Phân loại & quy mô"));
        var g2 = Grid2();
        g2.Children.Add(MakeCombo("Quốc gia chính", _country, v => _country = v, CountryOptions(), "Chọn quốc gia"));
        g2.Children.Add(MakeCombo("Ngôn ngữ", _language, v => _language = v, LanguageOptions(), "Chọn ngôn ngữ"));
        StepContentHost.Children.Add(g2);

        var g2b = Grid2();
        g2b.Children.Add(MakeCombo("Mục đích chuyến đi", _tripPurpose, v => _tripPurpose = v, TripPurposeOptions(), "Chọn mục đích"));
        g2b.Children.Add(MakeCombo("Loại sự kiện", _eventType, v => _eventType = v, EventTypeOptions(), "Chọn loại sự kiện"));
        StepContentHost.Children.Add(g2b);

        var g2c = Grid3();
        g2c.Children.Add(MakeIntField("Số khách dự kiến *", _guestCount, v => { _guestCount = v; OnFieldChanged(); }));
        g2c.Children.Add(MakeIntField("Số phòng dự kiến *", _roomCount, v => { _roomCount = v; OnFieldChanged(); }));
        g2c.Children.Add(MakeField("Trưởng đoàn", _groupLeader, v => _groupLeader = v, "Tên trưởng đoàn"));
        StepContentHost.Children.Add(g2c);

        StepContentHost.Children.Add(SubTitle("Phân loại & ưu tiên"));
        var g3 = Grid2();
        g3.Children.Add(MakeCombo("VIP Level", _vipLevel, v => _vipLevel = v, VipOptions()));
        g3.Children.Add(MakeCombo("Priority", _priority, v => _priority = v, PriorityOptions()));
        StepContentHost.Children.Add(g3);

        var g3b = Grid1();
        g3b.Children.Add(MakeField("Tags", _tags, v => _tags = v, "Tag1, Tag2, ..."));
        StepContentHost.Children.Add(g3b);

        StepContentHost.Children.Add(SubTitle("Ghi chú"));
        StepContentHost.Children.Add(MakeTextArea("Ghi chú nội bộ", _internalNote, v => _internalNote = v, "Ghi chú cho nhân viên khách sạn..."));
    }

    // ═══ STEP B — CONTACT & ORGANIZATION ═══
    private void BuildStepBCompact()
    {
        StepContentHost.Children.Add(STitle("Li\u00ean h\u1ec7 & \u0111\u01a1n v\u1ecb"));
        StepContentHost.Children.Add(SText("X\u00e1c \u0111\u1ecbnh c\u00f4ng ty, \u0111\u1ea1i l\u00fd v\u00e0 c\u00e1c \u0111\u1ea7u m\u1ed1i li\u00ean h\u1ec7."));
        var orgTypes = new[]
        {
            ("Kh\u00f4ng c\u00f3 \u0111\u01a1n v\u1ecb", "None"), ("C\u00f4ng ty", "Company"),
            ("\u0110\u1ea1i l\u00fd", "Agent"), ("Ban t\u1ed5 ch\u1ee9c", "Organizer"), ("C\u00e1 nh\u00e2n", "Individual")
        };
        var tabs = new TabControl
        {
            Style = (Style)FindResource("WizardTabControl"),
            ItemContainerStyle = (Style)FindResource("WizardTabItem"),
            Margin = new Thickness(0, 0, 0, 12)
        };
        foreach (var (label, key) in orgTypes)
            tabs.Items.Add(new TabItem { Header = label, Tag = key, Content = BuildOrganizationPanelCompact(key) });
        _stepBTabIndex = Math.Max(0, Array.FindIndex(orgTypes, x => x.Item2 == _orgType));
        tabs.SelectedIndex = _stepBTabIndex;
        tabs.SelectionChanged += (_, _) =>
        {
            if (tabs.SelectedItem is TabItem selected && selected.Tag is string key && key != _orgType)
            {
                _orgType = key;
                _stepBTabIndex = tabs.SelectedIndex;
                GoToStep(1);
            }
        };
        StepContentHost.Children.Add(tabs);

        StepContentHost.Children.Add(SubTitle("Ng\u01b0\u1eddi li\u00ean h\u1ec7 ch\u00ednh"));
        var contact = Grid4();
        contact.Children.Add(MakeField("H\u1ecd t\u00ean *", _primaryContactName, v => { _primaryContactName = v; OnFieldChanged(); }, "H\u1ecd v\u00e0 t\u00ean"));
        contact.Children.Add(MakeField("Vai tr\u00f2", _primaryContactRole, v => _primaryContactRole = v, "Vai tr\u00f2"));
        contact.Children.Add(MakeField("\u0110i\u1ec7n tho\u1ea1i *", _primaryContactPhone, v => { _primaryContactPhone = v; OnFieldChanged(); }, "S\u0110T"));
        contact.Children.Add(MakeField("Email", _primaryContactEmail, v => _primaryContactEmail = v, "Email"));
        StepContentHost.Children.Add(contact);

        var contactMeta = Grid4();
        contactMeta.Children.Add(MakeCombo("Ng\u00f4n ng\u1eef", _primaryContactLang, v => _primaryContactLang = v, LanguageOptions()));
        contactMeta.Children.Add(MakeCombo("K\u00eanh li\u00ean h\u1ec7", _primaryContactChannel, v => _primaryContactChannel = v, new[] { "Phone", "Email", "Zalo", "WhatsApp", "WeChat" }));
        contactMeta.Children.Add(MakeField("Th\u1eddi gian LH", _primaryContactTime, v => _primaryContactTime = v, "Gi\u1edd h\u00e0nh ch\u00ednh"));
        contactMeta.Children.Add(MakeField("Ghi ch\u00fa", _primaryContactNote, v => _primaryContactNote = v, "Ghi ch\u00fa th\u00eam"));
        StepContentHost.Children.Add(contactMeta);

        StepContentHost.Children.Add(SubTitle("Li\u00ean h\u1ec7 ph\u1ee5"));
        var addContact = new Button { Content = "+ Th\u00eam li\u00ean h\u1ec7", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        addContact.Click += (_, _) => { _secondaryContacts.Add(new SecondaryContact()); GoToStep(1); };
        StepContentHost.Children.Add(addContact);
        for (var i = 0; i < _secondaryContacts.Count; i++)
        {
            var index = i;
            var secondary = _secondaryContacts[i];
            var row = Grid4();
            row.Children.Add(MakeField("H\u1ecd t\u00ean", secondary.Name, v => { secondary.Name = v; OnFieldChanged(); }, "T\u00ean"));
            row.Children.Add(MakeField("Vai tr\u00f2", secondary.Role, v => secondary.Role = v, "Vai tr\u00f2"));
            row.Children.Add(MakeField("S\u0110T", secondary.Phone, v => secondary.Phone = v, "S\u0110T"));
            var delete = QuickBtn("X\u00f3a", () => { _secondaryContacts.RemoveAt(index); GoToStep(1); });
            delete.Margin = new Thickness(0, 17, 0, 0);
            row.Children.Add(delete);
            StepContentHost.Children.Add(row);
            var email = Grid1();
            email.Children.Add(MakeField("Email", secondary.Email, v => secondary.Email = v, "Email"));
            StepContentHost.Children.Add(email);
        }
    }

    private StackPanel BuildOrganizationPanelCompact(string key)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        if (key == "Company")
        {
            panel.Children.Add(SubTitle("Th\u00f4ng tin c\u00f4ng ty"));
            var row1 = Grid4();
            row1.Children.Add(MakeField("T\u00ean c\u00f4ng ty *", _companyName, v => { _companyName = v; OnFieldChanged(); }, "T\u00ean c\u00f4ng ty"));
            row1.Children.Add(MakeField("M\u00e3 c\u00f4ng ty", _companyCode, v => _companyCode = v, "M\u00e3"));
            row1.Children.Add(MakeField("M\u00e3 s\u1ed1 thu\u1ebf", _taxCode, v => _taxCode = v, "MST"));
            row1.Children.Add(MakeCombo("Qu\u1ed1c gia", _companyCountry, v => _companyCountry = v, CountryOptions()));
            panel.Children.Add(row1);
            var row2 = Grid4();
            row2.Children.Add(MakeField("\u0110i\u1ec7n tho\u1ea1i", _companyPhone, v => _companyPhone = v, "S\u0110T"));
            row2.Children.Add(MakeField("Email", _companyEmail, v => _companyEmail = v, "Email"));
            row2.Children.Add(MakeField("Ng\u01b0\u1eddi \u0111\u1ea1i di\u1ec7n", _representative, v => _representative = v, "T\u00ean"));
            row2.Children.Add(MakeField("Ch\u1ee9c v\u1ee5", _repTitle, v => _repTitle = v, "Ch\u1ee9c v\u1ee5"));
            panel.Children.Add(row2);
            var row3 = Grid4();
            row3.Children.Add(MakeField("\u0110\u1ecba ch\u1ec9", _companyAddress, v => _companyAddress = v, "\u0110\u1ecba ch\u1ec9"));
            row3.Children.Add(MakeField("Website", _companyWebsite, v => _companyWebsite = v, "Website"));
            row3.Children.Add(MakeCombo("Credit Status", _creditStatus, v => _creditStatus = v, CreditOptions()));
            panel.Children.Add(row3);
        }
        else if (key == "Agent")
        {
            panel.Children.Add(SubTitle("Th\u00f4ng tin \u0111\u1ea1i l\u00fd"));
            var row1 = Grid4();
            row1.Children.Add(MakeField("T\u00ean \u0111\u1ea1i l\u00fd *", _agentName, v => { _agentName = v; OnFieldChanged(); }, "T\u00ean \u0111\u1ea1i l\u00fd"));
            row1.Children.Add(MakeField("M\u00e3 \u0111\u1ea1i l\u00fd", _agentCode, v => _agentCode = v, "M\u00e3"));
            row1.Children.Add(MakeField("IATA Code", _iataCode, v => _iataCode = v, "IATA"));
            row1.Children.Add(MakeCombo("Market", _agentMarket, v => _agentMarket = v, MarketOptions()));
            panel.Children.Add(row1);
            var row2 = Grid4();
            row2.Children.Add(MakeDecimalField("Commission %", _commissionPct, v => _commissionPct = v, "0-100"));
            row2.Children.Add(MakeField("Payment Terms", _agentPaymentTerms, v => _agentPaymentTerms = v, "\u0110i\u1ec1u kho\u1ea3n"));
            row2.Children.Add(MakeField("Contact", _agentContact, v => _agentContact = v, "T\u00ean"));
            row2.Children.Add(MakeField("Email", _agentEmail, v => _agentEmail = v, "Email"));
            panel.Children.Add(row2);
            panel.Children.Add(MakeField("Phone", _agentPhone, v => _agentPhone = v, "S\u0110T"));
        }
        else
        {
            panel.Children.Add(EmptyTabMessage());
        }
        return panel;
    }

    private void BuildStepB()
    {
        StepContentHost.Children.Add(STitle("Liên hệ & đơn vị"));
        StepContentHost.Children.Add(SText("Xác định công ty, đại lý, ban tổ chức và các đầu mối liên hệ."));

        // Org type selector
        StepContentHost.Children.Add(SubTitle("Loại đơn vị"));
        var orgTypes = new[] { ("Không có đơn vị", "None"), ("Công ty", "Company"), ("Đại lý", "Agent"), ("Ban tổ chức", "Organizer"), ("Cá nhân", "Individual") };
        var orgWp = new WrapPanel();
        foreach (var (label, key) in orgTypes)
        {
            var active = _orgType == key;
            var chip = new Border
            {
                CornerRadius = new CornerRadius(8), Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(0, 0, 8, 8),
                Cursor = Cursors.Hand, Background = active ? Cb("#F5F3FF") : Cb("#F3F4F6"),
                BorderBrush = active ? Cb("#7C3AED") : Cb("Transparent"), BorderThickness = new Thickness(1),
                Child = new TextBlock { Text = label, FontSize = 12, FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal, Foreground = active ? C("#6D28D9") : C("#475569") }
            };
            chip.MouseLeftButtonDown += (_, _) => { _orgType = key; BuildStepB(); };
            orgWp.Children.Add(chip);
        }
        StepContentHost.Children.Add(orgWp);
        orgWp.Visibility = Visibility.Collapsed;

        // Replace the old chip row with a real tab strip. The selected tab
        // still drives the existing organization-specific form below it.
        var orgTabs = new TabControl { Margin = new Thickness(0, 0, 0, 12), Style = (Style)FindResource("WizardTabControl"), ItemContainerStyle = (Style)FindResource("WizardTabItem") };
        foreach (var (label, key) in orgTypes)
            orgTabs.Items.Add(new TabItem { Header = label, Tag = key, Content = new Border { Height = 1, Background = Brushes.Transparent } });
        _stepBTabIndex = Math.Max(0, Array.FindIndex(orgTypes, x => x.Item2 == _orgType));
        orgTabs.SelectedIndex = _stepBTabIndex;
        orgTabs.SelectionChanged += (_, _) =>
        {
            if (orgTabs.SelectedItem is TabItem selected && selected.Tag is string key && key != _orgType)
            {
                _orgType = key;
                _stepBTabIndex = orgTabs.SelectedIndex;
                GoToStep(1);
            }
        };
        StepContentHost.Children.Add(orgTabs);
        var orgContentStart = StepContentHost.Children.Count;

        if (_orgType == "Company")
        {
            StepContentHost.Children.Add(SubTitle("Thông tin công ty"));
            var gc = Grid2();
            gc.Children.Add(MakeField("Tên công ty *", _companyName, v => _companyName = v, "Tên công ty"));
            gc.Children.Add(MakeField("Mã công ty", _companyCode, v => _companyCode = v, "Mã"));
            StepContentHost.Children.Add(gc);

            var gcCo2 = Grid2();
            gcCo2.Children.Add(MakeField("Mã số thuế", _taxCode, v => _taxCode = v, "MST"));
            gcCo2.Children.Add(MakeCombo("Quốc gia", _companyCountry, v => _companyCountry = v, CountryOptions()));
            StepContentHost.Children.Add(gcCo2);

            var gcCo3 = Grid2();
            gcCo3.Children.Add(MakeField("Điện thoại", _companyPhone, v => _companyPhone = v, "SĐT"));
            gcCo3.Children.Add(MakeField("Email", _companyEmail, v => _companyEmail = v, "Email"));
            StepContentHost.Children.Add(gcCo3);

            var gc4 = Grid1();
            gc4.Children.Add(MakeField("Địa chỉ", _companyAddress, v => _companyAddress = v, "Địa chỉ"));
            StepContentHost.Children.Add(gc4);

            var gc5 = Grid3();
            gc5.Children.Add(MakeField("Người đại diện", _representative, v => _representative = v, "Tên"));
            gc5.Children.Add(MakeField("Chức vụ", _repTitle, v => _repTitle = v, "Chức vụ"));
            gc5.Children.Add(MakeCombo("Credit Status", _creditStatus, v => _creditStatus = v, CreditOptions()));
            StepContentHost.Children.Add(gc5);
        }

        if (_orgType == "Agent")
        {
            StepContentHost.Children.Add(SubTitle("Thông tin đại lý"));
            var ga = Grid2();
            ga.Children.Add(MakeField("Tên đại lý *", _agentName, v => _agentName = v, "Tên đại lý"));
            ga.Children.Add(MakeField("Mã đại lý", _agentCode, v => _agentCode = v, "Mã"));
            StepContentHost.Children.Add(ga);

            var ga2 = Grid2();
            ga2.Children.Add(MakeField("IATA Code", _iataCode, v => _iataCode = v, "IATA"));
            ga2.Children.Add(MakeCombo("Market", _agentMarket, v => _agentMarket = v, MarketOptions()));
            StepContentHost.Children.Add(ga2);

            var ga3 = Grid2();
            ga3.Children.Add(MakeDecimalField("Commission %", _commissionPct, v => _commissionPct = v, "0-100"));
            ga3.Children.Add(MakeField("Payment Terms", _agentPaymentTerms, v => _agentPaymentTerms = v, "Điều khoản"));
            StepContentHost.Children.Add(ga3);

            var ga4 = Grid3();
            ga4.Children.Add(MakeField("Contact", _agentContact, v => _agentContact = v, "Tên"));
            ga4.Children.Add(MakeField("Email", _agentEmail, v => _agentEmail = v, "Email"));
            ga4.Children.Add(MakeField("Phone", _agentPhone, v => _agentPhone = v, "SĐT"));
            StepContentHost.Children.Add(ga4);
        }

        MoveBOrganizationContentIntoSelectedTab(orgTabs, orgContentStart);

        // Primary Contact (always visible)
        StepContentHost.Children.Add(SubTitle("Người liên hệ chính"));
        var gc1 = Grid2();
        gc1.Children.Add(MakeField("Họ tên *", _primaryContactName, v => { _primaryContactName = v; OnFieldChanged(); }, "Họ và tên"));
        gc1.Children.Add(MakeField("Vai trò", _primaryContactRole, v => _primaryContactRole = v, "Vai trò"));
        StepContentHost.Children.Add(gc1);

        var gc2 = Grid2();
        gc2.Children.Add(MakeField("Điện thoại *", _primaryContactPhone, v => { _primaryContactPhone = v; OnFieldChanged(); }, "SĐT"));
        gc2.Children.Add(MakeField("Email", _primaryContactEmail, v => _primaryContactEmail = v, "Email"));
        StepContentHost.Children.Add(gc2);

        var gc3 = Grid3();
        gc3.Children.Add(MakeCombo("Ngôn ngữ", _primaryContactLang, v => _primaryContactLang = v, LanguageOptions()));
        gc3.Children.Add(MakeCombo("Kênh liên hệ", _primaryContactChannel, v => _primaryContactChannel = v, new[] { "Phone", "Email", "Zalo", "WhatsApp", "WeChat" }));
        gc3.Children.Add(MakeField("Thời gian LH", _primaryContactTime, v => _primaryContactTime = v, "Giờ hành chính"));
        StepContentHost.Children.Add(gc3);

        StepContentHost.Children.Add(MakeField("Ghi chú", _primaryContactNote, v => _primaryContactNote = v, "Ghi chú thêm"));

        // Secondary contacts
        StepContentHost.Children.Add(SubTitle("Liên hệ phụ"));
        var scAdd = new Button { Content = "+ Thêm liên hệ", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        scAdd.Click += (_, _) => { _secondaryContacts.Add(new SecondaryContact()); BuildStepB(); };
        StepContentHost.Children.Add(scAdd);

        for (int i = 0; i < _secondaryContacts.Count; i++)
        {
            var idx = i;
            var sc = _secondaryContacts[i];
            var sg = Grid3();
            sg.Children.Add(MakeField("Họ tên", sc.Name, v => { sc.Name = v; OnFieldChanged(); }, "Tên"));
            sg.Children.Add(MakeField("Vai trò", sc.Role, v => sc.Role = v, "Vai trò"));
            sg.Children.Add(MakeField("SĐT", sc.Phone, v => sc.Phone = v, "SĐT"));
            StepContentHost.Children.Add(sg);
            var sg2 = Grid2();
            sg2.Children.Add(MakeField("Email", sc.Email, v => sc.Email = v, "Email"));
            var del = new Button { Content = "✕ Xóa", Style = (Style)FindResource("GrayBtn"), Height = 34, FontSize = 12, Margin = new Thickness(0, 16, 0, 0), HorizontalAlignment = HorizontalAlignment.Left };
            del.Click += (_, _) => { _secondaryContacts.RemoveAt(idx); BuildStepB(); };
            sg2.Children.Add(del);
            StepContentHost.Children.Add(sg2);
        }

        // Quick actions
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Thao tác nhanh"));
        var qaWp = new WrapPanel();
        qaWp.Children.Add(QuickBtn("Dùng thông tin công ty", () => { if (!string.IsNullOrEmpty(_companyName)) { _primaryContactName = _representative; _primaryContactPhone = _companyPhone; _primaryContactEmail = _companyEmail; BuildStepB(); } }));
        qaWp.Children.Add(QuickBtn("Dùng thông tin đại lý", () => { if (!string.IsNullOrEmpty(_agentName)) { _primaryContactName = _agentContact; _primaryContactPhone = _agentPhone; _primaryContactEmail = _agentEmail; BuildStepB(); } }));
        qaWp.Children.Add(QuickBtn("Dùng trưởng đoàn làm contact", () => { if (!string.IsNullOrEmpty(_groupLeader)) { _primaryContactName = _groupLeader; BuildStepB(); } }));
        qaWp.Children.Add(QuickBtn("Xóa dữ liệu", () => { _companyName = ""; _agentName = ""; _primaryContactName = ""; _primaryContactPhone = ""; _primaryContactEmail = ""; _orgType = "None"; BuildStepB(); }));
        StepContentHost.Children.Add(qaWp);
    }

    // ═══ STEP C — STAY ═══
    private void BuildStepCCompact()
    {
        StepContentHost.Children.Add(STitle("L\u01b0u tr\u00fa d\u1ef1 ki\u1ebfn"));
        StepContentHost.Children.Add(SText("X\u00e1c \u0111\u1ecbnh th\u1eddi gian v\u00e0 ph\u01b0\u01a1ng ti\u1ec7n c\u1ee7a \u0111o\u00e0n."));
        StepContentHost.Children.Add(SubTitle("Th\u1eddi gian l\u01b0u tr\u00fa"));

        var stay = Grid4();
        stay.Children.Add(MakeDate("Ng\u00e0y \u0111\u1ebfn *", _arrivalDate, v => { _arrivalDate = v; OnFieldChanged(); }));
        stay.Children.Add(MakeDate("Ng\u00e0y \u0111i *", _departureDate, v => { _departureDate = v; OnFieldChanged(); }));
        stay.Children.Add(MakeField("Gi\u1edd \u0111\u1ebfn", _arrivalTime, v => _arrivalTime = v, "HH:mm"));
        stay.Children.Add(MakeField("Gi\u1edd \u0111i", _departureTime, v => _departureTime = v, "HH:mm"));
        StepContentHost.Children.Add(stay);
        if (_arrivalDate.HasValue && _departureDate.HasValue)
        {
            var nights = (_departureDate.Value - _arrivalDate.Value).Days;
            StepContentHost.Children.Add(new Border { Background = Cb("#F0FDF4"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 4, 0, 8), Child = new TextBlock { Text = $"S\u1ed1 \u0111\u00eam: {nights} \u0111\u00eam", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } });
        }

        StepContentHost.Children.Add(SubTitle("Ng\u00e0y c\u1eaft ph\u00f2ng"));
        var cutoff = Grid4();
        cutoff.Children.Add(MakeDate("Ng\u00e0y cut-off", _cutoffDate, v => _cutoffDate = v));
        cutoff.Children.Add(MakeField("Gi\u1edd cut-off", _cutoffTime, v => _cutoffTime = v, "HH:mm"));
        cutoff.Children.Add(MakeCheck("Early Arrival", _earlyArrival, v => _earlyArrival = v));
        cutoff.Children.Add(MakeCheck("Late Departure", _lateDeparture, v => _lateDeparture = v));
        StepContentHost.Children.Add(cutoff);

        StepContentHost.Children.Add(SubTitle("Ph\u01b0\u01a1ng ti\u1ec7n"));
        var transport = Grid4();
        transport.Children.Add(MakeCombo("Ph\u01b0\u01a1ng ti\u1ec7n \u0111\u1ebfn", _arrivalMethod, v => _arrivalMethod = v, new[] { "Flight", "Train", "Bus", "Car", "Ship", "Other" }));
        transport.Children.Add(MakeCombo("Ph\u01b0\u01a1ng ti\u1ec7n \u0111i", _departureMethod, v => _departureMethod = v, new[] { "Flight", "Train", "Bus", "Car", "Ship", "Other" }));
        transport.Children.Add(MakeField("S\u1ed1 hi\u1ec7u", _flightNumber, v => _flightNumber = v, "Flight/Train/Bus"));
        transport.Children.Add(MakeField("\u0110i\u1ec3m t\u1eadp trung", _meetingPoint, v => _meetingPoint = v, "\u0110i\u1ec3m \u0111\u00f3n/ti\u1ec5n"));
        StepContentHost.Children.Add(transport);
        var transportMeta = Grid4();
        transportMeta.Children.Add(MakeField("ETA", _eta, v => _eta = v, "Gi\u1edd d\u1ef1 ki\u1ebfn \u0111\u1ebfn"));
        transportMeta.Children.Add(MakeField("ETD", _etd, v => _etd = v, "Gi\u1edd d\u1ef1 ki\u1ebfn \u0111i"));
        transportMeta.Children.Add(MakeCheck("\u0110\u00f3n s\u00e2n bay", _pickupRequired, v => _pickupRequired = v));
        transportMeta.Children.Add(MakeCheck("Ti\u1ec5n s\u00e2n bay", _dropoffRequired, v => _dropoffRequired = v));
        StepContentHost.Children.Add(transportMeta);

        StepContentHost.Children.Add(SubTitle("Stay Pattern"));
        StepContentHost.Children.Add(MakeCombo("Ki\u1ec3u l\u01b0u tr\u00fa", _stayPattern, v => _stayPattern = v, new[] { "C\u00f9ng ng\u00e0y \u0111\u1ebfn/\u0111i", "Nhi\u1ec1u \u0111\u1ee3t \u0111\u1ebfn", "Nhi\u1ec1u \u0111\u1ee3t \u0111i", "Rolling Arrival", "Rolling Departure", "Split Stay" }));

        StepContentHost.Children.Add(SubTitle("\u0110\u1ee3t \u0111\u1ebfn"));
        var arrivalAdd = new Button { Content = "+ Th\u00eam \u0111\u1ee3t \u0111\u1ebfn", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        arrivalAdd.Click += (_, _) => { _arrivalWaves.Add(new ArrivalWave()); GoToStep(2); };
        StepContentHost.Children.Add(arrivalAdd);
        for (var i = 0; i < _arrivalWaves.Count; i++) { var index = i; var wave = _arrivalWaves[i]; StepContentHost.Children.Add(WaveEditor(wave, () => { _arrivalWaves.RemoveAt(index); GoToStep(2); })); }

        StepContentHost.Children.Add(SubTitle("\u0110\u1ee3t \u0111i"));
        var departureAdd = new Button { Content = "+ Th\u00eam \u0111\u1ee3t \u0111i", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        departureAdd.Click += (_, _) => { _departureWaves.Add(new DepartureWave()); GoToStep(2); };
        StepContentHost.Children.Add(departureAdd);
        for (var i = 0; i < _departureWaves.Count; i++) { var index = i; var wave = _departureWaves[i]; StepContentHost.Children.Add(WaveDepartureEditor(wave, () => { _departureWaves.RemoveAt(index); GoToStep(2); })); }
    }

    private void BuildStepC()
    {
        StepContentHost.Children.Add(STitle("Lưu trú dự kiến"));
        StepContentHost.Children.Add(SText("Xác định thời gian đoàn dự kiến lưu trú."));

        StepContentHost.Children.Add(SubTitle("Thời gian lưu trú"));
        var g1 = Grid2();
        g1.Children.Add(MakeDate("Ngày đến *", _arrivalDate, v => { _arrivalDate = v; OnFieldChanged(); }));
        g1.Children.Add(MakeDate("Ngày đi *", _departureDate, v => { _departureDate = v; OnFieldChanged(); }));
        StepContentHost.Children.Add(g1);

        var g1b = Grid2();
        g1b.Children.Add(MakeField("Giờ đến", _arrivalTime, v => _arrivalTime = v, "HH:mm"));
        g1b.Children.Add(MakeField("Giờ đi", _departureTime, v => _departureTime = v, "HH:mm"));
        StepContentHost.Children.Add(g1b);

        if (_arrivalDate.HasValue && _departureDate.HasValue)
        {
            var nights = (_departureDate.Value - _arrivalDate.Value).Days;
            StepContentHost.Children.Add(new Border { Background = Cb("#F0FDF4"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 4, 0, 8), Child = new TextBlock { Text = $"Số đêm: {nights} đêm", FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = C("#16A34A") } });
        }

        StepContentHost.Children.Add(SubTitle("Ngày cắt phòng"));
        var g2 = Grid2();
        g2.Children.Add(MakeDate("Ngày cut-off", _cutoffDate, v => _cutoffDate = v));
        g2.Children.Add(MakeField("Giờ cut-off", _cutoffTime, v => _cutoffTime = v, "HH:mm"));
        StepContentHost.Children.Add(g2);

        var g2b = Grid2();
        g2b.Children.Add(MakeCheck("Early Arrival", _earlyArrival, v => _earlyArrival = v));
        g2b.Children.Add(MakeCheck("Late Departure", _lateDeparture, v => _lateDeparture = v));
        StepContentHost.Children.Add(g2b);

        StepContentHost.Children.Add(SubTitle("Phương tiện"));
        var g3 = Grid2();
        g3.Children.Add(MakeCombo("Phương tiện đến", _arrivalMethod, v => _arrivalMethod = v, new[] { "Flight", "Train", "Bus", "Car", "Ship", "Other" }));
        g3.Children.Add(MakeCombo("Phương tiện đi", _departureMethod, v => _departureMethod = v, new[] { "Flight", "Train", "Bus", "Car", "Ship", "Other" }));
        StepContentHost.Children.Add(g3);

        var g3b = Grid3();
        g3b.Children.Add(MakeField("Số hiệu", _flightNumber, v => _flightNumber = v, "Flight/Train/Bus"));
        g3b.Children.Add(MakeField("ETA", _eta, v => _eta = v, "Giờ dự kiến đến"));
        g3b.Children.Add(MakeField("ETD", _etd, v => _etd = v, "Giờ dự kiến đi"));
        StepContentHost.Children.Add(g3b);

        var g3c = Grid3();
        g3c.Children.Add(MakeCheck("Đón sân bay", _pickupRequired, v => _pickupRequired = v));
        g3c.Children.Add(MakeCheck("Tiễn sân bay", _dropoffRequired, v => _dropoffRequired = v));
        g3c.Children.Add(MakeField("Điểm tập trung", _meetingPoint, v => _meetingPoint = v, "Điểm đón/tiễn"));
        StepContentHost.Children.Add(g3c);

        StepContentHost.Children.Add(SubTitle("Stay Pattern"));
        StepContentHost.Children.Add(MakeCombo("Kiểu lưu trú", _stayPattern, v => _stayPattern = v, new[] { "Cùng ngày đến/đi", "Nhiều đợt đến", "Nhiều đợt đi", "Rolling Arrival", "Rolling Departure", "Split Stay" }));

        // Arrival/Departure Waves
        StepContentHost.Children.Add(SubTitle("Đợt đến (Arrival Waves)"));
        var awAdd = new Button { Content = "+ Thêm đợt đến", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        awAdd.Click += (_, _) => { _arrivalWaves.Add(new ArrivalWave()); BuildStepC(); };
        StepContentHost.Children.Add(awAdd);
        for (int i = 0; i < _arrivalWaves.Count; i++) { var idx = i; var w = _arrivalWaves[i]; StepContentHost.Children.Add(WaveEditor(w, () => { _arrivalWaves.RemoveAt(idx); BuildStepC(); })); }

        StepContentHost.Children.Add(SubTitle("Đợt đi (Departure Waves)"));
        var dwAdd = new Button { Content = "+ Thêm đợt đi", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        dwAdd.Click += (_, _) => { _departureWaves.Add(new DepartureWave()); BuildStepC(); };
        StepContentHost.Children.Add(dwAdd);
        for (int i = 0; i < _departureWaves.Count; i++) { var idx = i; var w = _departureWaves[i]; StepContentHost.Children.Add(WaveDepartureEditor(w, () => { _departureWaves.RemoveAt(idx); BuildStepC(); })); }
    }

    // ═══ STEP D — ROOM REQUIREMENTS ═══
    private void BuildStepD()
    {
        StepContentHost.Children.Add(STitle("Nhu cầu phòng"));
        StepContentHost.Children.Add(SText("Xác định room block thật theo hạng phòng đang hoạt động. Block giữ số lượng theo kế hoạch; việc gán phòng vật lý thực hiện ở Rooming List."));

        // KPI row
        var kpiWp = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        var totalRooms = _roomRequirements.Sum(r => r.RoomCount);
        var totalNights = _roomRequirements.Sum(r => r.RoomCount * (_departureDate.HasValue && _arrivalDate.HasValue ? (_departureDate.Value - _arrivalDate.Value).Days : 0));
        var totalGuests = _roomRequirements.Sum(r => r.RoomCount * r.GuestsPerRoom);
        kpiWp.Children.Add(KpiBox("Tổng phòng", totalRooms.ToString(), "#7C3AED"));
        kpiWp.Children.Add(KpiBox("Room Nights", totalNights.ToString(), "#2563EB"));
        kpiWp.Children.Add(KpiBox("Tổng khách", totalGuests.ToString(), "#16A34A"));
        kpiWp.Children.Add(KpiBox("Extra Bed", _roomRequirements.Sum(r => r.ExtraBed).ToString(), "#D97706"));
        StepContentHost.Children.Add(kpiWp);

        // Actions
        var actWp = new WrapPanel { Margin = new Thickness(0, 0, 0, 10) };
        actWp.Children.Add(new Button { Content = "+ Thêm hạng phòng", Style = (Style)FindResource("PrimaryBtn"), Height = 36, FontSize = 12, Padding = new Thickness(14, 0, 14, 0), Margin = new Thickness(0, 0, 8, 0) });
        ((Button)actWp.Children[^1]).Click += (_, _) => { _roomRequirements.Add(new RoomRequirement()); BuildStepD(); };
        actWp.Children.Add(new Button { Content = "Chia đều", Style = (Style)FindResource("GrayBtn"), Height = 36, FontSize = 12, Padding = new Thickness(12, 0, 12, 0), Margin = new Thickness(0, 0, 8, 0) });
        ((Button)actWp.Children[^1]).Click += (_, _) => { if (_roomRequirements.Count > 0 && _roomCount > 0) { var each = _roomCount / _roomRequirements.Count; foreach (var r in _roomRequirements) r.RoomCount = each; BuildStepD(); } };
        actWp.Children.Add(new Button { Content = "Kiểm tra khả dụng", Style = (Style)FindResource("ActionBtn"), Height = 36, FontSize = 12, Margin = new Thickness(0, 0, 8, 0) });
        ((Button)actWp.Children[^1]).Click += async (_, _) => await CheckRoomAvailabilityAsync();
        StepContentHost.Children.Add(actWp);

        // Room requirement cards
        for (int i = 0; i < _roomRequirements.Count; i++)
        {
            var idx = i;
            var rr = _roomRequirements[i];
            var card = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(14, 12, 14, 12), Margin = new Thickness(0, 0, 0, 8) };
            var stack = new StackPanel();

            stack.Children.Add(new TextBlock { Text = $"Yêu cầu #{i + 1}", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#7C3AED"), Margin = new Thickness(0, 0, 0, 8) });

            var rg1 = Grid4();
            rg1.Children.Add(MakeComboInline("Hạng phòng *", rr.RoomType, v => { rr.RoomType = v; OnFieldChanged(); }, RoomTypeOptions()));
            rg1.Children.Add(MakeIntInline("Số phòng", rr.RoomCount, v => { rr.RoomCount = v; OnFieldChanged(); }));
            rg1.Children.Add(MakeIntInline("Người/phòng", rr.GuestsPerRoom, v => rr.GuestsPerRoom = v));
            stack.Children.Add(rg1);

            var rg2 = Grid4();
            rg2.Children.Add(MakeIntInline("Extra Bed", rr.ExtraBed, v => rr.ExtraBed = v));
            rg2.Children.Add(MakeCheckInline("Connecting", rr.Connecting, v => rr.Connecting = v));
            rg2.Children.Add(MakeComboInline("Smoking", rr.Smoking, v => rr.Smoking = v, new[] { "Non-smoking", "Smoking", "Either" }));
            stack.Children.Add(rg2);

            var rg3 = Grid4();
            rg3.Children.Add(MakeComboInline("View", rr.View, v => rr.View = v, new[] { "Không yêu cầu", "Biển", "Thành phố", "Vườn", "Hồ bơi" }));
            rg3.Children.Add(MakeFieldInline("Tầng", rr.FloorPreference, v => rr.FloorPreference = v, "Ưu tiên tầng"));
            rg3.Children.Add(MakeCheckInline("Accessible", rr.Accessible, v => rr.Accessible = v));
            stack.Children.Add(rg3);

            var rg4 = Grid4();
            rg4.Children.Add(MakeFieldInline("Ghi chú", rr.Note, v => rr.Note = v, "Ghi chú thêm"));
            var delBtn = new Button { Content = "✕ Xóa yêu cầu", Style = (Style)FindResource("GrayBtn"), Height = 34, FontSize = 12, Margin = new Thickness(0, 16, 0, 0) };
            delBtn.Click += (_, _) => { _roomRequirements.RemoveAt(idx); BuildStepD(); };
            rg4.Children.Add(delBtn);
            stack.Children.Add(rg4);

            card.Child = stack;
            StepContentHost.Children.Add(card);
        }

        // Availability matrix
        if (_roomRequirements.Count > 0)
        {
            StepContentHost.Children.Add(Sep());
            StepContentHost.Children.Add(SubTitle("Khả dụng phòng"));
            StepContentHost.Children.Add(new TextBlock
            {
                Text = _roomTypes.Count == 0
                    ? "Chưa tải được hạng phòng thật từ cơ sở dữ liệu. Không hiển thị số liệu mẫu; hãy kiểm tra kết nối và thử lại."
                    : $"{_roomAvailabilitySummary}. Bấm ‘Kiểm tra khả dụng’ để đối chiếu tồn phòng thật theo ngày và booking hiện có.",
                FontSize = 12, Foreground = C("#92400E"), TextWrapping = TextWrapping.Wrap
            });
        }
    }

    // ═══ STEP E — RATE & PAYMENT ═══
    private void BuildStepE()
    {
        StepContentHost.Children.Add(STitle("Giá & thanh toán"));
        StepContentHost.Children.Add(SText("Thiết lập điều kiện thương mại sơ bộ."));

        // Tabs
        var tabWp = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        var tabs = new[] { "Giá phòng", "Package", "Thanh toán", "Đặt cọc", "Hoa hồng", "Thuế & phí" };
        var tabIndex = 0; // simplified
        for (int i = 0; i < tabs.Length; i++)
        {
            var idx = i;
            var tab = new Border
            {
                CornerRadius = new CornerRadius(8, 8, 0, 0), Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 2, 0), Cursor = Cursors.Hand,
                Background = idx == tabIndex ? Cb("#F5F3FF") : Cb("#F1F5F9"),
                BorderBrush = idx == tabIndex ? Cb("#7C3AED") : Cb("Transparent"),
                BorderThickness = new Thickness(idx == tabIndex ? 1.5 : 1, 1, 1, 0),
                Child = new TextBlock { Text = tabs[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = idx == tabIndex ? C("#6D28D9") : C("#64748B") }
            };
            tab.Width = 110; tab.Height = 34;
            tabWp.Children.Add(tab);
        }
        StepContentHost.Children.Add(tabWp);

        // Giá phòng
        StepContentHost.Children.Add(SubTitle("Giá phòng"));
        var g1 = Grid2();
        g1.Children.Add(MakeCombo("Rate Plan", _ratePlan, v => _ratePlan = v, new[] { "RACK", "CORP", "GOV", "TOUR", "PROMO", "GROUP" }));
        g1.Children.Add(MakeCombo("Currency", _currency, v => { _currency = v; OnFieldChanged(); }, new[] { "VND", "USD", "EUR", "JPY", "CNY" }));
        StepContentHost.Children.Add(g1);

        var g1b = Grid3();
        g1b.Children.Add(MakeDecimalField("Giá trước thuế", _rateBeforeTax, v => _rateBeforeTax = v, "VNĐ"));
        g1b.Children.Add(MakeDecimalField("Giá sau thuế", _rateAfterTax, v => _rateAfterTax = v, "VNĐ"));
        g1b.Children.Add(MakeDecimalField("Tỷ giá", _exchangeRate, v => _exchangeRate = v, "Rate"));
        StepContentHost.Children.Add(g1b);

        var g1c = Grid4();
        g1c.Children.Add(MakeDecimalField("Single Supplement", _singleSupplement, v => _singleSupplement = v, ""));
        g1c.Children.Add(MakeDecimalField("Double Rate", _doubleRate, v => _doubleRate = v, ""));
        g1c.Children.Add(MakeDecimalField("Extra Bed", _extraBedRate, v => _extraBedRate = v, ""));
        g1c.Children.Add(MakeDecimalField("Trẻ em", _childRate, v => _childRate = v, ""));
        StepContentHost.Children.Add(g1c);

        var g1d = Grid2();
        g1d.Children.Add(MakeIntField("Phòng miễn phí", _complimentaryRooms, v => _complimentaryRooms = v));
        g1d.Children.Add(MakeField("Tỷ lệ phòng free", _freeRoomRatio, v => _freeRoomRatio = v, "vd: 1/15"));
        StepContentHost.Children.Add(g1d);

        // Package
        StepContentHost.Children.Add(SubTitle("Package"));
        var pkgGrid = Grid5();
        pkgGrid.Children.Add(MakeCheck("Breakfast", _breakfast, v => _breakfast = v));
        pkgGrid.Children.Add(MakeCheck("Lunch", _lunch, v => _lunch = v));
        pkgGrid.Children.Add(MakeCheck("Dinner", _dinner, v => _dinner = v));
        pkgGrid.Children.Add(MakeCheck("Meeting", _meetingPackage, v => _meetingPackage = v));
        pkgGrid.Children.Add(MakeCheck("Transfer", _airportTransfer, v => _airportTransfer = v));
        StepContentHost.Children.Add(pkgGrid);
        var pkgGrid2 = Grid5();
        pkgGrid2.Children.Add(MakeCheck("Welcome Drink", _welcomeDrink, v => _welcomeDrink = v));
        pkgGrid2.Children.Add(MakeCheck("Gala Dinner", _galaDinner, v => _galaDinner = v));
        pkgGrid2.Children.Add(MakeCheck("Spa", _spa, v => _spa = v));
        pkgGrid2.Children.Add(MakeCheck("Laundry", _laundry, v => _laundry = v));
        pkgGrid2.Children.Add(MakeCheck("Other", _otherPackage, v => _otherPackage = v));
        StepContentHost.Children.Add(pkgGrid2);

        // Payment
        StepContentHost.Children.Add(SubTitle("Thanh toán"));
        var g2 = Grid2();
        g2.Children.Add(MakeCombo("Payer *", _payer, v => { _payer = v; OnFieldChanged(); }, new[] { "Công ty", "Đại lý", "Khách tự trả", "Công ty + Khách", "Đại lý + Khách" }));
        g2.Children.Add(MakeCombo("Phương thức", _paymentMethod, v => _paymentMethod = v, new[] { "Chuyển khoản", "Tiền mặt", "Thẻ tín dụng", "Công nợ", "Hỗn hợp" }));
        StepContentHost.Children.Add(g2);

        var g2b = Grid2();
        g2b.Children.Add(MakeCheck("Cho phép công nợ", _creditAllowed, v => _creditAllowed = v));
        g2b.Children.Add(MakeDecimalField("Hạn mức công nợ", _creditLimit, v => _creditLimit = v, "VNĐ"));
        StepContentHost.Children.Add(g2b);

        var g2c = Grid3();
        g2c.Children.Add(MakeField("Billing Instruction", _billingInstruction, v => _billingInstruction = v, "Hướng dẫn"));
        g2c.Children.Add(MakeField("Routing Rule", _routingRule, v => _routingRule = v, "Quy tắc"));
        g2c.Children.Add(MakeField("Payment Terms", _paymentTerms, v => _paymentTerms = v, "Điều khoản"));
        StepContentHost.Children.Add(g2c);

        var g2d = Grid4();
        g2d.Children.Add(MakeCheck("City Ledger", _cityLedger, v => _cityLedger = v));
        g2d.Children.Add(MakeCheck("Master Folio", _masterFolio, v => _masterFolio = v));
        g2d.Children.Add(MakeCheck("Cá nhân trả", _individualPay, v => _individualPay = v));
        g2d.Children.Add(MakeCheck("Hỗn hợp", _mixedPayment, v => _mixedPayment = v));
        StepContentHost.Children.Add(g2d);

        // Deposit
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Đặt cọc"));
        var g3 = Grid2();
        g3.Children.Add(MakeCheck("Yêu cầu đặt cọc", _depositRequired, v => _depositRequired = v));
        g3.Children.Add(MakeCombo("Loại đặt cọc", _depositType, v => _depositType = v, new[] { "Tiền mặt", "Chuyển khoản", "Thẻ tín dụng", "Guarantee letter" }));
        StepContentHost.Children.Add(g3);

        var g3b = Grid3();
        g3b.Children.Add(MakeDecimalField("Số tiền", _depositAmount, v => _depositAmount = v, "VNĐ"));
        g3b.Children.Add(MakeDecimalField("Phần trăm", _depositPercent, v => _depositPercent = v, "%"));
        g3b.Children.Add(MakeDate("Hạn đặt cọc", _depositDeadline, v => _depositDeadline = v));
        StepContentHost.Children.Add(g3b);

        var g3c = Grid3();
        g3c.Children.Add(MakeIntField("Số đợt", _depositInstallments, v => _depositInstallments = v));
        g3c.Children.Add(MakeCheck("Hoàn lại", _depositRefundable, v => _depositRefundable = v));
        g3c.Children.Add(MakeCombo("Guarantee Type", _guaranteeType, v => _guaranteeType = v, new[] { "Credit Card", "Cash", "Bank Transfer", "Letter" }));
        StepContentHost.Children.Add(g3c);

        // Commission
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Hoa hồng"));
        var g4 = Grid2();
        g4.Children.Add(MakeCheck("Hoa hồng đại lý", _agentCommissionEnabled, v => _agentCommissionEnabled = v));
        g4.Children.Add(MakeDecimalField("% Hoa hồng", _agentCommissionPct, v => _agentCommissionPct = v, "%"));
        StepContentHost.Children.Add(g4);

        var g4b = Grid3();
        g4b.Children.Add(MakeCombo("Cơ sở tính", _commissionBase, v => _commissionBase = v, new[] { "Room Revenue", "Total Revenue", "Room + F&B" }));
        g4b.Children.Add(MakeCombo("Thuế hoa hồng", _commissionTax, v => _commissionTax = v, new[] { "Đã gồm thuế", "Chưa thuế", "Khấu trừ" }));
        g4b.Children.Add(MakeCombo("Thời điểm trả", _commissionTiming, v => _commissionTiming = v, new[] { "Sau checkout", "Hàng tháng", "Theo hợp đồng" }));
        StepContentHost.Children.Add(g4b);

        // Tax & Fees
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Thuế & phí"));
        var g5 = Grid4();
        g5.Children.Add(MakeDecimalField("VAT %", _vatPct, v => _vatPct = v, "%"));
        g5.Children.Add(MakeDecimalField("Service Charge %", _serviceChargePct, v => _serviceChargePct = v, "%"));
        g5.Children.Add(MakeDecimalField("City Tax", _cityTax, v => _cityTax = v, ""));
        g5.Children.Add(MakeDecimalField("Tourism Fee", _tourismFee, v => _tourismFee = v, ""));
        StepContentHost.Children.Add(g5);

        var g5b = Grid2();
        g5b.Children.Add(MakeCheck("Đã gồm thuế", _taxIncluded, v => _taxIncluded = v));
        g5b.Children.Add(MakeCheck("Miễn thuế", _taxExemption, v => _taxExemption = v));
        StepContentHost.Children.Add(g5b);
        ConvertStepEToTabs();
    }

    // ═══ STEP F — SERVICES & REQUIREMENTS ═══
    private void ConvertStepEToTabs()
    {
        var all = StepContentHost.Children.Cast<UIElement>().ToList();
        if (all.Count < 29) return;

        StepContentHost.Children.Clear();
        var tabs = new TabControl { Margin = new Thickness(0, 4, 0, 0), Style = (Style)FindResource("WizardTabControl"), ItemContainerStyle = (Style)FindResource("WizardTabItem") };
        AddWizardTab(tabs, "Gi\u00E1 ph\u00F2ng", all.GetRange(3, 5));
        AddWizardTab(tabs, "Package", all.GetRange(8, 3));
        AddWizardTab(tabs, "Thanh to\u00E1n", all.GetRange(11, 4));
        AddWizardTab(tabs, "\u0110\u1EB7t c\u1ECDc", all.GetRange(16, 5));
        AddWizardTab(tabs, "Hoa h\u1ED3ng", all.GetRange(22, 3));
        AddWizardTab(tabs, "Thu\u1EBF & ph\u00ED", all.GetRange(26, 3));
        tabs.SelectedIndex = Math.Clamp(_stepETabIndex, 0, tabs.Items.Count - 1);
        tabs.SelectionChanged += (_, _) => _stepETabIndex = tabs.SelectedIndex;
        StepContentHost.Children.Add(all[0]);
        StepContentHost.Children.Add(all[1]);
        StepContentHost.Children.Add(tabs);
    }

    private void ConvertStepFToTabs()
    {
        var all = StepContentHost.Children.Cast<UIElement>().ToList();
        var serviceCount = _services.Count;
        if (all.Count < 23 + serviceCount) return;

        StepContentHost.Children.Clear();
        var tabs = new TabControl { Margin = new Thickness(0, 4, 0, 0), Style = (Style)FindResource("WizardTabControl"), ItemContainerStyle = (Style)FindResource("WizardTabItem") };
        AddWizardTab(tabs, "D\u1ECBch v\u1EE5", all.GetRange(3, 6 + serviceCount));
        AddWizardTab(tabs, "F&B", new[] { (UIElement)EmptyTabMessage() });
        AddWizardTab(tabs, "H\u1ED9i ngh\u1ECB", new[] { (UIElement)EmptyTabMessage() });
        AddWizardTab(tabs, "V\u1EADn chuy\u1EC3n", new[] { (UIElement)EmptyTabMessage() });
        AddWizardTab(tabs, "VIP & An ninh", all.GetRange(10 + serviceCount, 3));
        AddWizardTab(tabs, "T\u00E0i li\u1EC7u", all.GetRange(14 + serviceCount, 3));
        AddWizardTab(tabs, "Ghi ch\u00FA", all.GetRange(18 + serviceCount, 5));
        tabs.SelectedIndex = Math.Clamp(_stepFTabIndex, 0, tabs.Items.Count - 1);
        tabs.SelectionChanged += (_, _) => _stepFTabIndex = tabs.SelectedIndex;
        StepContentHost.Children.Add(all[0]);
        StepContentHost.Children.Add(all[1]);
        StepContentHost.Children.Add(tabs);
    }

    private static TextBlock EmptyTabMessage() => new()
    {
        Text = "Ch\u01B0a c\u00F3 c\u1EA5u h\u00ECnh cho m\u1EE5c n\u00E0y.",
        FontSize = 13,
        Foreground = C("#64748B"),
        Margin = new Thickness(0, 12, 0, 12)
    };

    private static void AddWizardTab(TabControl tabs, string header, IEnumerable<UIElement> children)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 0) };
        foreach (var child in children) panel.Children.Add(child);
        tabs.Items.Add(new TabItem { Header = header, Content = panel });
    }

    private void MoveBOrganizationContentIntoSelectedTab(TabControl tabs, int startIndex)
    {
        var content = StepContentHost.Children.Cast<UIElement>().Skip(startIndex).ToList();
        foreach (var child in content) StepContentHost.Children.Remove(child);

        var panel = new StackPanel { Margin = new Thickness(0, 2, 0, 0) };
        foreach (var child in content) panel.Children.Add(child);
        if (panel.Children.Count == 0) panel.Children.Add(EmptyTabMessage());

        if (tabs.SelectedItem is TabItem selected) selected.Content = panel;
    }

    private void BuildStepF()
    {
        StepContentHost.Children.Add(STitle("Dịch vụ & yêu cầu"));
        StepContentHost.Children.Add(SText("Thiết lập các dịch vụ đoàn và yêu cầu vận hành."));

        // Service tabs
        var sTabs = new[] { "Dịch vụ", "F&B", "Hội nghị", "Vận chuyển", "VIP & An ninh", "Tài liệu", "Ghi chú" };
        var sTabWp = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        for (int i = 0; i < sTabs.Length; i++)
        {
            var idx = i;
            var tab = new Border
            {
                CornerRadius = new CornerRadius(7), Padding = new Thickness(14, 6, 14, 6), Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand, Background = idx == 0 ? Cb("#F5F3FF") : Cb("#F1F5F9"),
                BorderBrush = idx == 0 ? Cb("#7C3AED") : Cb("Transparent"), BorderThickness = new Thickness(1),
                Child = new TextBlock { Text = sTabs[i], FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = idx == 0 ? C("#6D28D9") : C("#475569") }
            };
            sTabWp.Children.Add(tab);
        }
        StepContentHost.Children.Add(sTabWp);

        // General services
        StepContentHost.Children.Add(SubTitle("Dịch vụ chung"));
        var sGrid = Grid4();
        sGrid.Children.Add(MakeCheck("Welcome Desk", false, v => { }));
        sGrid.Children.Add(MakeCheck("Check-in Desk riêng", false, v => { }));
        sGrid.Children.Add(MakeCheck("Hành lý", false, v => { }));
        sGrid.Children.Add(MakeCheck("Bell Service", false, v => { }));
        StepContentHost.Children.Add(sGrid);
        var sGrid2 = Grid4();
        sGrid2.Children.Add(MakeCheck("Wake-up Call", false, v => { }));
        sGrid2.Children.Add(MakeCheck("Báo", false, v => { }));
        sGrid2.Children.Add(MakeCheck("Amenity", false, v => { }));
        sGrid2.Children.Add(MakeCheck("Thư chào mừng", false, v => { }));
        StepContentHost.Children.Add(sGrid2);

        // Add service
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Yêu cầu dịch vụ"));
        var svcAdd = new Button { Content = "+ Thêm yêu cầu", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        svcAdd.Click += (_, _) => { _services.Add(new GroupService()); BuildStepF(); };
        StepContentHost.Children.Add(svcAdd);

        for (int i = 0; i < _services.Count; i++)
        {
            var idx = i; var svc = _services[i];
            var card = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 0, 6) };
            var stk = new StackPanel();
            stk.Children.Add(new TextBlock { Text = $"Dịch vụ #{i + 1}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#7C3AED"), Margin = new Thickness(0, 0, 0, 6) });
            var sg = Grid3();
            sg.Children.Add(MakeComboInline("Loại", svc.Type, v => svc.Type = v, new[] { "F&B", "Hội nghị", "Vận chuyển", "Housekeeping", "VIP", "An ninh", "Khác" }));
            sg.Children.Add(MakeFieldInline("Nội dung", svc.Description, v => svc.Description = v, "Mô tả"));
            sg.Children.Add(MakeComboInline("Priority", svc.Priority, v => svc.Priority = v, new[] { "Thấp", "Bình thường", "Cao", "Khẩn" }));
            stk.Children.Add(sg);
            var sg2 = Grid3();
            sg2.Children.Add(MakeFieldInline("Ngày", svc.Date, v => svc.Date = v, "dd/MM"));
            sg2.Children.Add(MakeFieldInline("Giờ", svc.Time, v => svc.Time = v, "HH:mm"));
            sg2.Children.Add(MakeIntInline("SL", svc.Quantity, v => svc.Quantity = v));
            stk.Children.Add(sg2);
            var sg3 = Grid3();
            sg3.Children.Add(MakeComboInline("Bộ phận", svc.Department, v => svc.Department = v, new[] { "Lễ tân", "Nhà buồng", "F&B", "An ninh", "Kinh doanh", "Tiệc" }));
            sg3.Children.Add(MakeComboInline("Status", svc.Status, v => svc.Status = v, new[] { "Chưa làm", "Đang làm", "Hoàn thành", "Đã hủy" }));
            var delS = new Button { Content = "✕ Xóa", Style = (Style)FindResource("GrayBtn"), Height = 30, FontSize = 11, Margin = new Thickness(0, 12, 0, 0) };
            delS.Click += (_, _) => { _services.RemoveAt(idx); BuildStepF(); };
            sg3.Children.Add(delS);
            stk.Children.Add(sg3);
            card.Child = stk;
            StepContentHost.Children.Add(card);
        }

        // VIP & Security
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("VIP & An ninh"));
        var vg = Grid2();
        vg.Children.Add(MakeCombo("VIP Level", _vipLevel, v => _vipLevel = v, VipOptions()));
        vg.Children.Add(MakeField("Security Note", _securityNote, v => _securityNote = v, "Ghi chú an ninh"));
        StepContentHost.Children.Add(vg);

        var vg2 = Grid4();
        vg2.Children.Add(MakeCheck("Security Watch", false, v => { }));
        vg2.Children.Add(MakeCheck("Government Protocol", false, v => { }));
        vg2.Children.Add(MakeCheck("Blacklist Check", false, v => { }));
        vg2.Children.Add(MakeCheck("Confidentiality", false, v => { }));
        StepContentHost.Children.Add(vg2);

        // Documents
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Tài liệu"));
        var docAdd = new Button { Content = "+ Tải tài liệu", Style = (Style)FindResource("ActionBtn"), Margin = new Thickness(0, 0, 0, 8) };
        docAdd.Click += (_, _) => MessageBox.Show("Chưa có contract lưu tài liệu thật cho Group Profile. Không tạo tài liệu giả.", "Tài liệu đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
        StepContentHost.Children.Add(docAdd);

        var docDg = new DataGrid { Style = (Style)FindResource("ModDataGrid"), MaxHeight = 160, IsReadOnly = true, ItemsSource = _documents };
        docDg.Columns.Add(new DataGridTextColumn { Header = "Tài liệu", Binding = new System.Windows.Data.Binding("Name"), Width = 180 });
        docDg.Columns.Add(new DataGridTextColumn { Header = "Loại", Binding = new System.Windows.Data.Binding("Type"), Width = 100 });
        docDg.Columns.Add(new DataGridTextColumn { Header = "Trạng thái", Binding = new System.Windows.Data.Binding("Status"), Width = 90 });
        docDg.Columns.Add(new DataGridTextColumn { Header = "Ngày tải", Binding = new System.Windows.Data.Binding("UploadDate"), Width = 100 });
        StepContentHost.Children.Add(docDg);

        // Notes
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Ghi chú bộ phận"));
        var ng1 = Grid2();
        ng1.Children.Add(MakeTextArea("Nội bộ", _noteInternal, v => _noteInternal = v, "Ghi chú nội bộ"));
        ng1.Children.Add(MakeTextArea("Lễ tân", _noteReception, v => _noteReception = v, "Ghi chú lễ tân"));
        StepContentHost.Children.Add(ng1);
        var ng2 = Grid2();
        ng2.Children.Add(MakeTextArea("Nhà buồng", _noteHousekeeping, v => _noteHousekeeping = v, "Ghi chú nhà buồng"));
        ng2.Children.Add(MakeTextArea("Thu ngân", _noteCashier, v => _noteCashier = v, "Ghi chú thu ngân"));
        StepContentHost.Children.Add(ng2);
        var ng3 = Grid2();
        ng3.Children.Add(MakeTextArea("F&B", _noteFnb, v => _noteFnb = v, "Ghi chú F&B"));
        ng3.Children.Add(MakeTextArea("An ninh", _noteSecurity, v => _noteSecurity = v, "Ghi chú an ninh"));
        StepContentHost.Children.Add(ng3);
        var ng4 = Grid2();
        ng4.Children.Add(MakeTextArea("Kinh doanh", _noteSales, v => _noteSales = v, "Ghi chú kinh doanh"));
        ng4.Children.Add(MakeTextArea("Tiệc", _noteBanquet, v => _noteBanquet = v, "Ghi chú tiệc"));
        StepContentHost.Children.Add(ng4);
        ConvertStepFToTabs();
    }

    // ═══ STEP G — REVIEW ═══
    private void BuildStepGCompact()
    {
        StepContentHost.Children.Add(STitle("Ki\u1ec3m tra"));
        StepContentHost.Children.Add(SText("Ki\u1ec3m tra to\u00e0n b\u1ed9 h\u1ed3 s\u01a1 \u0111o\u00e0n tr\u01b0\u1edbc khi x\u00e1c nh\u1eadn."));
        StepContentHost.Children.Add(SubTitle("Danh s\u00e1ch ki\u1ec3m tra"));
        var checkGrid = Grid4();
        foreach (var (label, ok, _) in GetChecklist())
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal };
            row.Children.Add(new TextBlock { Text = ok ? "\u2713 " : "\u2717 ", FontSize = 13, FontWeight = FontWeights.Bold, Foreground = ok ? C("#16A34A") : C("#DC2626") });
            row.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155"), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center });
            checkGrid.Children.Add(new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(0, 2, 0, 2), Background = ok ? Cb("#F0FDF4") : Cb("#FEF2F2"), BorderBrush = ok ? Cb("#BBF7D0") : Cb("#FECACA"), BorderThickness = new Thickness(1), Child = row });
        }
        StepContentHost.Children.Add(checkGrid);
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("\u0110i\u1ec3m s\u1eb5n s\u00e0ng"));
        var scores = Grid4();
        scores.Children.Add(ScoreBox("H\u1ed3 s\u01a1", GetScore("profile")));
        scores.Children.Add(ScoreBox("Ph\u00f2ng", GetScore("rooms")));
        scores.Children.Add(ScoreBox("T\u00e0i ch\u00ednh", GetScore("finance")));
        scores.Children.Add(ScoreBox("D\u1ecbch v\u1ee5", GetScore("services")));
        StepContentHost.Children.Add(scores);
        var blockings = GetBlockingIssues();
        var warnings = GetWarnings();
        var status = blockings.Count > 0 ? $"\u26d4 Ch\u01b0a s\u1eb5n s\u00e0ng \u2014 {blockings.Count} v\u1ea5n \u0111\u1ec1 ch\u1eb7n" : warnings.Count > 0 ? $"\u26a0 C\u1ea7n b\u1ed5 sung \u2014 {warnings.Count} c\u1ea3nh b\u00e1o" : "\u2713 S\u1eb5n s\u00e0ng t\u1ea1o \u0111o\u00e0n";
        var color = blockings.Count > 0 ? "#DC2626" : warnings.Count > 0 ? "#D97706" : "#16A34A";
        StepContentHost.Children.Add(new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}1A")), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}66")), BorderThickness = new Thickness(1), Child = new TextBlock { Text = status, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)), TextAlignment = TextAlignment.Center } });
    }

    private void BuildStepG()
    {
        StepContentHost.Children.Add(STitle("Kiểm tra"));
        StepContentHost.Children.Add(SText("Kiểm tra toàn bộ hồ sơ đoàn trước khi xác nhận."));

        // Validation checklist
        StepContentHost.Children.Add(SubTitle("Danh sách kiểm tra"));
        var checks = GetChecklist();
        foreach (var (label, ok, detail) in checks)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 0) };
            row.Children.Add(new TextBlock { Text = ok ? "✓ " : "✗ ", FontSize = 13, Foreground = ok ? C("#16A34A") : C("#DC2626"), FontWeight = FontWeights.Bold });
            row.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center });

            var card = new Border
            {
                CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2),
                Background = ok ? Cb("#F0FDF4") : Cb("#FEF2F2"),
                BorderBrush = ok ? Cb("#BBF7D0") : Cb("#FECACA"), BorderThickness = new Thickness(1),
                Child = row
            };
            StepContentHost.Children.Add(card);
        }

        // Readiness score
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Điểm sẵn sàng"));
        var scoreWp = new WrapPanel { Margin = new Thickness(0, 0, 0, 12) };
        scoreWp.Children.Add(ScoreBox("Hồ sơ", GetScore("profile")));
        scoreWp.Children.Add(ScoreBox("Phòng", GetScore("rooms")));
        scoreWp.Children.Add(ScoreBox("Tài chính", GetScore("finance")));
        scoreWp.Children.Add(ScoreBox("Dịch vụ", GetScore("services")));
        scoreWp.Children.Add(ScoreBox("Vận hành", GetScore("operations")));
        scoreWp.Children.Add(ScoreBox("Tài liệu", GetScore("documents")));
        StepContentHost.Children.Add(scoreWp);

        // Overall readiness
        var blockings = GetBlockingIssues();
        var warnings = GetWarnings();
        string statusText;
        string statusColor;
        if (blockings.Count > 0) { statusText = $"⛔ Chưa sẵn sàng — {blockings.Count} vấn đề chặn"; statusColor = "#DC2626"; }
        else if (warnings.Count > 0) { statusText = $"⚠ Cần bổ sung — {warnings.Count} cảnh báo"; statusColor = "#D97706"; }
        else { statusText = "✓ Sẵn sàng tạo đoàn"; statusColor = "#16A34A"; }
        StepContentHost.Children.Add(new Border { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{statusColor}1A")), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{statusColor}66")), BorderThickness = new Thickness(1), Child = new TextBlock { Text = statusText, FontSize = 14, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(statusColor)), TextAlignment = TextAlignment.Center } });
    }

    private int GetScore(string cat) => cat switch
    {
        "profile" => string.IsNullOrWhiteSpace(_groupName) ? 20 : string.IsNullOrEmpty(_groupType) ? 50 : _guestCount > 0 ? 90 : 70,
        "rooms" => _roomRequirements.Count == 0 ? 10 : _roomRequirements.Any(r => r.RoomCount == 0) ? 40 : 85,
        "finance" => string.IsNullOrEmpty(_currency) ? 10 : string.IsNullOrEmpty(_payer) ? 40 : 80,
        "services" => _services.Count > 0 ? 75 : 50,
        "operations" => _arrivalDate.HasValue && _departureDate.HasValue ? 80 : 30,
        "documents" => _documents.Count > 0 ? 70 : 30,
        _ => 50
    };

    private static Border ScoreBox(string label, int score)
    {
        var color = score >= 80 ? "#16A34A" : score >= 50 ? "#D97706" : "#DC2626";
        return new Border
        {
            Width = 130, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}1A")),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 8, 8),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}66")), BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B") },
                    new TextBlock { Text = $"{score}%", FontSize = 20, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) }
                }
            }
        };
    }

    private List<(string, bool, string)> GetChecklist()
    {
        return new()
        {
            ("Thông tin đoàn hợp lệ", !string.IsNullOrWhiteSpace(_groupName) && !string.IsNullOrEmpty(_groupType) && _guestCount > 0, ""),
            ("Contact chính hợp lệ", !string.IsNullOrWhiteSpace(_primaryContactName) && !string.IsNullOrWhiteSpace(_primaryContactPhone), ""),
            ("Công ty/Đại lý hợp lệ", _orgType != "Company" || !string.IsNullOrWhiteSpace(_companyName), ""),
            ("Ngày lưu trú hợp lệ", _arrivalDate.HasValue && _departureDate.HasValue && _departureDate > _arrivalDate, ""),
            ("Quy mô hợp lệ", _guestCount > 0 && _roomCount > 0, ""),
            ("Room requirement hợp lệ", _roomRequirements.Count > 0 && _roomRequirements.All(r => r.RoomCount > 0 && !string.IsNullOrEmpty(r.RoomType)), ""),
            ("Rate hợp lệ", !string.IsNullOrEmpty(_currency) && !string.IsNullOrEmpty(_ratePlan), ""),
            ("Payment hợp lệ", !string.IsNullOrEmpty(_payer), ""),
            ("Deposit hợp lệ", !_depositRequired || _depositAmount > 0 || _depositPercent > 0, ""),
            ("Commission hợp lệ", !_agentCommissionEnabled || (_agentCommissionPct >= 0 && _agentCommissionPct <= 100), ""),
            ("Dịch vụ hợp lệ", true, ""),
            ("Tài liệu tối thiểu", true, ""),
        };
    }

    private List<string> GetBlockingIssues()
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(_groupName)) issues.Add("Thiếu tên đoàn");
        if (string.IsNullOrEmpty(_groupType)) issues.Add("Thiếu loại đoàn");
        if (_guestCount <= 0) issues.Add("Số khách <= 0");
        if (_roomCount <= 0) issues.Add("Số phòng <= 0");
        if (_groupName.Length > 0 && _groupName.Length < 3) issues.Add("Tên đoàn quá ngắn");
        if (_arrivalDate.HasValue && _departureDate.HasValue && _departureDate <= _arrivalDate) issues.Add("Ngày đi <= ngày đến");
        if (_roomRequirements.Count == 0 || _roomRequirements.All(r => r.RoomCount <= 0)) issues.Add("Thiếu room requirement");
        if (string.IsNullOrWhiteSpace(_primaryContactName) || string.IsNullOrWhiteSpace(_primaryContactPhone)) issues.Add("Thiếu contact chính");
        if (_orgType == "Company" && string.IsNullOrWhiteSpace(_companyName)) issues.Add("Chọn Công ty nhưng thiếu tên");
        if (_orgType == "Agent" && string.IsNullOrWhiteSpace(_agentName)) issues.Add("Chọn Đại lý nhưng thiếu tên");
        if (_commissionPct < 0 || _commissionPct > 100) issues.Add("Commission ngoài 0-100%");
        if (_depositRequired && _depositDeadline.HasValue && _arrivalDate.HasValue && _depositDeadline > _arrivalDate) issues.Add("Hạn đặt cọc sau ngày đến");
        if (string.IsNullOrEmpty(_payer)) issues.Add("Chưa chọn Payer");
        if (string.IsNullOrEmpty(_currency)) issues.Add("Chưa chọn Currency");
        if (_creditAllowed && _creditLimit <= 0) issues.Add("Credit limit <= 0 khi cho phép công nợ");
        return issues;
    }

    private List<string> GetWarnings()
    {
        var warnings = new List<string>();
        if (_guestCount > 50) warnings.Add("Đoàn quy mô lớn (>50 khách)");
        if (_vipLevel is "VIP 1" or "VIP 2") warnings.Add("Đoàn VIP — cần chú ý đặc biệt");
        if (_groupType == "Government") warnings.Add("Đoàn Government — yêu cầu protocol");
        if (_roomCount > 30) warnings.Add($"Số phòng lớn ({_roomCount})");
        if (string.IsNullOrWhiteSpace(_groupLeader)) warnings.Add("Chưa có trưởng đoàn");
        if (_orgType == "Company" && string.IsNullOrWhiteSpace(_taxCode)) warnings.Add("Công ty chưa có MST");
        if (_orgType == "Company" && _creditStatus == "Kém") warnings.Add("Credit status công ty yếu");
        if (string.IsNullOrWhiteSpace(_primaryContactEmail)) warnings.Add("Contact không có email");
        if (_earlyArrival) warnings.Add("Early arrival — cần chuẩn bị phòng sớm");
        if (_lateDeparture) warnings.Add("Late departure — cần lưu ý housekeeping");
        if (_pickupRequired && string.IsNullOrWhiteSpace(_flightNumber)) warnings.Add("Cần đón nhưng chưa có số hiệu");
        if (_guestCount > 20 && !_depositRequired) warnings.Add("Đoàn lớn nhưng không yêu cầu đặt cọc");
        if (_depositRequired && _depositAmount > 0 && _roomRequirements.Count > 0 && _depositAmount < 1000000) warnings.Add("Tiền cọc thấp so với quy mô đoàn");
        if (_agentCommissionEnabled && _agentCommissionPct > 20) warnings.Add("Commission cao (>20%)");
        if (_currency != "VND") warnings.Add("Currency khác VND");
        return warnings;
    }

    private bool HasBlockingIssues() => GetBlockingIssues().Count > 0;

    // ═══ STEP H — CONFIRM ═══
    private void BuildStepHCompact()
    {
        StepContentHost.Children.Add(STitle("X\u00e1c nh\u1eadn"));
        StepContentHost.Children.Add(SText("X\u00e1c nh\u1eadn to\u00e0n b\u1ed9 th\u00f4ng tin tr\u01b0\u1edbc khi t\u1ea1o Group Profile th\u1eadt."));
        StepContentHost.Children.Add(new Border
        {
            Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 0, 0, 12), BorderBrush = Cb("#FCD34D"), BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = "\u26a0 Một số trường dịch vụ/tài liệu chưa có contract lưu trữ. Chúng sẽ không được ghi giả vào database.", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#92400E"), TextWrapping = TextWrapping.Wrap }
        });

        StepContentHost.Children.Add(SubTitle("T\u00f3m t\u1eaft h\u1ed3 s\u01a1 \u0111o\u00e0n"));
        var summary = Grid3();
        summary.Children.Add(PreviewRow("T\u00ean \u0111o\u00e0n", _groupName));
        summary.Children.Add(PreviewRow("Lo\u1ea1i \u0111o\u00e0n", _groupType ?? "\u2014"));
        summary.Children.Add(PreviewRow("C\u00f4ng ty", string.IsNullOrEmpty(_companyName) ? "\u2014" : _companyName));
        summary.Children.Add(PreviewRow("\u0110\u1ea1i l\u00fd", string.IsNullOrEmpty(_agentName) ? "\u2014" : _agentName));
        summary.Children.Add(PreviewRow("Contact ch\u00ednh", $"{_primaryContactName} \u2014 {_primaryContactPhone}"));
        summary.Children.Add(PreviewRow("L\u01b0u tr\u00fa", _arrivalDate.HasValue && _departureDate.HasValue ? $"{_arrivalDate:dd/MM/yyyy} \u2192 {_departureDate:dd/MM/yyyy} ({(int)(_departureDate.Value - _arrivalDate.Value).TotalDays} \u0111\u00eam)" : "\u2014"));
        summary.Children.Add(PreviewRow("S\u1ed1 kh\u00e1ch", _guestCount.ToString()));
        summary.Children.Add(PreviewRow("S\u1ed1 ph\u00f2ng", _roomCount.ToString()));
        summary.Children.Add(PreviewRow("Currency", _currency));
        summary.Children.Add(PreviewRow("Payer", _payer ?? "\u2014"));
        summary.Children.Add(PreviewRow("\u0110\u1eb7t c\u1ecdc", _depositRequired ? $"{_depositAmount:N0} {_currency}" : "Kh\u00f4ng"));
        summary.Children.Add(PreviewRow("Hoa h\u1ed3ng", _agentCommissionEnabled ? $"{_agentCommissionPct}%" : "Kh\u00f4ng"));
        StepContentHost.Children.Add(summary);

        var blockings = GetBlockingIssues();
        if (blockings.Count > 0)
        {
            StepContentHost.Children.Add(Sep());
            StepContentHost.Children.Add(SubTitle("V\u1ea5n \u0111\u1ec1 ch\u1eb7n (Blocking)"));
            var blockingGrid = Grid3();
            foreach (var issue in blockings)
                blockingGrid.Children.Add(new Border { Background = Cb("#FEF2F2"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Child = new TextBlock { Text = $"\u26d4 {issue}", FontSize = 12, Foreground = C("#DC2626"), TextWrapping = TextWrapping.Wrap } });
            StepContentHost.Children.Add(blockingGrid);
        }

        var warnings = GetWarnings();
        if (warnings.Count > 0)
        {
            StepContentHost.Children.Add(Sep());
            StepContentHost.Children.Add(SubTitle("C\u1ea3nh b\u00e1o (Warning)"));
            var warningGrid = Grid3();
            foreach (var warning in warnings)
                warningGrid.Children.Add(new Border { Background = Cb("#FFFBEB"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Child = new TextBlock { Text = $"\u26a0 {warning}", FontSize = 12, Foreground = C("#D97706"), TextWrapping = TextWrapping.Wrap } });
            StepContentHost.Children.Add(warningGrid);
        }

        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("X\u00e1c nh\u1eadn"));
        var confirmations = Grid3();
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i \u0111\u00e3 ki\u1ec3m tra t\u00ean v\u00e0 lo\u1ea1i \u0111o\u00e0n", _confirmName, v => { _confirmName = v; UpdateFooter(); }));
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i \u0111\u00e3 ki\u1ec3m tra c\u00f4ng ty/\u0111\u1ea1i l\u00fd", _confirmOrg, v => { _confirmOrg = v; UpdateFooter(); }));
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i \u0111\u00e3 ki\u1ec3m tra contact ch\u00ednh", _confirmContact, v => { _confirmContact = v; UpdateFooter(); }));
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i \u0111\u00e3 ki\u1ec3m tra th\u1eddi gian l\u01b0u tr\u00fa", _confirmStay, v => { _confirmStay = v; UpdateFooter(); }));
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i \u0111\u00e3 ki\u1ec3m tra nhu c\u1ea7u ph\u00f2ng", _confirmRooms, v => { _confirmRooms = v; UpdateFooter(); }));
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i \u0111\u00e3 ki\u1ec3m tra gi\u00e1 v\u00e0 thanh to\u00e1n", _confirmFinance, v => { _confirmFinance = v; UpdateFooter(); }));
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i \u0111\u00e3 ki\u1ec3m tra d\u1ecbch v\u1ee5", _confirmServices, v => { _confirmServices = v; UpdateFooter(); }));
        confirmations.Children.Add(MakeConfirmCheck("T\u00f4i x\u00e1c nh\u1eadn th\u00f4ng tin \u0111\u00fang", _confirmAll, v => { _confirmAll = v; UpdateFooter(); }));
        StepContentHost.Children.Add(confirmations);
    }

    private void BuildStepH()
    {
        StepContentHost.Children.Add(STitle("Xác nhận"));
        StepContentHost.Children.Add(SText("Xác nhận toàn bộ thông tin trước khi tạo Group Profile thật."));

        // Banner
        StepContentHost.Children.Add(new Border { Background = Cb("#FEF3C7"), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 10, 14, 10), Margin = new Thickness(0, 0, 0, 12), BorderBrush = Cb("#FCD34D"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = "⚠ Kiểm tra lại các phần dịch vụ/tài liệu chưa có contract lưu trữ trước khi vận hành.", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#92400E"), TextWrapping = TextWrapping.Wrap } });

        // Summary preview
        StepContentHost.Children.Add(SubTitle("Tóm tắt hồ sơ đoàn"));
        var pp = new StackPanel();
        pp.Children.Add(PreviewRow("Tên đoàn", _groupName));
        pp.Children.Add(PreviewRow("Loại đoàn", _groupType ?? "—"));
        pp.Children.Add(PreviewRow("Công ty", string.IsNullOrEmpty(_companyName) ? "—" : _companyName));
        pp.Children.Add(PreviewRow("Đại lý", string.IsNullOrEmpty(_agentName) ? "—" : _agentName));
        pp.Children.Add(PreviewRow("Contact chính", $"{_primaryContactName} — {_primaryContactPhone}"));
        pp.Children.Add(PreviewRow("Lưu trú", _arrivalDate.HasValue && _departureDate.HasValue ? $"{_arrivalDate:dd/MM/yyyy} → {_departureDate:dd/MM/yyyy} ({(int)(_departureDate.Value - _arrivalDate.Value).TotalDays} đêm)" : "—"));
        pp.Children.Add(PreviewRow("Số khách", _guestCount.ToString()));
        pp.Children.Add(PreviewRow("Số phòng", _roomCount.ToString()));
        pp.Children.Add(PreviewRow("Currency", _currency));
        pp.Children.Add(PreviewRow("Payer", _payer ?? "—"));
        pp.Children.Add(PreviewRow("Đặt cọc", _depositRequired ? $"{_depositAmount:N0} {_currency}" : "Không"));
        pp.Children.Add(PreviewRow("Hoa hồng", _agentCommissionEnabled ? $"{_agentCommissionPct}%" : "Không"));
        StepContentHost.Children.Add(pp);

        // Blocking issues
        var blockings = GetBlockingIssues();
        if (blockings.Count > 0)
        {
            StepContentHost.Children.Add(Sep());
            StepContentHost.Children.Add(SubTitle("Vấn đề chặn (Blocking)"));
            foreach (var b in blockings)
                StepContentHost.Children.Add(new Border { Background = Cb("#FEF2F2"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Child = new TextBlock { Text = $"⛔ {b}", FontSize = 12, Foreground = C("#DC2626") } });
        }

        // Warnings
        var warnings = GetWarnings();
        if (warnings.Count > 0)
        {
            StepContentHost.Children.Add(Sep());
            StepContentHost.Children.Add(SubTitle("Cảnh báo (Warning)"));
            foreach (var w in warnings)
                StepContentHost.Children.Add(new Border { Background = Cb("#FFFBEB"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 2, 0, 2), Child = new TextBlock { Text = $"⚠ {w}", FontSize = 12, Foreground = C("#D97706") } });
        }

        // Confirm checkboxes
        StepContentHost.Children.Add(Sep());
        StepContentHost.Children.Add(SubTitle("Xác nhận"));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi đã kiểm tra tên và loại đoàn", _confirmName, v => { _confirmName = v; UpdateFooter(); }));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi đã kiểm tra công ty/đại lý", _confirmOrg, v => { _confirmOrg = v; UpdateFooter(); }));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi đã kiểm tra contact chính", _confirmContact, v => { _confirmContact = v; UpdateFooter(); }));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi đã kiểm tra thời gian lưu trú", _confirmStay, v => { _confirmStay = v; UpdateFooter(); }));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi đã kiểm tra nhu cầu phòng", _confirmRooms, v => { _confirmRooms = v; UpdateFooter(); }));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi đã kiểm tra giá và thanh toán", _confirmFinance, v => { _confirmFinance = v; UpdateFooter(); }));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi đã kiểm tra dịch vụ", _confirmServices, v => { _confirmServices = v; UpdateFooter(); }));
        StepContentHost.Children.Add(MakeConfirmCheck("Tôi xác nhận thông tin đúng", _confirmAll, v => { _confirmAll = v; UpdateFooter(); }));
    }

    private bool CanCreateGroup()
    {
        return !HasBlockingIssues() && _confirmName && _confirmOrg && _confirmContact && _confirmStay
               && _confirmRooms && _confirmFinance && _confirmServices && _confirmAll;
    }

    private async void OnCreate(object s, RoutedEventArgs e)
    {
        if (!CanCreateGroup()) return;
        BtnCreate.IsEnabled = false;
        try
        {
            var created = await PersistGroupAsync();
            _isSubmitted = true;
            _createdGroupId = created.GroupReservationId;
            _createdGroupCode = created.GroupCode;
            RenderCreateSuccess(created);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo đoàn.\n{ex.Message}", "Tạo đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (!_isSubmitted) BtnCreate.IsEnabled = true;
        }
    }

    private async Task<CreatedGroupDto> PersistGroupAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { return await PersistGroupCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task<CreatedGroupDto> PersistGroupCoreAsync()
    {
        if (_isClosed) throw new InvalidOperationException("GROUP_WINDOW_CLOSED");
        if (_groupCommandService is null)
            throw new InvalidOperationException("GROUP_COMMAND_SERVICE_NOT_CONFIGURED");
        var context = _contextAccessor?.Current
            ?? throw new InvalidOperationException("FRONT_OFFICE_CONTEXT_REQUIRED");
        if (!_arrivalDate.HasValue || !_departureDate.HasValue)
            throw new ArgumentException("GROUP_DATES_REQUIRED");

        var propertyId = context.Property.PropertyId.Value;
        var totalRooms = _roomRequirements.Count > 0 ? _roomRequirements.Sum(r => r.RoomCount) : _roomCount;
        var contactName = string.IsNullOrWhiteSpace(_primaryContactName) ? _groupLeader : _primaryContactName;
        var companyName = string.IsNullOrWhiteSpace(_companyName) ? null : _companyName;
        var agencyName = string.IsNullOrWhiteSpace(_agentName) ? null : _agentName;
        var isVip = !string.IsNullOrWhiteSpace(_vipLevel) && _vipLevel != "Không";
        var notes = string.Join("\n", new[] { _internalNote, _noteInternal }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var specialRequests = string.Join("\n", new[]
        {
            _primaryContactNote,
            _housekeepingNote,
            _vipNote,
            _securityNote,
            _noteReception,
            _noteHousekeeping,
            _noteCashier,
            _noteFnb,
            _noteSecurity,
            _noteSales,
            _noteBanquet
        }.Where(x => !string.IsNullOrWhiteSpace(x)));
        var serviceDrafts = BuildServiceDrafts();
        var roomBlockDrafts = await BuildRoomBlockDraftsAsync(propertyId);

        return await _groupCommandService.CreateAsync(new CreateGroupRequest(
            propertyId,
            string.IsNullOrWhiteSpace(_externalRef) ? null : _externalRef,
            _groupName,
            DateOnly.FromDateTime(_arrivalDate.Value),
            DateOnly.FromDateTime(_departureDate.Value),
            Math.Max(totalRooms, 0),
            Math.Max(_guestCount, 0),
            0,
            companyName,
            agencyName,
            string.IsNullOrWhiteSpace(contactName) ? null : contactName,
            string.IsNullOrWhiteSpace(_primaryContactPhone) ? null : _primaryContactPhone,
            string.IsNullOrWhiteSpace(_primaryContactEmail) ? null : _primaryContactEmail,
            _source,
            null,
            _segment,
            isVip,
            ChargeRouting.CompanyPaysAll,
            string.IsNullOrWhiteSpace(specialRequests) ? null : specialRequests,
            string.IsNullOrWhiteSpace(notes) ? null : notes,
            serviceDrafts,
            roomBlockDrafts),
            CancellationToken.None);
    }

    private async Task<IReadOnlyCollection<CreateGroupRoomBlockDraft>> BuildRoomBlockDraftsAsync(Guid propertyId)
    {
        if (_roomRequirements.Count == 0) return Array.Empty<CreateGroupRoomBlockDraft>();
        if (_roomMasterDataRepository is null) throw new InvalidOperationException("HOTEL_ROOM_MASTER_DATA_NOT_CONFIGURED");
        if (!_arrivalDate.HasValue || !_departureDate.HasValue) throw new ArgumentException("GROUP_DATES_REQUIRED");

        var arrival = DateOnly.FromDateTime(_arrivalDate.Value);
        var departure = DateOnly.FromDateTime(_departureDate.Value);
        decimal? negotiatedRate = null;
        if (!string.IsNullOrWhiteSpace(_groupRate) && decimal.TryParse(_groupRate, NumberStyles.Number, CultureInfo.CurrentCulture, out var parsedRate) && parsedRate >= 0)
            negotiatedRate = parsedRate;

        var roomsByType = new Dictionary<Guid, int>();
        foreach (var requirement in _roomRequirements)
        {
            if (requirement.RoomCount <= 0) throw new ArgumentException("GROUP_ROOM_BLOCK_ROOMS_REQUIRED");
            var code = requirement.RoomType?.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("GROUP_ROOM_TYPE_REQUIRED");
            var roomType = await _roomMasterDataRepository.GetRoomTypeByCodeAsync(propertyId, code, CancellationToken.None)
                ?? throw new KeyNotFoundException($"HOTEL_ROOM_TYPE_NOT_FOUND:{code}");
            if (!roomType.IsActive) throw new InvalidOperationException($"HOTEL_ROOM_TYPE_INACTIVE:{code}");
            roomsByType[roomType.HotelRoomTypeId] = roomsByType.GetValueOrDefault(roomType.HotelRoomTypeId) + requirement.RoomCount;
        }
        DateOnly? cutoff = _cutoffDate.HasValue ? DateOnly.FromDateTime(_cutoffDate.Value) : null;
        return roomsByType.Select(x => new CreateGroupRoomBlockDraft(x.Key, x.Value, arrival, departure, cutoff, negotiatedRate)).ToList();
    }

    private async Task LoadRoomTypesAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { await LoadRoomTypesCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task LoadRoomTypesCoreAsync()
    {
        if (_isClosed) return;
        if (_roomMasterDataRepository is null || _contextAccessor?.Current is not { } context) return;
        try
        {
            var result = await _roomMasterDataRepository.SearchRoomTypesAsync(context.Property.PropertyId.Value, null, true, new ASMILE.BO.Application.FrontOffice.Foundation.Cqrs.PageRequest(1, 200), "display", CancellationToken.None);
            _roomTypes.Clear();
            _roomTypes.AddRange(result.Items);
            if (_currentStep == 3)
            {
                StepContentHost.Children.Clear();
                BuildStepD();
            }
        }
        catch
        {
            // Keep the screen fail-closed. PersistGroupAsync still validates against the real repository.
        }
    }

    private async Task CheckRoomAvailabilityAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { await CheckRoomAvailabilityCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task CheckRoomAvailabilityCoreAsync()
    {
        if (_isClosed) return;
        _roomAvailabilityChecked = false;
        _roomAvailabilityAvailable = false;
        _roomAvailabilitySummary = "Đang kiểm tra";
        UpdateInspector(_currentStep);
        try
        {
            if (_availabilityEngine is null || _contextAccessor?.Current is not { } context)
                throw new InvalidOperationException("HOTEL_AVAILABILITY_SERVICE_NOT_CONFIGURED");
            if (!_arrivalDate.HasValue || !_departureDate.HasValue)
                throw new ArgumentException("GROUP_DATES_REQUIRED");
            if (_roomRequirements.Count == 0)
                throw new ArgumentException("GROUP_ROOM_BLOCK_REQUIRED");

            var propertyId = context.Property.PropertyId.Value;
            var arrival = DateOnly.FromDateTime(_arrivalDate.Value);
            var departure = DateOnly.FromDateTime(_departureDate.Value);
            var lines = new List<string>();
            var allAvailable = true;
            foreach (var requirement in _roomRequirements)
            {
                var code = requirement.RoomType?.Split(" - ", 2, StringSplitOptions.TrimEntries)[0];
                if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("GROUP_ROOM_TYPE_REQUIRED");
                if (requirement.RoomCount <= 0) throw new ArgumentException("GROUP_ROOM_BLOCK_ROOMS_REQUIRED");
                var type = _roomTypes.FirstOrDefault(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase))
                    ?? (_roomMasterDataRepository is not null
                        ? await _roomMasterDataRepository.GetRoomTypeByCodeAsync(propertyId, code, CancellationToken.None)
                        : null)
                    ?? throw new KeyNotFoundException($"HOTEL_ROOM_TYPE_NOT_FOUND:{code}");
                var result = await _availabilityEngine.CheckAvailabilityAsync(
                    new AvailabilityRequest(
                        propertyId,
                        context.BusinessDate.BusinessDate,
                        arrival,
                        departure,
                        type.HotelRoomTypeId,
                        Math.Max(requirement.RoomCount * Math.Max(requirement.GuestsPerRoom, 1), _guestCount),
                        0,
                        requirement.RoomCount),
                    CancellationToken.None);
                allAvailable &= result.IsAvailable;
                var status = result.IsAvailable ? "Đủ" : "Thiếu";
                var reason = string.IsNullOrWhiteSpace(result.UnavailabilityReason) ? string.Empty : $" ({result.UnavailabilityReason})";
                lines.Add($"{type.Code} - {type.Name}: cần {requirement.RoomCount}, còn {result.AvailableRoomCount} — {status}{reason}.");
            }
            _roomAvailabilityChecked = true;
            _roomAvailabilityAvailable = allAvailable;
            _roomAvailabilitySummary = allAvailable ? "Đủ phòng theo tồn thực" : "Thiếu phòng theo tồn thực";
            UpdateInspector(_currentStep);
            MessageBox.Show(string.Join(Environment.NewLine, lines), "Khả dụng phòng", MessageBoxButton.OK, allAvailable ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _roomAvailabilitySummary = "Chưa kiểm tra được";
            UpdateInspector(_currentStep);
            MessageBox.Show($"Không thể đọc dữ liệu hạng/phòng thật.\n{ex.Message}", "Khả dụng phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        await _dataOperationGate.WaitAsync();
        try { _serviceScope?.Dispose(); }
        finally { _dataOperationGate.Release(); _dataOperationGate.Dispose(); }
    }

    private IReadOnlyCollection<CreateGroupServiceDraft> BuildServiceDrafts()
    {
        if (_services.Count == 0) return Array.Empty<CreateGroupServiceDraft>();
        var fallbackDate = _arrivalDate.HasValue ? DateOnly.FromDateTime(_arrivalDate.Value) : DateOnly.FromDateTime(DateTime.Today);
        var fallbackTime = TimeOnly.MinValue;
        var chargeTo = _individualPay ? "Individual" : _masterFolio ? "MasterFolio" : "Company";

        return _services
            .Where(service => !string.IsNullOrWhiteSpace(service.Description) || !string.IsNullOrWhiteSpace(service.Type))
            .Select(service =>
            {
                var date = ParseServiceDate(service.Date, fallbackDate);
                var time = ParseServiceTime(service.Time, fallbackTime);
                var name = string.IsNullOrWhiteSpace(service.Description) ? service.Type! : service.Description.Trim();
                var category = string.IsNullOrWhiteSpace(service.Type) ? "Other" : service.Type.Trim();
                var remark = string.Join(" · ", new[] { service.Priority, service.Status }.Where(value => !string.IsNullOrWhiteSpace(value)));
                return new CreateGroupServiceDraft(
                    name,
                    category,
                    date,
                    time,
                    0,
                    Math.Max(1, service.Quantity),
                    0m,
                    0m,
                    Math.Clamp(_vatPct / 100m, 0m, 1m),
                    Math.Clamp(_serviceChargePct / 100m, 0m, 1m),
                    chargeTo,
                    Department: service.Department,
                    Remark: string.IsNullOrWhiteSpace(remark) ? null : remark);
            })
            .ToList();
    }

    private static DateOnly ParseServiceDate(string value, DateOnly fallback)
    {
        if (DateTime.TryParseExact(value, new[] { "dd/MM/yyyy", "d/M/yyyy", "dd/MM", "d/M" }, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed))
        {
            var year = parsed.Year == 1 ? fallback.Year : parsed.Year;
            return new DateOnly(year, parsed.Month, parsed.Day);
        }

        return fallback;
    }

    private static TimeOnly ParseServiceTime(string value, TimeOnly fallback)
        => TimeOnly.TryParseExact(value, new[] { "HH:mm", "H:mm", "HHmm" }, CultureInfo.CurrentCulture, DateTimeStyles.None, out var parsed) ? parsed : fallback;

    private void RenderCreateSuccess(CreatedGroupDto created)
    {

        // Show success in content area
        StepContentHost.Children.Clear();
        StepContentHost.Children.Add(new Border { Width = 64, Height = 64, CornerRadius = new CornerRadius(32), Background = Cb("#DCFCE7"), HorizontalAlignment = HorizontalAlignment.Center, Child = new TextBlock { Text = "✓", FontSize = 32, FontWeight = FontWeights.Bold, Foreground = C("#16A34A"), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } });
        StepContentHost.Children.Add(new TextBlock { Text = "Đoàn đã được tạo", FontSize = 22, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 12, 0, 4) });
        StepContentHost.Children.Add(new TextBlock { Text = "Thông tin đoàn, room block theo hạng phòng và các service draft đã được lưu vào hệ thống thật. Đặt cọc, tài liệu, tồn phòng theo booking và gán phòng vật lý vẫn là bước vận hành tiếp theo.", FontSize = 13, Foreground = C("#64748B"), HorizontalAlignment = HorizontalAlignment.Center, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 16) });

        // Summary card
        var resultCard = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(18, 14, 18, 14), MaxWidth = 500, HorizontalAlignment = HorizontalAlignment.Center };
        var rsp = new StackPanel();
        rsp.Children.Add(ResultRow("Mã đoàn", created.GroupCode));
        rsp.Children.Add(ResultRow("Tên đoàn", _groupName));
        rsp.Children.Add(ResultRow("Loại đoàn", _groupType ?? "—"));
        rsp.Children.Add(ResultRow("Company/Agent", string.IsNullOrEmpty(_companyName) ? (_agentName ?? "—") : _companyName));
        rsp.Children.Add(ResultRow("Contact", $"{_primaryContactName} — {_primaryContactPhone}"));
        rsp.Children.Add(ResultRow("Ngày đến/đi", _arrivalDate.HasValue && _departureDate.HasValue ? $"{_arrivalDate:dd/MM/yyyy} → {_departureDate:dd/MM/yyyy}" : "—"));
        rsp.Children.Add(ResultRow("Số đêm / Phòng / Khách", $"{(int)(_departureDate.GetValueOrDefault() - _arrivalDate.GetValueOrDefault()).TotalDays} đêm · {_roomCount} phòng · {_guestCount} khách"));
        rsp.Children.Add(ResultRow("Currency", _currency));
        rsp.Children.Add(ResultRow("Đặt cọc", _depositRequired ? $"{_depositAmount:N0} {_currency}" : "Không"));
        rsp.Children.Add(ResultRow("Trạng thái", created.Status.ToString()));
        rsp.Children.Add(ResultRow("Người tạo", "Theo phiên Front Office hiện tại"));
        rsp.Children.Add(ResultRow("Thời gian", DateTime.Now.ToString("dd/MM/yyyy HH:mm")));
        resultCard.Child = rsp;
        StepContentHost.Children.Add(resultCard);

        // Actions
        StepContentHost.Children.Add(new TextBlock { Text = "", Margin = new Thickness(0, 12, 0, 0) });
        var actWp = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
        actWp.Children.Add(SuccessActionBtn("Xem hồ sơ đoàn"));
        actWp.Children.Add(SuccessActionBtn("Đặt phòng đoàn"));
        actWp.Children.Add(SuccessActionBtn("Mở Rooming List"));
        actWp.Children.Add(SuccessActionBtn("Thêm dịch vụ đoàn"));
        actWp.Children.Add(SuccessActionBtn("Quay lại danh sách đoàn"));
        actWp.Children.Add(SuccessActionBtn("Tạo đoàn khác"));
        StepContentHost.Children.Add(actWp);

        BtnBack.IsEnabled = false;
        BtnCreate.Visibility = Visibility.Collapsed;
        BtnNext.Visibility = Visibility.Collapsed;
        BtnSaveDraft.IsEnabled = false;
    }

    private static FrameworkElement ResultRow(string label, string value)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0),
            Children =
            {
                new TextBlock { Text = label + ": ", FontSize = 12, Foreground = C("#94A3B8"), Width = 160 },
                new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }
            }
        };
    }

    private Button SuccessActionBtn(string text)
    {
        var btn = new Button { Content = text, Style = (Style)FindResource("ActionBtn"), Height = 34, FontSize = 12, Margin = new Thickness(0, 0, 8, 6) };
        btn.Click += (_, _) => OpenSuccessAction(text);
        return btn;
    }

    private void OpenSuccessAction(string text)
    {
        Window? window = text switch
        {
            "Xem hồ sơ đoàn" => new GroupReservationWindow(),
            "Đặt phòng đoàn" => new GroupRoomAssignmentWindow(_createdGroupId == Guid.Empty ? null : _createdGroupId),
            "Mở Rooming List" => new GroupRoomingListWindow(),
            "Thêm dịch vụ đoàn" => new GroupServiceWindow(),
            "Quay lại danh sách đoàn" => new GroupReservationWindow(),
            _ => null
        };

        if (text == "Tạo đoàn khác")
        {
            var next = new CreateGroupWindow
            {
                Owner = Owner ?? System.Windows.Application.Current.MainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            next.Show();
            Close();
            return;
        }

        if (window is null) return;
        window.Owner = Owner ?? System.Windows.Application.Current.MainWindow;
        window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        window.Show();
        Close();
    }

    // ═══ INSPECTOR ═══
    private void UpdateInspector(int step)
    {
        InspectorContent.Children.Clear();
        switch (step)
        {
            case 0: BuildInspectorA(); break;
            case 1: BuildInspectorB(); break;
            case 2: BuildInspectorC(); break;
            case 3: BuildInspectorD(); break;
            case 4: BuildInspectorE(); break;
            case 5: BuildInspectorF(); break;
            case 6: BuildInspectorG(); break;
            case 7: BuildInspectorH(); break;
        }
    }

    private void BuildInspectorA()
    {
        InspectorContent.Children.Add(InspectorSection("Nhận diện"));
        InspectorContent.Children.Add(InspectorRow("Tên đoàn", string.IsNullOrWhiteSpace(_groupName) ? "Chưa nhập" : _groupName));
        InspectorContent.Children.Add(InspectorRow("Loại", _groupType ?? "Chưa chọn"));
        InspectorContent.Children.Add(InspectorRow("Quy mô", $"{_guestCount} khách / {_roomCount} phòng"));
        InspectorContent.Children.Add(InspectorRow("Segment", _segment ?? "—"));
        InspectorContent.Children.Add(InspectorRow("Priority", _priority ?? "—"));

        InspectorContent.Children.Add(Sep());
        InspectorContent.Children.Add(InspectorSection("Trạng thái nhập liệu"));
        var hasName = !string.IsNullOrWhiteSpace(_groupName);
        var hasType = !string.IsNullOrEmpty(_groupType);
        var hasCount = _guestCount > 0 && _roomCount > 0;
        InspectorContent.Children.Add(InspectorStatus("Tên đoàn", hasName));
        InspectorContent.Children.Add(InspectorStatus("Loại đoàn", hasType));
        InspectorContent.Children.Add(InspectorStatus("Quy mô", hasCount));

        var blockings = GetBlockingIssues().Where(b => b.Contains("tên") || b.Contains("loại") || b.Contains("khách") || b.Contains("phòng") || b.Contains("ngắn")).ToList();
        if (blockings.Count > 0)
        {
            InspectorContent.Children.Add(Sep());
            foreach (var b in blockings)
                InspectorContent.Children.Add(new TextBlock { Text = $"⛔ {b}", FontSize = 11, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) });
        }
    }

    private void BuildInspectorB()
    {
        InspectorContent.Children.Add(InspectorSection("Đơn vị"));
        InspectorContent.Children.Add(InspectorRow("Loại", _orgType switch { "None" => "Không có", "Company" => "Công ty", "Agent" => "Đại lý", _ => _orgType }));
        if (_orgType == "Company")
        {
            InspectorContent.Children.Add(InspectorRow("Công ty", _companyName));
            InspectorContent.Children.Add(InspectorRow("Credit", _creditStatus ?? "—"));
        }
        if (_orgType == "Agent")
        {
            InspectorContent.Children.Add(InspectorRow("Đại lý", _agentName));
            InspectorContent.Children.Add(InspectorRow("Commission", $"{_commissionPct}%"));
        }

        InspectorContent.Children.Add(Sep());
        InspectorContent.Children.Add(InspectorSection("Contact chính"));
        InspectorContent.Children.Add(InspectorRow("Tên", string.IsNullOrWhiteSpace(_primaryContactName) ? "Chưa nhập" : _primaryContactName));
        InspectorContent.Children.Add(InspectorRow("SĐT", string.IsNullOrWhiteSpace(_primaryContactPhone) ? "Chưa nhập" : _primaryContactPhone));
        InspectorContent.Children.Add(InspectorRow("Email", string.IsNullOrWhiteSpace(_primaryContactEmail) ? "—" : _primaryContactEmail));

        var warnings = new List<string>();
        if (_orgType == "Company" && _creditStatus == "Kém") warnings.Add("Credit công ty yếu");
        if (string.IsNullOrWhiteSpace(_primaryContactEmail)) warnings.Add("Thiếu email contact");
        if (warnings.Count > 0)
        {
            InspectorContent.Children.Add(Sep());
            foreach (var w in warnings) InspectorContent.Children.Add(new TextBlock { Text = $"⚠ {w}", FontSize = 11, Foreground = C("#D97706"), Margin = new Thickness(0, 2, 0, 0) });
        }
    }

    private void BuildInspectorC()
    {
        InspectorContent.Children.Add(InspectorSection("Lưu trú"));
        InspectorContent.Children.Add(InspectorRow("Ngày đến", _arrivalDate?.ToString("dd/MM/yyyy") ?? "Chưa chọn"));
        InspectorContent.Children.Add(InspectorRow("Ngày đi", _departureDate?.ToString("dd/MM/yyyy") ?? "Chưa chọn"));
        if (_arrivalDate.HasValue && _departureDate.HasValue)
            InspectorContent.Children.Add(InspectorRow("Số đêm", $"{(int)(_departureDate.Value - _arrivalDate.Value).TotalDays} đêm"));
        InspectorContent.Children.Add(InspectorRow("Arrival waves", _arrivalWaves.Count.ToString()));
        InspectorContent.Children.Add(InspectorRow("Departure waves", _departureWaves.Count.ToString()));
        InspectorContent.Children.Add(InspectorRow("Cut-off", _cutoffDate?.ToString("dd/MM/yyyy") ?? "—"));
        InspectorContent.Children.Add(InspectorRow("Đón/Tiễn", $"{( _pickupRequired ? "Đón" : "—")} / {(_dropoffRequired ? "Tiễn" : "—")}"));

        if (_arrivalDate.HasValue && _arrivalDate < DateTime.Today)
            InspectorContent.Children.Add(new TextBlock { Text = "⚠ Ngày đến trong quá khứ", FontSize = 11, Foreground = C("#D97706"), Margin = new Thickness(0, 4, 0, 0) });
    }

    private void BuildInspectorD()
    {
        InspectorContent.Children.Add(InspectorSection("Phòng"));
        InspectorContent.Children.Add(InspectorRow("Tổng phòng", _roomRequirements.Sum(r => r.RoomCount).ToString()));
        InspectorContent.Children.Add(InspectorRow("Hạng phòng", _roomRequirements.Count.ToString()));
        InspectorContent.Children.Add(InspectorRow("Extra bed", _roomRequirements.Sum(r => r.ExtraBed).ToString()));
        InspectorContent.Children.Add(InspectorRow("Connecting", _roomRequirements.Count(r => r.Connecting).ToString()));
        InspectorContent.Children.Add(InspectorRow("Accessible", _roomRequirements.Count(r => r.Accessible).ToString()));

        InspectorContent.Children.Add(new TextBlock
        {
            Text = _roomAvailabilityChecked
                ? $"Khả dụng: {_roomAvailabilitySummary}."
                : _availabilityEngine is null
                    ? "Chưa cấu hình dịch vụ kiểm tra khả dụng phòng."
                    : "Bấm ‘Kiểm tra khả dụng’ để đối chiếu tồn phòng theo thời gian lưu trú và booking hiện có.",
            FontSize = 11, Foreground = C(_roomAvailabilityAvailable ? "#16A34A" : "#D97706"), TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 6, 0, 0)
        });
    }

    private void BuildInspectorE()
    {
        InspectorContent.Children.Add(InspectorSection("Tài chính"));
        InspectorContent.Children.Add(InspectorRow("Rate Plan", _ratePlan ?? "—"));
        InspectorContent.Children.Add(InspectorRow("Currency", _currency));
        InspectorContent.Children.Add(InspectorRow("Payer", _payer ?? "—"));
        InspectorContent.Children.Add(InspectorRow("Đặt cọc", _depositRequired ? $"{_depositAmount:N0} {_currency}" : "Không"));
        InspectorContent.Children.Add(InspectorRow("Hoa hồng", _agentCommissionEnabled ? $"{_agentCommissionPct}%" : "Không"));

        // Revenue estimate
        if (_roomRequirements.Count > 0 && _rateAfterTax > 0 && _arrivalDate.HasValue && _departureDate.HasValue)
        {
            var nights = (int)(_departureDate.Value - _arrivalDate.Value).TotalDays;
            var totalRooms = _roomRequirements.Sum(r => r.RoomCount);
            var estRev = totalRooms * nights * _rateAfterTax;
            InspectorContent.Children.Add(Sep());
            InspectorContent.Children.Add(InspectorRow("Doanh thu dự kiến", $"{estRev:N0} {_currency}"));
        }
    }

    private void BuildInspectorF()
    {
        InspectorContent.Children.Add(InspectorSection("Dịch vụ"));
        InspectorContent.Children.Add(InspectorRow("Dịch vụ", _services.Count.ToString()));
        InspectorContent.Children.Add(InspectorRow("VIP", _vipLevel ?? "—"));
        InspectorContent.Children.Add(InspectorRow("Tài liệu", _documents.Count.ToString()));

        if (_vipLevel is "VIP 1" or "VIP 2")
            InspectorContent.Children.Add(new TextBlock { Text = "⚠ Đoàn VIP — cần chú ý", FontSize = 11, Foreground = C("#D97706"), Margin = new Thickness(0, 4, 0, 0) });
    }

    private void BuildInspectorG()
    {
        InspectorContent.Children.Add(InspectorSection("Điểm sẵn sàng"));
        InspectorContent.Children.Add(InspectorRow("Hồ sơ", $"{GetScore("profile")}%"));
        InspectorContent.Children.Add(InspectorRow("Phòng", $"{GetScore("rooms")}%"));
        InspectorContent.Children.Add(InspectorRow("Tài chính", $"{GetScore("finance")}%"));
        InspectorContent.Children.Add(InspectorRow("Dịch vụ", $"{GetScore("services")}%"));
        InspectorContent.Children.Add(InspectorRow("Vận hành", $"{GetScore("operations")}%"));
        InspectorContent.Children.Add(InspectorRow("Tài liệu", $"{GetScore("documents")}%"));

        var blockings = GetBlockingIssues();
        var warnings = GetWarnings();

        if (blockings.Count > 0)
        {
            InspectorContent.Children.Add(Sep());
            InspectorContent.Children.Add(InspectorSection($"⛔ Blocking ({blockings.Count})"));
            foreach (var b in blockings.Take(5))
                InspectorContent.Children.Add(new TextBlock { Text = b, FontSize = 11, Foreground = C("#DC2626"), Margin = new Thickness(0, 2, 0, 0) });
        }

        if (warnings.Count > 0)
        {
            InspectorContent.Children.Add(Sep());
            InspectorContent.Children.Add(InspectorSection($"⚠ Warning ({warnings.Count})"));
            foreach (var w in warnings.Take(5))
                InspectorContent.Children.Add(new TextBlock { Text = w, FontSize = 11, Foreground = C("#D97706"), Margin = new Thickness(0, 2, 0, 0) });
        }
    }

    private void BuildInspectorH()
    {
        InspectorContent.Children.Add(InspectorSection("Xác nhận"));
        InspectorContent.Children.Add(InspectorStatus("Tên + loại đoàn", _confirmName));
        InspectorContent.Children.Add(InspectorStatus("Công ty/đại lý", _confirmOrg));
        InspectorContent.Children.Add(InspectorStatus("Contact chính", _confirmContact));
        InspectorContent.Children.Add(InspectorStatus("Thời gian lưu trú", _confirmStay));
        InspectorContent.Children.Add(InspectorStatus("Nhu cầu phòng", _confirmRooms));
        InspectorContent.Children.Add(InspectorStatus("Giá & thanh toán", _confirmFinance));
        InspectorContent.Children.Add(InspectorStatus("Dịch vụ", _confirmServices));
        InspectorContent.Children.Add(InspectorStatus("Xác nhận cuối", _confirmAll));

        var ready = CanCreateGroup();
        InspectorContent.Children.Add(Sep());
        InspectorContent.Children.Add(new Border { Background = ready ? Cb("#DCFCE7") : Cb("#FEF2F2"), CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 8, 10, 8), Child = new TextBlock { Text = ready ? "✓ Sẵn sàng tạo đoàn" : "⛔ Chưa sẵn sàng", FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = ready ? C("#16A34A") : C("#DC2626"), TextAlignment = TextAlignment.Center } });
    }

    // ═══ HELPER: FIELD BUILDERS ═══
    // The step builders add controls in order. Assigning their Grid.Column
    // once the grid is loaded keeps the spacing columns intact and prevents
    // every control from falling into column 0 (which caused label overlap).
    private static Grid FormGrid(int fieldCount, double gap)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        for (var i = 0; i < fieldCount; i++)
        {
            if (i > 0) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(gap) });
            grid.ColumnDefinitions.Add(new ColumnDefinition());
        }

        grid.Loaded += (_, _) =>
        {
            for (var i = 0; i < grid.Children.Count; i++)
                Grid.SetColumn(grid.Children[i], Math.Min(i * 2, grid.ColumnDefinitions.Count - 1));
        };
        return grid;
    }

    private static Grid Grid1() => FormGrid(1, 0);
    private static Grid Grid2() => FormGrid(2, 12);
    private static Grid Grid3() => FormGrid(3, 8);
    private static Grid Grid4() => FormGrid(4, 6);
    private static Grid Grid5() => FormGrid(5, 6);

    private FrameworkElement MakeField(string label, string value, Action<string> setter, string placeholder = "")
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value, Style = (Style)FindResource("ModTextBox") };
        if (!string.IsNullOrEmpty(placeholder)) { tb.Tag = placeholder; SetPlaceholder(tb, placeholder); }
        tb.TextChanged += (_, _) => { setter(tb.Text); UpdateFooter(); UpdateSummary(); };
        sp.Children.Add(tb);
        return sp;
    }

    private FrameworkElement MakeFieldInline(string label, string value, Action<string> setter, string placeholder = "")
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel"), FontSize = 11 });
        var tb = new TextBox { Text = value, Style = (Style)FindResource("ModTextBox"), Height = 34, FontSize = 12 };
        tb.TextChanged += (_, _) => setter(tb.Text);
        sp.Children.Add(tb);
        return sp;
    }

    private FrameworkElement MakeIntField(string label, int value, Action<int> setter)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value > 0 ? value.ToString() : "", Style = (Style)FindResource("ModTextBox") };
        tb.TextChanged += (_, _) => { if (int.TryParse(tb.Text, out var v)) setter(v); else if (string.IsNullOrEmpty(tb.Text)) setter(0); };
        sp.Children.Add(tb);
        return sp;
    }

    private FrameworkElement MakeIntInline(string label, int value, Action<int> setter)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel"), FontSize = 11 });
        var tb = new TextBox { Text = value > 0 ? value.ToString() : "", Style = (Style)FindResource("ModTextBox"), Height = 34, FontSize = 12 };
        tb.TextChanged += (_, _) => { if (int.TryParse(tb.Text, out var v)) setter(v); else if (string.IsNullOrEmpty(tb.Text)) setter(0); };
        sp.Children.Add(tb);
        return sp;
    }

    private FrameworkElement MakeDecimalField(string label, decimal value, Action<decimal> setter, string suffix = "")
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value > 0 ? $"{value:N0}" : "", Style = (Style)FindResource("ModTextBox") };
        tb.TextChanged += (_, _) => { if (decimal.TryParse(tb.Text, out var v)) setter(v); else if (string.IsNullOrEmpty(tb.Text)) setter(0); };
        sp.Children.Add(tb);
        return sp;
    }

    private FrameworkElement MakeCombo(string label, string? selectedValue, Action<string> setter, string[] options, string placeholder = "")
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var cb = new ComboBox { Style = (Style)FindResource("ModCombo"), ItemsSource = options };
        if (!string.IsNullOrEmpty(placeholder) && options.Length > 0)
        {
            cb.Text = placeholder;
            cb.Foreground = C("#94A3B8");
        }
        if (selectedValue is not null) cb.SelectedItem = selectedValue;
        cb.SelectionChanged += (_, _) =>
        {
            if (cb.SelectedItem is string s) { setter(s); cb.Foreground = C("#17365D"); }
            UpdateFooter(); UpdateSummary();
        };
        sp.Children.Add(cb);
        return sp;
    }

    private FrameworkElement MakeComboInline(string label, string? selectedValue, Action<string> setter, string[] options)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel"), FontSize = 11 });
        var cb = new ComboBox { Style = (Style)FindResource("ModCombo"), Height = 34, FontSize = 12, ItemsSource = options };
        if (selectedValue is not null) cb.SelectedItem = selectedValue;
        cb.SelectionChanged += (_, _) => { if (cb.SelectedItem is string s) setter(s); };
        sp.Children.Add(cb);
        return sp;
    }

    private FrameworkElement MakeDate(string label, DateTime? value, Action<DateTime?> setter)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var dp = new DatePicker { Style = (Style)FindResource("ModDatePicker"), SelectedDate = value };
        dp.SelectedDateChanged += (_, _) => { setter(dp.SelectedDate); OnFieldChanged(); };
        sp.Children.Add(dp);
        return sp;
    }

    private static FrameworkElement MakeCheck(string label, bool value, Action<bool> setter)
    {
        var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center };
        cb.Checked += (_, _) => setter(true);
        cb.Unchecked += (_, _) => setter(false);
        return cb;
    }

    private static FrameworkElement MakeCheckInline(string label, bool value, Action<bool> setter)
    {
        var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 12, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalAlignment = VerticalAlignment.Center };
        cb.Checked += (_, _) => setter(true);
        cb.Unchecked += (_, _) => setter(false);
        return cb;
    }

    private FrameworkElement MakeTextArea(string label, string value, Action<string> setter, string placeholder = "")
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 4) };
        sp.Children.Add(new TextBlock { Text = label, Style = (Style)FindResource("FieldLabel") });
        var tb = new TextBox { Text = value, Height = 70, Padding = new Thickness(12, 8, 12, 8), VerticalContentAlignment = VerticalAlignment.Top, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#17365D"), Background = Brushes.White, BorderBrush = Cb("#D7E2F0"), BorderThickness = new Thickness(1), TextWrapping = TextWrapping.Wrap, AcceptsReturn = true };
        tb.TextChanged += (_, _) => setter(tb.Text);
        sp.Children.Add(tb);
        return sp;
    }

    private FrameworkElement MakeConfirmCheck(string label, bool value, Action<bool> setter)
    {
        var border = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 3, 0, 0), Background = value ? Cb("#F0FDF4") : Cb("#FAFBFD"), BorderBrush = value ? Cb("#86EFAC") : Cb("#E2E8F0"), BorderThickness = new Thickness(1) };
        var cb = new CheckBox { Content = label, IsChecked = value, FontSize = 13, FontFamily = new FontFamily("Segoe UI"), Foreground = C("#334155"), VerticalContentAlignment = VerticalAlignment.Center };
        cb.Checked += (_, _) => { setter(true); border.Background = Cb("#F0FDF4"); border.BorderBrush = Cb("#86EFAC"); };
        cb.Unchecked += (_, _) => { setter(false); border.Background = Cb("#FAFBFD"); border.BorderBrush = Cb("#E2E8F0"); };
        border.Child = cb;
        return border;
    }

    private static FrameworkElement PreviewRow(string label, string value)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0),
            Children =
            {
                new TextBlock { Text = label + ": ", FontSize = 12, Foreground = C("#94A3B8"), Width = 140 },
                new TextBlock { Text = value, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#17365D") }
            }
        };
    }

    private static FrameworkElement InspectorSection(string title)
    {
        return new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(0, 0, 0, 6) };
    }

    private static FrameworkElement InspectorRow(string label, string value)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal, Margin = new Thickness(0, 1, 0, 0),
            Children =
            {
                new TextBlock { Text = label + ": ", FontSize = 11, Foreground = C("#94A3B8"), Width = 90 },
                new TextBlock { Text = value, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = C("#334155") }
            }
        };
    }

    private static FrameworkElement InspectorStatus(string label, bool ok)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0),
            Children =
            {
                new TextBlock { Text = ok ? "✓ " : "○ ", FontSize = 11, Foreground = ok ? C("#16A34A") : C("#94A3B8"), Width = 20 },
                new TextBlock { Text = label, FontSize = 11, Foreground = C("#475569") }
            }
        };
    }

    private FrameworkElement WaveEditor(ArrivalWave w, Action onDelete)
    {
        var card = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 6) };
        var stk = new StackPanel();
        stk.Children.Add(new TextBlock { Text = $"Wave {w.WaveNumber}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#7C3AED"), Margin = new Thickness(0, 0, 0, 6) });
        var sg = Grid3();
        sg.Children.Add(MakeDateInline("Ngày", w.Date, v => w.Date = v));
        sg.Children.Add(MakeFieldInline("Giờ", w.Time, v => w.Time = v, "HH:mm"));
        sg.Children.Add(MakeIntInline("Số khách", w.GuestCount, v => w.GuestCount = v));
        stk.Children.Add(sg);
        var sg2 = Grid3();
        sg2.Children.Add(MakeFieldInline("Phương tiện", w.Method, v => w.Method = v, "Flight/Bus/Car"));
        sg2.Children.Add(MakeFieldInline("Ghi chú", w.Note, v => w.Note = v, "Ghi chú"));
        var del = new Button { Content = "✕ Xóa wave", Style = (Style)FindResource("GrayBtn"), Height = 30, FontSize = 11, Margin = new Thickness(0, 12, 0, 0) };
        del.Click += (_, _) => onDelete();
        sg2.Children.Add(del);
        stk.Children.Add(sg2);
        card.Child = stk;
        return card;
    }

    private FrameworkElement WaveDepartureEditor(DepartureWave w, Action onDelete)
    {
        var card = new Border { Style = (Style)FindResource("PanelCard"), Padding = new Thickness(12, 8, 12, 8), Margin = new Thickness(0, 0, 0, 6) };
        var stk = new StackPanel();
        stk.Children.Add(new TextBlock { Text = $"Departure Wave {w.WaveNumber}", FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#EA580C"), Margin = new Thickness(0, 0, 0, 6) });
        var sg = Grid3();
        sg.Children.Add(MakeDateInline("Ngày", w.Date, v => w.Date = v));
        sg.Children.Add(MakeFieldInline("Giờ", w.Time, v => w.Time = v, "HH:mm"));
        sg.Children.Add(MakeIntInline("Số khách", w.GuestCount, v => w.GuestCount = v));
        stk.Children.Add(sg);
        var sg2 = Grid3();
        sg2.Children.Add(MakeFieldInline("Phương tiện", w.Method, v => w.Method = v, "Flight/Bus/Car"));
        sg2.Children.Add(MakeFieldInline("Ghi chú", w.Note, v => w.Note = v, "Ghi chú"));
        var del = new Button { Content = "✕ Xóa wave", Style = (Style)FindResource("GrayBtn"), Height = 30, FontSize = 11, Margin = new Thickness(0, 12, 0, 0) };
        del.Click += (_, _) => onDelete();
        sg2.Children.Add(del);
        stk.Children.Add(sg2);
        card.Child = stk;
        return card;
    }

    private static FrameworkElement MakeDateInline(string label, DateTime? value, Action<DateTime?> setter)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = C("#94A3B8"), Margin = new Thickness(0, 0, 0, 2) });
        var dp = new DatePicker { Height = 34, FontSize = 12, FontFamily = new FontFamily("Segoe UI"), SelectedDate = value };
        dp.SelectedDateChanged += (_, _) => setter(dp.SelectedDate);
        sp.Children.Add(dp);
        return sp;
    }

    private static FrameworkElement KpiBox(string label, string value, string color)
    {
        return new Border
        {
            Width = 130, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}14")),
            CornerRadius = new CornerRadius(10), Padding = new Thickness(12, 10, 12, 10), Margin = new Thickness(0, 0, 8, 6),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{color}33")), BorderThickness = new Thickness(1),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = label, FontSize = 11, Foreground = C("#64748B") },
                    new TextBlock { Text = value, FontSize = 22, FontWeight = FontWeights.Bold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) }
                }
            }
        };
    }

    private Button QuickBtn(string text, Action action)
    {
        var btn = new Button { Content = text, Style = (Style)FindResource("ActionBtn"), Height = 32, FontSize = 11, Margin = new Thickness(0, 0, 8, 6), Padding = new Thickness(10, 0, 10, 0) };
        btn.Click += (_, _) => action();
        return btn;
    }

    // ═══ OPTIONS ═══
    private static string[] GroupTypeOptions() => new[] { "Corporate", "Tour", "Conference", "Wedding", "Government", "Airline Crew", "Sports", "School", "Family", "MICE", "Travel Series", "Other" };
    private static string[] SegmentOptions() => new[] { "Corporate", "Leisure", "Government", "Wholesale", "Groups" };
    private static string[] MarketOptions() => new[] { "Nội địa", "Quốc tế", "Đông Nam Á", "Đông Bắc Á", "Châu Âu", "Châu Mỹ", "Châu Úc" };
    private static string[] SourceOptions() => new[] { "Direct", "Website", "OTA", "Travel Agent", "Corporate", "Referral", "Social Media" };
    private static string[] CountryOptions() => new[] { "Việt Nam", "Hàn Quốc", "Nhật Bản", "Trung Quốc", "Mỹ", "Anh", "Pháp", "Đức", "Úc", "Singapore", "Thái Lan", "Khác" };
    private static string[] LanguageOptions() => new[] { "Tiếng Việt", "English", "한국어", "日本語", "中文", "Français" };
    private static string[] TripPurposeOptions() => new[] { "Hội nghị", "Du lịch", "Công tác", "Sự kiện", "Cưới", "Đào tạo", "Thi đấu", "Phi hành đoàn", "Khác" };
    private static string[] EventTypeOptions() => new[] { "Không có", "Hội thảo", "Triển lãm", "Gala Dinner", "Team Building", "Họp báo", "Tiệc cưới", "Year-end Party" };
    private static string[] VipOptions() => new[] { "Không", "VIP 3", "VIP 2", "VIP 1", "VVIP" };
    private static string[] PriorityOptions() => new[] { "Bình thường", "Cao", "Khẩn", "Đặc biệt" };
    private static string[] CreditOptions() => new[] { "Tốt", "Trung bình", "Kém", "Chưa xác minh" };
    private string[] RoomTypeOptions() => _roomTypes.Count == 0
        ? Array.Empty<string>()
        : _roomTypes.Select(x => $"{x.Code} - {x.Name}").ToArray();

    private static void SetPlaceholder(TextBox tb, string placeholder)
    {
        tb.GotFocus += (_, _) => { if ((string)tb.Tag == placeholder && tb.Text == "") tb.Text = ""; };
        tb.LostFocus += (_, _) => { if (string.IsNullOrWhiteSpace(tb.Text)) tb.Text = ""; };
    }

    private void OnFieldChanged()
    {
        _hasUnsavedChanges = true;
        _roomAvailabilityChecked = false;
        _roomAvailabilityAvailable = false;
        _roomAvailabilitySummary = "Chưa kiểm tra";
        UpdateFooter();
        UpdateSummary();
        UpdateInspector(_currentStep);
    }

    // ═══ COLOR HELPERS ═══
    private static SolidColorBrush C(string h) => new((Color)ColorConverter.ConvertFromString(h));
    private static SolidColorBrush Cb(string h) => new((Color)ColorConverter.ConvertFromString(h));

    private static TextBlock STitle(string t) => new() { Text = t, FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A"), Margin = new Thickness(0, 0, 0, 4) };
    private static TextBlock SText(string t) => new() { Text = t, FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 0, 0, 10) };
    private static TextBlock SubTitle(string t) => new() { Text = t, FontSize = 13, FontWeight = FontWeights.SemiBold, Foreground = C("#334155"), Margin = new Thickness(0, 12, 0, 6) };
    private static Border Sep() => new() { Height = 1, Background = Cb("#E2E8F0"), Margin = new Thickness(0, 8, 0, 8) };

    private static Border Chip(string text, string bgHex, string fgHex)
    {
        return new Border
        {
            CornerRadius = new CornerRadius(6), Padding = new Thickness(10, 4, 10, 4), Margin = new Thickness(0, 0, 6, 4),
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{bgHex}20")),
            BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString($"{bgHex}40")), BorderThickness = new Thickness(1),
            Child = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fgHex)) }
        };
    }
}

// ═══ DATA CLASSES ═══
public class SecondaryContact
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
}

public class ArrivalWave
{
    public int WaveNumber { get; set; } = 1;
    public DateTime? Date { get; set; }
    public string Time { get; set; } = "";
    public int GuestCount { get; set; }
    public string Method { get; set; } = "";
    public string Note { get; set; } = "";
}

public class DepartureWave
{
    public int WaveNumber { get; set; } = 1;
    public DateTime? Date { get; set; }
    public string Time { get; set; } = "";
    public int GuestCount { get; set; }
    public string Method { get; set; } = "";
    public string Note { get; set; } = "";
}

public class RoomRequirement
{
    public string? RoomType { get; set; }
    public int RoomCount { get; set; } = 1;
    public int GuestsPerRoom { get; set; } = 2;
    public int ExtraBed { get; set; }
    public bool Connecting { get; set; }
    public string Smoking { get; set; } = "Non-smoking";
    public string? View { get; set; }
    public string FloorPreference { get; set; } = "";
    public bool Accessible { get; set; }
    public string Note { get; set; } = "";
}

public class GroupService
{
    public string? Type { get; set; }
    public string Description { get; set; } = "";
    public string Date { get; set; } = "";
    public string Time { get; set; } = "";
    public int Quantity { get; set; } = 1;
    public string? Department { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; } = "Chưa làm";
}

public sealed class GroupDocumentRow
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "Hợp đồng";
    public string Status { get; set; } = "Chưa có";
    public string UploadDate { get; set; } = "—";
}
