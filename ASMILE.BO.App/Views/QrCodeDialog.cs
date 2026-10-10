using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

public sealed class QrCodeDialog : Window
{
    private readonly byte[] _pngBytes;

    public QrCodeDialog(string roomNumber, string payload, byte[] pngBytes)
    {
        _pngBytes = pngBytes;
        Title = $"QR Code — Phòng {roomNumber}";
        Width = 440;
        Height = 560;
        MinWidth = 380;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var image = new Image
        {
            Width = 320,
            Height = 320,
            Stretch = Stretch.Uniform,
            Source = LoadBitmap(pngBytes)
        };

        var payloadText = new TextBox
        {
            Text = payload,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            FontSize = 11,
            Margin = new Thickness(0, 4, 0, 12)
        };

        var saveButton = new Button
        {
            Content = "Lưu PNG",
            Padding = new Thickness(18, 8, 18, 8),
            Margin = new Thickness(0, 0, 8, 0)
        };
        saveButton.Click += (_, _) => SavePng(roomNumber);

        var closeButton = new Button
        {
            Content = "Đóng",
            Padding = new Thickness(18, 8, 18, 8)
        };
        closeButton.Click += (_, _) => Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        buttons.Children.Add(saveButton);
        buttons.Children.Add(closeButton);

        var content = new StackPanel
        {
            Margin = new Thickness(28),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        content.Children.Add(new TextBlock
        {
            Text = $"Phòng {roomNumber}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 54, 93)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 14)
        });
        content.Children.Add(image);
        content.Children.Add(new TextBlock
        {
            Text = "Dữ liệu mã hóa từ phòng thật đang chọn",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139)),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 0)
        });
        content.Children.Add(payloadText);
        content.Children.Add(buttons);
        Content = content;
    }

    private void SavePng(string roomNumber)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu QR Code phòng",
            Filter = "PNG image (*.png)|*.png",
            FileName = $"room-{roomNumber}-qr.png",
            AddExtension = true,
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true)
            return;

        try
        {
            File.WriteAllBytes(dialog.FileName, _pngBytes);
            MessageBox.Show(this, $"Đã lưu QR Code phòng {roomNumber}.", "QR Code", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Không thể lưu QR Code: {ex.Message}", "QR Code", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static BitmapImage LoadBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}
