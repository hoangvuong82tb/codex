using ASMILE.BO.Application.FrontOffice.Concierge;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ConciergeTests
{
    [Fact] public void CON_001_events_unique() => typeof(ConciergeEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems();
}
