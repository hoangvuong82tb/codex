using System.Collections.ObjectModel;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Application.FrontOffice.HotelMasterData;
using ASMILE.BO.Core.FrontOffice.HotelMasterData;
using ASMILE.BO.Shared.Foundation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public enum HotelPartnerProfileMode { Company, TravelAgent, Commission }

public sealed record HotelPartnerRow
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public byte[] RowVersion { get; init; } = [];
}

public abstract partial class HotelPartnerProfileViewModel : ViewModelBase
{
    private readonly IHotelStructureMasterDataRepository? _repository;
    private readonly IFrontOfficeExecutionContextAccessor? _contextAccessor;
    private readonly CreateHotelCompanyProfileCommandHandler? _createCompany;
    private readonly UpdateHotelCompanyProfileCommandHandler? _updateCompany;
    private readonly ActivateHotelCompanyProfileCommandHandler? _activateCompany;
    private readonly DeactivateHotelCompanyProfileCommandHandler? _deactivateCompany;
    private readonly CreateHotelTravelAgentProfileCommandHandler? _createAgent;
    private readonly UpdateHotelTravelAgentProfileCommandHandler? _updateAgent;
    private readonly ActivateHotelTravelAgentProfileCommandHandler? _activateAgent;
    private readonly DeactivateHotelTravelAgentProfileCommandHandler? _deactivateAgent;
    private readonly CreateHotelCommissionRuleProfileCommandHandler? _createCommission;
    private readonly UpdateHotelCommissionRuleProfileCommandHandler? _updateCommission;
    private readonly ActivateHotelCommissionRuleProfileCommandHandler? _activateCommission;
    private readonly DeactivateHotelCommissionRuleProfileCommandHandler? _deactivateCommission;
    private bool _initialized;

    protected HotelPartnerProfileViewModel(IHotelStructureMasterDataRepository? repository, IFrontOfficeExecutionContextAccessor? contextAccessor,
        CreateHotelCompanyProfileCommandHandler? createCompany, UpdateHotelCompanyProfileCommandHandler? updateCompany, ActivateHotelCompanyProfileCommandHandler? activateCompany, DeactivateHotelCompanyProfileCommandHandler? deactivateCompany,
        CreateHotelTravelAgentProfileCommandHandler? createAgent, UpdateHotelTravelAgentProfileCommandHandler? updateAgent, ActivateHotelTravelAgentProfileCommandHandler? activateAgent, DeactivateHotelTravelAgentProfileCommandHandler? deactivateAgent,
        CreateHotelCommissionRuleProfileCommandHandler? createCommission, UpdateHotelCommissionRuleProfileCommandHandler? updateCommission, ActivateHotelCommissionRuleProfileCommandHandler? activateCommission, DeactivateHotelCommissionRuleProfileCommandHandler? deactivateCommission,
        HotelPartnerProfileMode mode)
    {
        _repository = repository; _contextAccessor = contextAccessor; _createCompany = createCompany; _updateCompany = updateCompany; _activateCompany = activateCompany; _deactivateCompany = deactivateCompany;
        _createAgent = createAgent; _updateAgent = updateAgent; _activateAgent = activateAgent; _deactivateAgent = deactivateAgent;
        _createCommission = createCommission; _updateCommission = updateCommission; _activateCommission = activateCommission; _deactivateCommission = deactivateCommission; Mode = mode;
    }

