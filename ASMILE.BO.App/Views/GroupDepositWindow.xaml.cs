using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.Core.FrontOffice.Group;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

/// <summary>
/// Records group deposits against the persisted group ledger. This window is
/// intentionally small and transactional: it never inserts a UI-only row.
/// </summary>
public sealed class GroupDepositWindow : Window
{
    private readonly Guid _groupId;
    private readonly string _groupCode;
    private readonly string _groupName;
    private readonly IGroupDepositService? _service;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly TextBox _amountBox = new();
    private readonly ComboBox _currencyBox = new();
    private readonly ComboBox _paymentMethodBox = new();
    private readonly DatePicker _businessDatePicker = new();
    private readonly TextBox _referenceBox = new();
    private readonly TextBox _noteBox = new();
    private readonly TextBox _reversalReasonBox = new();
    private readonly DataGrid _grid = new();
    private readonly TextBlock _summaryText = new();
    private readonly TextBlock _statusText = new();
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);

    public GroupDepositWindow(Guid groupId, string groupCode, string groupName)
    {
        _groupId = groupId;
        _groupCode = groupCode;
        _groupName = groupName;
        var applicationServices = (System.Windows.Application.Current as App)?.Services;
        // Deposits are read/write EF operations. Keep this window isolated
        // from the shell and from other group workflows.
        _serviceScope = applicationServices?.CreateScope();
        var services = _serviceScope?.ServiceProvider;
        _service = services?.GetService<IGroupDepositService>();
        _contextAccessor = services?.GetService<IFrontOfficeExecutionContextAccessor>();

        Title = $"Đặt cọc đoàn · {_groupCode}";
        Width = 980;
        Height = 680;
        MinWidth = 820;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResize;
        ShowInTaskbar = false;
        Background = Brush("#F8FAFC");
        Content = BuildContent();
        Loaded += async (_, _) => await LoadAsync();
        Closed += async (_, _) =>
        {
            await _dataOperationGate.WaitAsync();
            _dataOperationGate.Release();
            _dataOperationGate.Dispose();
            _serviceScope?.Dispose();
        };
    }

    private FrameworkElement BuildContent()
    {
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition());
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new Border { Background = Brushes.White, Padding = new Thickness(20, 16, 20, 16), BorderBrush = Brush("#E2E8F0"), BorderThickness = new Thickness(0, 0, 0, 1) };
        header.Child = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = "Đặt cọc đoàn", FontSize = 20, FontWeight = FontWeights.SemiBold, Foreground = Brush("#102E5A") },
                new TextBlock { Text = $"{_groupCode} · {_groupName} · ghi nhận vào sổ đặt cọc dữ liệu thật", FontSize = 12, Foreground = Brush("#64748B"), Margin = new Thickness(0, 4, 0, 0) }
            }
        };
        Grid.SetRow(header, 0);
        root.Children.Add(header);

        var form = new Border { Background = Brushes.White, Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(14, 14, 14, 8), BorderBrush = Brush("#E2E8F0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };
        var formGrid = new Grid();
        for (var i = 0; i < 7; i++) formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = i is 0 or 1 ? new GridLength(1, GridUnitType.Star) : new GridLength(1.2, GridUnitType.Star) });
        AddField(formGrid, 0, "Số tiền *", _amountBox, "0");
        AddField(formGrid, 1, "Currency", _currencyBox);
        AddField(formGrid, 2, "Phương thức *", _paymentMethodBox);
        AddField(formGrid, 3, "Ngày hạch toán *", _businessDatePicker);
        AddField(formGrid, 4, "Mã tham chiếu", _referenceBox, "Số phiếu/ủy nhiệm chi");
        AddField(formGrid, 5, "Ghi chú", _noteBox, "Nội dung đặt cọc");
        ConfigureInput(_amountBox); ConfigureInput(_referenceBox); ConfigureInput(_noteBox);
        _noteBox.Height = 34;
        _currencyBox.ItemsSource = new[] { "VND", "USD", "EUR" }; _currencyBox.SelectedIndex = 0; ConfigureCombo(_currencyBox);
        _paymentMethodBox.ItemsSource = new[] { "Chuyển khoản", "Tiền mặt", "Thẻ", "Khác" }; _paymentMethodBox.SelectedIndex = 0; ConfigureCombo(_paymentMethodBox);
        _businessDatePicker.SelectedDate = DateTime.Today; _businessDatePicker.Height = 34;
        var addButton = new Button { Content = "Ghi nhận đặt cọc", Width = 150, Height = 34, Margin = new Thickness(10, 21, 0, 0), Background = Brush("#2563EB"), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontWeight = FontWeights.SemiBold, Cursor = System.Windows.Input.Cursors.Hand };
        addButton.Click += async (_, _) => await RecordAsync();
        Grid.SetColumn(addButton, 6);
        formGrid.Children.Add(addButton);
        form.Child = formGrid;
        Grid.SetRow(form, 1);
        root.Children.Add(form);

        var listPanel = new Border { Background = Brushes.White, Margin = new Thickness(14, 0, 14, 10), Padding = new Thickness(12), BorderBrush = Brush("#E2E8F0"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(10) };
        var listGrid = new Grid();
        listGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        listGrid.RowDefinitions.Add(new RowDefinition());
        listGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _summaryText.FontSize = 12; _summaryText.Foreground = Brush("#475569"); _summaryText.Margin = new Thickness(0, 0, 0, 8);
        Grid.SetRow(_summaryText, 0); listGrid.Children.Add(_summaryText);
        _grid.AutoGenerateColumns = false; _grid.IsReadOnly = true; _grid.SelectionMode = DataGridSelectionMode.Single; _grid.HeadersVisibility = DataGridHeadersVisibility.Column; _grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal; _grid.RowBackground = Brushes.White; _grid.AlternatingRowBackground = Brush("#F8FAFC");
        AddColumn("Ngày", nameof(GroupDepositDto.BusinessDate), "dd/MM/yyyy", 100);
        AddColumn("Số tiền", nameof(GroupDepositDto.Amount), "N2", 120);
        AddColumn("Currency", nameof(GroupDepositDto.CurrencyCode), null, 80);
        AddColumn("Phương thức", nameof(GroupDepositDto.PaymentMethod), null, 130);
        AddColumn("Tham chiếu", nameof(GroupDepositDto.ExternalReference), null, 150);
        AddColumn("Trạng thái", nameof(GroupDepositDto.Status), null, 100);
        Grid.SetRow(_grid, 1); listGrid.Children.Add(_grid);
        var reversalPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        ConfigureInput(_reversalReasonBox); _reversalReasonBox.Width = 360; _reversalReasonBox.Text = "Hoàn tác theo yêu cầu vận hành";
        reversalPanel.Children.Add(new TextBlock { Text = "Lý do hoàn tác:", FontSize = 11, Foreground = Brush("#64748B"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 8, 0) });
        reversalPanel.Children.Add(_reversalReasonBox);
        var reverseButton = new Button { Content = "Hoàn tác khoản chọn", Height = 34, Margin = new Thickness(10, 0, 0, 0), Padding = new Thickness(14, 0, 14, 0), Background = Brush("#FEF2F2"), Foreground = Brush("#B91C1C"), BorderBrush = Brush("#FECACA"), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand };
        reverseButton.Click += async (_, _) => await ReverseAsync(); reversalPanel.Children.Add(reverseButton);
        Grid.SetRow(reversalPanel, 2); listGrid.Children.Add(reversalPanel);
        listPanel.Child = listGrid; Grid.SetRow(listPanel, 2); root.Children.Add(listPanel);

        var footer = new Border { Background = Brushes.White, Padding = new Thickness(14, 10, 14, 10), BorderBrush = Brush("#E2E8F0"), BorderThickness = new Thickness(0, 1, 0, 0) };
        _statusText.FontSize = 11; _statusText.Foreground = Brush("#64748B"); footer.Child = _statusText; Grid.SetRow(footer, 3); root.Children.Add(footer);
        return root;
    }

    private async Task LoadAsync()
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await LoadCoreAsync();
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task LoadCoreAsync()
    {
        if (_service is null || _contextAccessor?.Current is not { } context)
        {
            _statusText.Text = "Chưa cấu hình GroupDepositService hoặc property context dữ liệu thật.";
            return;
        }
        try
        {
            var rows = await _service.GetAsync(context.Property.PropertyId.Value, _groupId, CancellationToken.None);
            _grid.ItemsSource = rows;
            var posted = rows.Where(x => x.Status == GroupDepositStatus.Posted).Sum(x => x.Amount);
            var reversed = rows.Where(x => x.Status == GroupDepositStatus.Reversed).Sum(x => x.Amount);
            _summaryText.Text = $"Đã ghi: {posted:N2} · Đã hoàn tác: {reversed:N2} · Còn hiệu lực: {posted - reversed:N2}";
            _statusText.Text = $"Đã tải {rows.Count:N0} giao dịch đặt cọc từ database thật.";
        }
        catch (Exception ex) { _statusText.Text = $"Không thể tải đặt cọc thật: {ex.Message}"; }
    }

    private async Task RecordAsync()
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await RecordCoreAsync();
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task RecordCoreAsync()
    {
        if (_service is null || _contextAccessor?.Current is not { }) { _statusText.Text = "Thiếu context dữ liệu thật."; return; }
        if (!TryParseDecimal(_amountBox.Text, out var amount) || amount <= 0m || _businessDatePicker.SelectedDate is not DateTime date)
        { MessageBox.Show("Số tiền và ngày hạch toán phải hợp lệ.", "Đặt cọc đoàn", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        try
        {
            IsEnabled = false;
            await _service.RecordAsync(new RecordGroupDepositRequest(_groupId, amount, _currencyBox.Text, _paymentMethodBox.Text, DateOnly.FromDateTime(date), _referenceBox.Text, _noteBox.Text), CancellationToken.None);
            _amountBox.Clear(); _referenceBox.Clear(); _noteBox.Clear();
            await LoadCoreAsync();
        }
        catch (Exception ex) { MessageBox.Show($"Không thể ghi nhận đặt cọc: {ex.Message}", "Đặt cọc đoàn", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { IsEnabled = true; }
    }

    private async Task ReverseAsync()
    {
        await _dataOperationGate.WaitAsync();
        try
        {
            await ReverseCoreAsync();
        }
        finally
        {
            _dataOperationGate.Release();
        }
    }

    private async Task ReverseCoreAsync()
    {
        if (_grid.SelectedItem is not GroupDepositDto row || row.Status != GroupDepositStatus.Posted) { MessageBox.Show("Hãy chọn một khoản đặt cọc đang còn hiệu lực.", "Đặt cọc đoàn", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        if (MessageBox.Show($"Hoàn tác khoản {row.Amount:N2} {row.CurrencyCode}?", "Xác nhận hoàn tác", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            IsEnabled = false;
            await _service!.ReverseAsync(new ReverseGroupDepositRequest(row.GroupDepositId, row.RowVersion ?? [], string.IsNullOrWhiteSpace(_reversalReasonBox.Text) ? "Hoàn tác theo yêu cầu vận hành" : _reversalReasonBox.Text.Trim()), CancellationToken.None);
            await LoadCoreAsync();
        }
        catch (Exception ex) { MessageBox.Show($"Không thể hoàn tác đặt cọc: {ex.Message}", "Đặt cọc đoàn", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { IsEnabled = true; }
    }

    private void AddColumn(string header, string path, string? format, double width)
    {
        _grid.Columns.Add(new DataGridTextColumn { Header = header, Width = width, Binding = new Binding(path) { StringFormat = format is null ? null : $"{{0:{format}}}" } });
    }

    private static void AddField(Grid grid, int column, string label, Control control, string? placeholder = null)
    {
        var panel = new StackPanel { Margin = new Thickness(column == 0 ? 0 : 5, 0, 5, 0) };
        panel.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold, Foreground = Brush("#475569"), Margin = new Thickness(0, 0, 0, 4) });
        if (placeholder is not null && control is TextBox textBox) textBox.ToolTip = placeholder;
        panel.Children.Add(control); Grid.SetColumn(panel, column); grid.Children.Add(panel);
    }

    private static void ConfigureInput(TextBox box)
    {
        box.Height = 34; box.Padding = new Thickness(9, 0, 9, 0); box.VerticalContentAlignment = VerticalAlignment.Center; box.FontSize = 12; box.Foreground = Brush("#17365D"); box.Background = Brushes.White; box.BorderBrush = Brush("#D7E2F0"); box.BorderThickness = new Thickness(1);
    }

    private static void ConfigureCombo(ComboBox box) { box.Height = 34; box.FontSize = 12; box.Foreground = Brush("#17365D"); box.Background = Brushes.White; box.BorderBrush = Brush("#D7E2F0"); }
    private static bool TryParseDecimal(string value, out decimal result) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out result) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));
}
