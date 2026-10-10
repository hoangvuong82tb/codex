using System.Windows;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class InvoiceProviderConfigurationWindow : Window
{
    private readonly IEInvoiceService? _service;
    private readonly IServiceScope? _serviceScope;
    private IReadOnlyList<InvoiceProviderConfigurationDto> _configurations = [];
    private InvoiceProviderConfigurationDto? _selected;
    private Guid? _editingId;

    public InvoiceProviderConfigurationWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        _service = (_serviceScope?.ServiceProvider ?? services)?.GetService<IEInvoiceService>();
        ProviderCodeBox.ItemsSource = new[] { "MISA", "VNPT", "VIETTEL" };
        ProviderCodeBox.SelectedIndex = 0;
        Loaded += async (_, _) => await LoadAsync();
        Closed += (_, _) => _serviceScope?.Dispose();
    }

    private async Task LoadAsync()
    {
        if (_service is null)
        {
            StatusText.Text = "Chưa có e-invoice service trong phiên hiện tại.";
            return;
        }

        try
        {
            _configurations = await _service.GetProviderConfigurationsAsync(CancellationToken.None);
            ProviderList.ItemsSource = _configurations;
            var readiness = await _service.GetProviderReadinessAsync(CancellationToken.None);
            StatusText.Text = readiness.IsReady
                ? $"✓ {readiness.Message}"
                : $"⚠ {readiness.Message}";
            if (_configurations.Count > 0)
            {
                ProviderList.SelectedIndex = 0;
            }
            else
            {
                ClearForm();
                StatusText.Text = $"⚠ {readiness.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Không thể tải cấu hình provider: " + ex.Message;
        }
    }

    private void OnSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ProviderList.SelectedItem is not InvoiceProviderConfigurationDto configuration) return;
        _selected = configuration;
        _editingId = configuration.Id;
        ProviderCodeBox.SelectedItem = configuration.ProviderCode;
        DisplayNameBox.Text = configuration.DisplayName;
        EndpointBox.Text = configuration.EndpointUrl;
        TenantBox.Text = configuration.TenantCode ?? string.Empty;
        UsernameBox.Text = configuration.Username ?? string.Empty;
        SecretBox.Clear();
        ActiveBox.IsChecked = configuration.IsActive;
        DefaultBox.IsChecked = configuration.IsDefault;
        ModeText.Text = configuration.HasSecret
            ? "Đang chỉnh sửa · secret hiện tại được bảo vệ và không hiển thị"
            : "Đang chỉnh sửa · chưa có secret được lưu";
        StatusText.Text = string.Empty;
    }

    private void OnNew(object sender, RoutedEventArgs e)
    {
        _selected = null;
        _editingId = null;
        ProviderList.SelectedItem = null;
        ClearForm();
    }

    private void ClearForm()
    {
        ProviderCodeBox.SelectedIndex = 0;
        DisplayNameBox.Text = "MISA";
        EndpointBox.Text = string.Empty;
        TenantBox.Text = string.Empty;
        UsernameBox.Text = string.Empty;
        SecretBox.Clear();
        ActiveBox.IsChecked = true;
        DefaultBox.IsChecked = _configurations.Count == 0;
        ModeText.Text = "Cấu hình mới";
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        if (_service is null)
        {
            StatusText.Text = "Chưa có e-invoice service trong phiên hiện tại.";
            return;
        }

        try
        {
            var provider = ProviderCodeBox.SelectedItem?.ToString() ?? string.Empty;
            var id = await _service.SaveProviderConfigurationAsync(new InvoiceProviderConfigurationRequestDto
            {
                Id = _editingId,
                ProviderCode = provider,
                DisplayName = DisplayNameBox.Text,
                EndpointUrl = EndpointBox.Text,
                TenantCode = TenantBox.Text,
                Username = UsernameBox.Text,
                Secret = string.IsNullOrWhiteSpace(SecretBox.Password) ? null : SecretBox.Password,
                IsActive = ActiveBox.IsChecked == true,
                IsDefault = DefaultBox.IsChecked == true
            }, CancellationToken.None);
            StatusText.Text = $"Đã lưu cấu hình {provider} ({id:N}). Adapter/API thật vẫn được kiểm tra riêng khi phát hành.";
            await LoadAsync();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Không thể lưu cấu hình provider: " + ex.Message;
        }
    }

    private async void OnRefresh(object sender, RoutedEventArgs e) => await LoadAsync();

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
