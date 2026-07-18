using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.Models;

public sealed partial class AppSettingsItemViewModel : ObservableObject
{
    public required string AppKey { get; init; }
    public required string AppName { get; init; }
    public required string Description { get; init; }
    public required string IconPath { get; init; }
    public string Version { get; set; } = "1.0.0";
    public bool IsEnabled { get; set; } = true;
    public bool ShowOnDesktop { get; set; } = true;
    public bool PinToTaskbar { get; set; }
    public bool AllowClientAccess { get; set; } = true;
    public string DataPath { get; set; } = string.Empty;
    public string ExportPath { get; set; } = string.Empty;
    public string TemplatePath { get; set; } = string.Empty;
    public string PermissionGroup { get; set; } = "Admin";
    public string CacheSizeText { get; set; } = "128 MB";
}
