using ASMILE.BO.Application.FrontOffice.Channel;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ChannelTests
{
    [Fact] public void CH_001_events_unique() => typeof(ChannelEvents).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).Select(f => f.GetValue(null)).Should().OnlyHaveUniqueItems();
}
