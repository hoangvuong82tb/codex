using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Loyalty;
using ASMILE.BO.Application.Interfaces;

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
    private readonly IVipLoyaltyService? _service;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly ICurrentUserService? _currentUserService;

    public VipLoyaltyViewModel(
        IVipLoyaltyService? service = null,
        IFrontOfficeExecutionContextAccessor? contextAccessor = null,
        ICurrentUserService? currentUserService = null)
    {
        _service = service;
        _contextAccessor = contextAccessor;
        _currentUserService = currentUserService;
    }

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
            SelectedMember = null;
            TotalStandard = 0;
            TotalGold = 0;
            TotalPlatinum = 0;
            TotalDiamond = 0;
            ErrorMessage = string.Empty;
            var propertyId = TryGetPropertyId();
            if (_service is null || propertyId is null)
            {
                InfoMessage = "Chưa chọn cơ sở hoặc nguồn dữ liệu VIP/Loyalty chưa được cấu hình; không hiển thị dữ liệu mẫu.";
                return;
            }
            var result = await _service.SearchAsync(propertyId.Value, cancellationToken: ct);
            foreach (var item in result.Members) Members.Add(ToDto(item));
            TotalStandard = result.TotalStandard;
            TotalGold = result.TotalGold;
            TotalPlatinum = result.TotalPlatinum;
            TotalDiamond = result.TotalDiamond;
            InfoMessage = $"Đã tải {result.TotalCount:N0} hồ sơ VIP từ CSDL thật.";
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể tải hồ sơ VIP: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectMember(VipProfileDto? m) => SelectedMember = m;

    [RelayCommand] private async Task EnrollMember(CancellationToken ct)
    {
        if (_service is null || TryGetPropertyId() is not { } propertyId)
        {
            ErrorMessage = "Chưa chọn cơ sở hoặc nguồn dữ liệu VIP/Loyalty chưa được cấu hình.";
            return;
        }
        try
        {
            ErrorMessage = string.Empty;
            var level = Enum.TryParse<ASMILE.BO.Core.FrontOffice.Loyalty.MembershipLevel>(NewLevel, true, out var parsed) ? parsed : ASMILE.BO.Core.FrontOffice.Loyalty.MembershipLevel.Standard;
            var created = await _service.EnrollAsync(new EnrollVipCommand(propertyId, string.Empty, null, NewGuestName, level, ActorId()), ct);
            NewGuestName = string.Empty;
            await LoadAsync(ct);
            SelectedMember = Members.FirstOrDefault(x => x.VipProfileId == created.VipProfileId);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể đăng ký VIP: {ex.Message}"; }
    }

    [RelayCommand] private Task Earn(CancellationToken ct) => ApplyPointsAsync(false, ct);
    [RelayCommand] private Task Redeem(CancellationToken ct) => ApplyPointsAsync(true, ct);

    private async Task ApplyPointsAsync(bool redeem, CancellationToken ct)
    {
        if (SelectedMember is null) return;
        if (_service is null || TryGetPropertyId() is not { } propertyId)
        {
            ErrorMessage = "Chưa chọn cơ sở hoặc nguồn dữ liệu VIP/Loyalty chưa được cấu hình.";
            return;
        }
        try
        {
            ErrorMessage = string.Empty;
            if (redeem) await _service.RedeemAsync(propertyId, SelectedMember.VipProfileId, RedeemPoints, RedeemDescription, ct);
            else await _service.EarnAsync(propertyId, SelectedMember.VipProfileId, EarnPoints, EarnDescription, ct);
            EarnPoints = RedeemPoints = 0;
            EarnDescription = RedeemDescription = string.Empty;
            await LoadAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không thể cập nhật điểm Loyalty: {ex.Message}"; }
    }

    private Guid? TryGetPropertyId() => _contextAccessor?.Current?.Property.PropertyId.Value is { } value && value != Guid.Empty ? value : null;
    private Guid? ActorId() => _contextAccessor?.Current?.Actor.ActorId?.Value ?? _currentUserService?.CurrentUser?.Id;
    private static VipProfileDto ToDto(ASMILE.BO.Application.FrontOffice.Loyalty.VipProfileDto item) => new()
    {
        VipProfileId = item.VipProfileId,
        VipNumber = item.VipNumber,
        GuestName = item.GuestName,
        Level = item.Level.ToString(),
        Status = item.Status.ToString(),
        PointsBalance = item.PointsBalance,
        LifetimePoints = item.LifetimePoints,
        EnrollmentDate = item.EnrollmentDate,
        TotalStayCount = item.TotalStayCount,
        TotalRevenue = item.TotalRevenue,
        AverageSpend = item.AverageSpend,
        Preferences = item.Preferences,
        Birthday = item.Birthday,
        Transactions = new ObservableCollection<LoyaltyTransactionDto>(item.Transactions.Select(x => new LoyaltyTransactionDto { Type = x.Type.ToString(), Points = x.Points, Description = x.Description, OccurredAt = x.OccurredAtUtc }))
    };

    partial void OnSelectedMemberChanged(VipProfileDto? value) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasError));
}
