using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class VipProfileDto
{
    public Guid VipProfileId { get; set; }
    public string VipNumber { get; set; } = string.Empty;
    public string? GuestName { get; set; }
    public string Level { get; set; } = "Standard";
    public string Status { get; set; } = "Active";
    public int PointsBalance { get; set; }
    public int LifetimePoints { get; set; }
    public DateTime EnrollmentDate { get; set; }
    public int TotalStayCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageSpend { get; set; }
    public string? Preferences { get; set; }
    public DateOnly? Birthday { get; set; }
    public ObservableCollection<LoyaltyTransactionDto> Transactions { get; set; } = new();
}
public sealed class LoyaltyTransactionDto { public string Type { get; set; } = ""; public int Points { get; set; } public string Description { get; set; } = ""; public DateTime OccurredAt { get; set; } public string PointsDisplay => Type == "Redeem" ? $"-{Points}" : $"+{Points}"; }

public sealed partial class VipLoyaltyViewModel : ViewModelBase
{
    public ObservableCollection<VipProfileDto> Members { get; } = new();
    [ObservableProperty] private VipProfileDto? _selectedMember;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _totalStandard;
    [ObservableProperty] private int _totalGold;
    [ObservableProperty] private int _totalPlatinum;
    [ObservableProperty] private int _totalDiamond;
    [ObservableProperty] private string _newGuestName = "";
    [ObservableProperty] private string _newLevel = "Standard";
    [ObservableProperty] private int _earnPoints;
    [ObservableProperty] private string _earnDescription = "";
    [ObservableProperty] private int _redeemPoints;
    [ObservableProperty] private string _redeemDescription = "";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedMember is not null;
    public string[] Levels { get; } = ["Standard", "Silver", "Gold", "Platinum", "Diamond"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Members.Clear();
            var v1 = new VipProfileDto { VipProfileId = Guid.NewGuid(), VipNumber = "VIP-202607-000001", GuestName = "Le Van C", Level = "Platinum", Status = "Active", PointsBalance = 45000, LifetimePoints = 78000, EnrollmentDate = DateTime.UtcNow.AddYears(-2), TotalStayCount = 15, TotalRevenue = 37500000, AverageSpend = 2500000, Preferences = "High floor, non-smoking, extra pillow, fruit basket" };
            v1.Transactions.Add(new LoyaltyTransactionDto { Type = "Earn", Points = 5000, Description = "Stay: 2026-06", OccurredAt = DateTime.UtcNow.AddMonths(-1) });
            v1.Transactions.Add(new LoyaltyTransactionDto { Type = "Earn", Points = 2000, Description = "Restaurant spend", OccurredAt = DateTime.UtcNow.AddMonths(-1) });
            v1.Transactions.Add(new LoyaltyTransactionDto { Type = "Redeem", Points = 3000, Description = "Free night redemption", OccurredAt = DateTime.UtcNow.AddMonths(-3) });
            Members.Add(v1);
            var v2 = new VipProfileDto { VipProfileId = Guid.NewGuid(), VipNumber = "VIP-202607-000002", GuestName = "Nguyen Van A", Level = "Gold", Status = "Active", PointsBalance = 15000, LifetimePoints = 22000, EnrollmentDate = DateTime.UtcNow.AddYears(-1), TotalStayCount = 8, TotalRevenue = 12000000, AverageSpend = 1500000, Preferences = "Late checkout, airport transfer" };
            v2.Transactions.Add(new LoyaltyTransactionDto { Type = "Earn", Points = 3000, Description = "Stay: 2026-06", OccurredAt = DateTime.UtcNow.AddMonths(-1) });
            Members.Add(v2);
            TotalStandard = Members.Count(m => m.Level == "Standard"); TotalGold = Members.Count(m => m.Level == "Silver" || m.Level == "Gold"); TotalPlatinum = Members.Count(m => m.Level == "Platinum"); TotalDiamond = Members.Count(m => m.Level == "Diamond");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectMember(VipProfileDto? m) => SelectedMember = m;

    [RelayCommand] private void EnrollMember() { if (string.IsNullOrWhiteSpace(NewGuestName)) { ErrorMessage = "Vui long nhap ten khach."; return; } var nm = new VipProfileDto { VipProfileId = Guid.NewGuid(), VipNumber = $"VIP-202607-{Members.Count + 1:D6}", GuestName = NewGuestName, Level = NewLevel, Status = "Active", EnrollmentDate = DateTime.UtcNow }; Members.Insert(0, nm); InfoMessage = $"Da dang ky VIP {nm.VipNumber} cho {nm.GuestName}."; }
    [RelayCommand] private void Earn() { if (SelectedMember is null || EarnPoints <= 0) return; SelectedMember.PointsBalance += EarnPoints; SelectedMember.LifetimePoints += EarnPoints; SelectedMember.Transactions.Insert(0, new LoyaltyTransactionDto { Type = "Earn", Points = EarnPoints, Description = EarnDescription, OccurredAt = DateTime.UtcNow }); InfoMessage = $"+{EarnPoints} diem cho {SelectedMember.GuestName}."; }
    [RelayCommand] private void Redeem() { if (SelectedMember is null || RedeemPoints <= 0) return; if (RedeemPoints > SelectedMember.PointsBalance) { ErrorMessage = "Khong du diem."; return; } SelectedMember.PointsBalance -= RedeemPoints; SelectedMember.Transactions.Insert(0, new LoyaltyTransactionDto { Type = "Redeem", Points = RedeemPoints, Description = RedeemDescription, OccurredAt = DateTime.UtcNow }); InfoMessage = $"-{RedeemPoints} diem cho {SelectedMember.GuestName}."; }

    partial void OnSelectedMemberChanged(VipProfileDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
