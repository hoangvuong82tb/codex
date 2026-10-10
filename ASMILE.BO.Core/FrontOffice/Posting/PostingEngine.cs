using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Posting;

// =============================================================================
// VALUE OBJECTS
// =============================================================================

public enum PostingSourceType { RoomCharge, Restaurant, Banquet, Spa, Laundry, Telephone, MiniBar, Manual, NightAudit, System }

public readonly record struct PostingSource(Guid PropertyId, PostingSourceType SourceType, Guid? ReferenceId, string? ReferenceNumber);

public readonly record struct PostingRequest(
    Guid RequestId,
    PostingSource Source,
    Guid TargetFolioId,
    decimal Amount,
    string Description,
    RevenueBucket RevenueBucket,
    DateOnly BusinessDate,
    DateTime CreatedAtUtc,
    Guid? CreatedBy,
    decimal TaxRate = 0,
    decimal ServiceChargeRate = 0
)
{
    public static PostingRequest Create(
        PostingSource source,
        Guid folioId,
        decimal amount,
        string description,
        RevenueBucket bucket,
        DateOnly businessDate,
        DateTime nowUtc,
        ActorId? actorId,
        decimal taxRate = 0,
        decimal serviceChargeRate = 0) =>
        new(Uuid7.NewGuid(), source, folioId, amount, description, bucket, businessDate, nowUtc, actorId?.Value, taxRate, serviceChargeRate);
}

public readonly record struct PostingResult(
    Guid RequestId,
    bool IsSuccess,
    Guid? FolioEntryId,
    string? ErrorCode,
    string? ErrorMessage,
    decimal TaxAmount,
    decimal ServiceChargeAmount,
    decimal NetAmount
)
{
    public static PostingResult Success(Guid requestId, Guid entryId, decimal tax, decimal serviceCharge, decimal net) =>
        new(requestId, true, entryId, null, null, tax, serviceCharge, net);

    public static PostingResult Failure(Guid requestId, string code, string message) =>
        new(requestId, false, null, code, message, 0, 0, 0);
}

public readonly record struct PostingBatchResult(
    IReadOnlyList<PostingResult> Results,
    int SuccessCount,
    int FailureCount,
    IReadOnlyList<string> Warnings
);

// =============================================================================
// POSTING RULE
// =============================================================================

public sealed class PostingRule
{
    public string RuleName { get; init; } = string.Empty;
    public int Priority { get; init; }
    public bool IsActive { get; set; } = true;
    public Func<PostingRequest, string?> Validate { get; init; } = _ => null;
    public Func<PostingRequest, decimal> CalculateTax { get; init; } = _ => 0;
    public Func<PostingRequest, decimal> CalculateServiceCharge { get; init; } = _ => 0;
}

// =============================================================================
// POSTING POLICY
// =============================================================================

public static class PostingPolicies
{
    public static RevenueBucket MapSourceToBucket(PostingSourceType source) => source switch
    {
        PostingSourceType.RoomCharge => RevenueBucket.Rooms,
        PostingSourceType.Restaurant => RevenueBucket.FoodAndBeverage,
        PostingSourceType.Banquet => RevenueBucket.FoodAndBeverage,
        PostingSourceType.Spa => RevenueBucket.SpaAndWellness,
        PostingSourceType.Laundry => RevenueBucket.Laundry,
        PostingSourceType.Telephone => RevenueBucket.Telephone,
        PostingSourceType.MiniBar => RevenueBucket.MiniBar,
        _ => RevenueBucket.OtherRevenue
    };

    public static bool IsAmountValid(decimal amount) => amount != 0 && amount >= -99_999_999 && amount <= 99_999_999;

    public static bool IsRateValid(decimal rate) => rate >= 0 && rate <= 1000;
}

// =============================================================================
// POSTING PIPELINE (DOMAIN SERVICE)
// =============================================================================

public sealed class PostingPipeline
{
    private readonly List<PostingRule> _rules = [];
    private readonly HashSet<Guid> _processedIds = [];

    public IReadOnlyList<PostingRule> Rules => _rules;

    public PostingPipeline AddRule(PostingRule rule) { _rules.Add(rule); return this; }

    public PostingResult Process(PostingRequest request, Action<PostingRequest, decimal, decimal, Guid> appendToFolio)
    {
        if (!_processedIds.Add(request.RequestId))
            return PostingResult.Failure(request.RequestId, "POSTING_DUPLICATE", $"Request {request.RequestId} already processed.");

        if (!PostingPolicies.IsAmountValid(request.Amount))
            return PostingResult.Failure(request.RequestId, "POSTING_AMOUNT_INVALID", $"Amount {request.Amount} is invalid.");

        if (!PostingPolicies.IsRateValid(request.TaxRate)
            || !PostingPolicies.IsRateValid(request.ServiceChargeRate))
            return PostingResult.Failure(request.RequestId, "POSTING_RATE_INVALID", "Tax and service-charge rates must be between 0 and 1000 percent.");

        // Run validation rules
        foreach (var rule in _rules.Where(r => r.IsActive).OrderBy(r => r.Priority))
        {
            var error = rule.Validate(request);
            if (error is not null) return PostingResult.Failure(request.RequestId, "POSTING_RULE_VIOLATION", error);
        }

        // Calculate tax and service charge
        var taxAmount = Math.Round(request.Amount * request.TaxRate / 100, 2);
        var scAmount = Math.Round(request.Amount * request.ServiceChargeRate / 100, 2);

        // Apply custom rules (override defaults)
        foreach (var rule in _rules.Where(r => r.IsActive))
        {
            var customTax = rule.CalculateTax(request);
            if (customTax != 0) taxAmount = customTax;
            var customSc = rule.CalculateServiceCharge(request);
            if (customSc != 0) scAmount = customSc;
        }

        var netAmount = request.Amount + taxAmount + scAmount;

        // Domain event for audit
        appendToFolio(request, taxAmount, scAmount, request.RequestId);

        return PostingResult.Success(request.RequestId, Guid.NewGuid(), taxAmount, scAmount, netAmount);
    }

    public PostingBatchResult ProcessBatch(IReadOnlyList<PostingRequest> requests, Action<PostingRequest, decimal, decimal, Guid> appendToFolio)
    {
        var results = new List<PostingResult>();
        foreach (var request in requests)
            results.Add(Process(request, appendToFolio));
        return new PostingBatchResult(results, results.Count(r => r.IsSuccess), results.Count(r => !r.IsSuccess), []);
    }
}
