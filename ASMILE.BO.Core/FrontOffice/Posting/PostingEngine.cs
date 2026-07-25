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
    Guid? CreatedBy
)
{
    public static PostingRequest Create(PostingSource source, Guid folioId, decimal amount, string description, RevenueBucket bucket, DateOnly businessDate, DateTime nowUtc, ActorId? actorId) =>
        new(Uuid7.NewGuid(), source, folioId, amount, description, bucket, businessDate, nowUtc, actorId?.Value);
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
    public static decimal DefaultTaxRate(RevenueBucket bucket) => bucket switch
    {
        RevenueBucket.Rooms => 10m,
        RevenueBucket.FoodAndBeverage => 8m,
        RevenueBucket.SpaAndWellness => 10m,
        RevenueBucket.Laundry => 10m,
        RevenueBucket.Telephone => 10m,
        RevenueBucket.MiniBar => 10m,
        _ => 0
    };

    public static decimal DefaultServiceChargeRate(RevenueBucket bucket) => bucket switch
    {
        RevenueBucket.Rooms => 5m,
        RevenueBucket.FoodAndBeverage => 5m,
        RevenueBucket.SpaAndWellness => 5m,
        _ => 0
    };

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

        var bucket = request.RevenueBucket == RevenueBucket.OtherRevenue
            ? PostingPolicies.MapSourceToBucket(request.Source.SourceType) : request.RevenueBucket;

        // Run validation rules
        foreach (var rule in _rules.Where(r => r.IsActive).OrderBy(r => r.Priority))
        {
            var error = rule.Validate(request);
            if (error is not null) return PostingResult.Failure(request.RequestId, "POSTING_RULE_VIOLATION", error);
        }

        // Calculate tax and service charge
        var taxRate = PostingPolicies.DefaultTaxRate(bucket);
        var scRate = PostingPolicies.DefaultServiceChargeRate(bucket);
        var taxAmount = Math.Round(request.Amount * taxRate / 100, 2);
        var scAmount = Math.Round(request.Amount * scRate / 100, 2);
        var netAmount = request.Amount + taxAmount + scAmount;

        // Apply custom rules (override defaults)
        foreach (var rule in _rules.Where(r => r.IsActive))
        {
            var customTax = rule.CalculateTax(request);
            if (customTax != 0) taxAmount = customTax;
            var customSc = rule.CalculateServiceCharge(request);
            if (customSc != 0) scAmount = customSc;
        }

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
