using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Enums;
using System.Globalization;
using System.IO;
using System.Windows.Data;

namespace ASMILE.BO.App.Converters;

public sealed class VirtualExplorerNodeIconConverter : IValueConverter
{
    private const string IconRoot = @"D:\ASMILE-BO\Windows_Icon\Icons";
    private const string FolderIcon = IconRoot + @"\folders\folder.ico";
    private const string FolderFilledIcon = IconRoot + @"\folders\filled.ico";
    private const string DriveIcon = IconRoot + @"\devices\drives\drive.ico";
    private const string NetworkIcon = IconRoot + @"\folders\network.ico";
    private const string DocumentIcon = IconRoot + @"\files\windowdesktop.ico";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not VirtualExplorerNodeDto node)
        {
            return FolderIcon;
        }

        var customIcon = ResolveCustomIcon(node.Icon);
        if (!string.IsNullOrWhiteSpace(customIcon))
        {
            return customIcon;
        }

        return node.NodeType switch
        {
            VirtualNodeType.Drive => DriveIcon,
            VirtualNodeType.NetworkItem => NetworkIcon,
            VirtualNodeType.Document => DocumentIcon,
            VirtualNodeType.Folder => node.HasChildren ? FolderFilledIcon : FolderIcon,
            VirtualNodeType.EntityFolder => node.HasChildren ? FolderFilledIcon : FolderIcon,
            VirtualNodeType.Report => node.HasChildren ? FolderFilledIcon : FolderIcon,
            VirtualNodeType.SystemTool => node.HasChildren ? FolderFilledIcon : FolderIcon,
            _ => FolderIcon
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private static string? ResolveCustomIcon(string icon)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return null;
        }

        if (Path.IsPathRooted(icon) && File.Exists(icon))
        {
            return icon;
        }

        var candidates = new[]
        {
            Path.Combine(IconRoot, "folders", icon),
            Path.Combine(IconRoot, "files", icon),
            Path.Combine(IconRoot, "devices", "drives", icon),
            Path.Combine(IconRoot, "applications", icon),
            Path.Combine(IconRoot, icon)
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
