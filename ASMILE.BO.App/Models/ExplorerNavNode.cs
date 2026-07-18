using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.Models;

public sealed partial class ExplorerNavNode : ObservableObject
{
    public ExplorerNavNode(string id, string title, string iconGlyph, string? virtualPath = null, bool isHeader = false, string? iconPath = null)
    {
        Id = id;
        Title = title;
        IconGlyph = iconGlyph;
        VirtualPath = virtualPath;
        IsHeader = isHeader;
        IconPath = iconPath;
    }

    public string Id { get; }
    public string Title { get; }
    public string IconGlyph { get; }
    public string? IconPath { get; }
    public string? VirtualPath { get; }
    public bool IsHeader { get; }
    public ObservableCollection<ExplorerNavNode> Children { get; } = new();

    [ObservableProperty]
    private bool isExpanded = true;

    [ObservableProperty]
    private bool hasUnrealizedChildren;

    [ObservableProperty]
    private bool childrenLoaded;
}
