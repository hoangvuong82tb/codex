using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Guest;
using ASMILE.BO.Core.FrontOffice.Guest;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

/// <summary>
/// Creates a persisted guest profile for front-office flows that require a real GuestId.
/// </summary>
public sealed class CreateGuestProfileWindow : Window
{
    private readonly IGuestProfileRepository? _guestRepository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly IServiceScope? _serviceScope;
    private readonly TextBox _nameBox = CreateTextBox();
    private readonly TextBox _phoneBox = CreateTextBox();
    private readonly TextBox _emailBox = CreateTextBox();
    private readonly TextBox _documentBox = CreateTextBox();
    private readonly TextBox _nationalityBox = CreateTextBox("Việt Nam");
    private readonly TextBox _addressBox = CreateTextBox();
    private readonly TextBlock _status = new();
    private bool _saving;

    public Guid CreatedGuestId { get; private set; }
    public string CreatedGuestName { get; private set; } = string.Empty;

    public CreateGuestProfileWindow()
    {
        Title = "Tạo hồ sơ khách mới";
        Width = 660;
        Height = 560;
        MinWidth = 560;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brushes.White;

        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _guestRepository = scopedServices?.GetService<IGuestProfileRepository>();
        _contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();

        BuildLayout();
        Closed += (_, _) => _serviceScope?.Dispose();
    }

    private static TextBox CreateTextBox(string? text = null) => new()
    {
        Text = text ?? string.Empty,
        Height = 36,
        Padding = new Thickness(10, 0, 10, 0),
        VerticalContentAlignment = VerticalAlignment.Center,
        FontSize = 14,
        Foreground = new SolidColorBrush(Color.FromRgb(23, 54, 93)),
        BorderBrush = new SolidColorBrush(Color.FromRgb(216, 226, 240)),
        BorderThickness = new Thickness(1)
    };

    private void BuildLayout()
    {
        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(new TextBlock
        {
            Text = "Tạo hồ sơ khách mới",
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(23, 54, 93)),
            Margin = new Thickness(0, 0, 0, 5)
        });

        var subtitle = new TextBlock
        {
            Text = "Lưu hồ sơ vào dữ liệu thật trước khi thực hiện Walk-In.",
            Foreground = Brushes.SlateGray,
            Margin = new Thickness(0, 0, 0, 18)
        };
        Grid.SetRow(subtitle, 1);
        root.Children.Add(subtitle);

        var form = new Grid();
        form.ColumnDefinitions.Add(new ColumnDefinition());
        form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
        form.ColumnDefinitions.Add(new ColumnDefinition());
        for (var i = 0; i < 6; i++)
            form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        AddField(form, 0, "Họ và tên *", _nameBox, 0);
        AddField(form, 0, "Điện thoại", _phoneBox, 2);
        AddField(form, 1, "Email", _emailBox, 0);
        AddField(form, 1, "Số CCCD/Hộ chiếu", _documentBox, 2);
        AddField(form, 2, "Quốc tịch", _nationalityBox, 0);
        AddField(form, 2, "Địa chỉ", _addressBox, 2);

        var note = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(239, 246, 255)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(191, 219, 254)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 18, 0, 0),
            Child = new TextBlock
            {
                Text = "Hệ thống sẽ kiểm tra hồ sơ trùng theo họ tên, điện thoại, email và số giấy tờ trước khi lưu.",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175))
            }
        };
        Grid.SetRow(note, 3);
        Grid.SetColumnSpan(note, 3);
        form.Children.Add(note);

        _status.TextWrapping = TextWrapping.Wrap;
        _status.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
        _status.Margin = new Thickness(0, 12, 0, 0);
        Grid.SetRow(_status, 4);
        Grid.SetColumnSpan(_status, 3);
        form.Children.Add(_status);

        Grid.SetRow(form, 2);
        root.Children.Add(form);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 18, 0, 0)
        };
        var cancel = new Button { Content = "Hủy", Padding = new Thickness(18, 7, 18, 7), Margin = new Thickness(0, 0, 8, 0) };
        cancel.Click += (_, _) => Close();
        buttons.Children.Add(cancel);
        var save = new Button
        {
            Content = "Lưu hồ sơ khách",
            Padding = new Thickness(18, 7, 18, 7),
            Background = new SolidColorBrush(Color.FromRgb(47, 107, 255)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            IsDefault = true
        };
        save.Click += async (_, _) => await SaveAsync();
        buttons.Children.Add(save);
        Grid.SetRow(buttons, 3);
        root.Children.Add(buttons);

        Content = root;
        Loaded += (_, _) => _nameBox.Focus();
    }

    private static void AddField(Grid grid, int row, string label, TextBox field, int column)
    {
        var panel = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(125) });
        panel.ColumnDefinitions.Add(new ColumnDefinition());
        panel.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromRgb(82, 101, 125)),
            FontWeight = FontWeights.Medium
        });
        Grid.SetColumn(field, 1);
        panel.Children.Add(field);
        Grid.SetRow(panel, row);
        Grid.SetColumn(panel, column);
        grid.Children.Add(panel);
    }

    private async System.Threading.Tasks.Task SaveAsync()
    {
        if (_saving) return;
        var name = _nameBox.Text.Trim();
        if (name.Length == 0)
        {
            _status.Text = "Vui lòng nhập họ và tên khách.";
            return;
        }

        if (_guestRepository is null || _contextAccessor?.Current is not { } context)
        {
            _status.Text = "Chưa cấu hình Guest service hoặc property context dữ liệu thật.";
            return;
        }

        var propertyId = context.Property.PropertyId.Value;

        _saving = true;
        _status.Text = "Đang kiểm tra trùng và lưu hồ sơ thật...";
        try
        {
            var phone = NullIfBlank(_phoneBox.Text);
            var email = NullIfBlank(_emailBox.Text);
            var documentNumber = NullIfBlank(_documentBox.Text);
            var duplicates = await _guestRepository.FindDuplicatesAsync(
                propertyId,
                new DuplicateDetectionCriteria(name, phone, email, documentNumber, documentNumber),
                default);
            if (duplicates.Count > 0)
            {
                _status.Text = $"Đã tìm thấy {duplicates.Count:N0} hồ sơ có khả năng trùng ({duplicates[0].ExistingGuestNumber} - {duplicates[0].ExistingFullName}; {duplicates[0].MatchReason}). Hãy dùng Khách cũ để chọn hồ sơ đó.";
                return;
            }

            var now = DateTime.UtcNow;
            var guestNumber = $"GST-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
            var profile = GuestProfile.Create(
                propertyId,
                guestNumber,
                name,
                phone,
                email,
                GuestSource.Direct,
                now,
                context.Actor.ActorId);
            profile.UpdateProfile(phone, email, NullIfBlank(_addressBox.Text), NullIfBlank(_nationalityBox.Text), null, null, now, context.Actor.ActorId);
            if (documentNumber is not null)
                profile.AddDocument(GuestDocumentType.NationalId, documentNumber, NullIfBlank(_nationalityBox.Text), null, now);

            await _guestRepository.AddAsync(profile, default);
            CreatedGuestId = profile.GuestProfileId;
            CreatedGuestName = profile.FullName;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            _status.Text = "Không thể lưu hồ sơ khách thật: " + ex.Message;
        }
        finally
        {
            _saving = false;
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
