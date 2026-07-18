using System.Windows;
using ASMILE.BO.App.ViewModels;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class AlacarteRoomSetupDialog : Window
{
    public AlacarteRoomSetupDraft ResultDraft { get; private set; }

    public AlacarteRoomSetupDialog(AlacarteRoomSetupDraft draft)
    {
        InitializeComponent();
        ResultDraft = draft;

        RoomInfoText.Text = $"{draft.RoomType} · {draft.RoomName}";
        CustomerNameTextBox.Text = draft.CustomerName;
        PhoneTextBox.Text = draft.Phone;
        GuestCountTextBox.Text = draft.GuestCount.ToString();
        TableCountTextBox.Text = draft.TableCount.ToString();
        MaxTablesText.Text = $"Tối đa {draft.MaxTables} bàn của phòng này";
        NoteTextBox.Text = draft.Note;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(GuestCountTextBox.Text, out var guestCount) || guestCount <= 0)
        {
            MessageBox.Show(this, "Số khách phải lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            GuestCountTextBox.Focus();
            return;
        }

        if (!int.TryParse(TableCountTextBox.Text, out var tableCount) || tableCount <= 0)
        {
            MessageBox.Show(this, "Số bàn mở phải lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            TableCountTextBox.Focus();
            return;
        }

        ResultDraft = new AlacarteRoomSetupDraft
        {
            OrderId = ResultDraft.OrderId,
            RoomId = ResultDraft.RoomId,
            RoomType = ResultDraft.RoomType,
            RoomName = ResultDraft.RoomName,
            MaxTables = ResultDraft.MaxTables,
            CustomerName = CustomerNameTextBox.Text,
            Phone = PhoneTextBox.Text,
            GuestCount = guestCount,
            TableCount = tableCount,
            Note = NoteTextBox.Text
        };

        DialogResult = true;
        Close();
    }
}
