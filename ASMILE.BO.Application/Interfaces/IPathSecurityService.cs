namespace ASMILE.BO.Application.Interfaces;

public interface IPathSecurityService
{
    string NormalizeVirtualPath(string virtualPath);
    bool ValidateVirtualPath(string virtualPath);
    string ResolveStoragePath(string root, string relativePath);
    void EnsureUnderRoot(string root, string fullPath);
    bool IsAllowedExtension(string fileName);
    string SanitizeFileName(string fileName);
}
