using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ASMILE.BO.App.Views;

public sealed class RoomLockExtensionDialog : Window
{
    private readonly DatePicker expiresDate = new() { Width = 170 };
    private readonly TextBox expiresTime = new() { Width = 90, Text = "23:59" };
    private readonly TextBox reason = new() { MinWidth = 260 };

    public DateTime ExpiresAtUtc
    {
        get
        {
            var local = expiresDate.SelectedDate!.Value.Date.Add(TimeSpan.ParseExact(expiresTime.Text.Trim(), "hh\\:mm", CultureInfo.InvariantCulture));
            return DateTime.SpecifyKind(local, DateTimeKind.Unspecified).ToUniversalTime();
        }
    }

    public string Reason => reason.Text.Trim();

    public RoomLockExtensionDialog(string roomNumber, DateTime? currentExpiryUtc)
    {
        Title = $"Gia hạn khóa phòng {roomNumber}";
        Width = 440;
        Height = 265;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;

        var suggested = currentExpiryUtc?.ToLocalTime().AddHours(1) ?? DateTime.Now.AddDays(1);
        if (suggested <= DateTime.Now)
            suggested = DateTime.Now.AddDays(1);
        expiresDate.SelectedDate = suggested.Date;
        reason.Text = "Gia hạn công việc đang xử lý";

        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = "Thời hạn mới", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
        var expiry = new StackPanel { Orientation = Orientation.Horizontal };
        expiry.Children.Add(expiresDate);
        expiry.Children.Add(new TextBlock { Text = "  giờ ", VerticalAlignment = VerticalAlignment.Center });
        expiry.Children.Add(expiresTime);
        panel.Children.Add(expiry);
        panel.Children.Add(new TextBlock { Text = "Lý do gia hạn", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 16, 0, 6) });
        panel.Children.Add(reason);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        var cancel = new Button { Content = "Hủy", Padding = new Thickness(18, 7, 18, 7), IsCancel = true };
        var ok = new Button { Content = "Lưu gia hạn", Padding = new Thickness(18, 7, 18, 7), IsDefault = true, Margin = new Thickness(8, 0, 0, 0) };
        ok.Click += OnConfirm;
        buttons.Children.Add(cancel);
        buttons.Children.Add(ok);
        panel.Children.Add(buttons);
        Content = panel;
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        if (expiresDate.SelectedDate is null || !TimeSpan.TryParseExact(expiresTime.Text.Trim(), "hh\\:mm", CultureInfo.InvariantCulture, out _))
        {
            MessageBox.Show("Vui lòng nhập thời hạn hợp lệ theo định dạng HH:mm.", "Gia hạn khóa phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(Reason))
        {
            MessageBox.Show("Vui lòng nhập lý do gia hạn.", "Gia hạn khóa phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (ExpiresAtUtc <= DateTime.UtcNow)
        {
            MessageBox.Show("Thời hạn mới phải lớn hơn thời điểm hiện tại.", "Gia hạn khóa phòng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
        Close();
    }
}
