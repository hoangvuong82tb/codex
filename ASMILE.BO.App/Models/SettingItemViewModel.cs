using ASMILE.BO.Core.Security;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Models;

public sealed partial class SettingItemViewModel : ObservableObject
{
    public SettingItemViewModel(
        string id,
        string iconGlyph,
        string title,
        string description,
        string value = "",
        string badge = "",
        string targetPageKey = "")
    {
        Id = id;
        IconGlyph = iconGlyph;
        Title = title;
        Description = description;
        Value = value;
        Badge = badge;
        TargetPageKey = targetPageKey;
        SearchKeywords = $"{title} {description} {value} {badge}";
    }

    public string Id { get; }
    public string IconGlyph { get; }
    public string Title { get; }
    public string Description { get; }
    public string SearchKeywords { get; }
    public string TargetPageKey { get; }

    [ObservableProperty] private string value = string.Empty;
    [ObservableProperty] private string badge = string.Empty;
    [ObservableProperty] private bool isToggle;
    [ObservableProperty] private bool toggleValue;
    [ObservableProperty] private bool isDanger;
    [ObservableProperty] private bool isClickable = true;
    [ObservableProperty] private bool isSensitive;
    [ObservableProperty] private bool requiresConfirmation;
    [ObservableProperty] private bool isReadOnly;
    [ObservableProperty] private Permission requiredPermission = Permission.ViewSystemSettings;
}