    public HotelPartnerProfileMode Mode { get; }
    public ObservableCollection<HotelPartnerRow> Items { get; } = [];
    public IReadOnlyList<string> KindOptions => Mode switch
    {
        HotelPartnerProfileMode.Company => Enum.GetNames<HotelCompanyKind>(),
        HotelPartnerProfileMode.TravelAgent => Enum.GetNames<HotelTravelAgentKind>(),
        _ => Enum.GetNames<HotelCommissionPartnerKind>()
    };
    public string PageTitle => Mode switch { HotelPartnerProfileMode.Company => "Công ty / đối tác doanh nghiệp", HotelPartnerProfileMode.TravelAgent => "Đại lý du lịch", _ => "Quy tắc hoa hồng" };
    public string PageDescription => Mode switch { HotelPartnerProfileMode.Company => "Quản lý hồ sơ công ty, credit, điều kiện hợp đồng và billing theo từng cơ sở.", HotelPartnerProfileMode.TravelAgent => "Quản lý đại lý, mã IATA, điều kiện thanh toán và quy tắc hoa hồng mặc định.", _ => "Quản lý cơ sở tính, tỷ lệ, giới hạn và phê duyệt hoa hồng theo đối tác." };
    public string AddLabel => Mode switch { HotelPartnerProfileMode.Company => "+ Thêm công ty", HotelPartnerProfileMode.TravelAgent => "+ Thêm đại lý", _ => "+ Thêm quy tắc" };
    public string SubCodeLabel => Mode switch { HotelPartnerProfileMode.Company => "Mã số thuế", HotelPartnerProfileMode.TravelAgent => "Mã IATA", _ => "Cơ sở tính" };
    public string SubCode2Label => Mode switch { HotelPartnerProfileMode.Company => "Giấy phép kinh doanh", HotelPartnerProfileMode.TravelAgent => "Số giấy phép", _ => "Mã tiền tệ" };
    public bool IsCompany => Mode == HotelPartnerProfileMode.Company;
    public bool IsTravelAgent => Mode == HotelPartnerProfileMode.TravelAgent;
    public bool IsCommission => Mode == HotelPartnerProfileMode.Commission;
    public bool IsNotCommission => !IsCommission;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool IsReadOnlyPanelVisible => !IsEditing;
    public string SelectedSummary => SelectedItem is null ? "Chọn một dòng để xem chi tiết." : $"{SelectedItem.Code} · {SelectedItem.Name}";
    public string SelectedStatus => SelectedItem?.IsActive == true ? "Đang sử dụng" : "Tạm ngưng";

    [ObservableProperty] private HotelPartnerRow? selectedItem;
    [ObservableProperty] private string searchText = string.Empty;
    [ObservableProperty] private string selectedKind = string.Empty;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private string errorMessage = string.Empty;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private string code = string.Empty;
    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string kind = string.Empty;
    [ObservableProperty] private string subCode = string.Empty;
    [ObservableProperty] private string subCode2 = string.Empty;
    [ObservableProperty] private string contactName = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string currencyCode = "VND";
    [ObservableProperty] private string creditLimit = "0";
    [ObservableProperty] private string creditDays = "30";
    [ObservableProperty] private string paymentTerms = "Prepaid";
    [ObservableProperty] private string commissionRuleCode = string.Empty;
    [ObservableProperty] private string defaultMarketSegmentCode = string.Empty;
    [ObservableProperty] private string ratePercent = "0";
    [ObservableProperty] private string fixedAmount = "0";
    [ObservableProperty] private bool includeTax;
    [ObservableProperty] private bool includeServiceCharge;
    [ObservableProperty] private bool approvalRequired;
    [ObservableProperty] private string effectiveFrom = DateTime.Today.ToString("yyyy-MM-dd");
    [ObservableProperty] private string effectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd");
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private bool requireContract;
    [ObservableProperty] private bool requirePurchaseOrder;

