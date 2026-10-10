using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class EfSecuritySettingsServiceTests
{
    [Fact]
    public async Task GetRequireAdminConfirmationAsync_reads_the_persisted_value()
    {
        var settings = new StubSettingsService
        {
            Value = new SettingValueDto
            {
                Key = "Security.RequireAdminConfirmation",
                Scope = SettingScope.System,
                Value = "false"
            }
        };
        var service = new EfSecuritySettingsService(settings);

        var result = await service.GetRequireAdminConfirmationAsync();

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRequireAdminConfirmationAsync_fails_safe_for_missing_or_invalid_values()
    {
        var settings = new StubSettingsService { Value = new SettingValueDto { Value = "not-a-boolean" } };
        var service = new EfSecuritySettingsService(settings);

        var result = await service.GetRequireAdminConfirmationAsync();

        result.Should().BeTrue();
    }

    private sealed class StubSettingsService : ISettingsService
    {
        public SettingValueDto? Value { get; init; }

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(Value);

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();

        public Task SaveValueAsync(SaveSettingValueRequest request)
            => throw new NotSupportedException();

        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests)
            => throw new NotSupportedException();

        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget)
            => throw new NotSupportedException();
    }
}
