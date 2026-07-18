namespace ASMILE.BO.App.Models;

public sealed class StartRecommendedItem
{
    public StartRecommendedItem(string iconGlyph, string title, string subtitle, string? moduleKey = null, string? quickActionKey = null)
    {
        IconGlyph = iconGlyph;
        Title = title;
        Subtitle = subtitle;
        ModuleKey = moduleKey;
        QuickActionKey = quickActionKey;
    }

    public string IconGlyph { get; }
    public string Title { get; }
    public string Subtitle { get; }
    public string? ModuleKey { get; }
    public string? QuickActionKey { get; }
}
