using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Stay;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

/// <summary>
/// Chooses an existing persisted guest profile for an in-house stay.
/// This dialog deliberately has no create/mock path: registration must point
/// to a real profile before the stay command is sent.
/// </summary>
public sealed class AddStayOccupantWindow : Window
{
    private readonly IGuestProfileRepository? _guestRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly TextBox _searchBox = new();
    private readonly ComboBox _typeBox = new();
    private readonly ListBox _guestList = new();
    private readonly TextBlock _status = new();
    private readonly ObservableCollection<GuestProfileDto> _guests = new();
    private readonly bool _primaryGuest;
    private readonly IServiceScope? _serviceScope;
    private readonly SemaphoreSlim _dataOperationGate = new(1, 1);
    private bool _isClosed;

    public Guid SelectedGuestId { get; private set; }
    public HotelStayOccupantType SelectedOccupantType { get; private set; } = HotelStayOccupantType.Adult;
    public bool WasConfirmed { get; private set; }

    public AddStayOccupantWindow(bool primaryGuest = false)
    {
        _primaryGuest = primaryGuest;
        Title = primaryGuest ? "Chọn khách cũ" : "Thêm khách đi cùng";
        Width = 620;
        Height = 520;
        MinWidth = 520;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _guestRepository = scopedServices?.GetService<IGuestProfileRepository>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();

        BuildLayout();
        Loaded += async (_, _) => await SearchAsync();
        Closed += OnClosed;
    }

    private void BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(20) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var title = new TextBlock
        {
            Text = _primaryGuest ? "Chọn hồ sơ khách thật" : "Chọn khách đi cùng từ hồ sơ thật",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 54, 93)),
            Margin = new Thickness(0, 0, 0, 6)
        };
        root.Children.Add(title);

        var subtitle = new TextBlock
        {
            Text = _primaryGuest
                ? "Chọn một hồ sơ khách đã tồn tại trong dữ liệu thật để tiếp tục Walk-In."
                : "Khách đi cùng phải tồn tại trong hồ sơ khách và còn sức chứa phòng.",
            Foreground = Brushes.SlateGray,
            Margin = new Thickness(0, 0, 0, 14)
        };
        Grid.SetRow(subtitle, 1);
        root.Children.Add(subtitle);

        var content = new Grid();
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var filters = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        filters.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
        filters.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _searchBox.Height = 32;
        _searchBox.Margin = new Thickness(0, 0, 8, 0);
        _searchBox.ToolTip = "Tên, số khách, điện thoại hoặc email";
        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                await SearchAsync();
        };
        filters.Children.Add(_searchBox);

        _typeBox.Height = 32;
        _typeBox.Margin = new Thickness(0, 0, 8, 0);
        _typeBox.Items.Add(new ComboBoxItem { Content = "Người lớn", Tag = HotelStayOccupantType.Adult });
        _typeBox.Items.Add(new ComboBoxItem { Content = "Trẻ em", Tag = HotelStayOccupantType.Child });
        _typeBox.SelectedIndex = 0;
        Grid.SetColumn(_typeBox, 1);
        filters.Children.Add(_typeBox);

        var searchButton = new Button { Content = "Tìm", Padding = new Thickness(16, 4, 16, 4), Height = 32 };
        searchButton.Click += async (_, _) => await SearchAsync();
        Grid.SetColumn(searchButton, 2);
        filters.Children.Add(searchButton);
        content.Children.Add(filters);

        _guestList.ItemsSource = _guests;
        _guestList.DisplayMemberPath = nameof(GuestProfileDto.FullName);
        _guestList.SelectionChanged += (_, _) => UpdateStatusForSelection();
        _guestList.Margin = new Thickness(0, 0, 0, 8);
        Grid.SetRow(_guestList, 1);
        content.Children.Add(_guestList);

        _status.Foreground = Brushes.SlateGray;
        _status.TextWrapping = TextWrapping.Wrap;
        Grid.SetRow(_status, 2);
        content.Children.Add(_status);

        Grid.SetRow(content, 2);
        root.Children.Add(content);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 14, 0, 0) };
        var cancel = new Button { Content = "Hủy", Padding = new Thickness(16, 7, 16, 7), Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(cancel);
        var confirm = new Button { Content = _primaryGuest ? "Chọn khách" : "Thêm khách", Padding = new Thickness(16, 7, 16, 7), IsDefault = true };
        confirm.Click += OnConfirm;
        buttons.Children.Add(confirm);
        Grid.SetRow(buttons, 3);
        root.Children.Add(buttons);

        Content = root;
    }

    private async Task SearchAsync()
    {
        await _dataOperationGate.WaitAsync();
        try { await SearchCoreAsync(); }
        finally { _dataOperationGate.Release(); }
    }

    private async Task SearchCoreAsync()
    {
        if (_isClosed) return;
        if (_guestRepository is null || _contextAccessor?.Current is not { } context)
        {
            _guests.Clear();
            _status.Text = "Chưa cấu hình được Guest service và property context dữ liệu thật.";
            return;
        }

        try
        {
            var filter = new GuestSearchFilter(_searchBox.Text, null, false, null, null, null, null, null, null);
            var page = await _guestRepository.SearchAsync(
                context.Property.PropertyId.Value,
                filter,
                new PageRequest(1, 100),
                CancellationToken.None);
            _guests.Clear();
            foreach (var guest in page.Items)
                _guests.Add(guest);
            _status.Text = _guests.Count == 0
                ? "Không tìm thấy hồ sơ khách phù hợp trong dữ liệu thật."
                : $"Đã tải {_guests.Count:N0} hồ sơ khách thật; không hiển thị hồ sơ bị blacklist.";
        }
        catch (Exception ex)
        {
            _guests.Clear();
            _status.Text = "Không tải được hồ sơ khách thật: " + ex.Message;
        }
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        _isClosed = true;
        await _dataOperationGate.WaitAsync();
        try { _serviceScope?.Dispose(); }
        finally { _dataOperationGate.Release(); _dataOperationGate.Dispose(); }
    }

    private void UpdateStatusForSelection()
    {
        if (_guestList.SelectedItem is GuestProfileDto guest)
            _status.Text = $"Đã chọn: {guest.FullName} · {guest.GuestNumber} · {guest.Phone ?? "không có điện thoại"}";
    }

    private void OnConfirm(object sender, RoutedEventArgs e)
    {
        if (_guestList.SelectedItem is not GuestProfileDto guest)
        {
            MessageBox.Show("Hãy chọn một hồ sơ khách thật.", "Thêm khách đi cùng", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        SelectedGuestId = guest.GuestProfileId;
        SelectedOccupantType = (_typeBox.SelectedItem as ComboBoxItem)?.Tag is HotelStayOccupantType type
            ? type
            : HotelStayOccupantType.Adult;
        WasConfirmed = true;
        DialogResult = true;
        Close();
    }
}
