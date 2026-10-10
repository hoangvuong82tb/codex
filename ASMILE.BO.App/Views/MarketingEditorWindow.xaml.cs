using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CoreMarketingSection = ASMILE.BO.Core.FrontOffice.Marketing.MarketingSection;

namespace ASMILE.BO.App.Views;

public sealed record MarketingEditorValues(string Code, string Name, string GroupName, string Owner, string? Notes);

/// <summary>
/// Small fail-closed editor for Marketing master data. It only returns values;
/// persistence remains in IMarketingCommandService owned by the parent window.
/// </summary>
public sealed class MarketingEditorWindow : Window
{
    private readonly TextBox codeBox = CreateTextBox();
    private readonly TextBox nameBox = CreateTextBox();
    private readonly TextBox groupBox = CreateTextBox();
    private readonly TextBox ownerBox = CreateTextBox();
    private readonly TextBox notesBox = CreateTextBox();

    public MarketingEditorValues? Values { get; private set; }

    public MarketingEditorWindow(string sectionTitle, CoreMarketingSection section, MarketingRecord? source)
    {
        Title = source is null ? $"Thêm {sectionTitle}" : $"Sửa {sectionTitle}";
        Width = 520;
        Height = 430;
        MinWidth = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;
        ResizeMode = ResizeMode.NoResize;

        codeBox.Text = source?.Id ?? string.Empty;
        nameBox.Text = source?.Name ?? string.Empty;
        groupBox.Text = source?.Group ?? string.Empty;
        ownerBox.Text = source?.Owner ?? string.Empty;
        notesBox.Text = source?.Notes ?? string.Empty;
        notesBox.AcceptsReturn = true;
        notesBox.TextWrapping = TextWrapping.Wrap;
        notesBox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;

        var root = new Grid { Margin = new Thickness(24) };
        for (var i = 0; i < 6; i++) root.RowDefinitions.Add(new RowDefinition { Height = i == 5 ? new GridLength(54) : GridLength.Auto });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        AddField(root, 0, "Mã", codeBox);
        AddField(root, 1, "Tên", nameBox);
        AddField(root, 2, "Nhóm / thị trường", groupBox);
        AddField(root, 3, "Phụ trách", ownerBox);
        AddField(root, 4, "Ghi chú", notesBox, 82);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        var cancel = new Button { Content = "Hủy", Padding = new Thickness(18, 8, 18, 8), Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => DialogResult = false;
        var save = new Button { Content = "Lưu", Padding = new Thickness(20, 8, 20, 8), Background = new SolidColorBrush(Color.FromRgb(38, 107, 234)), Foreground = Brushes.White };
        save.Click += Save_Click;
        buttons.Children.Add(cancel);
        buttons.Children.Add(save);
        Grid.SetRow(buttons, 5);
        Grid.SetColumn(buttons, 1);
        root.Children.Add(buttons);
        Content = root;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(codeBox.Text) || string.IsNullOrWhiteSpace(nameBox.Text)
            || string.IsNullOrWhiteSpace(groupBox.Text) || string.IsNullOrWhiteSpace(ownerBox.Text))
        {
            MessageBox.Show("Vui lòng nhập đủ mã, tên, nhóm và người phụ trách.", "Marketing", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Values = new MarketingEditorValues(
            codeBox.Text.Trim(), nameBox.Text.Trim(), groupBox.Text.Trim(), ownerBox.Text.Trim(),
            string.IsNullOrWhiteSpace(notesBox.Text) ? null : notesBox.Text.Trim());
        DialogResult = true;
    }

    private static void AddField(Grid grid, int row, string label, TextBox box, double height = 34)
    {
        var labelBlock = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, Foreground = new SolidColorBrush(Color.FromRgb(80, 103, 128)), Margin = new Thickness(0, 0, 10, 8) };
        Grid.SetRow(labelBlock, row);
        grid.Children.Add(labelBlock);
        box.Height = height;
        box.Margin = new Thickness(0, 0, 0, 8);
        Grid.SetRow(box, row);
        Grid.SetColumn(box, 1);
        grid.Children.Add(box);
    }

    private static TextBox CreateTextBox() => new() { Padding = new Thickness(9, 6, 9, 6), BorderBrush = new SolidColorBrush(Color.FromRgb(203, 217, 235)), BorderThickness = new Thickness(1) };
}
