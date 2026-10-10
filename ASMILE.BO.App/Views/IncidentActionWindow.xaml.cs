using System.Windows;
using ASMILE.BO.Application.FrontOffice.Incident;

namespace ASMILE.BO.App.Views;

public partial class IncidentActionWindow : Window
{
    private readonly IncidentStatusAction _action;
    public string? ValueOne { get; private set; }
    public string? ValueTwo { get; private set; }

    public IncidentActionWindow(IncidentStatusAction action)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _action = action;
        Configure(action);
    }

    private void Configure(IncidentStatusAction action)
    {
        (TitleText.Text, DescriptionText.Text, PromptOneText.Text, PromptTwoText.Text) = action switch
        {
            IncidentStatusAction.Assign => ("Phân công sự cố", "Chọn tên người/bộ phận chịu trách nhiệm xử lý.", "Người được phân công *", string.Empty),
            IncidentStatusAction.SetPending => ("Tạm chờ sự cố", "Ghi rõ lý do để ca sau tiếp tục xử lý.", "Lý do tạm chờ *", string.Empty),
            IncidentStatusAction.Resolve => ("Đánh dấu đã xử lý", "Cần lưu nguyên nhân và hành động khắc phục.", "Nguyên nhân gốc *", "Hành động khắc phục *"),
            IncidentStatusAction.Close => ("Đóng sự cố", "Chỉ đóng sau khi sự cố đã được đánh dấu đã xử lý.", "Biện pháp phòng ngừa *", string.Empty),
            IncidentStatusAction.Cancel => ("Hủy sự cố", "Thao tác này sẽ ghi lý do hủy vào audit.", "Lý do hủy *", string.Empty),
            _ => ("Cập nhật sự cố", "Nhập thông tin cập nhật.", "Nội dung *", string.Empty)
        };
        PromptTwoText.Visibility = string.IsNullOrWhiteSpace(PromptTwoText.Text) ? Visibility.Collapsed : Visibility.Visible;
        PromptTwoBox.Visibility = PromptTwoText.Visibility;
        PromptOneBox.AcceptsReturn = action == IncidentStatusAction.Resolve;
        PromptTwoBox.AcceptsReturn = action == IncidentStatusAction.Resolve;
        Height = action == IncidentStatusAction.Resolve ? 440 : 330;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PromptOneBox.Text) ||
            (PromptTwoBox.Visibility == Visibility.Visible && string.IsNullOrWhiteSpace(PromptTwoBox.Text)))
        {
            MessageBox.Show("Vui lòng nhập đủ thông tin bắt buộc.", "Cập nhật sự cố", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ValueOne = PromptOneBox.Text.Trim();
        ValueTwo = PromptTwoBox.Visibility == Visibility.Visible ? PromptTwoBox.Text.Trim() : null;
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
