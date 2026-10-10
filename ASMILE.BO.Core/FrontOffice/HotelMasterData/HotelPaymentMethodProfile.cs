using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

public enum HotelPaymentMethodKind
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    Qr = 4,
    EWallet = 5,
    Credit = 6,
    Company = 7,
    Ota = 8,
    Deposit = 9
}

public sealed class HotelPaymentMethodProfile
{
    private HotelPaymentMethodProfile() { }

    public Guid HotelPaymentMethodProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public HotelPaymentMethodKind Kind { get; private set; }
    public string CurrencyCode { get; private set; } = "VND";
    public string? BankAccount { get; private set; }
    public string? CardTerminal { get; private set; }
    public decimal FeeRate { get; private set; }
    public string? AccountingMapping { get; private set; }
    public bool RequireReference { get; private set; }
    public bool RequireApproval { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelPaymentMethodProfile Create(PropertyId propertyId, string code, string name, HotelPaymentMethodKind kind,
        string currencyCode, string? bankAccount, string? cardTerminal, decimal feeRate, string? accountingMapping,
        bool requireReference, bool requireApproval, DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, kind, currencyCode, feeRate);
        return new()
        {
            HotelPaymentMethodProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value,
            Code = code.Trim().ToUpperInvariant(), Name = name.Trim(), Kind = kind,
            CurrencyCode = currencyCode.Trim().ToUpperInvariant(), BankAccount = Normalize(bankAccount),
            CardTerminal = Normalize(cardTerminal), FeeRate = feeRate, AccountingMapping = Normalize(accountingMapping),
            RequireReference = requireReference, RequireApproval = requireApproval, IsActive = true,
            CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedAtUtc = nowUtc, UpdatedBy = actorId?.Value
        };
    }

    public void Update(string code, string name, HotelPaymentMethodKind kind, string currencyCode, string? bankAccount,
        string? cardTerminal, decimal feeRate, string? accountingMapping, bool requireReference, bool requireApproval,
        DateTime nowUtc, ActorId? actorId)
    {
        Validate(code, name, kind, currencyCode, feeRate);
        Code = code.Trim().ToUpperInvariant(); Name = name.Trim(); Kind = kind;
        CurrencyCode = currencyCode.Trim().ToUpperInvariant(); BankAccount = Normalize(bankAccount);
        CardTerminal = Normalize(cardTerminal); FeeRate = feeRate; AccountingMapping = Normalize(accountingMapping);
        RequireReference = requireReference; RequireApproval = requireApproval; Touch(nowUtc, actorId);
    }

    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }

    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, HotelPaymentMethodKind kind, string currencyCode, decimal feeRate)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_PAYMENT_METHOD_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("HOTEL_PAYMENT_METHOD_NAME_REQUIRED");
        if (!Enum.IsDefined(kind)) throw new ArgumentException("HOTEL_PAYMENT_METHOD_KIND_INVALID");
        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length is < 3 or > 10) throw new ArgumentException("HOTEL_PAYMENT_METHOD_CURRENCY_INVALID");
        if (feeRate is < 0 or > 100) throw new ArgumentException("HOTEL_PAYMENT_METHOD_FEE_INVALID");
    }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
