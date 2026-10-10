using ASMILE.BO.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMILE.BO.App.Views;

public partial class SearchReservationView : UserControl
{
    public SearchReservationView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        if (DataContext is ReservationWorkspaceViewModel vm)
            _ = vm.RefreshAsync();
    }

    private void OnDataGridDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Detail panel updates via SelectedReservation binding
    }

    private async void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ReservationWorkspaceViewModel workspace)
        {
            MessageBox.Show(Window.GetWindow(this), "Không tải được dữ liệu đặt phòng để sửa. Vui lòng làm mới danh sách rồi thử lại.", "Không thể sửa đặt phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (workspace.SelectedReservation is not { } reservation)
        {
            MessageBox.Show(Window.GetWindow(this), "Vui lòng chọn một đặt phòng trước khi sửa.", "Chưa chọn đặt phòng", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Window.GetWindow(this) is not SearchReservationWindow window)
        {
            MessageBox.Show(Window.GetWindow(this), "Không xác định được cửa sổ tra cứu đặt phòng để mở màn hình sửa.", "Không thể sửa đặt phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await window.RequestEditAsync(reservation);
    }

}
