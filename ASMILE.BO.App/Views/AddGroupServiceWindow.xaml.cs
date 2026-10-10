using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ASMILE.BO.Application.FrontOffice.Group;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Core.FrontOffice.Group;

namespace ASMILE.BO.App.Views;

public partial class AddGroupServiceWindow
{
    private readonly IGroupServiceOrderCommandService _commandService;
    private readonly GroupServiceDto? _source;

    public AddGroupServiceWindow(
        IGroupServiceOrderCommandService commandService,
        IReadOnlyList<GroupServiceGroupOption> groups,
        GroupServiceDto? source = null)
    {
        _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
        _source = source;
        InitializeComponent();
        GroupCombo.ItemsSource = groups;
        ServiceDatePicker.SelectedDate = DateTime.Today;
        if (source is not null)
            PopulateForEdit(source, groups);
    }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (GroupCombo.SelectedItem is not GroupServiceGroupOption group)
        {
            ShowValidation("Vui lòng chọn đoàn từ dữ liệu thật.");
            return;
        }

        if (string.IsNullOrWhiteSpace(ServiceNameTextBox.Text))
        {
            ShowValidation("Vui lòng nhập tên dịch vụ.");
            return;
        }

        var selectedTime = (ServiceTimeCombo.SelectedItem as ComboBoxItem)?.Content ?? ServiceTimeCombo.Text;
        if (ServiceDatePicker.SelectedDate is not DateTime serviceDate ||
            !TryParseTime(selectedTime, out var serviceTime) ||
            !TryParseInt(DurationTextBox.Text, out var duration) ||
            !TryParseInt(QuantityTextBox.Text, out var quantity) ||
            !TryParseDecimal(UnitPriceTextBox.Text, out var unitPrice) ||
            !TryParsePercent(DiscountTextBox.Text, out var discount) ||
            !TryParsePercent(TaxRateTextBox.Text, out var tax) ||
            !TryParsePercent(ServiceChargeRateTextBox.Text, out var serviceCharge))
        {
            ShowValidation("Ngày, giờ, số lượng, thời lượng và đơn giá phải hợp lệ.");
            return;
        }

        try
        {
            IsEnabled = false;
            if (_source is null)
            {
                await _commandService.CreateAsync(
                    new CreateGroupServiceOrderRequest(
                        group.GroupReservationId,
                        ServiceNameTextBox.Text.Trim(),
                        CategoryCode(CategoryCombo.Text),
                        null,
                        DepartmentCombo.Text,
                        ProviderCombo.Text,
                        LocationTextBox.Text.Trim(),
                        DateOnly.FromDateTime(serviceDate),
                        serviceTime,
                        duration,
                        quantity,
                        unitPrice,
                        discount,
                        tax,
                        serviceCharge,
                        ChargeTo(),
                        null,
                        RemarkTextBox.Text.Trim()),
                    CancellationToken.None);
            }
            else
            {
                await _commandService.UpdateAsync(
                    new UpdateGroupServiceOrderRequest(
                        _source.ServiceId,
                        ServiceNameTextBox.Text.Trim(),
                        CategoryCode(CategoryCombo.Text),
                        null,
                        DepartmentCombo.Text,
                        ProviderCombo.Text,
                        LocationTextBox.Text.Trim(),
                        DateOnly.FromDateTime(serviceDate),
                        serviceTime,
                        duration,
                        quantity,
                        unitPrice,
                        discount,
                        tax,
                        serviceCharge,
                        ChargeTo(),
                        _source.GuestName,
                        RemarkTextBox.Text.Trim(),
                        _source.RowVersion),
                    CancellationToken.None);
            }
            DialogResult = true;
        }
        catch (Exception ex)
        {
            IsEnabled = true;
            MessageBox.Show($"Không thể lưu dịch vụ: {ex.Message}", "Dịch vụ đoàn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => Close();

    private void PopulateForEdit(GroupServiceDto source, IReadOnlyList<GroupServiceGroupOption> groups)
    {
        Title = "Sửa dịch vụ đoàn";
        GroupCombo.IsEnabled = false;
        GroupCombo.SelectedItem = groups.FirstOrDefault(x => x.GroupReservationId == source.GroupReservationId);
        CategoryCombo.SelectedIndex = CategoryIndex(source.Category);
        ServiceNameTextBox.Text = source.ServiceName;
        DepartmentCombo.Text = source.Department;
        ProviderCombo.Text = source.Provider;
        LocationTextBox.Text = source.Location;
        ServiceDatePicker.SelectedDate = source.ServiceDate.ToDateTime(TimeOnly.MinValue);
        ServiceTimeCombo.Text = source.ServiceTime.ToString("HH:mm", CultureInfo.InvariantCulture);
        DurationTextBox.Text = source.DurationMinutes.ToString(CultureInfo.InvariantCulture);
        QuantityTextBox.Text = source.Quantity.ToString(CultureInfo.InvariantCulture);
        UnitPriceTextBox.Text = source.UnitPrice.ToString(CultureInfo.CurrentCulture);
        DiscountTextBox.Text = (source.Discount * 100m).ToString(CultureInfo.CurrentCulture);
        TaxRateTextBox.Text = (source.TaxRate * 100m).ToString(CultureInfo.CurrentCulture);
        ServiceChargeRateTextBox.Text = (source.ServiceChargeRate * 100m).ToString(CultureInfo.CurrentCulture);
        MasterFolioRadio.IsChecked = source.ChargeTo == "MasterFolio";
        GuestFolioRadio.IsChecked = source.ChargeTo == "GuestFolio";
        RoomRadio.IsChecked = source.ChargeTo == "Room";
        CompanyRadio.IsChecked = source.ChargeTo == "Company";
        AgentRadio.IsChecked = source.ChargeTo == "Agent";
        RemarkTextBox.Text = source.Remark ?? string.Empty;
    }

    private static int CategoryIndex(string category) => category switch
    {
        "Restaurant" => 0, "Meeting" => 1, "Transport" => 2, "Spa" => 3,
        "Laundry" => 4, "Banquet" => 5, "Equipment" => 6, _ => 7
    };

    private string ChargeTo() => GuestFolioRadio.IsChecked == true ? "GuestFolio"
        : RoomRadio.IsChecked == true ? "Room"
        : CompanyRadio.IsChecked == true ? "Company"
        : AgentRadio.IsChecked == true ? "Agent"
        : "MasterFolio";

    private static string CategoryCode(string value) => value switch
    {
        "Nhà hàng" => "Restaurant",
        "Hội họp" => "Meeting",
        "Vận chuyển" => "Transport",
        "Spa" => "Spa",
        "Giặt ủi" => "Laundry",
        "Tiệc" => "Banquet",
        "Thiết bị" => "Equipment",
        _ => "Other"
    };

    private static bool TryParseInt(string value, out int result) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) && result > 0;

    private static bool TryParseDecimal(string value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out result) ||
               decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParsePercent(string value, out decimal result)
    {
        if (!TryParseDecimal(value, out var percent) || percent < 0 || percent > 100)
        {
            result = 0;
            return false;
        }

        result = percent / 100m;
        return true;
    }

    private static bool TryParseTime(object? value, out TimeOnly result)
    {
        var text = value?.ToString();
        return TimeOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static void ShowValidation(string message) => MessageBox.Show(message, "Dịch vụ đoàn", MessageBoxButton.OK, MessageBoxImage.Warning);
}
