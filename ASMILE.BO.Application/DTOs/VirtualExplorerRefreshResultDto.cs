namespace ASMILE.BO.Application.DTOs;

public sealed class VirtualExplorerRefreshResultDto
{
    public int RootNodeCount { get; init; }
    public int DriveNodeCount { get; init; }
    public int DocumentCount { get; init; }
    public DateTime RefreshedAtUtc { get; init; }
}
