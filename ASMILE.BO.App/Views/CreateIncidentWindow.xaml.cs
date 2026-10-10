using System.Windows;
using ASMILE.BO.Application.FrontOffice.Incident;
using ASMILE.BO.Core.FrontOffice.Incident;

namespace ASMILE.BO.App.Views;

public partial class CreateIncidentWindow : Window
{
    public CreateIncidentRequest? Request { get; private set; }

    public CreateIncidentWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        CategoryBox.ItemsSource = new[]
        {
            "Guest Incident", "Security", "Fire", "Medical", "Power Failure", "Water Leak",
            "Door Lock", "Network", "POS", "Printer", "Elevator", "Air Conditioner",
            "Housekeeping", "Cashiering", "Lost Property", "Theft", "Violence", "Noise", "Parking", "Other"
        };
        PriorityBox.ItemsSource = Enum.GetValues<IncidentPriority>();
        ImpactBox.ItemsSource = new[] { "Guest", "Room", "Floor", "Hotel", "Multiple Guests" };
        CategoryBox.SelectedIndex = 0;
        PriorityBox.SelectedItem = IncidentPriority.Medium;
        ImpactBox.SelectedIndex = 0;
        OccurredDateBox.SelectedDate = DateTime.UtcNow.Date;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text) || CategoryBox.SelectedItem is not string category ||
            PriorityBox.SelectedItem is not IncidentPriority priority || ImpactBox.SelectedItem is not string impact ||
            string.IsNullOrWhiteSpace(DescriptionBox.Text))
        {
            MessageBox.Show("Vui lòng nhập đủ tiêu đề, danh mục, mức độ, ảnh hưởng và mô tả.", "Tạo sự cố", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var date = OccurredDateBox.SelectedDate?.Date ?? DateTime.UtcNow.Date;
        Request = new CreateIncidentRequest(
            DateTime.SpecifyKind(date, DateTimeKind.Utc),
            TitleBox.Text,
            category,
            priority,
            NullIfEmpty(RoomBox.Text),
            NullIfEmpty(GuestBox.Text),
            NullIfEmpty(ReservationBox.Text),
            NullIfEmpty(DepartmentBox.Text),
            impact,
            DescriptionBox.Text,
            VipBox.IsChecked == true);
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
