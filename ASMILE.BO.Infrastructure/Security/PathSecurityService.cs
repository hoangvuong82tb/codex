using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Security;

namespace ASMILE.BO.Infrastructure.Security;

public sealed class PathSecurityService : IPathSecurityService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".xlsx", ".xls", ".docx", ".doc", ".png", ".jpg", ".jpeg", ".txt", ".csv", ".xml", ".bak", ".zip", ".json", ".html"
    };

    public string NormalizeVirtualPath(string virtualPath)
    {
        var normalized = InputSanitizer.TrimAndLimit(virtualPath, ValidationConstants.MaxPathLength).Replace('/', '\\');
        normalized = normalized.Trim();
        return normalized;
    }

    public bool ValidateVirtualPath(string virtualPath)
    {
        var normalized = NormalizeVirtualPath(virtualPath);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (normalized.StartsWith("\\\\", StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    public string ResolveStoragePath(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new ArgumentException("Storage root is required.", nameof(root));
        }

        var rootFull = Path.GetFullPath(root);
        var relative = NormalizeVirtualPath(relativePath);
        if (Path.IsPathRooted(relative))
        {
            throw new UnauthorizedAccessException("Absolute path is not allowed.");
        }

        var combined = Path.GetFullPath(Path.Combine(rootFull, relative));
        EnsureUnderRoot(rootFull, combined);
        return combined;
    }

    public void EnsureUnderRoot(string root, string fullPath)
    {
        var rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var target = Path.GetFullPath(fullPath);
        if (!target.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Path traversal detected.");
        }
    }

    public bool IsAllowedExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrWhiteSpace(ext) && AllowedExtensions.Contains(ext);
    }

    public string SanitizeFileName(string fileName)
    {
        var input = InputSanitizer.TrimAndLimit(fileName, 200);
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(input.Where(c => !invalidChars.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized;
    }
}