    public async Task InitializeAsync(CancellationToken ct) { if (_initialized) return; _initialized = true; await LoadAsync(ct); }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true; ErrorMessage = string.Empty;
        try
        {
            if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId || propertyId == Guid.Empty) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; }
            Items.Clear();
            switch (Mode)
            {
                case HotelPartnerProfileMode.Company:
                    foreach (var x in (await _repository.SearchCompanyProfilesAsync(propertyId, ParseCompanyKind(SelectedKind), NullIfEmpty(SearchText), null, new PageRequest(1, 200), "code", ct)).Items) Items.Add(new HotelPartnerRow { Id = x.HotelCompanyProfileId, Code = x.Code, Name = x.Name, Kind = x.Kind.ToString(), Details = $"MST: {x.TaxCode ?? "—"} · Credit: {x.CreditLimit:N0} {x.CurrencyCode}", Status = x.IsActive ? "Đang dùng" : "Tạm ngưng", IsActive = x.IsActive, RowVersion = x.RowVersion ?? [] });
                    InfoMessage = $"Đã tải {Items.Count} công ty từ dữ liệu thật."; break;
                case HotelPartnerProfileMode.TravelAgent:
                    foreach (var x in (await _repository.SearchTravelAgentProfilesAsync(propertyId, ParseAgentKind(SelectedKind), NullIfEmpty(SearchText), null, new PageRequest(1, 200), "code", ct)).Items) Items.Add(new HotelPartnerRow { Id = x.HotelTravelAgentProfileId, Code = x.Code, Name = x.Name, Kind = x.Kind.ToString(), Details = $"IATA: {x.IataCode ?? "—"} · Rule: {x.CommissionRuleCode ?? "—"}", Status = x.IsActive ? "Đang dùng" : "Tạm ngưng", IsActive = x.IsActive, RowVersion = x.RowVersion ?? [] });
                    InfoMessage = $"Đã tải {Items.Count} đại lý từ dữ liệu thật."; break;
                default:
                    foreach (var x in (await _repository.SearchCommissionRuleProfilesAsync(propertyId, ParsePartnerKind(SelectedKind), NullIfEmpty(SearchText), null, new PageRequest(1, 200), "partner", ct)).Items) Items.Add(new HotelPartnerRow { Id = x.HotelCommissionRuleProfileId, Code = x.Code, Name = x.Name, Kind = $"{x.PartnerKind} · {x.Basis}", Details = $"{x.RatePercent:0.####}% · {x.FixedAmount:N0} {x.CurrencyCode}", Status = x.IsActive ? "Đang dùng" : "Tạm ngưng", IsActive = x.IsActive, RowVersion = x.RowVersion ?? [] });
                    InfoMessage = $"Đã tải {Items.Count} quy tắc hoa hồng từ dữ liệu thật."; break;
            }
            if (SelectedItem is null || Items.All(x => x.Id != SelectedItem.Id)) SelectedItem = Items.FirstOrDefault();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex) { ErrorMessage = $"Không tải được dữ liệu: {ex.Message}"; }
        finally { IsLoading = false; RaiseState(); }
    }

    [RelayCommand] private Task SearchAsync(CancellationToken ct) => LoadAsync(ct);
    [RelayCommand] private void NewItem() { SelectedItem = null; Code = Name = SubCode = SubCode2 = ContactName = Phone = Email = Address = CommissionRuleCode = DefaultMarketSegmentCode = Description = string.Empty; Kind = KindOptions.FirstOrDefault() ?? string.Empty; if (IsCommission) SubCode = Enum.GetNames<HotelCommissionBasis>().FirstOrDefault() ?? string.Empty; CurrencyCode = "VND"; CreditLimit = RatePercent = FixedAmount = "0"; CreditDays = "30"; PaymentTerms = "Prepaid"; IncludeTax = IncludeServiceCharge = ApprovalRequired = RequireContract = RequirePurchaseOrder = false; EffectiveFrom = DateTime.Today.ToString("yyyy-MM-dd"); EffectiveTo = DateTime.Today.AddYears(1).ToString("yyyy-MM-dd"); IsEditing = true; RaiseState(); }
    [RelayCommand] private async Task EditItemAsync(CancellationToken ct) { if (SelectedItem is null) return; await LoadEditDetailsAsync(SelectedItem.Id, ct); if (string.IsNullOrWhiteSpace(ErrorMessage)) { IsEditing = true; RaiseState(); } }
    [RelayCommand] private void CancelEdit() { IsEditing = false; RaiseState(); }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (!DateTime.TryParse(EffectiveFrom, out var from) || !DateTime.TryParse(EffectiveTo, out var to)) { ErrorMessage = "Khoảng hiệu lực không hợp lệ."; RaiseState(); return; }
        try
        {
            if (Mode == HotelPartnerProfileMode.Company) await SaveCompany(from, to, ct);
            else if (Mode == HotelPartnerProfileMode.TravelAgent) await SaveAgent(from, to, ct);
            else await SaveCommission(from, to, ct);
        }
        catch (Exception ex) { ErrorMessage = $"Không lưu được dữ liệu: {ex.Message}"; }
        finally { RaiseState(); }
    }

    private async Task SaveCompany(DateTime from, DateTime to, CancellationToken ct)
    {
        if (!Enum.TryParse<HotelCompanyKind>(Kind, true, out var parsed) || !decimal.TryParse(CreditLimit, out var limit) || !int.TryParse(CreditDays, out var days)) { ErrorMessage = "Loại công ty hoặc credit không hợp lệ."; return; }
        Result<HotelCompanyProfileDto> result = SelectedItem is null ? _createCompany is null ? Missing<HotelCompanyProfileDto>() : await _createCompany.HandleAsync(new(Code, Name, parsed, NullIfEmpty(SubCode), NullIfEmpty(SubCode2), NullIfEmpty(ContactName), NullIfEmpty(Phone), NullIfEmpty(Email), NullIfEmpty(Address), CurrencyCode, limit, days, RequireContract, RequirePurchaseOrder, from, to, NullIfEmpty(Description)), ct) : _updateCompany is null ? Missing<HotelCompanyProfileDto>() : await _updateCompany.HandleAsync(new(SelectedItem.Id, Code, Name, parsed, NullIfEmpty(SubCode), NullIfEmpty(SubCode2), NullIfEmpty(ContactName), NullIfEmpty(Phone), NullIfEmpty(Email), NullIfEmpty(Address), CurrencyCode, limit, days, RequireContract, RequirePurchaseOrder, from, to, NullIfEmpty(Description), SelectedItem.RowVersion), ct);
        Complete(result, "công ty");
    }

    private async Task SaveAgent(DateTime from, DateTime to, CancellationToken ct)
    {
        if (!Enum.TryParse<HotelTravelAgentKind>(Kind, true, out var parsed) || !decimal.TryParse(CreditLimit, out var limit)) { ErrorMessage = "Loại đại lý hoặc credit không hợp lệ."; return; }
        Result<HotelTravelAgentProfileDto> result = SelectedItem is null ? _createAgent is null ? Missing<HotelTravelAgentProfileDto>() : await _createAgent.HandleAsync(new(Code, Name, parsed, NullIfEmpty(SubCode), NullIfEmpty(SubCode2), NullIfEmpty(ContactName), NullIfEmpty(Phone), NullIfEmpty(Email), NullIfEmpty(Address), NullIfEmpty(CommissionRuleCode), NullIfEmpty(DefaultMarketSegmentCode), PaymentTerms, limit, from, to, NullIfEmpty(Description)), ct) : _updateAgent is null ? Missing<HotelTravelAgentProfileDto>() : await _updateAgent.HandleAsync(new(SelectedItem.Id, Code, Name, parsed, NullIfEmpty(SubCode), NullIfEmpty(SubCode2), NullIfEmpty(ContactName), NullIfEmpty(Phone), NullIfEmpty(Email), NullIfEmpty(Address), NullIfEmpty(CommissionRuleCode), NullIfEmpty(DefaultMarketSegmentCode), PaymentTerms, limit, from, to, NullIfEmpty(Description), SelectedItem.RowVersion), ct);
        Complete(result, "đại lý");
    }

    private async Task SaveCommission(DateTime from, DateTime to, CancellationToken ct)
    {
        if (!Enum.TryParse<HotelCommissionPartnerKind>(Kind, true, out var partner) || !Enum.TryParse<HotelCommissionBasis>(SubCode, true, out var basis) || !decimal.TryParse(RatePercent, out var rate) || !decimal.TryParse(FixedAmount, out var fixedValue)) { ErrorMessage = "Đối tượng, cơ sở tính hoặc giá trị hoa hồng không hợp lệ."; return; }
        Result<HotelCommissionRuleProfileDto> result = SelectedItem is null ? _createCommission is null ? Missing<HotelCommissionRuleProfileDto>() : await _createCommission.HandleAsync(new(Code, Name, partner, basis, rate, fixedValue, CurrencyCode, IncludeTax, IncludeServiceCharge, null, null, ApprovalRequired, from, to, NullIfEmpty(Description)), ct) : _updateCommission is null ? Missing<HotelCommissionRuleProfileDto>() : await _updateCommission.HandleAsync(new(SelectedItem.Id, Code, Name, partner, basis, rate, fixedValue, CurrencyCode, IncludeTax, IncludeServiceCharge, null, null, ApprovalRequired, from, to, NullIfEmpty(Description), SelectedItem.RowVersion), ct);
        Complete(result, "quy tắc hoa hồng");
    }

    [RelayCommand]
    private async Task ToggleActiveAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        if (Mode == HotelPartnerProfileMode.Company) { var r = SelectedItem.IsActive ? _deactivateCompany is null ? Missing<HotelCompanyProfileDto>() : await _deactivateCompany.HandleAsync(new(SelectedItem.Id, SelectedItem.RowVersion), ct) : _activateCompany is null ? Missing<HotelCompanyProfileDto>() : await _activateCompany.HandleAsync(new(SelectedItem.Id, SelectedItem.RowVersion), ct); Complete(r, "trạng thái công ty"); }
        else if (Mode == HotelPartnerProfileMode.TravelAgent) { var r = SelectedItem.IsActive ? _deactivateAgent is null ? Missing<HotelTravelAgentProfileDto>() : await _deactivateAgent.HandleAsync(new(SelectedItem.Id, SelectedItem.RowVersion), ct) : _activateAgent is null ? Missing<HotelTravelAgentProfileDto>() : await _activateAgent.HandleAsync(new(SelectedItem.Id, SelectedItem.RowVersion), ct); Complete(r, "trạng thái đại lý"); }
        else { var r = SelectedItem.IsActive ? _deactivateCommission is null ? Missing<HotelCommissionRuleProfileDto>() : await _deactivateCommission.HandleAsync(new(SelectedItem.Id, SelectedItem.RowVersion), ct) : _activateCommission is null ? Missing<HotelCommissionRuleProfileDto>() : await _activateCommission.HandleAsync(new(SelectedItem.Id, SelectedItem.RowVersion), ct); Complete(r, "trạng thái quy tắc"); }
        if (string.IsNullOrWhiteSpace(ErrorMessage)) await LoadAsync(ct);
    }

    private void Complete<T>(Result<T> result, string label) { if (result.IsFailure) { ErrorMessage = result.Error?.Message ?? $"Không lưu được {label}."; return; } InfoMessage = $"Đã lưu {label} vào dữ liệu thật."; IsEditing = false; }
    private async Task LoadEditDetailsAsync(Guid id, CancellationToken ct)
    {
        ErrorMessage = string.Empty;
        if (_repository is null || _contextAccessor?.Current?.Property.PropertyId.Value is not { } propertyId) { ErrorMessage = "Chưa có kết nối dữ liệu hoặc property context."; return; }
        var selected = SelectedItem;
        if (selected is null) { ErrorMessage = "Chưa chọn bản ghi để chỉnh sửa."; return; }
        try
        {
            if (Mode == HotelPartnerProfileMode.Company && await _repository.GetCompanyProfileDtoAsync(propertyId, id, ct) is { } company)
            { Code = company.Code; Name = company.Name; Kind = company.Kind.ToString(); SubCode = company.TaxCode ?? string.Empty; SubCode2 = company.BusinessLicense ?? string.Empty; ContactName = company.ContactName ?? string.Empty; Phone = company.Phone ?? string.Empty; Email = company.Email ?? string.Empty; Address = company.Address ?? string.Empty; CurrencyCode = company.CurrencyCode; CreditLimit = company.CreditLimit.ToString("0.##"); CreditDays = company.CreditDays.ToString(); RequireContract = company.RequireContract; RequirePurchaseOrder = company.RequirePurchaseOrder; EffectiveFrom = company.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = company.EffectiveTo.ToString("yyyy-MM-dd"); Description = company.Description ?? string.Empty; SelectedItem = selected with { RowVersion = company.RowVersion ?? [] }; }
            else if (Mode == HotelPartnerProfileMode.TravelAgent && await _repository.GetTravelAgentProfileDtoAsync(propertyId, id, ct) is { } agent)
            { Code = agent.Code; Name = agent.Name; Kind = agent.Kind.ToString(); SubCode = agent.IataCode ?? string.Empty; SubCode2 = agent.LicenseNumber ?? string.Empty; ContactName = agent.ContactName ?? string.Empty; Phone = agent.Phone ?? string.Empty; Email = agent.Email ?? string.Empty; Address = agent.Address ?? string.Empty; CommissionRuleCode = agent.CommissionRuleCode ?? string.Empty; DefaultMarketSegmentCode = agent.DefaultMarketSegmentCode ?? string.Empty; PaymentTerms = agent.PaymentTerms; CreditLimit = agent.CreditLimit.ToString("0.##"); EffectiveFrom = agent.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = agent.EffectiveTo.ToString("yyyy-MM-dd"); Description = agent.Description ?? string.Empty; SelectedItem = selected with { RowVersion = agent.RowVersion ?? [] }; }
            else if (Mode == HotelPartnerProfileMode.Commission && await _repository.GetCommissionRuleProfileDtoAsync(propertyId, id, ct) is { } commission)
            { Code = commission.Code; Name = commission.Name; Kind = commission.PartnerKind.ToString(); SubCode = commission.Basis.ToString(); SubCode2 = commission.CurrencyCode; CurrencyCode = commission.CurrencyCode; RatePercent = commission.RatePercent.ToString("0.####"); FixedAmount = commission.FixedAmount.ToString("0.##"); IncludeTax = commission.IncludeTax; IncludeServiceCharge = commission.IncludeServiceCharge; ApprovalRequired = commission.ApprovalRequired; EffectiveFrom = commission.EffectiveFrom.ToString("yyyy-MM-dd"); EffectiveTo = commission.EffectiveTo.ToString("yyyy-MM-dd"); Description = commission.Description ?? string.Empty; SelectedItem = selected with { RowVersion = commission.RowVersion ?? [] }; }
            else ErrorMessage = "Không tìm thấy bản ghi để chỉnh sửa.";
        }
        catch (Exception ex) { ErrorMessage = $"Không đọc được dữ liệu chỉnh sửa: {ex.Message}"; }
    }
    private static HotelCompanyKind? ParseCompanyKind(string value) => Enum.TryParse<HotelCompanyKind>(value, true, out var x) ? x : null;
    private static HotelTravelAgentKind? ParseAgentKind(string value) => Enum.TryParse<HotelTravelAgentKind>(value, true, out var x) ? x : null;
    private static HotelCommissionPartnerKind? ParsePartnerKind(string value) => Enum.TryParse<HotelCommissionPartnerKind>(value, true, out var x) ? x : null;
    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static Result<T> Missing<T>() => Result<T>.Failure(Error.System("HOTEL_PARTNER_COMMAND_MISSING", "Partner command is unavailable."));
    private void RaiseState() { OnPropertyChanged(nameof(PageTitle)); OnPropertyChanged(nameof(PageDescription)); OnPropertyChanged(nameof(AddLabel)); OnPropertyChanged(nameof(SubCodeLabel)); OnPropertyChanged(nameof(SubCode2Label)); OnPropertyChanged(nameof(IsCompany)); OnPropertyChanged(nameof(IsTravelAgent)); OnPropertyChanged(nameof(IsCommission)); OnPropertyChanged(nameof(IsNotCommission)); OnPropertyChanged(nameof(HasError)); OnPropertyChanged(nameof(IsReadOnlyPanelVisible)); OnPropertyChanged(nameof(SelectedSummary)); OnPropertyChanged(nameof(SelectedStatus)); }
    partial void OnSelectedItemChanged(HotelPartnerRow? value) { RaiseState(); }
}

