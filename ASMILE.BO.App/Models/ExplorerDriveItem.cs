namespace ASMILE.BO.App.Models;

public sealed class ExplorerDriveItemModel
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconGlyph { get; init; } = "\uE7F1";
    public string? IconPath { get; init; }
    public double UsedPercent { get; init; }
    public bool IsAlert { get; init; }
    public bool IsNetworkLocation { get; init; }
    public string VirtualPath { get; init; } = string.Empty;
}
