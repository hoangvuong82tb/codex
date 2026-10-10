using System.Windows;
using ASMILE.BO.Application.FrontOffice.ShiftLog;
using ASMILE.BO.Core.FrontOffice.Incident;

namespace ASMILE.BO.App.Views;

public partial class CreateShiftLogWindow : Window
{
    public CreateShiftLogEntryRequest? Request { get; private set; }

    public CreateShiftLogWindow()
    {
        InitializeComponent();
        ShiftBox.ItemsSource = new[] { "Ca sáng", "Ca chiều", "Ca đêm" };
        CategoryBox.ItemsSource = new[] { "Bàn giao", "Theo dõi", "Reservation", "Check-in", "Check-out", "Housekeeping", "Cashiering", "Guest Service", "Khác" };
        PriorityBox.ItemsSource = Enum.GetValues<IncidentPriority>();
        ShiftBox.SelectedIndex = DateTime.Now.Hour < 12 ? 0 : DateTime.Now.Hour < 18 ? 1 : 2;
        CategoryBox.SelectedIndex = 0;
        PriorityBox.SelectedItem = IncidentPriority.Medium;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text) || CategoryBox.SelectedItem is not string category ||
            ShiftBox.SelectedItem is not string shift || PriorityBox.SelectedItem is not IncidentPriority priority ||
            string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            MessageBox.Show("Vui lòng nhập đủ ca, tiêu đề, danh mục, ưu tiên và mô tả.", "Tạo nhật ký ca", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Request = new CreateShiftLogEntryRequest(
            DateTime.UtcNow, shift, TitleBox.Text, category, priority,
            NullIfEmpty(RoomBox.Text), NullIfEmpty(GuestBox.Text), NullIfEmpty(ReservationBox.Text),
            NullIfEmpty(AssignedBox.Text), DescriptionBox.Text, VipBox.IsChecked == true);
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
