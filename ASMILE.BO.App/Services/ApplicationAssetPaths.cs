using System.IO;

namespace ASMILE.BO.App.Services;

/// <summary>
/// Resolves shipped visual assets relative to the application installation.
/// The desktop app must not depend on the developer's source-drive layout.
/// </summary>
public static class ApplicationAssetPaths
{
    public static string IconRoot => Path.Combine(AppContext.BaseDirectory, "Windows_Icon");

    public static string Icon(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Asset path is required.", nameof(relativePath));
        }

        return Path.Combine(
            IconRoot,
            relativePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }

    public static string Directory(string relativePath)
    {
        var path = Icon(relativePath);
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}
