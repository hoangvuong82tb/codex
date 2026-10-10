using ASMILE.BO.App.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class SettingsConfirmationPolicyTests
{
    [Fact]
    public async Task Disabled_policy_skips_confirmation_but_allows_authorized_caller_to_continue()
    {
        var promptCalled = false;

        var result = await SettingsConfirmationPolicy.ConfirmIfRequiredAsync(
            requireConfirmation: false,
            confirmAsync: () =>
            {
                promptCalled = true;
                return Task.FromResult(false);
            });

        result.Should().BeTrue();
        promptCalled.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task Enabled_policy_returns_the_confirmation_result(bool confirmationResult, bool expected)
    {
        var promptCalls = 0;

        var result = await SettingsConfirmationPolicy.ConfirmIfRequiredAsync(
            requireConfirmation: true,
            confirmAsync: () =>
            {
                promptCalls++;
                return Task.FromResult(confirmationResult);
            });

        result.Should().Be(expected);
        promptCalls.Should().Be(1);
    }
}
