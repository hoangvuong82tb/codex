namespace ASMILE.BO.Shared.Storage;

/// <summary>
/// Writable per-user locations for settings, diagnostics, and generated exports.
/// These paths are deliberately kept outside the installed application directory.
/// </summary>
public static class ApplicationDataPaths
{
    private const string ProductFolderName = "ASMILE-BO";
    private static string? _userExportsDirectoryOverride;

    public static string UserDataRoot => Path.Combine(GetRequiredFolder(Environment.SpecialFolder.LocalApplicationData), ProductFolderName);

    public static string UserSettingsDirectory => Path.Combine(UserDataRoot, "Data", "Settings");

    public static string UserLogsDirectory => Path.Combine(UserDataRoot, "Logs");

    public static string UserLogFilePattern => Path.Combine(UserLogsDirectory, "asmile-bo-.log");

    public static string DefaultUserExportsDirectory
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return string.IsNullOrWhiteSpace(documents)
                ? Path.Combine(UserDataRoot, "Exports")
                : Path.Combine(documents, "ASMILE-BO", "Exports");
        }
    }

    public static string UserExportsDirectory
        => Volatile.Read(ref _userExportsDirectoryOverride) ?? DefaultUserExportsDirectory;

    public static void ConfigureUserExportsDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            Volatile.Write(ref _userExportsDirectoryOverride, null);
            return;
        }

        if (!Path.IsPathFullyQualified(path.Trim()))
        {
            throw new ArgumentException("The export directory must be an absolute local or UNC path.", nameof(path));
        }

        var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim()));
        Volatile.Write(ref _userExportsDirectoryOverride, fullPath);
    }

    public static string GetUserSettingsFile(string fileName)
        => Path.Combine(UserSettingsDirectory, fileName);

    public static string GetUserExportPath(params string[] segments)
        => CombineUnderRoot(UserExportsDirectory, segments);

    private static string CombineUnderRoot(string root, IReadOnlyList<string> segments)
    {
        var fullRoot = Path.GetFullPath(root);
        var candidate = Path.GetFullPath(Path.Combine([fullRoot, .. segments]));
        var prefix = fullRoot.EndsWith(Path.DirectorySeparatorChar)
            ? fullRoot
            : fullRoot + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("A path segment resolves outside the application data directory.", nameof(segments));
        }

        return candidate;
    }

    private static string GetRequiredFolder(Environment.SpecialFolder folder)
    {
        var path = Environment.GetFolderPath(folder);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"Windows did not provide the required {folder} directory.");
        }

        return path;
    }
}
