using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class GroupCheckInWindow : Window
{
    private int _step;
    private readonly Panel[] _panels;
    private readonly Border[] _stepBoxes;
    private readonly TextBlock[] _labels;
    private static readonly string[] StepLetters = { "A", "B", "C", "D", "E", "F" };

    public GroupCheckInWindow()
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        _panels = new[] { PanelA, PanelB, PanelC, PanelD, PanelE, PanelF };
        _stepBoxes = new[] { Step0, Step1, Step2, Step3, Step4, Step5 };
        _labels = new[] { Lbl0, Lbl1, Lbl2, Lbl3, Lbl4, Lbl5 };
        LoadMockData();
        UpdateNavigation();
    }

    private void LoadMockData()
    {
        var groups = new List<MockRow> { new("GRP-2024-001","Đoàn ABC Corporation","ABC Corp","24/07/2026","26/07/2026","10","22","Đã xác nhận"), new("GRP-2024-002","Đoàn VinGroup Tour","VinGroup","25/07/2026","27/07/2026","15","35","Chờ đến"), new("GRP-2024-003","Đoàn FPT Conference","FPT","24/07/2026","25/07/2026","5","12","Đang check-in") };
        GroupGrid.ItemsSource = groups;

        var rooms = new List<MockRow> { new("201","DLX","2","Trống sạch","2","2","Nguyễn Văn A","Chưa check-in"), new("202","DLX","2","Trống sạch","2","2","Trần Thị B","Chưa check-in"), new("203","DLX","2","Trống bẩn","2","1","—","Chưa phân"), new("301","SUP","3","Trống sạch","3","3","Lê Văn C","Chưa check-in"), new("302","SUP","3","Đang dọn","3","2","Phạm Thị D","Chưa check-in"), new("303","SUP","3","Trống sạch","3","0","—","Chưa phân"), new("401","DLX","4","Trống sạch","2","2","Hoàng Văn E","Chưa check-in"), new("402","DLX","4","Trống sạch","2","2","Vũ Thị F","Chưa check-in"), new("501","JNR","5","Trống sạch","2","0","—","Chưa phân"), new("502","JNR","5","Trống sạch","2","1","Đặng Văn G","Chưa check-in") };
        RoomGrid.ItemsSource = rooms;
    }

    private int CurrentStep { get => _step; set { _step = value; UpdateNavigation(); } }

    private void UpdateNavigation()
    {
        StepLabel.Text = $"Bước {StepLetters[CurrentStep]}/6";
        BackBtn.Visibility = CurrentStep > 0 ? Visibility.Visible : Visibility.Collapsed;
        NextBtn.Visibility = CurrentStep < 5 ? Visibility.Visible : Visibility.Collapsed;
        ExecuteBtn.Visibility = CurrentStep == 5 ? Visibility.Visible : Visibility.Collapsed;
        for (int i = 0; i < 6; i++)
        {
            _panels[i].Visibility = i == CurrentStep ? Visibility.Visible : Visibility.Collapsed;
            if (i < CurrentStep) { _stepBoxes[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FDF4")); _stepBoxes[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#86EFAC")); _labels[i].Text = "Hoàn tất"; _labels[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#166534")); }
            else if (i == CurrentStep) { _stepBoxes[i].Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF")); _stepBoxes[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3B82F6")); _stepBoxes[i].BorderThickness = new Thickness(1.5); _labels[i].Text = "Đang nhập"; _labels[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E40AF")); }
            else { _stepBoxes[i].Background = Brushes.White; _stepBoxes[i].BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0")); _stepBoxes[i].BorderThickness = new Thickness(1); _labels[i].Text = "Chưa bắt đầu"; _labels[i].Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#64748B")); }
        }
    }

    private void OnGroupSelected(object sender, SelectionChangedEventArgs e)
    {
        if (GroupGrid.SelectedItem is MockRow r)
        {
            DetailGroup.Text = r.Col2; DetailGroupInfo.Text = $"{r.Col3} · {r.Col8}";
            DetailStay.Text = $"{r.Col4} → {r.Col5}"; DetailNights.Text = "2 đêm";
            DetailRooms.Text = $"{r.Col6} phòng"; DetailGuests.Text = $"{r.Col7} khách";
        }
    }

    private void OnStepClick(object sender, MouseButtonEventArgs e) { if (sender is FrameworkElement fe && fe.Tag is string t && int.TryParse(t, out var s)) CurrentStep = s; }
    private void OnNext(object sender, RoutedEventArgs e) { if (CurrentStep < 5) CurrentStep++; }
    private void OnBack(object sender, RoutedEventArgs e) { if (CurrentStep > 0) CurrentStep--; }
    private void OnExecute(object sender, RoutedEventArgs e) { MessageBox.Show("Chức năng thực thi sẽ được kết nối ở giai đoạn sau.", "Check-in đoàn", MessageBoxButton.OK, MessageBoxImage.Information); }
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private void OnRefresh(object sender, RoutedEventArgs e) { }
}

public record MockRow(string Col1, string Col2 = "", string Col3 = "", string Col4 = "", string Col5 = "", string Col6 = "", string Col7 = "", string Col8 = "");
