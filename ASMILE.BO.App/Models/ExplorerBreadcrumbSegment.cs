namespace ASMILE.BO.App.Models;

public sealed class ExplorerBreadcrumbSegment
{
    public ExplorerBreadcrumbSegment(string title, string path, string iconGlyph = "\uE8B7", string? iconPath = null)
    {
        Title = title;
        Path = path;
        IconGlyph = iconGlyph;
        IconPath = iconPath;
    }

    public string Title { get; }
    public string Path { get; }
    public string IconGlyph { get; }
    public string? IconPath { get; }
    public bool ShowSeparator { get; set; }
}
