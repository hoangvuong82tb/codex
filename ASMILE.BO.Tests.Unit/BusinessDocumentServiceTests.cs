using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Application.Services;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Security;
using ASMILE.BO.Shared.Exceptions;
using ASMILE.BO.Shared.Storage;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class BusinessDocumentServiceTests
{
    [Fact]
    public async Task ExportPdfAsync_uses_physical_storage_path_instead_of_logical_catalog_path()
    {
        var id = Guid.NewGuid();
        var storageRoot = Path.Combine(AppContext.BaseDirectory, "Storage");
        var configuredRoot = Path.Combine(Path.GetTempPath(), $"ASMILE-BO-new-data-root-{Guid.NewGuid():N}");
        var sourceDirectory = Path.Combine(storageRoot, "BusinessDocuments", id.ToString("N"));
        var sourcePath = Path.Combine(sourceDirectory, "attachment.pdf");
        var exportDirectory = ApplicationDataPaths.GetUserExportPath("business_documents");
        Directory.CreateDirectory(sourceDirectory);
        await File.WriteAllTextAsync(sourcePath, "%PDF-1.4 test");

        var document = new BusinessDocumentDto
        {
            Id = id,
            Title = "Customer receipt",
            FileName = "attachment.pdf",
            VirtualPath = @"D:\ACCOUNTING\Khách hàng\CUS-001\Công nợ\2026\09",
            PhysicalFilePath = sourcePath
        };

        try
        {
            var service = new BusinessDocumentService(
                new SingleDocumentRepository(document),
                new PathSecurityService(),
                CreateSettings(configuredRoot));

            await service.ExportPdfAsync(id);

            Directory.GetFiles(exportDirectory, $"*_{id:N}.pdf").Should().ContainSingle();
            (await service.ResolvePhysicalPathAsync(id)).Should().Be(sourcePath);
        }
        finally
        {
            if (Directory.Exists(exportDirectory))
            {
                foreach (var file in Directory.GetFiles(exportDirectory, $"*_{id:N}.pdf")) File.Delete(file);
            }
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
            if (Directory.Exists(sourceDirectory)) Directory.Delete(sourceDirectory, recursive: false);
        }
    }

    [Fact]
    public async Task ExportPdfAsync_does_not_resolve_a_logical_drive_path_as_a_physical_file()
    {
        var id = Guid.NewGuid();
        var document = new BusinessDocumentDto
        {
            Id = id,
            Title = "Payment voucher",
            FileName = "PC-001.pdf",
            VirtualPath = @"D:\ACCOUNTING\Nhà cung cấp\NCC-001\Thanh toán\2026\09",
            PhysicalFilePath = $"dbdoc://{id:N}"
        };
        var service = new BusinessDocumentService(
            new SingleDocumentRepository(document),
            new PathSecurityService(),
            CreateSettings(Path.Combine(AppContext.BaseDirectory, "Storage")));

        var act = () => service.ExportPdfAsync(id);

        var exception = await Assert.ThrowsAsync<AppValidationException>(act);
        exception.Message.Should().Contain("DOCUMENT_FILE_NOT_FOUND");
    }

    [Fact]
    public async Task UploadAttachmentAsync_writes_to_configured_data_root()
    {
        var id = Guid.NewGuid();
        var configuredRoot = Path.Combine(Path.GetTempPath(), $"ASMILE-BO-document-root-{Guid.NewGuid():N}");
        var sourcePath = Path.Combine(Path.GetTempPath(), $"ASMILE-BO-document-source-{Guid.NewGuid():N}.pdf");
        var repository = new SingleDocumentRepository(new BusinessDocumentDto());
        await File.WriteAllTextAsync(sourcePath, "%PDF-1.4 test");

        try
        {
            var service = new BusinessDocumentService(
                repository,
                new PathSecurityService(),
                CreateSettings(configuredRoot));

            await service.UploadAttachmentAsync(
                BusinessEntityType.Customer,
                id,
                "Customer attachment",
                sourcePath,
                "unit-test");

            repository.AddedDocument.Should().NotBeNull();
            var storedDocument = repository.AddedDocument!;
            Path.GetFullPath(storedDocument.PhysicalFilePath!)
                .StartsWith(Path.GetFullPath(configuredRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                .Should().BeTrue();
            storedDocument.VirtualPath.Should().StartWith($"BusinessDocuments/{id:N}");
            File.Exists(storedDocument.PhysicalFilePath).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(configuredRoot)) Directory.Delete(configuredRoot, recursive: true);
            if (File.Exists(sourcePath)) File.Delete(sourcePath);
        }
    }

    private static ISettingsService CreateSettings(string storageRoot) => new FixedStorageSettingsService(storageRoot);

    private sealed class FixedStorageSettingsService(string storageRoot) : ISettingsService
    {
        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingDefinitionDto>>([]);

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingValueDto>>([]);

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult<SettingValueDto?>(key == "Storage.DataRootPath"
                ? new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = storageRoot, IsPersisted = true }
                : null);

        public Task SaveValueAsync(SaveSettingValueRequest request) => throw new NotSupportedException();
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => throw new NotSupportedException();
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => throw new NotSupportedException();
    }

    private sealed class SingleDocumentRepository(BusinessDocumentDto document) : IBusinessDocumentRepository
    {
        public BusinessDocument? AddedDocument { get; private set; }

        public Task<IReadOnlyList<BusinessDocumentDto>> GetByEntityAsync(BusinessEntityType entityType, Guid entityId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BusinessDocumentDto>>([document]);

        public Task<IReadOnlyList<BusinessDocumentDto>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BusinessDocumentDto>>([document]);

        public Task<IReadOnlyList<BusinessDocumentDto>> GetByCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BusinessDocumentDto>>([document]);

        public Task<IReadOnlyList<BusinessDocumentDto>> GetByDateAsync(DateTime date, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BusinessDocumentDto>>([document]);

        public Task<BusinessDocumentDto?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
            => Task.FromResult<BusinessDocumentDto?>(document.Id == documentId ? document : null);

        public Task<BusinessDocumentDto> AddAsync(BusinessDocument entity, CancellationToken cancellationToken = default)
        {
            AddedDocument = entity;
            return Task.FromResult(document);
        }
    }
}
