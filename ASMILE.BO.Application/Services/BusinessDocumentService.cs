using ASMILE.BO.Application.DTOs;
using System.Diagnostics;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Shared.Exceptions;
using ASMILE.BO.Shared.Storage;

namespace ASMILE.BO.Application.Services;

public sealed class BusinessDocumentService : IBusinessDocumentService
{
    private readonly IBusinessDocumentRepository _repository;
    private readonly IPathSecurityService _pathSecurity;
    private readonly ISettingsService _settingsService;

    public BusinessDocumentService(
        IBusinessDocumentRepository repository,
        IPathSecurityService pathSecurity,
        ISettingsService settingsService)
    {
        _repository = repository;
        _pathSecurity = pathSecurity;
        _settingsService = settingsService;
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByEntityAsync(BusinessEntityType entityType, Guid entityId)
        => _repository.GetByEntityAsync(entityType, entityId);

    public Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsBySupplierAsync(Guid supplierId)
    {
        return _repository.GetBySupplierAsync(supplierId);
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByCustomerAsync(Guid customerId)
    {
        return _repository.GetByCustomerAsync(customerId);
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByDateAsync(DateTime date)
    {
        return _repository.GetByDateAsync(date);
    }

    public Task<BusinessDocumentDto?> GetDocumentAsync(Guid documentId)
    {
        return _repository.GetByIdAsync(documentId);
    }

    public async Task<BusinessDocumentDto> UploadAttachmentAsync(
        BusinessEntityType entityType,
        Guid entityId,
        string title,
        string sourceFilePath,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        if (entityId == Guid.Empty) throw new AppValidationException("DOCUMENT_ENTITY_REQUIRED");
        if (string.IsNullOrWhiteSpace(sourceFilePath) || !File.Exists(sourceFilePath))
            throw new AppValidationException("DOCUMENT_FILE_NOT_FOUND");
        if (!_pathSecurity.IsAllowedExtension(sourceFilePath))
            throw new AppValidationException("DOCUMENT_EXTENSION_NOT_ALLOWED");

        var fileInfo = new FileInfo(sourceFilePath);
        if (fileInfo.Length > 20 * 1024 * 1024)
            throw new AppValidationException("DOCUMENT_FILE_TOO_LARGE");

        var extension = fileInfo.Extension.ToLowerInvariant();
        var fileName = _pathSecurity.SanitizeFileName(fileInfo.Name);
        var relativePath = Path.Combine("BusinessDocuments", entityId.ToString("N"), $"{Guid.NewGuid():N}{extension}");
        var storageRoot = await GetConfiguredStorageRootAsync();
        var destination = _pathSecurity.ResolveStoragePath(storageRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(sourceFilePath, destination, overwrite: false);

        try
        {
            var now = DateTime.UtcNow;
            var document = new BusinessDocument
            {
                Code = $"ATT-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
                Title = string.IsNullOrWhiteSpace(title) ? fileName : title.Trim(),
                Module = BusinessModule.Logs,
                EntityType = entityType,
                EntityId = entityId,
                DocumentType = BusinessDocumentType.Attachment,
                DocumentDate = now,
                VirtualPath = relativePath.Replace('\\', '/'),
                PhysicalFilePath = destination,
                FileName = fileName,
                FileExtension = extension,
                FileSize = fileInfo.Length,
                CreatedBy = string.IsNullOrWhiteSpace(createdBy) ? "system" : createdBy.Trim(),
                CreatedAt = now,
                UpdatedAt = now
            };
            return await _repository.AddAsync(document, cancellationToken);
        }
        catch
        {
            try { File.Delete(destination); } catch { /* preserve original database error */ }
            throw;
        }
    }

    public async Task PrintAsync(Guid documentId)
    {
        var document = await _repository.GetByIdAsync(documentId);
        if (document is null)
        {
            throw new AppValidationException("Không tìm thấy chứng từ để in.");
        }

        var sourcePath = await ResolveDocumentPathAsync(document);
        if (!File.Exists(sourcePath))
            throw new AppValidationException("DOCUMENT_FILE_NOT_FOUND");

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = sourcePath,
                Verb = "print",
                UseShellExecute = true,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            throw new AppValidationException($"PRINT_COMMAND_FAILED:{ex.Message}");
        }
    }

    public async Task ExportPdfAsync(Guid documentId)
    {
        var document = await _repository.GetByIdAsync(documentId);
        if (document is null)
        {
            throw new AppValidationException("Không tìm thấy chứng từ để xuất PDF.");
        }

        var sourcePath = await ResolveDocumentPathAsync(document);
        if (!File.Exists(sourcePath))
            throw new AppValidationException("DOCUMENT_FILE_NOT_FOUND");

        if (!string.Equals(Path.GetExtension(sourcePath), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new AppValidationException($"PDF_CONVERSION_NOT_SUPPORTED:{Path.GetExtension(sourcePath)}");

        var exportDirectory = ApplicationDataPaths.GetUserExportPath("business_documents");
        Directory.CreateDirectory(exportDirectory);
        var safeName = _pathSecurity.SanitizeFileName(
            string.IsNullOrWhiteSpace(document.FileName) ? document.Title : document.FileName);
        var outputName = $"{Path.GetFileNameWithoutExtension(safeName)}_{DateTime.Now:yyyyMMdd_HHmmss}_{document.Id:N}.pdf";
        var outputPath = Path.Combine(exportDirectory, outputName);
        File.Copy(sourcePath, outputPath, overwrite: false);
    }

    public async Task<string?> ResolvePhysicalPathAsync(Guid documentId)
    {
        var document = await _repository.GetByIdAsync(documentId);
        if (document is null)
        {
            return null;
        }

        try
        {
            var path = await ResolveDocumentPathAsync(document);
            return File.Exists(path) ? path : null;
        }
        catch (AppValidationException)
        {
            return null;
        }
    }

    private async Task<string> ResolveDocumentPathAsync(BusinessDocumentDto document)
    {
        var configuredRoot = await GetConfiguredStorageRootAsync();
        var legacyRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Storage"));

        // VirtualPath is the Explorer/catalog location, not necessarily a disk path.
        // Prefer the explicit physical location and only use VirtualPath for legacy
        // storage-relative records (never for rooted drive/UNC paths).
        var physicalPath = document.PhysicalFilePath?.Trim();
        var relativePath = string.IsNullOrWhiteSpace(physicalPath)
            || physicalPath.StartsWith("dbdoc://", StringComparison.OrdinalIgnoreCase)
            || physicalPath.StartsWith("storage://", StringComparison.OrdinalIgnoreCase)
            || physicalPath.StartsWith("vfc://", StringComparison.OrdinalIgnoreCase)
                ? document.VirtualPath?.Trim()
                : physicalPath;

        if (string.IsNullOrWhiteSpace(relativePath)
            || relativePath.StartsWith("dbdoc://", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("storage://", StringComparison.OrdinalIgnoreCase)
            || relativePath.StartsWith("vfc://", StringComparison.OrdinalIgnoreCase))
            throw new AppValidationException("DOCUMENT_PATH_REQUIRED");

        try
        {
            if (!Path.IsPathRooted(relativePath))
            {
                var configuredPath = _pathSecurity.ResolveStoragePath(configuredRoot, relativePath);
                if (File.Exists(configuredPath))
                {
                    return configuredPath;
                }

                // Records created before Storage.DataRootPath was applied stored
                // relative paths under the original application-local Storage folder.
                var legacyPath = _pathSecurity.ResolveStoragePath(legacyRoot, relativePath);
                return File.Exists(legacyPath) ? legacyPath : configuredPath;
            }

            var path = Path.GetFullPath(relativePath);
            if (IsUnderRoot(configuredRoot, path) || IsUnderRoot(legacyRoot, path))
            {
                return path;
            }

            throw new UnauthorizedAccessException("Document path is outside configured storage roots.");
        }
        catch (UnauthorizedAccessException)
        {
            // Legacy absolute drive paths in VirtualPath are logical catalog paths.
            // Report a missing physical file without leaking or opening an external path.
            throw new AppValidationException("DOCUMENT_FILE_NOT_FOUND");
        }
    }

    private async Task<string> GetConfiguredStorageRootAsync()
    {
        var setting = await _settingsService.GetValueAsync("Storage.DataRootPath", SettingScope.System, null);
        var configuredPath = setting?.Value?.Trim();
        if (string.IsNullOrWhiteSpace(configuredPath) || !Path.IsPathRooted(configuredPath))
        {
            throw new AppValidationException("STORAGE_ROOT_NOT_CONFIGURED");
        }

        try
        {
            return Path.GetFullPath(configuredPath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new AppValidationException("STORAGE_ROOT_INVALID");
        }
    }

    private bool IsUnderRoot(string root, string path)
    {
        try
        {
            _pathSecurity.EnsureUnderRoot(root, path);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
