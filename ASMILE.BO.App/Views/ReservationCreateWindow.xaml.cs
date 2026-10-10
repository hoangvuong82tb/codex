using ASMILE.BO.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class ReservationCreateWindow : UserControl, INotifyPropertyChanged
{
    private int _currentStep;
    public int CurrentStep
    {
        get => _currentStep;
        set
        {
            if (_currentStep == value) return;
            if (IsEditMode && value is not (0 or 5 or 6)) return;
            if (IsEditMode && value == 6)
            {
                Modal.Workspace.InternalNotes = BookingNotes;
                Modal.Workspace.SpecialRequests = SpecialRequests;
            }
            _currentStep = value;
            if (value is 2 or 6) EnsureGuestState();
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsStepA)); OnPropertyChanged(nameof(IsStepB)); OnPropertyChanged(nameof(IsStepC));
            OnPropertyChanged(nameof(IsStepD)); OnPropertyChanged(nameof(IsStepE)); OnPropertyChanged(nameof(IsStepF)); OnPropertyChanged(nameof(IsStepG));
            OnPropertyChanged(nameof(CanGoBack)); OnPropertyChanged(nameof(CanGoNext)); OnPropertyChanged(nameof(IsLastStep)); OnPropertyChanged(nameof(IsFirstStep)); OnPropertyChanged(nameof(CurrentStepLabel));
            UpdateStepStates();
            if (value == 6)
            {
                RefreshStepG();
                if (IsEditMode) DetectChanges();
            }
        }
    }
    public bool IsStepA => CurrentStep == 0; public bool IsStepB => CurrentStep == 1; public bool IsStepC => CurrentStep == 2; public bool IsStepD => CurrentStep == 3; public bool IsStepE => CurrentStep == 4; public bool IsStepF => CurrentStep == 5; public bool IsStepG => CurrentStep == 6;
    public bool CanGoBack => IsEditMode ? CurrentStep is 5 or 6 : CurrentStep > 0;
    public bool CanGoNext => IsEditMode ? CurrentStep is 0 or 5 : CurrentStep < 6;
    public bool IsFirstStep => CurrentStep == 0; public bool IsLastStep => CurrentStep == 6;
    public string CurrentStepLabel => ((char)('A' + CurrentStep)).ToString();
    public ReservationModalWorkspaceViewModel Modal { get; }

    // ═══ EDIT MODE ═══
    private bool _isEditMode;
    public bool IsEditMode
    {
        get => _isEditMode;
        set
        {
            if (_isEditMode == value) return;
            _isEditMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsCreateMode));
            OnPropertyChanged(nameof(WindowTitle));
            OnPropertyChanged(nameof(WindowSubtitle));
            OnPropertyChanged(nameof(ReviewTitle));
            OnPropertyChanged(nameof(FooterActionLabel));
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoNext));
            OnPropertyChanged(nameof(IsLastStep));
            OnPropertyChanged(nameof(CanCreateReservation));
            OnPropertyChanged(nameof(CanFinish));
            OnPropertyChanged(nameof(ReadyMessage));
            UpdateStepStates();
        }
    }
    public bool IsCreateMode => !_isEditMode;
    public string WindowTitle => _isEditMode ? "Sửa đặt phòng" : "Đặt phòng mới";
    public string ReviewTitle => _isEditMode ? "Kiểm tra thay đổi" : "Kiểm tra trước khi tạo";
    public string WindowSubtitle => _isEditMode && !string.IsNullOrWhiteSpace(EditReservationNumber) ? $"Số đặt phòng: {EditReservationNumber}" : "Tạo hồ sơ đặt phòng — Bước A–G";
    public string FooterActionLabel => _isEditMode ? "Lưu thay đổi" : "Tạo đặt phòng";
    private string? _editReservationNumber;
    public string? EditReservationNumber { get => _editReservationNumber; set { _editReservationNumber = value; OnPropertyChanged(); OnPropertyChanged(nameof(WindowSubtitle)); } }
    private Guid? _editReservationId;
    public Guid? EditReservationId => _editReservationId;
    public bool HasChanges { get; private set; }
    public bool CanSaveChanges => IsEditMode ? HasChanges : true;
    private readonly Dictionary<string, object?> _originalSnapshot = new();

    public void SetEditMode(Guid reservationId, string reservationNumber)
    {
        _editReservationId = reservationId;
        EditReservationNumber = reservationNumber;
        if (Modal.Workspace.SelectedReservation is { } reservation)
        {
            BookingNotes = reservation.InternalNotes ?? string.Empty;
            SpecialRequests = reservation.SpecialRequests ?? string.Empty;
        }
        IsEditMode = true;
        CurrentStep = 0;
        SyncGuestsFromStepA();
        if (SelectedGuest is { } primaryGuest && Modal.Workspace.SelectedReservation is { } selected)
        {
            primaryGuest.FullName = selected.GuestName;
            primaryGuest.Phone = selected.GuestPhone ?? string.Empty;
            primaryGuest.Email = selected.GuestEmail ?? string.Empty;
        }
        SyncSelectedGuestToWorkspace();
        HasChanges = false;
        CaptureSnapshot();
        OnPropertyChanged(nameof(CanSaveChanges));
        OnPropertyChanged(nameof(CanFinish));
        OnPropertyChanged(nameof(ReadyMessage));
    }

    public void CaptureSnapshot()
    {
        _originalSnapshot.Clear();
        _originalSnapshot["GuestName"] = Modal.Workspace.GuestName;
        _originalSnapshot["GuestPhone"] = Modal.Workspace.GuestPhone;
        _originalSnapshot["GuestEmail"] = Modal.Workspace.GuestEmail;
        _originalSnapshot["ArrivalDate"] = Modal.Workspace.ArrivalDate;
        _originalSnapshot["DepartureDate"] = Modal.Workspace.DepartureDate;
        _originalSnapshot["Adults"] = Modal.Workspace.Adults;
        _originalSnapshot["Children"] = Modal.Workspace.Children;
        _originalSnapshot["Source"] = Modal.Workspace.Source;
        _originalSnapshot["InternalNotes"] = Modal.Workspace.InternalNotes;
        _originalSnapshot["SpecialRequests"] = Modal.Workspace.SpecialRequests;
        _originalSnapshot["RoomTypeId"] = Modal.Workspace.SelectedRoomType?.HotelRoomTypeId;
        HasChanges = false;
        OnPropertyChanged(nameof(CanSaveChanges));
        OnPropertyChanged(nameof(CanFinish));
        OnPropertyChanged(nameof(ReadyMessage));
    }

    public void DetectChanges()
    {
        HasChanges = !ValuesEqual(_originalSnapshot.GetValueOrDefault("GuestName"), Modal.Workspace.GuestName)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("GuestPhone"), Modal.Workspace.GuestPhone)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("GuestEmail"), Modal.Workspace.GuestEmail)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("ArrivalDate"), Modal.Workspace.ArrivalDate)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("DepartureDate"), Modal.Workspace.DepartureDate)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("Adults"), Modal.Workspace.Adults)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("Children"), Modal.Workspace.Children)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("Source"), Modal.Workspace.Source)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("InternalNotes"), Modal.Workspace.InternalNotes)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("SpecialRequests"), Modal.Workspace.SpecialRequests)
            || !ValuesEqual(_originalSnapshot.GetValueOrDefault("RoomTypeId"), Modal.Workspace.SelectedRoomType?.HotelRoomTypeId);
        OnPropertyChanged(nameof(HasChanges));
        OnPropertyChanged(nameof(CanSaveChanges));
        OnPropertyChanged(nameof(CanFinish));
        OnPropertyChanged(nameof(ReadyMessage));
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        if (a is string sa && string.IsNullOrWhiteSpace(sa) && b is string sb && string.IsNullOrWhiteSpace(sb)) return true;
        return Equals(a, b);
    }

    public List<ChangeSummaryItem> ChangeSummary
    {
        get
        {
            var list = new List<ChangeSummaryItem>();
            Compare("Tên khách", _originalSnapshot.GetValueOrDefault("GuestName"), Modal.Workspace.GuestName, list);
            Compare("Điện thoại", _originalSnapshot.GetValueOrDefault("GuestPhone"), Modal.Workspace.GuestPhone, list);
            Compare("Email", _originalSnapshot.GetValueOrDefault("GuestEmail"), Modal.Workspace.GuestEmail, list);
            Compare("Ngày đến", FormatDate(_originalSnapshot.GetValueOrDefault("ArrivalDate") as DateTime?), FormatDate(Modal.Workspace.ArrivalDate), list);
            Compare("Ngày đi", FormatDate(_originalSnapshot.GetValueOrDefault("DepartureDate") as DateTime?), FormatDate(Modal.Workspace.DepartureDate), list);
            Compare("Người lớn", _originalSnapshot.GetValueOrDefault("Adults"), Modal.Workspace.Adults, list);
            Compare("Trẻ em", _originalSnapshot.GetValueOrDefault("Children"), Modal.Workspace.Children, list);
            Compare("Nguồn", _originalSnapshot.GetValueOrDefault("Source"), Modal.Workspace.Source, list);
            Compare("Loại phòng", _originalSnapshot.GetValueOrDefault("RoomTypeId"), Modal.Workspace.SelectedRoomType?.HotelRoomTypeId, list);
            Compare("Ghi chú", _originalSnapshot.GetValueOrDefault("InternalNotes"), Modal.Workspace.InternalNotes, list);
            Compare("Yêu cầu đặc biệt", _originalSnapshot.GetValueOrDefault("SpecialRequests"), Modal.Workspace.SpecialRequests, list);
            return list;
        }
    }

    private static void Compare(string label, object? old, object? newVal, List<ChangeSummaryItem> list)
    {
        if (!ValuesEqual(old, newVal))
            list.Add(new ChangeSummaryItem { Label = label, OldValue = FormatVal(old), NewValue = FormatVal(newVal) });
    }

    private static string FormatVal(object? v) => v switch
    {
        null => "—",
        bool b => b ? "Có" : "Không",
        DateTime dt => dt.ToString("dd/MM/yyyy"),
        _ => v.ToString() ?? "—"
    };
    private static string? FormatDate(DateTime? dt) => dt?.ToString("dd/MM/yyyy");

    // Step E: Financial properties
    private bool _useRateCode = true;
    public bool UseRateCode { get => _useRateCode; set { _useRateCode = value; OnPropertyChanged(); OnPropertyChanged(nameof(UseNegotiatedRate)); OnPropertyChanged(nameof(PriceSourceLabel)); UpdateFinancialSummary(); if (IsEditMode) DetectChanges(); } }
    public bool UseNegotiatedRate { get => !_useRateCode; set { _useRateCode = !value; OnPropertyChanged(nameof(UseRateCode)); OnPropertyChanged(nameof(UseNegotiatedRate)); OnPropertyChanged(nameof(PriceSourceLabel)); UpdateFinancialSummary(); } }
    public string PriceSourceLabel => _useRateCode ? "Rate Code" : "Giá thỏa thuận";

    private string _rateCode = "RACK";
    public string RateCodeValue
    {
        get => Modal?.Workspace?.SelectedRatePlan?.PlanCode ?? _rateCode;
        set { _rateCode = value; OnPropertyChanged(); UpdateFinancialSummary(); }
    }

    private string _roomRate = "";
    public string RoomRateInput { get => _roomRate; set { _roomRate = value; OnPropertyChanged(); UpdateFinancialSummary(); } }

    private string _currency = "VND";
    public string CurrencyValue { get => _currency; set { _currency = value; OnPropertyChanged(); UpdateFinancialSummary(); } }

    private bool _isVatIncluded = true;
    public bool IsVatIncluded { get => _isVatIncluded; set { _isVatIncluded = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsVatExcluded)); OnPropertyChanged(nameof(VatLabel)); UpdateFinancialSummary(); } }
    public bool IsVatExcluded { get => !_isVatIncluded; set { _isVatIncluded = !value; OnPropertyChanged(nameof(IsVatIncluded)); OnPropertyChanged(nameof(IsVatExcluded)); OnPropertyChanged(nameof(VatLabel)); UpdateFinancialSummary(); } }
    public string VatLabel => _isVatIncluded ? "Đã gồm" : "Chưa gồm";

    private string _depositAmount = "";
    public string DepositAmountInput { get => _depositAmount; set { _depositAmount = value; OnPropertyChanged(); UpdateFinancialSummary(); } }

    private DateTime? _depositDueDate = DateTime.Today;
    public DateTime? DepositDueDate { get => _depositDueDate; set { _depositDueDate = value; OnPropertyChanged(); } }

    private string _paymentMethod = "";
    public string PaymentMethodValue { get => _paymentMethod; set { _paymentMethod = value; OnPropertyChanged(); UpdateFinancialSummary(); } }

    private bool _isDepositCollected;
    public bool IsDepositCollected { get => _isDepositCollected; set { _isDepositCollected = value; OnPropertyChanged(); UpdateFinancialSummary(); } }

    private string _discountPercent = "";
    public string DiscountPercentInput { get => _discountPercent; set { _discountPercent = value; OnPropertyChanged(); UpdateFinancialSummary(); } }

    private string _discountAmount = "";
    public string DiscountAmountInput { get => _discountAmount; set { _discountAmount = value; OnPropertyChanged(); UpdateFinancialSummary(); } }

    public string EffectiveRoomRateDisplay => _useRateCode ? RateCodeValue : (string.IsNullOrWhiteSpace(_roomRate) ? "—" : _roomRate);
    public string RoomRateDisplay => GetRoomRate() > 0 ? $"{GetRoomRate():N0} {_currency}" : "—";
    public string RoomChargeDisplay => CalcRoomCharge() > 0 ? $"{CalcRoomCharge():N0} {_currency}" : "—";
    public string DiscountDisplay => CalcDiscount() > 0 ? $"{CalcDiscount():N0} {_currency}" : "—";
    public string DepositDisplay => GetDeposit() > 0 ? $"{GetDeposit():N0} {_currency}" : "—";
    public string PaymentDisplay => string.IsNullOrWhiteSpace(_paymentMethod) ? "—" : _paymentMethod;
    public string BalanceDisplay => CalcBalance() >= 0 ? $"{CalcBalance():N0} {_currency}" : "—";

    private decimal GetRoomRate()
    {
        if (_useRateCode && Modal?.Workspace?.SelectedRatePlan is { } plan)
            return plan.BaseRate;
        decimal.TryParse(_roomRate, out var r);
        return r;
    }
    private int GetNights() => Modal.Workspace.Nights > 0 ? Modal.Workspace.Nights : 1;
    private decimal GetDeposit() { decimal.TryParse(_depositAmount, out var d); return d; }
    private decimal CalcRoomCharge() => GetRoomRate() * GetNights();
    private decimal CalcDiscount() { if (decimal.TryParse(_discountAmount, out var da) && da > 0) return da; if (decimal.TryParse(_discountPercent, out var dp) && dp > 0) return CalcRoomCharge() * dp / 100; return 0; }
    private decimal CalcBalance() => CalcRoomCharge() - CalcDiscount() - GetDeposit();

    public void UpdateFinancialSummary()
    {
        OnPropertyChanged(nameof(EffectiveRoomRateDisplay)); OnPropertyChanged(nameof(RoomRateDisplay));
        OnPropertyChanged(nameof(RoomChargeDisplay)); OnPropertyChanged(nameof(DiscountDisplay));
        OnPropertyChanged(nameof(DepositDisplay)); OnPropertyChanged(nameof(PaymentDisplay));
        OnPropertyChanged(nameof(BalanceDisplay)); OnPropertyChanged(nameof(PriceSourceLabel));
        OnPropertyChanged(nameof(VatLabel));
    }

    // Step B: Guest document tracking
    public ObservableCollection<GuestDocument> Guests { get; } = new();
    private int _selectedGuest;
    public int SelectedGuestIndex
    {
        get => _selectedGuest;
        set
        {
            if (_selectedGuest == value) return;
            _selectedGuest = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedGuest));
            AttachGuestHandlers();
            SyncSelectedGuestToWorkspace();
        }
    }
    public GuestDocument? SelectedGuest => Guests.Count > 0 && SelectedGuestIndex < Guests.Count ? Guests[SelectedGuestIndex] : null;
    private string _docType = "CCCD";
    public string DocumentType { get => _docType; set { _docType = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsCCCD)); OnPropertyChanged(nameof(IsPassport)); if (SelectedGuest is not null) SelectedGuest.DocumentType = value; } }
    public bool IsCCCD => DocumentType == "CCCD";
    public bool IsPassport => DocumentType == "Hộ chiếu";

    public ICommand GoNextCommand { get; }
    public ICommand GoBackCommand { get; }
    public List<WizardStep> Steps { get; } = new();

    private static readonly string[] StepLabels = { "Thông tin lưu trú", "Quét giấy tờ", "Hồ sơ khách", "Thông tin đặt phòng", "Tài chính", "Ghi chú", "Xác nhận" };
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F", "G" };

    public ReservationCreateWindow(ReservationModalWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Modal = viewModel;
        Modal.PropertyChanged += OnModalPropertyChanged;
        Modal.Workspace.PropertyChanged += OnWorkspacePropertyChanged;
        DataContext = this;
        GoNextCommand = new WizRelayCommand(GoNext, () => CanGoNext);
        GoBackCommand = new WizRelayCommand(GoBack, () => CanGoBack);

        for (int i = 0; i < 7; i++)
            Steps.Add(new WizardStep { Index = i, Letter = StepLetters[i], Label = StepLabels[i], ShowArrow = i < 6 });
        // Step C edits a GuestDocument while the reservation service consumes
        // Workspace.GuestName. Keep both representations synchronized.
        UpdateStepStates();
    }

    private void OnModalPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ReservationModalWorkspaceViewModel.IsSavingNew)) return;
        OnPropertyChanged(nameof(CanCreateReservation));
        OnPropertyChanged(nameof(CanFinish));
    }

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ReservationWorkspaceViewModel.SelectedRatePlan)
            or nameof(ReservationWorkspaceViewModel.SelectedRoomType)
            or nameof(ReservationWorkspaceViewModel.ArrivalDate)
            or nameof(ReservationWorkspaceViewModel.DepartureDate)
            or nameof(ReservationWorkspaceViewModel.HasRatePlanRepository))
        {
            OnPropertyChanged(nameof(RateCodeValue));
            OnPropertyChanged(nameof(EffectiveRoomRateDisplay));
            OnPropertyChanged(nameof(RoomRateDisplay));
            OnPropertyChanged(nameof(RoomChargeDisplay));
            OnPropertyChanged(nameof(CanCreateReservation));
            OnPropertyChanged(nameof(CanFinish));
            OnPropertyChanged(nameof(ReadyMessage));
            OnPropertyChanged(nameof(StepGStatuses));
        }
    }

    public void UpdateStepStates()
    {
        for (int i = 0; i < 7; i++)
        {
            var s = Steps[i];
            s.IsNavigable = !IsEditMode || IsEditableStepInEditMode(i);
            if (IsEditMode && !s.IsNavigable)
            {
                s.Bg = "#F8FAFC"; s.Brd = "#E2E8F0"; s.Fg = "#94A3B8";
                s.Status = "Chỉ áp dụng khi tạo mới"; s.Icon = "–"; s.IconVis = Visibility.Visible;
                s.BadgeBg = "#F1F5F9"; s.BadgeFg = "#94A3B8";
                continue;
            }
            if (i < CurrentStep) { s.Bg = "#F0FDF4"; s.Brd = "#86EFAC"; s.Fg = "#166534"; s.Status = "Hoàn tất"; s.Icon = "✓"; s.IconVis = Visibility.Visible; s.BadgeBg = "#16A34A"; s.BadgeFg = "White"; }
            else if (i == CurrentStep) { s.Bg = "#EFF6FF"; s.Brd = "#3B82F6"; s.Fg = "#1E40AF"; s.Status = "Đang nhập"; s.Icon = "●"; s.IconVis = Visibility.Visible; s.BadgeBg = "#2563EB"; s.BadgeFg = "White"; }
            else { s.Bg = "#FFFFFF"; s.Brd = "#E2E8F0"; s.Fg = "#64748B"; s.Status = "Chưa bắt đầu"; s.Icon = ""; s.IconVis = Visibility.Collapsed; s.BadgeBg = "#F1F5F9"; s.BadgeFg = "#64748B"; }
        }
    }

    private void GoNext()
    {
        if (!CanGoNext) return;
        if (CurrentStep == 0 && !IsEditMode) SyncGuestsFromStepA();
        if (CurrentStep == 2 && !IsEditMode) SyncSelectedGuestToWorkspace();
        CurrentStep = IsEditMode ? (CurrentStep == 0 ? 5 : 6) : CurrentStep + 1;
        if (CurrentStep == 6)
        {
            if (!IsEditMode) SyncSelectedGuestToWorkspace();
            else
            {
                Modal.Workspace.InternalNotes = BookingNotes;
                Modal.Workspace.SpecialRequests = SpecialRequests;
            }
            RefreshStepG();
            if (IsEditMode) DetectChanges();
        }
    }
    private void GoBack()
    {
        if (!CanGoBack) return;
        CurrentStep = IsEditMode ? (CurrentStep == 6 ? 5 : 0) : CurrentStep - 1;
    }

    public void SyncGuestsFromStepA()
    {
        var adults = Modal.Workspace.Adults > 0 ? Modal.Workspace.Adults : 1;
        while (Guests.Count < adults)
            Guests.Add(new GuestDocument { GuestIndex = Guests.Count, Role = Guests.Count == 0 ? "Khách chính" : "Người lớn" });
        while (Guests.Count > adults && Guests.Count > 0)
            Guests.RemoveAt(Guests.Count - 1);
        if (SelectedGuestIndex >= Guests.Count) SelectedGuestIndex = Guests.Count - 1;
        if (Guests.Count > 0 && SelectedGuestIndex < 0) SelectedGuestIndex = 0;
        AttachGuestHandlers();
        SyncSelectedGuestToWorkspace();
    }

    private GuestDocument? _observedGuest;

    private void AttachGuestHandlers()
    {
        if (_observedGuest is not null)
            _observedGuest.PropertyChanged -= OnGuestPropertyChanged;

        _observedGuest = SelectedGuest;
        if (_observedGuest is not null)
            _observedGuest.PropertyChanged += OnGuestPropertyChanged;

        // Guests is populated after the control is constructed. Notify the
        // nested bindings so TextBox/ComboBox editors bind to the actual
        // GuestDocument instead of remaining unbound temporary inputs.
        OnPropertyChanged(nameof(SelectedGuest));
    }

    private void OnGuestPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender == SelectedGuest && (e.PropertyName is null || e.PropertyName == nameof(GuestDocument.FullName)))
            SyncSelectedGuestToWorkspace();

        // The preview binds through SelectedGuest.*. Re-publish the parent
        // property as well so every preview binding is refreshed immediately,
        // including Run-based TextBlocks in the right-hand panel.
        OnPropertyChanged(nameof(SelectedGuest));
        OnPropertyChanged(nameof(GuestNameDisplay));
        OnPropertyChanged(nameof(CanCreateReservation));
        OnPropertyChanged(nameof(StepGStatuses));
    }

    private void SyncSelectedGuestToWorkspace()
    {
        if (IsEditMode) return;
        var name = SelectedGuest?.FullName?.Trim() ?? string.Empty;
        if (!string.Equals(Modal.Workspace.GuestName, name, StringComparison.Ordinal))
            Modal.Workspace.GuestName = name;
        Modal.Workspace.GuestPhone = SelectedGuest?.Phone ?? string.Empty;
        Modal.Workspace.GuestEmail = SelectedGuest?.Email ?? string.Empty;

        OnPropertyChanged(nameof(GuestNameDisplay));
        OnPropertyChanged(nameof(CanCreateReservation));
        OnPropertyChanged(nameof(StepGStatuses));
    }

    private void Step_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is int idx && idx >= 0 && idx < Steps.Count)
        {
            if (Steps[idx].IsNavigable) CurrentStep = idx;
        }
    }

    private void SkipToStepC(object sender, RoutedEventArgs e) => CurrentStep = 2;
    private void GoToStepA(object sender, RoutedEventArgs e) => CurrentStep = 0;
    private void GoToStepC(object sender, RoutedEventArgs e) => CurrentStep = 2;
    private void GoToStepD(object sender, RoutedEventArgs e) => CurrentStep = 3;
    private void GoToStepE(object sender, RoutedEventArgs e) => CurrentStep = 4;
    private void GoToStepF(object sender, RoutedEventArgs e) => CurrentStep = 5;

    private async void CreateReservation_Click(object sender, RoutedEventArgs e)
    {
        if (IsEditMode)
        {
            Modal.Workspace.InternalNotes = BookingNotes;
            Modal.Workspace.SpecialRequests = SpecialRequests;
            DetectChanges();
            if (!CanSaveChanges)
            {
                Modal.Workspace.InfoMessage = "Không có thay đổi để lưu.";
                return;
            }

            await Modal.Workspace.SaveAsync();
            if (!Modal.Workspace.HasError)
            {
                CaptureSnapshot();
                RefreshStepG();
                MessageBox.Show($"Cập nhật đặt phòng {EditReservationNumber} hoàn tất.", "Sửa đặt phòng thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            return;
        }

        SyncSelectedGuestToWorkspace();

        if (Modal.IsSavingNew) return;
        if (!CanCreateReservation)
        {
            Modal.Workspace.ErrorMessage = "Chưa đủ điều kiện tạo đặt phòng. Vui lòng kiểm tra lại thông tin.";
            return;
        }

        if (await Modal.CreateNewAsync())
        {
            var depositAmount = GetDeposit();
            if (depositAmount > 0)
            {
                var dueDate = DateOnly.FromDateTime((DepositDueDate ?? Modal.Workspace.ArrivalDate ?? DateTime.Today).Date);
                if (!await Modal.Workspace.RecordDepositAsync(depositAmount, dueDate))
                    return;
            }

            MessageBox.Show(
                $"Tạo đặt phòng {Modal.Workspace.SelectedReservation?.ReservationNumber} hoàn tất.",
                "Tạo đặt phòng thành công",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    // ═══ STEP F: NOTES ═══
    private string _bookingNotes = "";
    public string BookingNotes { get => _bookingNotes; set { _bookingNotes = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private string _specialRequests = "";
    public string SpecialRequests { get => _specialRequests; set { _specialRequests = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private bool _needPickup;
    public bool NeedPickup { get => _needPickup; set { _needPickup = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private bool _needDropoff;
    public bool NeedDropoff { get => _needDropoff; set { _needDropoff = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private string _flightArrival = "";
    public string FlightArrival { get => _flightArrival; set { _flightArrival = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private string _flightDeparture = "";
    public string FlightDeparture { get => _flightDeparture; set { _flightDeparture = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private string _flightArrivalTime = "";
    public string FlightArrivalTime { get => _flightArrivalTime; set { _flightArrivalTime = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private string _flightDepartureTime = "";
    public string FlightDepartureTime { get => _flightDepartureTime; set { _flightDepartureTime = value; OnPropertyChanged(); UpdateNotesSummary(); } }

    private static string Truncate(string? s, int max = 80)
        => string.IsNullOrWhiteSpace(s) ? "—" : (s.Length <= max ? s : s[..(max - 3)] + "...");

    public string BookingNotesPreview => Truncate(_bookingNotes);
    public string InternalNotesPreview => Truncate(Modal?.Workspace?.InternalNotes);
    public string SpecialRequestsPreview => Truncate(_specialRequests);
    public string PickupDisplay => _needPickup ? "Có" : "—";
    public string DropoffDisplay => _needDropoff ? "Có" : "—";
    public string FlightArrivalDisplay => string.IsNullOrWhiteSpace(_flightArrival) ? "—" : string.IsNullOrWhiteSpace(_flightArrivalTime) ? _flightArrival : $"{_flightArrival} ({_flightArrivalTime})";
    public string FlightDepartureDisplay => string.IsNullOrWhiteSpace(_flightDeparture) ? "—" : string.IsNullOrWhiteSpace(_flightDepartureTime) ? _flightDeparture : $"{_flightDeparture} ({_flightDepartureTime})";
    public bool HasNotes => !string.IsNullOrWhiteSpace(_bookingNotes) || !string.IsNullOrWhiteSpace(_specialRequests) || !string.IsNullOrWhiteSpace(_flightArrival) || !string.IsNullOrWhiteSpace(_flightDeparture);

    public void UpdateNotesSummary()
    {
        OnPropertyChanged(nameof(BookingNotesPreview));
        OnPropertyChanged(nameof(InternalNotesPreview));
        OnPropertyChanged(nameof(SpecialRequestsPreview));
        OnPropertyChanged(nameof(PickupDisplay));
        OnPropertyChanged(nameof(DropoffDisplay));
        OnPropertyChanged(nameof(FlightArrivalDisplay));
        OnPropertyChanged(nameof(FlightDepartureDisplay));
        OnPropertyChanged(nameof(HasNotes));
    }

    // ═══ STEP G: CONFIRMATION ═══
    public bool HasGuestName => IsEditMode
        ? !string.IsNullOrWhiteSpace(Modal?.Workspace?.GuestName)
        : !string.IsNullOrWhiteSpace(Modal?.Workspace?.GuestName) || !string.IsNullOrWhiteSpace(SelectedGuest?.FullName);
    public bool HasRoomType => Modal?.Workspace?.SelectedRoomType is not null;
    public bool HasValidDates => Modal?.Workspace?.ArrivalDate is not null && Modal?.Workspace?.DepartureDate is not null && Modal.Workspace.DepartureDate.Value > Modal.Workspace.ArrivalDate.Value;
    public bool HasValidAdults => (Modal?.Workspace?.Adults ?? 0) >= 1;
    public bool HasValidPriceSource => _useRateCode
        ? (Modal?.Workspace?.HasRatePlanRepository == true
            ? Modal.Workspace.SelectedRatePlan is not null
            : !string.IsNullOrWhiteSpace(_rateCode))
        : GetRoomRate() > 0;
    public string GuestNameDisplay => HasGuestName ? (string.IsNullOrWhiteSpace(Modal?.Workspace?.GuestName) ? SelectedGuest!.FullName : Modal!.Workspace!.GuestName) : "—";
    public string RoomTypeDisplay => HasRoomType ? Modal!.Workspace!.SelectedRoomType!.Name : "—";
    public string StaySummary => HasValidDates ? $"{Modal!.Workspace!.ArrivalDate:dd/MM/yyyy} → {Modal!.Workspace!.DepartureDate:dd/MM/yyyy}" : "—";
    public string GuestsSummary => $"{Modal?.Workspace?.Adults ?? 0} người lớn, {Modal?.Workspace?.Children ?? 0} trẻ em";
    public string StepGSourceDisplay => Modal?.Workspace?.Source ?? "—";

    public bool CanCreateReservation => !Modal.IsSavingNew && HasGuestName && HasValidDates && HasValidAdults && (IsEditMode || (HasValidPriceSource && HasRoomType));
    public bool CanFinish => IsEditMode ? HasChanges && HasGuestName && HasValidDates && HasValidAdults : CanCreateReservation;
    public string ReadyMessage => IsEditMode
        ? HasChanges ? "Sẵn sàng lưu thay đổi" : "Thông tin hợp lệ — chưa có thay đổi"
        : "Sẵn sàng tạo đặt phòng";

    private static bool IsEditableStepInEditMode(int index) => index is 0 or 5 or 6;

    private void EnsureGuestState()
    {
        if (Guests.Count == 0 || Guests.Count < Math.Max(1, Modal.Workspace.Adults))
            SyncGuestsFromStepA();
        else
        {
            AttachGuestHandlers();
            SyncSelectedGuestToWorkspace();
        }
    }

    public ObservableCollection<StepGValidationItem> StepGIssues { get; } = new();

    public void RefreshStepG()
    {
        StepGIssues.Clear();
        if (!HasGuestName) StepGIssues.Add(new StepGValidationItem { Label = "Chưa có tên khách", TargetStep = IsEditMode ? 0 : 2 });
        if (!HasValidDates) StepGIssues.Add(new StepGValidationItem { Label = "Ngày lưu trú không hợp lệ", TargetStep = 0 });
        if (!HasValidAdults) StepGIssues.Add(new StepGValidationItem { Label = "Số người lớn phải >= 1", TargetStep = 0 });
        if (!IsEditMode && !HasRoomType) StepGIssues.Add(new StepGValidationItem { Label = "Chưa chọn hạng phòng", TargetStep = 0 });
        if (!IsEditMode && !HasValidPriceSource) StepGIssues.Add(new StepGValidationItem { Label = "Chưa có nguồn giá hợp lệ", TargetStep = 4 });

        OnPropertyChanged(nameof(HasGuestName));
        OnPropertyChanged(nameof(HasRoomType));
        OnPropertyChanged(nameof(HasValidDates));
        OnPropertyChanged(nameof(HasValidAdults));
        OnPropertyChanged(nameof(HasValidPriceSource));
        OnPropertyChanged(nameof(GuestNameDisplay));
        OnPropertyChanged(nameof(RoomTypeDisplay));
        OnPropertyChanged(nameof(StaySummary));
        OnPropertyChanged(nameof(GuestsSummary));
        OnPropertyChanged(nameof(StepGSourceDisplay));
        OnPropertyChanged(nameof(CanCreateReservation));
        OnPropertyChanged(nameof(CanFinish));
        OnPropertyChanged(nameof(ReadyMessage));
        OnPropertyChanged(nameof(StepGStatuses));
    }

    public StepGStatusItem[] StepGStatuses => new[]
    {
        new StepGStatusItem { Label = "A. Thông tin lưu trú", IsComplete = HasValidDates && HasValidAdults },
        new StepGStatusItem { Label = "B. Quét giấy tờ", IsComplete = true, IsOptional = true },
        new StepGStatusItem { Label = "C. Hồ sơ khách", IsComplete = HasGuestName },
        new StepGStatusItem { Label = "D. Thông tin đặt phòng", IsComplete = true, IsOptional = true },
        new StepGStatusItem { Label = "E. Tài chính", IsComplete = true, IsOptional = true },
        new StepGStatusItem { Label = "F. Ghi chú", IsComplete = true, IsOptional = true },
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class WizardStep : INotifyPropertyChanged
{
    public int Index { get; set; }
    public string Letter { get; set; } = "";
    public string Label { get; set; } = "";
    public bool ShowArrow { get; set; }
    private bool _isNavigable = true;
    public bool IsNavigable { get => _isNavigable; set { _isNavigable = value; OnPropertyChanged(); } }
    private string _bg = "#FFFFFF", _brd = "#E2E8F0", _fg = "#64748B", _status = "Chưa bắt đầu", _icon = "", _badgeBg = "#F1F5F9", _badgeFg = "#64748B";
    private Visibility _iconVis = Visibility.Collapsed;
    public string Bg { get => _bg; set { _bg = value; OnPropertyChanged(); } }
    public string Brd { get => _brd; set { _brd = value; OnPropertyChanged(); } }
    public string Fg { get => _fg; set { _fg = value; OnPropertyChanged(); } }
    public string Status { get => _status; set { _status = value; OnPropertyChanged(); } }
    public string Icon { get => _icon; set { _icon = value; OnPropertyChanged(); } }
    public Visibility IconVis { get => _iconVis; set { _iconVis = value; OnPropertyChanged(); } }
    public string BadgeBg { get => _badgeBg; set { _badgeBg = value; OnPropertyChanged(); } }
    public string BadgeFg { get => _badgeFg; set { _badgeFg = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class GuestDocument : INotifyPropertyChanged
{
    public int GuestIndex { get; set; }
    public string Role { get; set; } = "Người lớn";
    private string _docType = "CCCD";
    public string DocumentType { get => _docType; set { _docType = value; OnPropertyChanged(); } }
    private string _fullName = "";
    public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }
    private string _docNumber = "";
    public string DocumentNumber { get => _docNumber; set { _docNumber = value; OnPropertyChanged(); } }
    private string _dateOfBirth = "";
    public string DateOfBirth { get => _dateOfBirth; set { _dateOfBirth = value; OnPropertyChanged(); } }
    private string _gender = "";
    public string Gender { get => _gender; set { _gender = value; OnPropertyChanged(); } }
    private string _nationality = "Việt Nam";
    public string Nationality { get => _nationality; set { _nationality = value; OnPropertyChanged(); } }
    private string _birthPlace = "";
    public string BirthPlace { get => _birthPlace; set { _birthPlace = value; OnPropertyChanged(); } }
    private string _address = "";
    public string PermanentAddress { get => _address; set { _address = value; OnPropertyChanged(); } }
    private string _phone = "";
    public string Phone { get => _phone; set { _phone = value; OnPropertyChanged(); } }
    private string _email = "";
    public string Email { get => _email; set { _email = value; OnPropertyChanged(); } }
    private string _issueDate = "";
    public string IssueDate { get => _issueDate; set { _issueDate = value; OnPropertyChanged(); } }
    private string _issuePlace = "";
    public string IssuePlace { get => _issuePlace; set { _issuePlace = value; OnPropertyChanged(); } }
    private string _expiryDate = "";
    public string ExpiryDate { get => _expiryDate; set { _expiryDate = value; OnPropertyChanged(); } }
    private string _status = "Chưa quét";
    public string ScanStatus { get => _status; set { _status = value; OnPropertyChanged(); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

internal class WizRelayCommand(Action execute, Func<bool> canExecute) : ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? p) => canExecute();
    public void Execute(object? p) => execute();
}

public class StepGValidationItem : INotifyPropertyChanged
{
    public string Label { get; set; } = "";
    public int TargetStep { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}

public class ChangeSummaryItem
{
    public string Label { get; set; } = "";
    public string OldValue { get; set; } = "";
    public string NewValue { get; set; } = "";
    public string Display => $"{Label}: {OldValue} → {NewValue}";
}

public class StepGStatusItem : INotifyPropertyChanged
{
    public string Label { get; set; } = "";
    public bool IsComplete { get; set; }
    public bool IsOptional { get; set; }
    public string Icon => IsComplete ? "✓" : (IsOptional ? "—" : "✗");
    public string IconColor => IsComplete ? "#16A34A" : (IsOptional ? "#94A3B8" : "#DC2626");
    public string FgColor => IsComplete ? "#166534" : (IsOptional ? "#64748B" : "#DC2626");
    public string StatusText => IsComplete ? "Hoàn tất" : (IsOptional ? "Có thể bỏ qua" : "Thiếu dữ liệu");
    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
