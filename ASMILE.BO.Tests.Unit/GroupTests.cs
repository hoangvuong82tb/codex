using ASMILE.BO.Application.FrontOffice.Group;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class GroupTests
{
    // Real-data coverage lives in GroupReservationWorkflowTests and
    // GroupBillingRealDataTests. This test keeps the event contract check
    // without reintroducing synthetic service implementations.
    [Fact]
    public void GRP_010_events_unique()
    {
        typeof(GroupEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => f.GetValue(null))
            .Should()
            .OnlyHaveUniqueItems();
    }
}
