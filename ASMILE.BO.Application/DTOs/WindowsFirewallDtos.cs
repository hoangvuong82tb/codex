namespace ASMILE.BO.Application.DTOs;

public sealed class WindowsFirewallSnapshotDto
{
    public bool IsAvailable { get; init; }
    public string ProviderStatus { get; init; } = string.Empty;
    public int RuleCount { get; init; }
    public bool IsTruncated { get; init; }
    public IReadOnlyList<WindowsFirewallRuleDto> Rules { get; init; } = Array.Empty<WindowsFirewallRuleDto>();
}

public sealed class WindowsFirewallRuleDto
{
    public string Name { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public string Protocol { get; init; } = string.Empty;
    public string LocalPorts { get; init; } = string.Empty;
    public string ApplicationName { get; init; } = string.Empty;
    public string ServiceName { get; init; } = string.Empty;
}