public sealed class HotelCompanyProfileViewModel : HotelPartnerProfileViewModel
{
    public HotelCompanyProfileViewModel(IHotelStructureMasterDataRepository? repository, IFrontOfficeExecutionContextAccessor? contextAccessor, CreateHotelCompanyProfileCommandHandler? createCompany, UpdateHotelCompanyProfileCommandHandler? updateCompany, ActivateHotelCompanyProfileCommandHandler? activateCompany, DeactivateHotelCompanyProfileCommandHandler? deactivateCompany, CreateHotelTravelAgentProfileCommandHandler? createAgent, UpdateHotelTravelAgentProfileCommandHandler? updateAgent, ActivateHotelTravelAgentProfileCommandHandler? activateAgent, DeactivateHotelTravelAgentProfileCommandHandler? deactivateAgent, CreateHotelCommissionRuleProfileCommandHandler? createCommission, UpdateHotelCommissionRuleProfileCommandHandler? updateCommission, ActivateHotelCommissionRuleProfileCommandHandler? activateCommission, DeactivateHotelCommissionRuleProfileCommandHandler? deactivateCommission) : base(repository, contextAccessor, createCompany, updateCompany, activateCompany, deactivateCompany, createAgent, updateAgent, activateAgent, deactivateAgent, createCommission, updateCommission, activateCommission, deactivateCommission, HotelPartnerProfileMode.Company) { }
}

