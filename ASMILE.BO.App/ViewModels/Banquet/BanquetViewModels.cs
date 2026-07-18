using System.Collections.ObjectModel;
using ASMILE.BO.Application.DTOs;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.ViewModels;


public sealed class LookupOption<T>(string label, T value)
{
    public string Label { get; set; } = label;
    public T Value { get; set; } = value;
}

internal sealed record BanquetEventEditorSnapshot(
    string EventType,
    string CustomerName,
    string Phone,
    DateTime EventDate,
    string StartTime,
    string EndTime,
    int GuestCount,
    int TableCount,
    int ExtraGuestCount,
    int ExtraTableCount,
    string BrideName,
    string GroomName,
    string ConferenceName,
    string BirthdayPersonName,
    DateTime? BirthdayDate,
    string MeetingTopic,
    string PartyTheme,
    string OtherEventName,
    Guid? RoomId,
    string Status,
    string Note);

public sealed class BanquetWorkflowStepVm
{
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string State { get; set; } = "Pending";
}

public sealed class BanquetAlertVm
{
    public Guid? EventId { get; set; }
    public string Level { get; set; } = "Info";
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}

public sealed class BanquetServiceCard
{
    public BanquetServiceCard(RestaurantExtraServiceDto dto)
    {
        Id = dto.Id;
        Code = dto.Code;
        Name = dto.Name;
        ServiceType = dto.ServiceType;
        UnitName = string.IsNullOrWhiteSpace(dto.UnitName) ? "lần" : dto.UnitName;
        Price = dto.DefaultPrice;
        VatRate = dto.VatRate;
        ImagePath = dto.ImagePath;
        ImageUrl = dto.ImageUrl;
        Emoji = ResolveServiceEmoji(dto);
    }

    public Guid Id { get; }
    public string Code { get; }
    public string Name { get; }
    public string ServiceType { get; }
    public string UnitName { get; }
    public string ServiceTypeDisplay => BanquetRestaurantViewModel.GetServiceTypeDisplayName(ServiceType);
    public decimal Price { get; }
    public decimal VatRate { get; }
    public string? ImagePath { get; }
    public string? ImageUrl { get; }
    public string Emoji { get; }
    public string? DisplayImageSource => !string.IsNullOrWhiteSpace(ImagePath) ? ImagePath : ImageUrl;

    private static string ResolveServiceEmoji(RestaurantExtraServiceDto dto)
    {
        var t = (dto.ServiceType ?? dto.Name).ToLowerInvariant();
        if (t.Contains("mc") || t.Contains("dẫn")) return "🎤";
        if (t.Contains("ca sĩ") || t.Contains("hát") || t.Contains("singer")) return "🎵";
        if (t.Contains("trang trí") || t.Contains("hoa") || t.Contains("decoration")) return "💐";
        if (t.Contains("âm thanh") || t.Contains("sound") || t.Contains("light")) return "🔊";
        if (t.Contains("led") || t.Contains("màn hình") || t.Contains("screen")) return "📺";
        if (t.Contains("ảnh") || t.Contains("photo") || t.Contains("quay")) return "📸";
        if (t.Contains("bàn") || t.Contains("ghế") || t.Contains("table")) return "🪑";
        if (t.Contains("xe") || t.Contains("transport") || t.Contains("đưa")) return "🚗";
        if (t.Contains("cổng") || t.Contains("gateway") || t.Contains("check")) return "🎪";
        return "🔧";
    }
}

public sealed class BanquetRevenueBarVm
{
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public double BarHeight { get; set; }
}

public sealed class BanquetCustomerSummaryVm
{
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime? LastEventDate { get; set; }
    public DateTime? NextEventDate { get; set; }
    public string LastEventNo { get; set; } = string.Empty;
}

