using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Payment;
using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Payment;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

/// <summary>Creates a real payment on an existing Folio.</summary>
public partial class CreateReceiptVoucherWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private int _step = 1;
    private readonly List<ReceiptFolioOption> _folios = [];
    private ReceiptFolioOption? _selectedFolio;
    private readonly IHotelFolioRepository? _folioRepository;
    private readonly HotelPaymentApplicationService? _paymentService;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private ComboBox? _folioBox;
    private TextBox? _payerNameBox, _payerCompanyBox, _amountBox, _referenceBox, _notesBox;
    private ComboBox? _methodBox, _currencyBox;
    private TextBlock? _stateText;
    private string _payerName = "", _payerCompany = "", _amountText = "", _reference = "", _notes = "";
    private bool _isSaving;

    public CreateReceiptVoucherWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioRepository = services?.GetService<IHotelFolioRepository>();
        _paymentService = services?.GetService<HotelPaymentApplicationService>();
        _executionContext = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        Loaded += async (_, _) => await LoadFoliosAsync();
        Closed += (_, _) => _dataScope?.Dispose();
        GoTo(1);
    }

    private async Task LoadFoliosAsync()
    {
        if (_folioRepository is null || _executionContext?.Current is null)
        {
            SetState("Chưa có property context hoặc dịch vụ Folio thật. Không thể lập phiếu thu.", false);
            return;
        }

        try
        {
            var propertyId = _executionContext.Current.Property.PropertyId.Value;
            var page = await _folioRepository.SearchAsync(
                new FolioSearchFilter(propertyId, null, null, null, null, null, null, null),
                new PageRequest(1, 500), CancellationToken.None);
            _folios.Clear();
            _folios.AddRange(page.Items.Select(MapFolio));
            if (_folioBox is not null)
            {
                _folioBox.ItemsSource = _folios;
                _folioBox.IsEnabled = _folios.Count > 0;
            }
            SetState(_folios.Count == 0 ? "Không có Folio thật để chọn trong property hiện tại." : $"Đã tải {_folios.Count:N0} Folio thật từ cơ sở dữ liệu.", _folios.Count > 0);
        }
        catch (Exception ex)
        {
            _folios.Clear();
            if (_folioBox is not null) _folioBox.ItemsSource = null;
            SetState($"Không tải được Folio thật: {ex.Message}", false);
        }
    }

    private static ReceiptFolioOption MapFolio(FolioLedgerDto ledger) => new(
        ledger.HotelFolioId, ledger.FolioNumber, ledger.GuestName, ledger.RoomNumber,
        ledger.RunningBalance, ledger.RowVersion,
        $"{ledger.FolioNumber} · {ledger.GuestName} · Phòng {ledger.RoomNumber} · Dư {ledger.RunningBalance:N0}");

    private void GoTo(int step)
    {
        _step = Math.Clamp(step, 1, 6);
        HideAllSteps(); BuildStepBar();
        switch (_step)
        {
            case 1: BuildStep1(); Step1.Visibility = Visibility.Visible; break;
            case 2: BuildStep2(); Step2.Visibility = Visibility.Visible; break;
            case 3: BuildStep3(); Step3.Visibility = Visibility.Visible; break;
            case 4: BuildStep4(); Step4.Visibility = Visibility.Visible; break;
            case 5: BuildStep5(); Step5.Visibility = Visibility.Visible; break;
            case 6: BuildStep6(); Step6.Visibility = Visibility.Visible; break;
        }
        UpdateNav(); UpdateFooter();
    }

    private void HideAllSteps() => Step1.Visibility = Step2.Visibility = Step3.Visibility = Step4.Visibility = Step5.Visibility = Step6.Visibility = Visibility.Collapsed;

    private void BuildStep1()
    {
        var panel = ContentPanel("A — Chọn nguồn thu", "Chọn một Folio đang tồn tại trong dữ liệu thật để ghi nhận khoản thu.");
        panel.Children.Add(Label("Folio thật"));
        _folioBox = new ComboBox { Style = FindResource("ModernComboBox") as Style, Width = 620, IsEnabled = _folios.Count > 0, ItemsSource = _folios, DisplayMemberPath = nameof(ReceiptFolioOption.Display), SelectedItem = _selectedFolio };
        _folioBox.SelectionChanged += OnFolioChanged;
        panel.Children.Add(_folioBox);
        _stateText = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 12, 0, 0) };
        panel.Children.Add(_stateText);
        Step1.Child = panel;
        SetState(_folios.Count == 0 ? "Đang tải Folio thật hoặc chưa có dữ liệu." : $"Đã tải {_folios.Count:N0} Folio thật.", _folios.Count > 0);
    }

    private void BuildStep2()
    {
        var panel = ContentPanel("B — Người nộp tiền", "Thông tin người nộp được lưu trong diễn giải payment; khoản thu vẫn gắn với Folio đã chọn.");
        panel.Children.Add(Input("Tên người nộp", _payerName, out _payerNameBox));
        panel.Children.Add(Input("Công ty / tổ chức (không bắt buộc)", _payerCompany, out _payerCompanyBox));
        Step2.Child = panel;
    }

    private void BuildStep3()
    {
        var panel = ContentPanel("C — Số tiền & phương thức", "Số tiền sẽ được ghi vào Folio và tạo HotelPayment thật trong cùng transaction.");
        panel.Children.Add(Input("Số tiền", _amountText, out _amountBox, 220));
        panel.Children.Add(Label("Phương thức"));
        _methodBox = new ComboBox { Style = FindResource("ModernComboBox") as Style, Width = 220, ItemsSource = new[] { "Tiền mặt", "Thẻ", "QR", "Chuyển khoản", "Ví điện tử", "Công ty", "Đại lý", "Voucher" }, SelectedIndex = 0 };
        panel.Children.Add(_methodBox);
        panel.Children.Add(Label("Tiền tệ"));
        _currencyBox = new ComboBox { Style = FindResource("ModernComboBox") as Style, Width = 140, ItemsSource = Enum.GetNames<PaymentCurrency>(), SelectedItem = PaymentCurrency.VND.ToString() };
        panel.Children.Add(_currencyBox);
        panel.Children.Add(Input("Mã tham chiếu (không bắt buộc)", _reference, out _referenceBox, 320));
        panel.Children.Add(Input("Ghi chú (không bắt buộc)", _notes, out _notesBox, 520));
        Step3.Child = panel;
    }

    private void BuildStep4()
    {
        var panel = ContentPanel("D — Phân bổ", "Payment thật sẽ được ghi trực tiếp vào Folio đã chọn.");
        panel.Children.Add(Info("Phạm vi hiện tại", "Backend hiện chưa có bước phân bổ thủ công theo từng FolioEntry cho phiếu thu này. Không tạo Folio đích, không tạo khoản mục giả và không đánh dấu phân bổ hoàn tất."));
        panel.Children.Add(Row("Folio đích", _selectedFolio?.FolioNumber ?? "Chưa chọn"));
        panel.Children.Add(Row("Cách ghi", "ReceiveAsync → Folio payment entry + HotelPayment"));
        Step4.Child = panel;
    }

    private void BuildStep5()
    {
        var panel = ContentPanel("E — Kiểm tra", "Kiểm tra dữ liệu trước khi chuyển sang bước ghi DB.");
        panel.Children.Add(CheckRow("Folio thật đã chọn", _selectedFolio is not null));
        panel.Children.Add(CheckRow("RowVersion hợp lệ", _selectedFolio?.RowVersion is { Length: 8 }));
        panel.Children.Add(CheckRow("Số tiền dương", TryGetAmount(out _)));
        panel.Children.Add(CheckRow("Phương thức được hỗ trợ", TryMapPaymentMethod(_methodBox?.SelectedItem?.ToString(), out _)));
        panel.Children.Add(CheckRow("Currency hợp lệ", Enum.TryParse<PaymentCurrency>(_currencyBox?.SelectedItem?.ToString(), out _)));
        panel.Children.Add(Info("Lưu ý", "Chỉ nút “Lập phiếu thu” ở bước cuối mới ghi dữ liệu. Nếu transaction lỗi hoặc xung đột RowVersion, màn hình sẽ giữ nguyên."));
        Step5.Child = panel;
    }

    private void BuildStep6()
    {
        var panel = ContentPanel("F — Xác nhận", "Xác nhận để tạo payment thật trên Folio. Đây không phải thao tác mock.");
        panel.Children.Add(Row("Folio", _selectedFolio?.Display ?? "Chưa chọn"));
        panel.Children.Add(Row("Người nộp", string.IsNullOrWhiteSpace(_payerName) ? "Chưa nhập" : _payerName));
        panel.Children.Add(Row("Số tiền", TryGetAmount(out var amount) ? $"{amount:N0}" : "Không hợp lệ"));
        panel.Children.Add(Row("Phương thức", _methodBox?.SelectedItem?.ToString() ?? "Chưa chọn"));
        panel.Children.Add(Row("Tham chiếu", string.IsNullOrWhiteSpace(_reference) ? "—" : _reference));
        _stateText = new TextBlock { FontSize = 12, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 14, 0, 0) };
        panel.Children.Add(_stateText); Step6.Child = panel;
        SetState("Sẵn sàng ghi payment thật nếu tất cả điều kiện hợp lệ.", true);
    }

    private StackPanel ContentPanel(string title, string subtitle)
    {
        var panel = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, Width = 820 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = C("#102E5A") });
        panel.Children.Add(new TextBlock { Text = subtitle, FontSize = 14, Foreground = C("#64748B"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 18) });
        return panel;
    }

    private StackPanel Input(string label, string value, out TextBox box, double width = 520)
    {
        var panel = new StackPanel(); panel.Children.Add(Label(label));
        box = new TextBox { Style = FindResource("ModernTextBox") as Style, Width = width, Text = value, HorizontalAlignment = HorizontalAlignment.Left };
        box.TextChanged += (_, _) => CaptureInputs(); panel.Children.Add(box); return panel;
    }

    private static TextBlock Label(string text) => new() { Text = text, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = C("#475569"), Margin = new Thickness(0, 8, 0, 5) };
    private static Border Info(string title, string message) => new() { Background = Cb("#EFF6FF"), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 12, 0, 0), Child = new StackPanel { Children = { new TextBlock { Text = title, FontWeight = FontWeights.SemiBold, Foreground = C("#1D4ED8") }, new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = C("#475569"), Margin = new Thickness(0, 4, 0, 0) } } } };
    private static TextBlock Row(string label, string value) { var tb = new TextBlock { FontSize = 13, Foreground = C("#64748B"), Margin = new Thickness(0, 4, 0, 0) }; tb.Inlines.Add(new System.Windows.Documents.Run(label + ": ") { Foreground = C("#94A3B8") }); tb.Inlines.Add(new System.Windows.Documents.Run(value) { Foreground = C("#17365D"), FontWeight = FontWeights.SemiBold }); return tb; }
    private static StackPanel CheckRow(string label, bool ok) => new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 0), Children = { new TextBlock { Text = ok ? "✓ " : "○ ", Foreground = ok ? C("#16A34A") : C("#DC2626"), FontSize = 13 }, new TextBlock { Text = label, Foreground = C("#334155"), FontSize = 12 } } };

    private void BuildStepBar()
    {
        StepBar.Children.Clear(); var steps = new[] { ("A", "Nguồn thu"), ("B", "Người nộp"), ("C", "Số tiền & PT"), ("D", "Phân bổ"), ("E", "Kiểm tra"), ("F", "Xác nhận") };
        for (var i = 0; i < steps.Length; i++) { var index = i + 1; var active = index == _step; var border = new Border { CornerRadius = new CornerRadius(8), Padding = new Thickness(10, 7, 10, 7), Margin = new Thickness(2, 5, 2, 5), Cursor = System.Windows.Input.Cursors.Hand, Background = active ? Cb("#EFF6FF") : Cb("Transparent"), BorderBrush = active ? Cb("#2F6BFF") : Cb("Transparent"), BorderThickness = new Thickness(1), Child = new TextBlock { Text = $"{steps[i].Item1} {steps[i].Item2}", FontSize = 12, Foreground = active ? C("#1E40AF") : C("#64748B") } }; border.MouseLeftButtonDown += (_, _) => { if (index <= _step || CanAdvance()) GoTo(index); }; StepBar.Children.Add(border); if (i < steps.Length - 1) StepBar.Children.Add(new TextBlock { Text = " › ", FontSize = 12, Foreground = C("#CBD5E1"), VerticalAlignment = VerticalAlignment.Center }); }
    }

    private void UpdateNav() { BtnBack.IsEnabled = _step > 1 && !_isSaving; BtnNext.Visibility = _step == 6 ? Visibility.Collapsed : Visibility.Visible; BtnCreate.Visibility = _step == 6 ? Visibility.Visible : Visibility.Collapsed; BtnCreate.IsEnabled = _step == 6 && CanAdvance(false) && !_isSaving; }
    private void UpdateFooter() => FooterLabel.Text = $"Bước {_step}/6 · {new[] { "Nguồn thu", "Người nộp", "Số tiền & phương thức", "Phân bổ", "Kiểm tra", "Xác nhận" }[_step - 1]}";

    private bool CanAdvance(bool showMessage = true)
    {
        CaptureInputs(); var valid = _step switch { 1 => _selectedFolio is not null, 2 => true, 3 => TryGetAmount(out _) && TryMapPaymentMethod(_methodBox?.SelectedItem?.ToString(), out _) && Enum.TryParse<PaymentCurrency>(_currencyBox?.SelectedItem?.ToString(), out _), 4 or 5 => _selectedFolio is not null && TryGetAmount(out _), _ => true };
        if (!valid && showMessage) MessageBox.Show("Vui lòng hoàn thiện dữ liệu thật của bước hiện tại trước khi tiếp tục.", "Phiếu thu", MessageBoxButton.OK, MessageBoxImage.Warning); return valid;
    }

    private void OnNext(object sender, RoutedEventArgs e) { if (_step < 6 && CanAdvance()) GoTo(_step + 1); }
    private void OnBack(object sender, RoutedEventArgs e) { if (_step > 1) GoTo(_step - 1); }
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private void OnFolioChanged(object sender, SelectionChangedEventArgs e) { _selectedFolio = _folioBox?.SelectedItem as ReceiptFolioOption; if (!string.IsNullOrWhiteSpace(_selectedFolio?.GuestName) && string.IsNullOrWhiteSpace(_payerName)) _payerName = _selectedFolio.GuestName; if (_selectedFolio is not null && string.IsNullOrWhiteSpace(_amountText) && _selectedFolio.Balance > 0) _amountText = _selectedFolio.Balance.ToString("0.##", CultureInfo.InvariantCulture); UpdateNav(); }
    private void CaptureInputs() { if (_payerNameBox is not null) _payerName = _payerNameBox.Text.Trim(); if (_payerCompanyBox is not null) _payerCompany = _payerCompanyBox.Text.Trim(); if (_amountBox is not null) _amountText = _amountBox.Text.Trim(); if (_referenceBox is not null) _reference = _referenceBox.Text.Trim(); if (_notesBox is not null) _notes = _notesBox.Text.Trim(); }
    private bool TryGetAmount(out decimal amount) { CaptureInputs(); return (decimal.TryParse(_amountText, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out amount) || decimal.TryParse(_amountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount)) && amount > 0; }

    private static bool TryMapPaymentMethod(string? text, out HotelPaymentMethod method) { method = text switch { "Tiền mặt" => HotelPaymentMethod.Cash, "Thẻ" => HotelPaymentMethod.Card, "QR" => HotelPaymentMethod.QR, "Chuyển khoản" => HotelPaymentMethod.BankTransfer, "Ví điện tử" => HotelPaymentMethod.EWallet, "Công ty" => HotelPaymentMethod.CorporateBilling, "Đại lý" => HotelPaymentMethod.CityLedger, "Voucher" => HotelPaymentMethod.Voucher, _ => default }; return text is "Tiền mặt" or "Thẻ" or "QR" or "Chuyển khoản" or "Ví điện tử" or "Công ty" or "Đại lý" or "Voucher"; }

    private async void OnCreate(object sender, RoutedEventArgs e)
    {
        if (!CanAdvance()) return;
        if (_selectedFolio is null || _paymentService is null || _executionContext?.Current is null || _selectedFolio.RowVersion is not { Length: 8 } || !TryGetAmount(out var amount) || !TryMapPaymentMethod(_methodBox?.SelectedItem?.ToString(), out var method) || !Enum.TryParse<PaymentCurrency>(_currencyBox?.SelectedItem?.ToString(), out var currency)) { SetState("Thiếu dữ liệu thật hoặc RowVersion đã hết hạn. Vui lòng tải lại.", false); return; }
        _isSaving = true; UpdateNav();
        try
        {
            var result = await _paymentService.ReceiveAsync(new ReceivePaymentCommand(_selectedFolio.FolioId, _selectedFolio.RowVersion, method, amount, currency, 1, NullIfEmpty(_reference), BuildNotes()), CancellationToken.None);
            if (result.IsFailure) { SetState(result.Error?.Message ?? "Không thể ghi nhận payment thật.", false); MessageBox.Show(result.Error?.Message ?? "Không thể ghi nhận payment thật.", "Phiếu thu", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            MessageBox.Show($"Đã ghi nhận payment {result.Value!.PaymentNumber} cho Folio {_selectedFolio.FolioNumber}.", "Phiếu thu", MessageBoxButton.OK, MessageBoxImage.Information); DialogResult = true; Close();
        }
        catch (Exception ex) { SetState($"Không ghi được dữ liệu: {ex.Message}", false); MessageBox.Show(ex.Message, "Phiếu thu", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { _isSaving = false; UpdateNav(); }
    }

    private string? BuildNotes() => string.IsNullOrWhiteSpace(_payerName) && string.IsNullOrWhiteSpace(_payerCompany) ? NullIfEmpty(_notes) : $"Người nộp: {_payerName}{(string.IsNullOrWhiteSpace(_payerCompany) ? "" : $" · Công ty: {_payerCompany}")}{(string.IsNullOrWhiteSpace(_notes) ? "" : $" · {_notes}")}";
    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
    private void SetState(string message, bool ok) { if (_stateText is null) return; _stateText.Text = message; _stateText.Foreground = ok ? C("#16A34A") : C("#DC2626"); }
    private static SolidColorBrush C(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
    private static SolidColorBrush Cb(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}

internal sealed record ReceiptFolioOption(Guid FolioId, string FolioNumber, string GuestName, string RoomNumber, decimal Balance, byte[]? RowVersion, string Display);
