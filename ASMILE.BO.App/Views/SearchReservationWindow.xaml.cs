using ASMILE.BO.App.ViewModels;
using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class SearchReservationWindow : Window
{
    public Func<ASMILE.BO.Application.FrontOffice.Reservation.ReservationDto, Task>? EditAction { get; set; }
    public ReservationWorkspaceViewModel Workspace { get; }

    public SearchReservationWindow(ReservationWorkspaceViewModel viewModel)
    {
        InitializeComponent();
        Workspace = viewModel;
        Owner = System.Windows.Application.Current.MainWindow;
        var view = new SearchReservationView();
        view.DataContext = viewModel;
        Host.Content = view;
    }

    public async Task RequestEditAsync(ASMILE.BO.Application.FrontOffice.Reservation.ReservationDto reservation)
    {
        if (EditAction is null)
        {
            System.Windows.MessageBox.Show(this, "Chức năng sửa đặt phòng chưa được kết nối.", "Không thể sửa đặt phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            await EditAction(reservation);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(this, $"Không thể mở màn hình sửa đặt phòng: {ex.Message}", "Lỗi sửa đặt phòng", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