public sealed class BanquetContractSummaryVm
{
    public Guid EventId { get; set; }
    public string EventNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class BanquetFinanceSummaryVm
{
    public Guid EventId { get; set; }
    public string EventNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class BanquetChecklistGroupVm
{
    public BanquetChecklistGroupVm(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public ObservableCollection<BanquetChecklistDisplayVm> Items { get; } = new();
    public int ItemCount => Items.Count;
    public int CompletedCount => Items.Count(x => x.IsDone);
    public int PendingCount => ItemCount - CompletedCount;
    public double ProgressPercent => ItemCount == 0 ? 0d : CompletedCount * 100d / ItemCount;
    public string ProgressText => $"{CompletedCount}/{ItemCount} hoàn thành";
}

public sealed class BanquetChecklistDisplayVm
{
    public BanquetChecklistDto? Source { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Responsible { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool IsDone => Source?.IsDone == true;
    public bool IsOverdue => !IsDone && DueDate.HasValue && DueDate.Value.Date < DateTime.Today;
    public string DueDateText => DueDate.HasValue ? $"Hạn {DueDate:dd/MM/yyyy}" : "Chưa có hạn";
}

public sealed class BanquetMilestoneVm
{
    public string Title { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Planned";
    public string Description { get; set; } = string.Empty;
    public string DueDateText => DueDate.ToString("dd/MM/yyyy");
    public string StatusDisplay => Status switch
    {
        "Done" => "Hoàn thành",
        "Overdue" => "Quá hạn",
        "Today" => "Hôm nay",
        _ => "Sắp tới"
    };
}

public sealed class BanquetScheduleColumnVm
{
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
}

public sealed class BanquetScheduleRowVm
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public ObservableCollection<BanquetScheduleCellVm> Cells { get; } = new();
}

public sealed class BanquetScheduleCellVm
{
    public Guid? EventId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string AccentBrush { get; set; } = "#CBD5E1";
    public string BackgroundBrush { get; set; } = "#FFFFFF";
    public string BorderBrush { get; set; } = "#E2E8F0";
    public bool IsEmpty { get; set; }
}

public sealed class BanquetRoomCalendarDayVm
{
    public DateTime Date { get; set; }
    public string DayLabel { get; set; } = string.Empty;
    public string DateLabel { get; set; } = string.Empty;
    public bool IsToday { get; set; }
}

public sealed class BanquetRoomCalendarRowVm
{
    public Guid RoomId { get; set; }
    public string RoomCode { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxTables { get; set; }
    public ObservableCollection<BanquetRoomCalendarCellVm> Days { get; } = new();
}

public sealed class BanquetRoomCalendarCellVm
{
    public DateTime Date { get; set; }
    public BanquetRoomCalendarSlotVm Lunch { get; set; } = new();
    public BanquetRoomCalendarSlotVm Dinner { get; set; } = new();
}

public sealed class BanquetRoomCalendarSlotVm
{
    public Guid RoomId { get; set; }
    public Guid? EventId { get; set; }
    public BanquetEventDto? SourceEvent { get; set; }
    public DateTime Date { get; set; }
    public string Shift { get; set; } = string.Empty;
    public string ShiftLabel { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string DetailText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string BackgroundBrush { get; set; } = "#FFFFFF";
    public string BorderBrush { get; set; } = "#DCE5F0";
    public string AccentBrush { get; set; } = "#94A3B8";
    public bool IsEmpty { get; set; }
}

public sealed class BanquetYearMonthVm
{
    public int Month { get; set; }
    public string MonthLabel { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public int BookedDayCount { get; set; }
    public ObservableCollection<BanquetYearDayVm> Days { get; } = new();
}

public sealed class BanquetYearDayVm
{
    public DateTime Date { get; set; }
    public string DayNumber { get; set; } = string.Empty;
    public bool IsCurrentMonth { get; set; }
    public bool IsToday { get; set; }
    public bool HasEvents { get; set; }
    public int EventCount { get; set; }
    public string BackgroundBrush { get; set; } = "Transparent";
    public string ForegroundBrush { get; set; } = "#1E293B";
    public string ToolTipText { get; set; } = string.Empty;
}

public sealed class BanquetReportTrendVm
{
    public string Label { get; set; } = string.Empty;
    public string PeriodText { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public decimal Revenue { get; set; }
    public double BarWidth { get; set; }
}

public sealed class BanquetReportRoomVm
{
    public Guid? RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public int GuestCount { get; set; }
    public decimal Revenue { get; set; }
    public double UtilizationRate { get; set; }
    public string UtilizationText => $"{UtilizationRate:0.#}%";
    public double UtilizationWidth => Math.Max(3, Math.Min(180, UtilizationRate * 1.8));
}

public sealed class BanquetReportTypeVm
{
    public string EventType { get; set; } = string.Empty;
    public string EventTypeDisplay { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public int GuestCount { get; set; }
    public decimal Revenue { get; set; }
    public double Share { get; set; }
    public string ShareText => $"{Share:0.#}%";
    public string AccentBrush { get; set; } = "#2563EB";
}

public sealed class BanquetQuantificationItemVm
{
    public int Sequence { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public int EventCount { get; set; }
    public int SourceCount { get; set; }
    public decimal EstimatedCost { get; set; }
    public string SourceText { get; set; } = string.Empty;
}

public sealed class BanquetQuantificationMissingVm
{
    public Guid MenuItemId { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public decimal MenuQuantity { get; set; }
    public int EventCount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public sealed partial class BanquetQuantificationEventVm : ObservableObject
{
    public Guid EventId { get; init; }
    public string EventNo { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string EventTypeDisplay { get; init; } = string.Empty;
    public DateTime EventDate { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public string RoomName { get; init; } = string.Empty;
    public int GuestCount { get; init; }
    public int TableCount { get; init; }
    public string CurrentFingerprint { get; init; } = string.Empty;
    public IReadOnlyList<BanquetQuantificationItemVm> Ingredients { get; init; } = [];
    public IReadOnlyList<BanquetQuantificationItemVm> Services { get; init; } = [];
    public IReadOnlyList<BanquetQuantificationMissingVm> MissingRecipes { get; init; } = [];

    [ObservableProperty] private int sendCount;
    [ObservableProperty] private string? lastSentFingerprint;
    [ObservableProperty] private DateTime? lastSentAt;

    public string EventDateText => EventDate.ToString("dd/MM/yyyy");
    public string TimeText => $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public string LocationText => string.IsNullOrWhiteSpace(RoomName) ? "Chưa xếp phòng" : RoomName;
    public string ScaleText => $"{GuestCount:N0} khách · {TableCount:N0} bàn";
    public int IngredientCount => Ingredients.Count;
    public int ServiceCount => Services.Count;
    public int MissingRecipeCount => MissingRecipes.Count;
    public bool HasBeenSent => SendCount > 0;
    public bool IsModifiedAfterSend => HasBeenSent && !string.Equals(CurrentFingerprint, LastSentFingerprint, StringComparison.Ordinal);
    public string SendStatusText => !HasBeenSent
        ? "Chưa gửi định lượng"
        : IsModifiedAfterSend
            ? $"Đã sửa sau lần gửi {SendCount}"
            : $"Đã gửi định lượng lần {SendCount}";
    public string SendStatusForeground => !HasBeenSent ? "#9A3412" : IsModifiedAfterSend ? "#B45309" : "#047857";
    public string SendStatusBackground => !HasBeenSent ? "#FFF1E8" : IsModifiedAfterSend ? "#FEF3C7" : "#D1FAE5";
    public string SendButtonText => !HasBeenSent ? "Gửi định lượng" : $"Gửi cập nhật lần {SendCount + 1}";
    public string LastSentText => LastSentAt.HasValue
        ? $"Lần gần nhất: {LastSentAt.Value.ToLocalTime():HH:mm dd/MM/yyyy}"
        : "Chưa có lịch sử gửi";

    public void MarkSent(DateTime sentAt)
    {
        SendCount++;
        LastSentFingerprint = CurrentFingerprint;
        LastSentAt = sentAt;
        NotifySendStateChanged();
    }

    partial void OnSendCountChanged(int value) => NotifySendStateChanged();
    partial void OnLastSentFingerprintChanged(string? value) => NotifySendStateChanged();
    partial void OnLastSentAtChanged(DateTime? value) => OnPropertyChanged(nameof(LastSentText));

    private void NotifySendStateChanged()
    {
        OnPropertyChanged(nameof(HasBeenSent));
        OnPropertyChanged(nameof(IsModifiedAfterSend));
        OnPropertyChanged(nameof(SendStatusText));
        OnPropertyChanged(nameof(SendStatusForeground));
        OnPropertyChanged(nameof(SendStatusBackground));
        OnPropertyChanged(nameof(SendButtonText));
        OnPropertyChanged(nameof(LastSentText));
    }
}

internal sealed class BanquetModuleSettings
{
    public string VenueName { get; set; } = "Nhà hàng Tiệc (Banquet)";
    public string VenueCode { get; set; } = "BNQ001";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public int GuestsPerTable { get; set; } = 10;
    public decimal DefaultDepositRate { get; set; } = 30m;
    public decimal DefaultVatRate { get; set; } = 8m;
    public int HoldDays { get; set; } = 7;
    public int ReminderDays { get; set; } = 3;
    public string LunchStartTime { get; set; } = "11:00";
    public string DinnerStartTime { get; set; } = "18:00";
    public bool AutoGenerateEventNo { get; set; } = true;
    public bool PreventRoomConflict { get; set; } = true;
    public bool EnableDepositReminder { get; set; } = true;
    public bool ShowCompletedEvents { get; set; } = true;
    public DateTime? UpdatedAt { get; set; }
}