public sealed class HotelTravelAgentProfileViewModel : HotelPartnerProfileViewModel
{
    public HotelTravelAgentProfileViewModel(IHotelStructureMasterDataRepository? repository, IFrontOfficeExecutionContextAccessor? contextAccessor, CreateHotelCompanyProfileCommandHandler? createCompany, UpdateHotelCompanyProfileCommandHandler? updateCompany, ActivateHotelCompanyProfileCommandHandler? activateCompany, DeactivateHotelCompanyProfileCommandHandler? deactivateCompany, CreateHotelTravelAgentProfileCommandHandler? createAgent, UpdateHotelTravelAgentProfileCommandHandler? updateAgent, ActivateHotelTravelAgentProfileCommandHandler? activateAgent, DeactivateHotelTravelAgentProfileCommandHandler? deactivateAgent, CreateHotelCommissionRuleProfileCommandHandler? createCommission, UpdateHotelCommissionRuleProfileCommandHandler? updateCommission, ActivateHotelCommissionRuleProfileCommandHandler? activateCommission, DeactivateHotelCommissionRuleProfileCommandHandler? deactivateCommission) : base(repository, contextAccessor, createCompany, updateCompany, activateCompany, deactivateCompany, createAgent, updateAgent, activateAgent, deactivateAgent, createCommission, updateCommission, activateCommission, deactivateCommission, HotelPartnerProfileMode.TravelAgent) { }
}

