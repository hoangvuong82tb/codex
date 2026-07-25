using ASMILE.BO.Core.FrontOffice.Folio;
using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;
using ASMILE.BO.Core.FrontOffice.Posting;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class PostingEngineTests
{
    [Fact]
    public void POSTING_U_001_valid_posting_succeeds()
    {
        var pipeline = new PostingPipeline();
        var request = PostingRequest.Create(
            new PostingSource(Guid.NewGuid(), PostingSourceType.RoomCharge, null, null),
            Guid.NewGuid(), 500000, "Room night 2026-07-18", RevenueBucket.Rooms,
            new DateOnly(2026, 7, 18), DateTime.UtcNow, null);

        Guid capturedEntry = Guid.Empty;
        var result = pipeline.Process(request, (r, tax, sc, net) => capturedEntry = r.RequestId);

        result.IsSuccess.Should().BeTrue();
        result.TaxAmount.Should().Be(50000);
        result.ServiceChargeAmount.Should().Be(25000);
        result.NetAmount.Should().Be(575000);
    }

    [Fact]
    public void POSTING_U_002_duplicate_is_rejected()
    {
        var pipeline = new PostingPipeline();
        var request = PostingRequest.Create(
            new PostingSource(Guid.NewGuid(), PostingSourceType.RoomCharge, null, null),
            Guid.NewGuid(), 100000, "Test", RevenueBucket.Rooms,
            new DateOnly(2026, 7, 18), DateTime.UtcNow, null);

        pipeline.Process(request, (_, _, _, _) => { });
        var duplicate = pipeline.Process(request, (_, _, _, _) => { });

        duplicate.IsSuccess.Should().BeFalse();
        duplicate.ErrorCode.Should().Be("POSTING_DUPLICATE");
    }

    [Fact]
    public void POSTING_U_003_zero_amount_is_rejected()
    {
        var pipeline = new PostingPipeline();
        var request = PostingRequest.Create(
            new PostingSource(Guid.NewGuid(), PostingSourceType.Manual, null, null),
            Guid.NewGuid(), 0, "Zero test", RevenueBucket.Rooms,
            new DateOnly(2026, 7, 18), DateTime.UtcNow, null);

        var result = pipeline.Process(request, (_, _, _, _) => { });
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("POSTING_AMOUNT_INVALID");
    }

    [Fact]
    public void POSTING_U_004_custom_rule_blocks_posting()
    {
        var pipeline = new PostingPipeline();
        pipeline.AddRule(new PostingRule
        {
            RuleName = "NoMiniBar",
            Priority = 1,
            Validate = r => r.Source.SourceType == PostingSourceType.MiniBar
                ? "MiniBar postings not allowed" : null
        });

        var request = PostingRequest.Create(
            new PostingSource(Guid.NewGuid(), PostingSourceType.MiniBar, null, null),
            Guid.NewGuid(), 50000, "Minibar drink", RevenueBucket.MiniBar,
            new DateOnly(2026, 7, 18), new DateTime(2026, 7, 18, 23, 0, 0, DateTimeKind.Utc), null);

        var result = pipeline.Process(request, (_, _, _, _) => { });
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be("POSTING_RULE_VIOLATION");
    }

    [Fact]
    public void POSTING_U_005_source_maps_to_correct_bucket()
    {
        PostingPolicies.MapSourceToBucket(PostingSourceType.Restaurant).Should().Be(RevenueBucket.FoodAndBeverage);
        PostingPolicies.MapSourceToBucket(PostingSourceType.Banquet).Should().Be(RevenueBucket.FoodAndBeverage);
        PostingPolicies.MapSourceToBucket(PostingSourceType.Spa).Should().Be(RevenueBucket.SpaAndWellness);
        PostingPolicies.MapSourceToBucket(PostingSourceType.RoomCharge).Should().Be(RevenueBucket.Rooms);
        PostingPolicies.MapSourceToBucket(PostingSourceType.Manual).Should().Be(RevenueBucket.OtherRevenue);
    }

    [Fact]
    public void POSTING_U_006_tax_rates_by_bucket()
    {
        PostingPolicies.DefaultTaxRate(RevenueBucket.Rooms).Should().Be(10);
        PostingPolicies.DefaultTaxRate(RevenueBucket.FoodAndBeverage).Should().Be(8);
        PostingPolicies.DefaultTaxRate(RevenueBucket.OtherRevenue).Should().Be(0);
    }

    [Fact]
    public void POSTING_U_007_batch_processing_counts_results()
    {
        var pipeline = new PostingPipeline();
        var requests = new[]
        {
            PostingRequest.Create(new(Guid.NewGuid(), PostingSourceType.RoomCharge, null, null), Guid.NewGuid(), 500000, "Room 1", RevenueBucket.Rooms, new(2026,7,18), DateTime.UtcNow, null),
            PostingRequest.Create(new(Guid.NewGuid(), PostingSourceType.RoomCharge, null, null), Guid.NewGuid(), 0, "Invalid", RevenueBucket.Rooms, new(2026,7,18), DateTime.UtcNow, null),
            PostingRequest.Create(new(Guid.NewGuid(), PostingSourceType.Restaurant, null, null), Guid.NewGuid(), 200000, "Dinner", RevenueBucket.FoodAndBeverage, new(2026,7,18), DateTime.UtcNow, null),
        };

        var batch = pipeline.ProcessBatch(requests, (_, _, _, _) => { });
        batch.SuccessCount.Should().Be(2);
        batch.FailureCount.Should().Be(1);
    }

    [Fact]
    public void POSTING_U_008_idempotent_posting_same_request_id()
    {
        var pipeline = new PostingPipeline();
        var request = PostingRequest.Create(
            new PostingSource(Guid.NewGuid(), PostingSourceType.RoomCharge, null, null),
            Guid.NewGuid(), 300000, "Room night", RevenueBucket.Rooms,
            new DateOnly(2026, 7, 18), DateTime.UtcNow, null);

        var r1 = pipeline.Process(request, (_, _, _, _) => { });
        var r2 = pipeline.Process(request, (_, _, _, _) => { });
        r1.IsSuccess.Should().BeTrue();
        r2.ErrorCode.Should().Be("POSTING_DUPLICATE");
    }

    [Fact]
    public void POSTING_U_009_service_charge_only_for_rooms_and_fb()
    {
        PostingPolicies.DefaultServiceChargeRate(RevenueBucket.Rooms).Should().Be(5);
        PostingPolicies.DefaultServiceChargeRate(RevenueBucket.FoodAndBeverage).Should().Be(5);
        PostingPolicies.DefaultServiceChargeRate(RevenueBucket.Laundry).Should().Be(0);
    }
}
