namespace ASMILE.BO.Application.DTOs;

public sealed class WindowsStartupAppsSnapshotDto
{
    public bool IsAvailable { get; init; }
    public string ProviderStatus { get; init; } = string.Empty;
    public IReadOnlyList<WindowsStartupAppEntryDto> Entries { get; init; } = Array.Empty<WindowsStartupAppEntryDto>();
}

public sealed class WindowsStartupAppEntryDto
{
    public string Name { get; init; } = string.Empty;
    public string Command { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
}
