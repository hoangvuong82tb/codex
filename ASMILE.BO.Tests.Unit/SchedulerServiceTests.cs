using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class SchedulerServiceTests
{
    [Fact]
    public async Task DefaultDefinitions_IncludeEnterpriseAccountingJobs()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new SchedulerService(db);

        var definitions = await service.GetDefinitionsAsync();

        definitions.Select(x => x.JobCode).Should().Contain(["DEPRECIATION", "TOOL_ALLOCATION", "VAT_SYNC", "REPORT_GENERATION", "CLEANUP"]);
    }

    [Fact]
    public async Task ManualTriggerRetryAndCancel_ArePersisted()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new SchedulerService(db);

        var executionId = await service.ManualTriggerAsync("VAT_SYNC", "unit-test");
        await service.RetryAsync(executionId);
        await service.CancelAsync(executionId);

        var execution = (await service.GetExecutionsAsync()).Single(x => x.Id == executionId);
        execution.CancellationRequested.Should().BeTrue();
        execution.Status.Should().Be("Cancelling");
        db.JobRetries.Should().ContainSingle(x => x.JobExecutionId == executionId);
        db.JobLogs.Count(x => x.JobExecutionId == executionId).Should().BeGreaterThanOrEqualTo(2);
    }
}
