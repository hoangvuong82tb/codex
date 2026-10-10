using System.Windows;
using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Payment;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class CashierReceiptWindow : Window
{
    private readonly IServiceScope? _dataScope;
    private readonly HotelFolioApplicationService? _folioService;
    private readonly IHotelPaymentRepository? _paymentRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly List<ReceiptRow> _rows = new();

    public CashierReceiptWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var rootServices = (System.Windows.Application.Current as App)?.Services;
        _dataScope = rootServices?.CreateScope();
        var services = _dataScope?.ServiceProvider;
        _folioService = services?.GetService<HotelFolioApplicationService>();
        _paymentRepository = services?.GetService<IHotelPaymentRepository>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();
        ReceiptGrid.ItemsSource = _rows;
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _dataScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e) => await LoadRealAsync();

    private async Task LoadRealAsync()
    {
        if (_folioService is null || _paymentRepository is null || _contextAccessor?.Current is not { } context)
        {
            _rows.Clear();
            ReceiptGrid.ItemsSource = _rows;
            return;
        }

        try
        {
            var result = await _folioService.SearchAsync(
                new SearchFoliosQuery(new FolioSearchFilter(context.Property.PropertyId.Value, null, null, null, null, null, null, null), new PageRequest(1, 500)),
                CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                _rows.Clear();
                ReceiptGrid.ItemsSource = _rows;
                return;
            }

            var rows = new List<ReceiptRow>();
            foreach (var folio in result.Value.Items)
            {
                var payments = await _paymentRepository.GetForFolioAsync(context.Property.PropertyId.Value, folio.HotelFolioId, CancellationToken.None);
                rows.AddRange(payments.Select(payment => new ReceiptRow(
                    payment.PaymentNumber,
                    payment.CreatedAtUtc.ToLocalTime(),
                    folio.GuestName,
                    folio.FolioNumber,
                    payment.Amount,
                    payment.Currency.ToString(),
                    payment.PaymentMethod.ToString(),
                    payment.Status.ToString())));
            }

            _rows.Clear();
            _rows.AddRange(rows.OrderByDescending(x => x.CreatedAt));
            ApplyFilter();
        }
        catch (Exception ex)
        {
            _rows.Clear();
            ReceiptGrid.ItemsSource = _rows;
            MessageBox.Show("Không tải được phiếu thu FO thật: " + ex.Message, "Phiếu thu FO", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ApplyFilter()
    {
        var keyword = SearchBox.Text?.Trim();
        var filtered = string.IsNullOrWhiteSpace(keyword)
            ? _rows.ToList()
            : _rows.Where(x => x.Display.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
        ReceiptGrid.ItemsSource = filtered;
        ReceiptFooterText.Text = filtered.Count == 0
            ? "Chưa có phiếu thu FO trong dữ liệu thật."
            : $"Hiển thị {filtered.Count:N0} phiếu thu FO từ dữ liệu thật.";
    }

    private void OnSearchChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();
    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadRealAsync();
    private async void OnCreate(object sender, RoutedEventArgs e)
    {
        var existing = System.Windows.Application.Current.Windows.OfType<CreateReceiptVoucherWindow>().FirstOrDefault(w => w.IsLoaded);
        if (existing is not null) { existing.Activate(); return; }

        var dialog = new CreateReceiptVoucherWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        if (dialog.ShowDialog() == true)
        {
            await LoadRealAsync();
        }
    }
    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

internal sealed record ReceiptRow(string Number, DateTime CreatedAt, string Guest, string Folio, decimal Amount, string Currency, string Method, string Status)
{
    public string Display => $"{Number} · {CreatedAt:dd/MM/yyyy HH:mm} · {Guest} · {Folio} · {Amount:N0} {Currency} · {Method} · {Status}";
}
