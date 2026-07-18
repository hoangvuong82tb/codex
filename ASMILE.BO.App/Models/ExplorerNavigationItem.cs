using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Models;

public sealed partial class ExplorerNavigationItem : ObservableObject
{
    public ExplorerNavigationItem(string id, string title, string virtualPath, string iconGlyph, int sortOrder = 0)
    {
        Id = id;
        Title = title;
        VirtualPath = virtualPath;
        IconGlyph = iconGlyph;
        SortOrder = sortOrder;
    }

    public string Id { get; }
    public string Title { get; }
    public string VirtualPath { get; }
    public string IconGlyph { get; }
    public int SortOrder { get; }

    [ObservableProperty]
    private bool isSelected;
}

