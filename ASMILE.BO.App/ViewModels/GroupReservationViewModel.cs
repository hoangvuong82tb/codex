using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class GroupRoomDto
{
    public Guid AssignmentId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public bool IsVip { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public bool IsCheckedIn { get; set; }
    public bool IsCheckedOut { get; set; }
    public string StatusDisplay => IsCheckedOut ? "Da tra" : IsCheckedIn ? "Da nhan" : "Chua nhan";
}

public sealed class AutoAssignCandidate { public string RoomNumber { get; set; } = ""; public string RoomType { get; set; } = ""; public string Floor { get; set; } = ""; public int Score { get; set; } public string Reason { get; set; } = ""; }

public sealed class GroupReservationDto
{
    public Guid GroupReservationId { get; set; }
    public string GroupCode { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? AgencyName { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactEmail { get; set; }
    public DateOnly ArrivalDate { get; set; }
    public DateOnly DepartureDate { get; set; }
    public int ExpectedRooms { get; set; }
    public int ConfirmedRooms { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Routing { get; set; } = "CompanyPaysAll";
    public decimal TotalDeposit { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public decimal CompanyPayAmount { get; set; }
    public decimal GuestPayAmount { get; set; }
    public decimal MasterFolioBalance { get; set; }
    public string? Notes { get; set; }
    public ObservableCollection<GroupRoomDto> RoomingList { get; set; } = new();
    public ObservableCollection<AutoAssignCandidate> AssignCandidates { get; set; } = new();
}

public sealed partial class GroupReservationViewModel : ViewModelBase
{
    public ObservableCollection<GroupReservationDto> Groups { get; } = new();
    [ObservableProperty] private GroupReservationDto? _selectedGroup;
    [ObservableProperty] private GroupRoomDto? _selectedRoom;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isDetailOpen;
    [ObservableProperty] private bool _isAssignmentPanelOpen;
    [ObservableProperty] private bool _isMasterFolioOpen;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private int _totalGroups;
    [ObservableProperty] private int _totalRooms;
    [ObservableProperty] private decimal _companyPayPct = 80;
    [ObservableProperty] private decimal _guestPayPct = 20;
    [ObservableProperty] private decimal _fixedCompanyAmount;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedGroup is not null;
    public string[] StatusFilters { get; } = ["Tat ca", "Draft", "Tentative", "Confirmed", "CheckedIn", "CheckedOut", "Cancelled"];
    public string[] RoutingOptions { get; } = ["CompanyPaysAll", "GuestPaysExtras", "SplitPercentage", "SplitFixed", "Custom"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            Groups.Clear();
            var g = new GroupReservationDto { GroupReservationId = Guid.NewGuid(), GroupCode = "GRP-202607-000001", GroupName = "Doan ABC Corporation", CompanyName = "ABC Corp", AgencyName = "Asia Travel", ContactPerson = "Tran Van K", ContactPhone = "0901234567", ArrivalDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)), DepartureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)), ExpectedRooms = 10, ConfirmedRooms = 4, Status = "Confirmed", Routing = "CompanyPaysAll", TotalDeposit = 20000000, TotalRevenue = 64000000, OutstandingBalance = 44000000, CompanyPayAmount = 64000000, GuestPayAmount = 0, MasterFolioBalance = 64000000 };
            g.RoomingList.Add(new GroupRoomDto { AssignmentId = Guid.NewGuid(), RoomNumber = "101", RoomType = "STD", GuestName = "Nguyen Van A", Adults = 2 });
            g.RoomingList.Add(new GroupRoomDto { AssignmentId = Guid.NewGuid(), RoomNumber = "102", RoomType = "STD", GuestName = "Tran Thi B", Adults = 1 });
            g.RoomingList.Add(new GroupRoomDto { AssignmentId = Guid.NewGuid(), RoomNumber = "103", RoomType = "STD", GuestName = "Le Van C", IsVip = true, Adults = 2 });
            g.RoomingList.Add(new GroupRoomDto { AssignmentId = Guid.NewGuid(), RoomNumber = "201", RoomType = "DLX", GuestName = "Pham Thi D", Adults = 2, IsCheckedIn = true });
            Groups.Add(g); TotalGroups = Groups.Count; TotalRooms = Groups.Sum(x => x.RoomingList.Count);
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private async Task SearchAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectGroup(GroupReservationDto? g) { SelectedGroup = g; IsDetailOpen = g is not null; }
    [RelayCommand] private void CloseDetail() { IsDetailOpen = false; IsAssignmentPanelOpen = false; IsMasterFolioOpen = false; }
    [RelayCommand] private void ToggleAssignmentPanel() => IsAssignmentPanelOpen = !IsAssignmentPanelOpen;
    [RelayCommand] private void ToggleMasterFolio() => IsMasterFolioOpen = !IsMasterFolioOpen;

    // AUTO ROOM ASSIGNMENT ENGINE
    [RelayCommand]
    private void RunAutoAssignment()
    {
        if (SelectedGroup is null) return;
        SelectedGroup.AssignCandidates.Clear();
        var need = SelectedGroup.ExpectedRooms - SelectedGroup.ConfirmedRooms;
        if (need <= 0) { InfoMessage = "Tat ca phong da duoc phan."; return; }
        // Simulate algorithm — in production queries HotelRoom repository
        var candidates = new List<AutoAssignCandidate> { new() { RoomNumber = "104", RoomType = "STD", Floor = "Tang 1", Score = 95, Reason = "STD, Tang 1, canh 101-103" }, new() { RoomNumber = "105", RoomType = "STD", Floor = "Tang 1", Score = 0, Reason = "OOO - khong kha dung" }, new() { RoomNumber = "202", RoomType = "DLX", Floor = "Tang 2", Score = 70, Reason = "DLX, Tang 2, khac tang" }, new() { RoomNumber = "301", RoomType = "STE", Floor = "Tang 3", Score = 60, Reason = "STE, khac loai, khac tang" }, new() { RoomNumber = "106", RoomType = "STD", Floor = "Tang 1", Score = 90, Reason = "STD, Tang 1, khong lien ke" } };
        foreach (var c in candidates.OrderByDescending(c => c.Score)) SelectedGroup.AssignCandidates.Add(c);
        InfoMessage = $"Da phan tich {candidates.Count} phong. {need} phong can duoc gan.";
    }

    [RelayCommand]
    private void AcceptAssignment(AutoAssignCandidate? candidate)
    {
        if (SelectedGroup is null || candidate is null) return;
        if (candidate.Score <= 0) { InfoMessage = $"Phong {candidate.RoomNumber} khong the gan: {candidate.Reason}"; return; }
        SelectedGroup.RoomingList.Add(new GroupRoomDto { AssignmentId = Guid.NewGuid(), RoomNumber = candidate.RoomNumber, RoomType = candidate.RoomType, Adults = 2 });
        SelectedGroup.ConfirmedRooms = SelectedGroup.RoomingList.Count;
        SelectedGroup.AssignCandidates.Remove(candidate);
        TotalRooms = Groups.Sum(g => g.RoomingList.Count);
        InfoMessage = $"Da gan phong {candidate.RoomNumber} ({candidate.Score} diem). Room Rack da cap nhat.";
    }

    // BULK OPERATIONS
    [RelayCommand] private void BulkCheckIn() { if (SelectedGroup is null) return; foreach (var r in SelectedGroup.RoomingList.Where(r => !r.IsCheckedIn && !r.IsCheckedOut)) r.IsCheckedIn = true; InfoMessage = $"Check-in hang loat: {SelectedGroup.RoomingList.Count(r => r.IsCheckedIn)} phong da nhan."; }
    [RelayCommand] private void BulkCheckOut() { if (SelectedGroup is null) return; foreach (var r in SelectedGroup.RoomingList.Where(r => r.IsCheckedIn && !r.IsCheckedOut)) { r.IsCheckedOut = true; r.IsCheckedIn = false; } InfoMessage = "Check-out hang loat hoan tat."; }

    [RelayCommand]
    private void BulkAssignRooms()
    { RunAutoAssignment(); var top = SelectedGroup?.AssignCandidates.FirstOrDefault(c => c.Score > 0); if (top is not null) AcceptAssignment(top); }

    // MASTER FOLIO
    [RelayCommand]
    private void TransferToMasterFolio()
    { if (SelectedGroup is null) return; SelectedGroup.MasterFolioBalance += 2000000; SelectedGroup.OutstandingBalance = SelectedGroup.MasterFolioBalance - SelectedGroup.TotalDeposit; InfoMessage = "Da chuyen 2,000,000 VND tu Folio ca nhan sang Master Folio."; }

    [RelayCommand]
    private void SettleMasterFolio()
    { if (SelectedGroup is null) return; SelectedGroup.MasterFolioBalance = 0; SelectedGroup.OutstandingBalance = 0; InfoMessage = "Master Folio da duoc quyet toan."; }

    // COMPANY BILLING
    [RelayCommand]
    private void ApplyCompanyBilling()
    {
        if (SelectedGroup is null) return;
        var total = SelectedGroup.TotalRevenue;
        switch (SelectedGroup.Routing)
        {
            case "CompanyPaysAll": SelectedGroup.CompanyPayAmount = total; SelectedGroup.GuestPayAmount = 0; break;
            case "GuestPaysExtras": SelectedGroup.CompanyPayAmount = total * 0.8m; SelectedGroup.GuestPayAmount = total * 0.2m; break;
            case "SplitPercentage": SelectedGroup.CompanyPayAmount = total * CompanyPayPct / 100; SelectedGroup.GuestPayAmount = total - SelectedGroup.CompanyPayAmount; break;
            case "SplitFixed": SelectedGroup.CompanyPayAmount = FixedCompanyAmount; SelectedGroup.GuestPayAmount = total - FixedCompanyAmount; break;
        }
        InfoMessage = $"Da ap dung hoa don: Cong ty={SelectedGroup.CompanyPayAmount:N0}, Khach={SelectedGroup.GuestPayAmount:N0}";
    }

    // GROUP PAYMENT
    [RelayCommand]
    private void RecordCompanyPayment() { if (SelectedGroup is null) return; SelectedGroup.TotalDeposit += SelectedGroup.CompanyPayAmount; SelectedGroup.OutstandingBalance = SelectedGroup.TotalRevenue - SelectedGroup.TotalDeposit; InfoMessage = $"Da ghi nhan thanh toan cong ty: {SelectedGroup.CompanyPayAmount:N0} VND."; }

    [RelayCommand]
    private void RecordGuestPayment() { if (SelectedGroup is null) return; SelectedGroup.TotalDeposit += SelectedGroup.GuestPayAmount; SelectedGroup.OutstandingBalance = SelectedGroup.TotalRevenue - SelectedGroup.TotalDeposit; InfoMessage = $"Da ghi nhan thanh toan khach: {SelectedGroup.GuestPayAmount:N0} VND."; }

    partial void OnSelectedGroupChanged(GroupReservationDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
