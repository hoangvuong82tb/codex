using ASMILE.BO.App.Controls;
using ASMILE.BO.App.Models;
using ASMILE.BO.App.Services;
using ASMILE.BO.App.ViewModels;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASMILE.BO.App.Views;

public partial class LockedRoomWindow : Window
{
    private List<LockedRoomRow> _allRooms = new();
    private readonly IReceptionUiService? _receptionService;
    private readonly SearchHotelRoomsQueryHandler? _roomSearch;
    private readonly GetHotelRoomByNumberQueryHandler? _roomByNumber;
    private readonly ChangeHotelRoomOperationalStatusCommandHandler? _changeRoomStatus;
    private readonly ExtendHotelRoomLockCommandHandler? _extendRoomLock;
    private readonly IAuditService? _auditService;
    private readonly IServiceScope? _serviceScope;
    private readonly TablePagerViewModel<LockedRoomRow> _pager = new() { ItemName = "phòng", SelectedPageSize = "20 / trang" };

    public LockedRoomWindow()
    {
        InitializeComponent();
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _receptionService = scopedServices?.GetService<IReceptionUiService>();
        _roomSearch = scopedServices?.GetService<SearchHotelRoomsQueryHandler>();
        _roomByNumber = scopedServices?.GetService<GetHotelRoomByNumberQueryHandler>();
        _changeRoomStatus = scopedServices?.GetService<ChangeHotelRoomOperationalStatusCommandHandler>();
        _extendRoomLock = scopedServices?.GetService<ExtendHotelRoomLockCommandHandler>();
        _auditService = scopedServices?.GetService<IAuditService>();
        LockedGrid.ItemsSource = _pager.Items;
        Pager.DataContext = _pager;
        Loaded += OnLoadedAsync;
        Closed += (_, _) => _serviceScope?.Dispose();
    }

    private async void OnLoadedAsync(object? sender, RoutedEventArgs e)
    {
        if (_receptionService is null)
        {
            FooterTotal.Text = "Chưa cấu hình service dữ liệu phòng thật.";
            return;
        }

        try
        {
            var result = await _receptionService.GetLockedRoomsAsync(CancellationToken.None);
            if (result.IsFailure || result.Value is null)
            {
                FooterTotal.Text = result.Error?.Message ?? "Không thể tải phòng khóa từ dữ liệu thật.";
                return;
            }

            _allRooms = result.Value.Select(MapRealRoom).ToList();
            ApplyFilters();
            FooterTotal.Text = $"Dữ liệu thật · Tổng: {_allRooms.Count} phòng";
        }
        catch (Exception ex)
        {
            _allRooms.Clear();
            ApplyFilters();
            FooterTotal.Text = "Không thể tải phòng khóa: " + ex.Message;
        }
    }