public sealed class HotelCommissionRuleProfileViewModel : HotelPartnerProfileViewModel
{
    public HotelCommissionRuleProfileViewModel(IHotelStructureMasterDataRepository? repository, IFrontOfficeExecutionContextAccessor? contextAccessor, CreateHotelCompanyProfileCommandHandler? createCompany, UpdateHotelCompanyProfileCommandHandler? updateCompany, ActivateHotelCompanyProfileCommandHandler? activateCompany, DeactivateHotelCompanyProfileCommandHandler? deactivateCompany, CreateHotelTravelAgentProfileCommandHandler? createAgent, UpdateHotelTravelAgentProfileCommandHandler? updateAgent, ActivateHotelTravelAgentProfileCommandHandler? activateAgent, DeactivateHotelTravelAgentProfileCommandHandler? deactivateAgent, CreateHotelCommissionRuleProfileCommandHandler? createCommission, UpdateHotelCommissionRuleProfileCommandHandler? updateCommission, ActivateHotelCommissionRuleProfileCommandHandler? activateCommission, DeactivateHotelCommissionRuleProfileCommandHandler? deactivateCommission) : base(repository, contextAccessor, createCompany, updateCompany, activateCompany, deactivateCompany, createAgent, updateAgent, activateAgent, deactivateAgent, createCommission, updateCommission, activateCommission, deactivateCommission, HotelPartnerProfileMode.Commission) { }
}
