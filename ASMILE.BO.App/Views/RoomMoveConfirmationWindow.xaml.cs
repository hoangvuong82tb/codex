using System.Windows;

namespace ASMILE.BO.App.Views;

public partial class RoomMoveConfirmationWindow : Window
{
    internal RoomMoveConfirmationWindow(RoomMoveConfirmationSummary summary)
    {
        InitializeComponent();

        GuestText.Text = summary.GuestName;
        OldRoomText.Text = summary.OldRoom;
        NewRoomText.Text = summary.NewRoom;
        EffectiveText.Text = summary.EffectiveAt;
        ReasonText.Text = summary.Reason;
        DifferenceText.Text = summary.PriceDifference;
        WarningText.Text = summary.RemainingTasks == 0
            ? "Không còn tác vụ bàn giao chưa đánh dấu."
            : $"Còn {summary.RemainingTasks} tác vụ bàn giao chưa đánh dấu. Đây là cảnh báo vận hành, không gọi backend.";
    }

    private void OnBack(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