    private static LockedRoomRow MapRealRoom(ReceptionReadOnlyRow room)
    {
        var isOutOfOrder = room.StatusText.Contains("Ngừng", StringComparison.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var lockStatus = room.OperationalStatusExpiresAtUtc is null
            ? "Đang khóa"
            : room.OperationalStatusExpiresAtUtc <= now ? "Đã hết hạn" : "Sắp mở";
        var changedAt = room.OperationalStatusChangedAtUtc?.ToLocalTime();
        var expiresAt = room.OperationalStatusExpiresAtUtc?.ToLocalTime();
        return new LockedRoomRow(
            room.PrimaryText,
            room.SecondaryText,
            "—",
            room.StatusText,
            lockStatus,
            string.IsNullOrWhiteSpace(room.OperationalStatusReason) ? "Trạng thái khai thác" : room.OperationalStatusReason,
            "—",
            changedAt?.ToString("dd/MM/yyyy HH:mm") ?? "Không xác định",
            expiresAt?.ToString("dd/MM/yyyy HH:mm") ?? "Không xác định",
            "Dữ liệu từ Hotel Master Data",
            lockStatus == "Đã hết hạn" ? "#FEF3C7" : isOutOfOrder ? "#FEE2E2" : "#FFF7ED",
            lockStatus == "Đã hết hạn" ? "#B45309" : isOutOfOrder ? "#DC2626" : "#EA580C");
    }

    private void ApplyFilters()
    {
        var filtered = _allRooms.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchBox.Text))
            filtered = filtered.Where(r =>
                r.RoomNumber.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase) ||
                r.Reason.Contains(SearchBox.Text, System.StringComparison.OrdinalIgnoreCase));
        if (FloorFilter.SelectedItem is ComboBoxItem cbiFloor && cbiFloor.Content is string floor && floor != "Tất cả")
            filtered = filtered.Where(r => r.Floor == floor);
        if (StatusFilter.SelectedItem is ComboBoxItem cbiStatus && cbiStatus.Content is string status && status != "Tất cả")
            filtered = filtered.Where(r => r.LockStatus == status);
        if (TypeFilter.SelectedItem is ComboBoxItem cbiType && cbiType.Content is string type && type != "Tất cả")
            filtered = filtered.Where(r => r.Reason == type);
        var list = filtered.ToList();
        _pager.SetSource(list);
        PagerSummary.Text = _pager.PageText;
        ResultCount.Text = $"{list.Count} phòng";
        FooterTotal.Text = $"Tổng: {list.Count} phòng";
    }

    private void OnRoomSelected(object sender, SelectionChangedEventArgs e)
    {
        if (LockedGrid.SelectedItem is not LockedRoomRow r)
        {
            DetailPanel.Children.Clear();
            DetailPanel.Children.Add(new TextBlock { Text = "Chọn một phòng để xem chi tiết.", FontSize = 13, Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")) });
            return;
        }

        DetailPanel.Children.Clear();

        DetailPanel.Children.Add(DetailRow("Phòng", $"{r.RoomNumber} · {r.RoomType}"));
        DetailPanel.Children.Add(DetailRow("Loại khóa", r.Reason));
        DetailPanel.Children.Add(DetailRow("Thời gian", $"{r.StartDate}  →  {r.EndDate}"));
        DetailPanel.Children.Add(DetailRow("Người thực hiện", r.LockedBy));
        DetailPanel.Children.Add(DetailRow("Trạng thái phòng", r.RoomStatus));
        DetailPanel.Children.Add(DetailRow("Ghi chú", string.IsNullOrWhiteSpace(r.Note) ? "—" : r.Note));
    }

    private static FrameworkElement DetailRow(string label, string value)
    {
        var sp = new StackPanel { Margin = new Thickness(0, 0, 0, 12) };
        sp.Children.Add(new TextBlock
        {
            Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8")),
            Margin = new Thickness(0, 0, 0, 2)
        });
        sp.Children.Add(new TextBlock
        {
            Text = value, FontSize = 13, FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#17365D")),
            TextWrapping = TextWrapping.Wrap
        });
        return sp;
    }

    private void OnFilterChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();
    private void OnClearFilter(object sender, RoutedEventArgs e)
    {
        SearchBox.Text = "";
        FloorFilter.SelectedIndex = 0;
        StatusFilter.SelectedIndex = 0;
        TypeFilter.SelectedIndex = 0;
        ApplyFilters();
    }

    private void OnRefresh(object sender, RoutedEventArgs e) => _ = ReloadAsync();

    private async Task ReloadAsync()
    {
        if (_receptionService is null)
        {
            FooterTotal.Text = "Chưa cấu hình service dữ liệu phòng thật.";
            return;
        }

        var result = await _receptionService.GetLockedRoomsAsync(CancellationToken.None);
        if (result.IsFailure || result.Value is null)
        {
            FooterTotal.Text = result.Error?.Message ?? "Không thể tải phòng khóa từ dữ liệu thật.";
            return;
        }

        _allRooms = result.Value.Select(MapRealRoom).ToList();
        ApplyFilters();
        FooterTotal.Text = $"Dữ liệu thật · Tổng: {_allRooms.Count} phòng";
    }

    private async void OnLockRoom(object sender, RoutedEventArgs e)
    {
        if (_roomSearch is null || _changeRoomStatus is null)
        {
            MessageBox.Show("Chưa cấu hình được command dữ liệu phòng thật.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var rooms = await _roomSearch.HandleAsync(
                new SearchHotelRoomsQuery(null, null, null, null, null, true, new PageRequest(1, 500), "number"),
                CancellationToken.None);
            if (rooms.IsFailure || rooms.Value is null)
            {
                MessageBox.Show(rooms.Error?.Message ?? "Không tải được danh sách phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var candidates = rooms.Value.Items
                .Where(room => room.OperationalStatus is not HotelRoomOperationalStatus.OutOfOrder and not HotelRoomOperationalStatus.OutOfService)
                .OrderBy(room => room.RoomNumber, StringComparer.CurrentCultureIgnoreCase)
                .ToArray();
            if (candidates.Length == 0)
            {
                MessageBox.Show("Không còn phòng đang khai thác để khóa.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var picker = new RoomLockSelectionDialog(candidates) { Owner = this };
            if (picker.ShowDialog() != true || picker.SelectedRoom is not { } selectedRoom)
                return;

            var statusDialog = new RoomOperationalStatusDialog(selectedRoom.OperationalStatus.ToString()) { Owner = this };
            if (statusDialog.ShowDialog() != true || statusDialog.SelectedStatus is not { } status || status == HotelRoomOperationalStatus.Available)
            {
                if (statusDialog.DialogResult == true)
                    MessageBox.Show("Hãy chọn trạng thái Ngừng khai thác hoặc Tạm dừng để khóa phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = await _changeRoomStatus.HandleAsync(
                new ChangeHotelRoomOperationalStatusCommand(selectedRoom.HotelRoomId, status, selectedRoom.RowVersion ?? []),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể khóa phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ReloadAsync();
            MessageBox.Show($"Đã khóa phòng {selectedRoom.RoomNumber}.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnUnlockRoom(object sender, RoutedEventArgs e)
    {
        if (LockedGrid.SelectedItem is not LockedRoomRow row)
        {
            MessageBox.Show("Vui lòng chọn một phòng trước.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_roomByNumber is null || _changeRoomStatus is null)
        {
            MessageBox.Show("Chưa cấu hình được command dữ liệu phòng thật.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var room = await _roomByNumber.HandleAsync(new GetHotelRoomByNumberQuery(row.RoomNumber), CancellationToken.None);
            if (room.IsFailure || room.Value is null)
            {
                MessageBox.Show(room.Error?.Message ?? "Không tìm thấy phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = await _changeRoomStatus.HandleAsync(
                new ChangeHotelRoomOperationalStatusCommand(room.Value.HotelRoomId, HotelRoomOperationalStatus.Available, room.Value.RowVersion ?? []),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể mở khóa phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ReloadAsync();
            MessageBox.Show($"Đã mở khóa phòng {row.RoomNumber}.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnExtendLock(object sender, RoutedEventArgs e)
    {
        if (LockedGrid.SelectedItem is not LockedRoomRow row)
        {
            MessageBox.Show("Vui lòng chọn một phòng trước.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_roomByNumber is null || _extendRoomLock is null)
        {
            MessageBox.Show("Chưa cấu hình được command dữ liệu phòng thật.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var room = await _roomByNumber.HandleAsync(new GetHotelRoomByNumberQuery(row.RoomNumber), CancellationToken.None);
            if (room.IsFailure || room.Value is null)
            {
                MessageBox.Show(room.Error?.Message ?? "Không tìm thấy phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (room.Value.OperationalStatus is not (HotelRoomOperationalStatus.OutOfOrder or HotelRoomOperationalStatus.OutOfService))
            {
                MessageBox.Show("Phòng không còn ở trạng thái khóa. Hãy làm mới danh sách rồi thử lại.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new RoomLockExtensionDialog(row.RoomNumber, room.Value.OperationalStatusExpiresAtUtc) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            var result = await _extendRoomLock.HandleAsync(
                new ExtendHotelRoomLockCommand(room.Value.HotelRoomId, dialog.ExpiresAtUtc, dialog.Reason, room.Value.RowVersion ?? []),
                CancellationToken.None);
            if (result.IsFailure)
            {
                MessageBox.Show(result.Error?.Message ?? "Không thể gia hạn khóa phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await ReloadAsync();
            MessageBox.Show($"Đã gia hạn khóa phòng {row.RoomNumber}.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnViewHistory(object sender, RoutedEventArgs e)
    {
        if (LockedGrid.SelectedItem is not LockedRoomRow row)
        {
            MessageBox.Show("Vui lòng chọn một phòng trước.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_roomByNumber is null || _auditService is null)
        {
            MessageBox.Show("Chưa cấu hình được query audit dữ liệu phòng thật.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var room = await _roomByNumber.HandleAsync(new GetHotelRoomByNumberQuery(row.RoomNumber), CancellationToken.None);
            if (room.IsFailure || room.Value is null)
            {
                MessageBox.Show(room.Error?.Message ?? "Không tìm thấy phòng.", "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var auditRows = await _auditService.QueryAsync(
                entityName: "HotelRoom",
                entityId: room.Value.HotelRoomId.ToString("D"),
                cancellationToken: CancellationToken.None);
            var lines = auditRows
                .OrderByDescending(audit => audit.CreatedAt)
                .Take(50)
                .Select(audit => $"{audit.CreatedAt:dd/MM/yyyy HH:mm} · {audit.Action} · {audit.Username}\n{audit.Detail}")
                .ToArray();
            MessageBox.Show(
                lines.Length == 0 ? "Phòng này chưa có bản ghi audit thật." : string.Join(Environment.NewLine + Environment.NewLine, lines),
                $"Lịch sử phòng {row.RoomNumber}",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Phòng khóa", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}

internal record LockedRoomRow(
    string RoomNumber, string RoomType, string Floor, string RoomStatus,
    string LockStatus, string Reason, string LockedBy,
    string StartDate, string EndDate, string Note,
    string LockStatusBadge, string LockStatusColor);
