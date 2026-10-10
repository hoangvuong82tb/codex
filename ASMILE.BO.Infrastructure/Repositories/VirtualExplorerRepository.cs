using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Enums;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Repositories;

public sealed class VirtualExplorerRepository : IVirtualExplorerRepository
{
    private const string ImportFolderName = "02. Phiếu nhập";
    private const string InvoiceFolderName = "03. Hóa đơn đầu vào";
    private const string PaymentFolderName = "04. Thanh toán - Tạm ứng";
    private const string PaymentFolderLegacyName = "04. Thanh toán / Tạm ứng";
    private const string DebtFolderName = "05. Công nợ";
    private const string ReportFolderName = "06. In ấn - Báo cáo";
    private const string EmptyFolderIcon = "folder.ico";
    private const string FilledFolderIcon = "filled.ico";

    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ISettingsRegistry _settingsRegistry;
    private readonly IPrinterSettingsService? _printerSettingsService;
    private readonly SemaphoreSlim _documentsCacheLock = new(1, 1);
    // VFC can be opened while the Settings page is still loading, and
    // navigation/search can also be raised twice by WPF (selection +
    // activation). Serialize repository operations to keep cache invalidation
    // and tree materialization deterministic even though each EF branch has
    // its own context from the factory.
    private readonly SemaphoreSlim _operationLock = new(1, 1);
    private IReadOnlyList<BusinessDocumentDto>? _documentsCache;
    private DateTime _documentsCacheLoadedAtUtc;
    private static readonly TimeSpan DocumentsCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly HttpClient EndpointProbeClient = new()
    {
        Timeout = TimeSpan.FromSeconds(2)
    };
    private static readonly IReadOnlyDictionary<string, DriveSetting> DriveSettingsByCode =
        new Dictionary<string, DriveSetting>(StringComparer.OrdinalIgnoreCase)
        {
            ["C"] = new("VFC.SystemDrivePath", "VFC.SystemDriveEnabled", BusinessModule.System),
            ["D"] = new("VFC.AccountingDrivePath", "VFC.AccountingDriveEnabled", BusinessModule.Accounting),
            ["E"] = new("VFC.SalesDrivePath", "VFC.SalesDriveEnabled", BusinessModule.Sales),
            ["F"] = new("VFC.HrDrivePath", "VFC.HrDriveEnabled", BusinessModule.HR),
            ["G"] = new("VFC.LogDrivePath", "VFC.LogDriveEnabled", BusinessModule.Logs)
        };

    public VirtualExplorerRepository(
        IDbContextFactory<AppDbContext> dbContextFactory,
        ISettingsRegistry settingsRegistry,
        IPrinterSettingsService? printerSettingsService = null)
    {
        _dbContextFactory = dbContextFactory;
        _settingsRegistry = settingsRegistry;
        _printerSettingsService = printerSettingsService;
    }

    public Task<IReadOnlyList<VirtualExplorerNodeDto>> GetRootAsync(CancellationToken cancellationToken = default)
    {
        var nodes = new List<VirtualExplorerNodeDto>
        {
            CreateNode("desktop", null, "Desktop", "Desktop", "desktop.ico", "Desktop", VirtualNodeType.Folder, BusinessModule.System, true, 10),
            CreateNode("this-pc", null, "This PC", "This PC", "thispc.ico", "This PC", VirtualNodeType.Folder, BusinessModule.System, true, 20),
            CreateNode("network", null, "Network", "Network", "network.ico", "Network", VirtualNodeType.NetworkItem, BusinessModule.Network, true, 30)
        };

        return Task.FromResult<IReadOnlyList<VirtualExplorerNodeDto>>(nodes);
    }

    public async Task<IReadOnlyList<VirtualExplorerNodeDto>> GetChildrenAsync(string virtualPath, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            return await GetChildrenCoreAsync(virtualPath, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>> GetChildrenCoreAsync(string virtualPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
        {
            return await GetRootAsync(cancellationToken);
        }

        var normalizedPath = NormalizePath(virtualPath);
        IReadOnlyList<BusinessDocumentDto>? documents = null;
        async Task<IReadOnlyList<BusinessDocumentDto>> EnsureDocumentsAsync()
        {
            if (documents is not null)
            {
                return documents;
            }

            documents = await GetDocumentsAsync(cancellationToken);
            return documents;
        }

        if (normalizedPath.Equals("Desktop", StringComparison.OrdinalIgnoreCase))
        {
            return new[]
            {
                CreateNode("desktop-this-pc", "desktop", "This PC", "This PC", "thispc.ico", "This PC", VirtualNodeType.Folder, BusinessModule.System, true, 10),
                CreateNode("desktop-network", "desktop", "Network", "Network", "network.ico", "Network", VirtualNodeType.NetworkItem, BusinessModule.Network, true, 20)
            };
        }

        if (normalizedPath.Equals("This PC", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildDriveNodesAsync(cancellationToken);
        }

        if (normalizedPath.Equals("Network", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildNetworkNodesAsync(cancellationToken);
        }

        // Business collections (customers, suppliers, invoices, inventory
        // vouchers and orders) must always be materialized from their source
        // tables. A partial/stale VirtualFolders catalog must not shadow the
        // live repository branch and make real records disappear.
        if (!IsDynamicBusinessDataPath(normalizedPath))
        {
            var persistedFolderNodes = await BuildPersistedFolderNodesAsync(normalizedPath, cancellationToken);
            if (persistedFolderNodes is not null)
            {
                return persistedFolderNodes;
            }
        }

        if (TryGetAccountingDocumentType(normalizedPath, out var accountingDocumentTypes))
        {
            var documentNodes = BuildAccountingDocumentNodes(normalizedPath, await EnsureDocumentsAsync(), accountingDocumentTypes);
            if (normalizedPath.Equals(@"This PC\D: ACCOUNTING\Phiếu xuất", StringComparison.OrdinalIgnoreCase))
            {
                var stockIssueNodes = await BuildStockIssueNodesAsync(normalizedPath, cancellationToken);
                return documentNodes.Concat(stockIssueNodes)
                    .OrderByDescending(x => x.DateModified)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return documentNodes;
        }

        if (normalizedPath.Equals(@"This PC\E: SALES\POS", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildSalesOrderNodesAsync(normalizedPath, posOnly: true, cancellationToken);
        }

        if (normalizedPath.Equals(@"This PC\E: SALES\Đơn hàng", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildSalesOrderNodesAsync(normalizedPath, posOnly: false, cancellationToken);
        }

        var persistedDrive = await FindPersistedDriveByPathAsync(normalizedPath, cancellationToken);
        if (persistedDrive is not null)
        {
            // System folder taxonomy is materialized by the startup catalog
            // seeder.  Do not silently synthesize a second in-memory tree when
            // the catalog is missing; an empty result is safer than showing
            // paths that cannot be configured, audited, or repaired.
            return Array.Empty<VirtualExplorerNodeDto>();
        }

        if (normalizedPath.Equals(@"This PC\D: ACCOUNTING\Nhà cung cấp", StringComparison.OrdinalIgnoreCase))
        {
            var suppliers = await GetSupplierSeedsAsync(cancellationToken);
            return suppliers
                .Select((supplier, idx) => CreateNode(
                    $"supplier-{supplier.Id}",
                    "accounting-suppliers",
                    supplier.Name,
                    supplier.Name,
                    "supplier.ico",
                    $@"This PC\D: ACCOUNTING\Nhà cung cấp\{supplier.Name}",
                    VirtualNodeType.EntityFolder,
                    BusinessModule.Accounting,
                    true,
                    idx + 1,
                    BusinessEntityType.Supplier,
                    supplier.Id))
                .ToList();
        }

        if (TryResolveSupplierPath(normalizedPath, out var supplierName, out var tailPath))
        {
            var documentRows = await EnsureDocumentsAsync();
            var supplierSeeds = await GetSupplierSeedsAsync(cancellationToken);
            var supplierSeed = supplierSeeds.FirstOrDefault(x =>
                string.Equals(x.Name, supplierName, StringComparison.OrdinalIgnoreCase));
            if (supplierSeed is null)
            {
                return Array.Empty<VirtualExplorerNodeDto>();
            }
            var supplierId = supplierSeed.Id;

            if (string.IsNullOrWhiteSpace(tailPath))
            {
                return BuildSupplierRootFolders(supplierName, supplierId, documentRows);
            }

            if (tailPath.Equals(InvoiceFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return BuildDateFolders(supplierId, normalizedPath, documentRows, BusinessDocumentType.Invoice);
            }

            if (tailPath.Equals(ImportFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return BuildSupplierDocumentRootNodes(
                    supplierId,
                    normalizedPath,
                    documentRows,
                    "THPN-",
                    BusinessDocumentType.ImportVoucher);
            }

            if (tailPath.Equals(PaymentFolderName, StringComparison.OrdinalIgnoreCase)
                || tailPath.Equals(PaymentFolderLegacyName, StringComparison.OrdinalIgnoreCase))
            {
                return BuildSupplierDocumentRootNodes(
                    supplierId,
                    normalizedPath,
                    documentRows,
                    "THTT-",
                    BusinessDocumentType.PaymentVoucher,
                    BusinessDocumentType.AdvancePayment);
            }
            
            if (tailPath.Equals(DebtFolderName, StringComparison.OrdinalIgnoreCase))
            {
                return BuildDebtRootNodes(supplierId, normalizedPath, documentRows);
            }

            if (TryResolveDatePath(normalizedPath, out var year, out var month, out var day, out var documentType, out var includeAdvancePayment))
            {
                BusinessDocumentType[] documentTypes;
                if (normalizedPath.Contains(@"\" + DebtFolderName, StringComparison.OrdinalIgnoreCase))
                {
                    documentTypes = new[]
                    {
                        BusinessDocumentType.ImportVoucher,
                        BusinessDocumentType.PaymentVoucher,
                        BusinessDocumentType.AdvancePayment
                    };
                }
                else
                {
                    documentTypes = includeAdvancePayment
                        ? new[] { BusinessDocumentType.PaymentVoucher, BusinessDocumentType.AdvancePayment }
                        : new[] { documentType };
                }

                if (year.HasValue && !month.HasValue)
                {
                    return BuildMonthFolders(supplierId, year.Value, normalizedPath, documentRows, documentTypes);
                }

                if (year.HasValue && month.HasValue && !day.HasValue)
                {
                    return BuildDocumentNodesByMonth(supplierId, year.Value, month.Value, normalizedPath, documentRows, documentTypes);
                }

                if (year.HasValue && month.HasValue && day.HasValue)
                {
                    return BuildDocumentNodesByMonth(supplierId, year.Value, month.Value, normalizedPath, documentRows, documentTypes);
                }
            }
        }

        if (normalizedPath.Equals(@"This PC\D: ACCOUNTING\Khách hàng", StringComparison.OrdinalIgnoreCase))
        {
            return await BuildCustomerFoldersAsync(cancellationToken);
        }

        if (TryResolveCustomerPath(normalizedPath, out var customerSegment, out var customerTailPath))
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            var customer = await dbContext.Customers
                .AsNoTracking()
                .Where(x => x.CustomerCode == customerSegment || x.CustomerName == customerSegment)
                .Select(x => new { x.Id, x.CustomerCode, x.CustomerName })
                .FirstOrDefaultAsync(cancellationToken);

            if (customer is null)
            {
                return Array.Empty<VirtualExplorerNodeDto>();
            }

            var customerId = GuidUtility.Create(GuidUtility.UrlNamespace, $"customer:{customer.Id}");
            var customerDocuments = (await EnsureDocumentsAsync())
                .Where(x => x.EntityType == BusinessEntityType.Customer && x.EntityId == customerId)
                .OrderByDescending(x => x.DocumentDate)
                .ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(customerTailPath))
            {
                return BuildCustomerDocumentNodes(normalizedPath, customerId, customerDocuments);
            }
        }

        return Array.Empty<VirtualExplorerNodeDto>();
    }

    public async Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            return await SearchCoreAsync(keyword, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    public async Task<VirtualExplorerRefreshResultDto> RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            await _documentsCacheLock.WaitAsync(cancellationToken);
            try
            {
                _documentsCache = null;
                _documentsCacheLoadedAtUtc = default;
            }
            finally
            {
                _documentsCacheLock.Release();
            }

            var documents = await GetDocumentsAsync(cancellationToken);
            var rootNodes = await GetRootAsync(cancellationToken);
            var driveNodes = await GetChildrenCoreAsync("This PC", cancellationToken);

            return new VirtualExplorerRefreshResultDto
            {
                RootNodeCount = rootNodes.Count,
                DriveNodeCount = driveNodes.Count,
                DocumentCount = documents.Count,
                RefreshedAtUtc = DateTime.UtcNow
            };
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchCoreAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var normalized = (keyword ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return await GetRootAsync(cancellationToken);
        }

        var documents = await GetDocumentsAsync(cancellationToken);

        var results = new List<VirtualExplorerNodeDto>();
        var roots = await GetRootAsync(cancellationToken);
        results.AddRange(roots.Where(x => Contains(x.DisplayName, normalized)));

        var thisPcChildren = await GetChildrenCoreAsync("This PC", cancellationToken);
        results.AddRange(thisPcChildren.Where(x => Contains(x.DisplayName, normalized)));

        var accountingChildren = await GetChildrenCoreAsync(@"This PC\D: ACCOUNTING", cancellationToken);
        results.AddRange(accountingChildren.Where(x => Contains(x.DisplayName, normalized)));

        var suppliers = await GetChildrenCoreAsync(@"This PC\D: ACCOUNTING\Nhà cung cấp", cancellationToken);
        results.AddRange(suppliers.Where(x => Contains(x.DisplayName, normalized)));

        var customers = await GetChildrenCoreAsync(@"This PC\D: ACCOUNTING\Khách hàng", cancellationToken);
        results.AddRange(customers.Where(x =>
            Contains(x.Name, normalized) || Contains(x.DisplayName, normalized)));

        results.AddRange(documents
            .Where(x => Contains(x.Code, normalized) || Contains(x.Title, normalized))
            .Select((doc, idx) => new VirtualExplorerNodeDto
            {
                Id = $"doc-{doc.Id}",
                Name = doc.Code,
                DisplayName = doc.Title,
                Icon = "document.ico",
                IconGlyph = "\uE8A5",
                VirtualPath = doc.VirtualPath + @"\" + doc.FileName,
                NodeType = VirtualNodeType.Document,
                Module = doc.Module,
                EntityType = doc.EntityType,
                EntityId = doc.EntityId,
                DocumentId = doc.Id,
                DateModified = doc.UpdatedAt,
                PhysicalPath = doc.PhysicalFilePath,
                FileSize = doc.FileSize,
                HasChildren = false,
                SortOrder = idx + 1
            }));

        var stockIssueNodes = await BuildStockIssueNodesAsync(
            @"This PC\D: ACCOUNTING\Phiếu xuất",
            cancellationToken);
        results.AddRange(stockIssueNodes.Where(x =>
            Contains(x.Name, normalized) || Contains(x.DisplayName, normalized)));

        var salesOrderNodes = await BuildSalesOrderNodesAsync(
            @"This PC\E: SALES\Đơn hàng",
            posOnly: false,
            cancellationToken,
            normalized);
        results.AddRange(salesOrderNodes);

        return results
            .GroupBy(x => x.VirtualPath, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToList();
    }

    public async Task<VirtualExplorerNodeDto?> GetNodeAsync(string virtualPath, CancellationToken cancellationToken = default)
    {
        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            return await GetNodeCoreAsync(virtualPath, cancellationToken);
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private async Task<VirtualExplorerNodeDto?> GetNodeCoreAsync(string virtualPath, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePath(virtualPath);
        var roots = await GetRootAsync(cancellationToken);
        var rootNode = roots.FirstOrDefault(x => x.VirtualPath.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (rootNode is not null)
        {
            return rootNode;
        }

        var searchResult = await SearchCoreAsync(normalized, cancellationToken);
        return searchResult.FirstOrDefault(x => x.VirtualPath.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>> BuildDriveNodesAsync(CancellationToken cancellationToken)
    {
        var (definitionMap, valueMap) = await LoadSystemSettingsAsync(cancellationToken);

        if (!ReadBooleanSetting("VFC.EnableVirtualDrives", definitionMap, valueMap, true))
        {
            return Array.Empty<VirtualExplorerNodeDto>();
        }

        var persistedDrives = await LoadPersistedDrivesAsync(cancellationToken);
        var nodes = new List<VirtualExplorerNodeDto>();
        foreach (var drive in persistedDrives)
        {
            var enabled = drive.EnabledKey is null
                || ReadBooleanSetting(drive.EnabledKey, definitionMap, valueMap, true);
            if (!enabled)
            {
                continue;
            }

            var physicalPath = string.IsNullOrWhiteSpace(drive.PathKey)
                ? null
                : ReadStringSetting(drive.PathKey, definitionMap, valueMap);
            var isPersisted = !string.IsNullOrWhiteSpace(drive.PathKey)
                && valueMap.ContainsKey(drive.PathKey);
            var health = BuildDriveHealth(physicalPath, isPersisted);

            nodes.Add(CreateNode(
                $"drive-{drive.Code.ToLowerInvariant()}",
                "this-pc",
                drive.DisplayName,
                drive.DisplayName,
                string.IsNullOrWhiteSpace(drive.Icon)
                    ? $"drive-{drive.Code.ToLowerInvariant()}.ico"
                    : drive.Icon,
                $@"This PC\{drive.DisplayName}",
                VirtualNodeType.Drive,
                drive.Module,
                true,
                drive.SortOrder,
                description: health.Description,
                status: health.Status,
                physicalPath: physicalPath,
                usedPercent: health.UsedPercent,
                hasUsageMetric: health.HasUsageMetric,
                isAlert: health.IsAlert));
        }

        return nodes;
    }

    private DriveHealth BuildDriveHealth(
        string? physicalPath,
        bool isPersisted)
    {
        var path = physicalPath?.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            return new DriveHealth(
                "Ổ logic VFC, không phải thư mục vật lý; nội dung được tải từ danh mục nghiệp vụ.",
                "Ổ logic",
                null,
                false,
                false);
        }

        path = Environment.ExpandEnvironmentVariables(path);

        if (!Path.IsPathFullyQualified(path))
        {
            return new DriveHealth($"Đường dẫn phải là đường dẫn tuyệt đối · {path}", "Không hợp lệ", null, false, true);
        }

        try
        {
            var attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == 0)
            {
                return new DriveHealth($"Đường dẫn đã cấu hình không phải thư mục · {path}", "Không hợp lệ", null, false, true);
            }
        }
        catch (FileNotFoundException)
        {
            var source = isPersisted ? "Đã lưu nhưng không tìm thấy" : "Mặc định, chưa kiểm tra";
            return new DriveHealth($"{source} · {path}", "Không khả dụng", null, false, true);
        }
        catch (DirectoryNotFoundException)
        {
            var source = isPersisted ? "Đã lưu nhưng không tìm thấy" : "Mặc định, chưa kiểm tra";
            return new DriveHealth($"{source} · {path}", "Không khả dụng", null, false, true);
        }
        catch (UnauthorizedAccessException)
        {
            return new DriveHealth($"Không có quyền truy cập thư mục đã cấu hình · {path}", "Bị từ chối", null, false, true);
        }
        catch (IOException)
        {
            return new DriveHealth($"Không thể kiểm tra thư mục đã cấu hình · {path}", "Không khả dụng", null, false, true);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return new DriveHealth("Đường dẫn thư mục đã cấu hình không hợp lệ.", "Không hợp lệ", null, false, true);
        }

        try
        {
            // A configured folder is useful only if this process can list it.
            // Do not create a probe file: VFC path health is a read-only check.
            _ = Directory.EnumerateFileSystemEntries(path).Take(1).ToArray();
        }
        catch (UnauthorizedAccessException)
        {
            return new DriveHealth($"Có thư mục nhưng tài khoản ứng dụng không có quyền đọc · {path}", "Bị từ chối", null, false, true);
        }
        catch (IOException)
        {
            return new DriveHealth($"Không thể đọc nội dung thư mục đã cấu hình · {path}", "Không khả dụng", null, false, true);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException)
        {
            return new DriveHealth("Đường dẫn thư mục đã cấu hình không hợp lệ.", "Không hợp lệ", null, false, true);
        }

        var root = Path.GetPathRoot(path);
        if (string.IsNullOrWhiteSpace(root))
        {
            return new DriveHealth($"Có thể đọc nội dung · {path}", "Đọc được", null, false, false);
        }

        try
        {
            var driveInfo = new DriveInfo(root);
            var usedPercent = driveInfo.TotalSize > 0
                ? 1d - (double)driveInfo.AvailableFreeSpace / driveInfo.TotalSize
                : 0d;
            var description = isPersisted
                ? $"Có thể đọc nội dung · {path} · còn trống {FormatBytes(driveInfo.AvailableFreeSpace)}/{FormatBytes(driveInfo.TotalSize)}"
                : $"Mặc định, chưa lưu trong Settings Engine · có thể đọc nội dung · {path} · còn trống {FormatBytes(driveInfo.AvailableFreeSpace)}/{FormatBytes(driveInfo.TotalSize)}";
            var isAlert = usedPercent >= 0.9d;
            return new DriveHealth(description, isPersisted ? "Đọc được" : "Mặc định · đọc được", usedPercent, true, isAlert);
        }
        catch (IOException)
        {
            return new DriveHealth($"Có thể đọc nội dung nhưng không đo được dung lượng · {path}", "Đọc được", null, false, false);
        }
        catch (UnauthorizedAccessException)
        {
            return new DriveHealth($"Có thể đọc nội dung nhưng không có quyền đọc dung lượng · {path}", "Đọc được", null, false, false);
        }
    }

    private async Task<IReadOnlyList<ConfiguredDrive>> LoadPersistedDrivesAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var rows = await dbContext.VirtualDrives
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.Code != "NET")
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToListAsync(cancellationToken);

        return rows
            .Select(MapPersistedDrive)
            .ToList();
    }

    private async Task<VirtualDrive?> FindPersistedDriveByPathAsync(
        string normalizedPath,
        CancellationToken cancellationToken)
    {
        const string prefix = "This PC\\";
        if (!normalizedPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || normalizedPath[prefix.Length..].Contains('\\'))
        {
            return null;
        }

        var displayName = normalizedPath[prefix.Length..];
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.VirtualDrives
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.DisplayName == displayName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>?> BuildPersistedFolderNodesAsync(
        string normalizedPath,
        CancellationToken cancellationToken)
    {
        if (!normalizedPath.StartsWith("This PC\\", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var prefix = normalizedPath + "\\";
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var folders = await dbContext.VirtualFolders
            .AsNoTracking()
            .Where(x => x.VirtualPath.StartsWith(prefix))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);

        var directFolders = folders
            .Where(x => x.VirtualPath[prefix.Length..].IndexOf('\\') < 0)
            .ToList();

        // Older deployments may be running in ValidateOnly/Disabled migration
        // mode and therefore have no persisted system folder catalog yet. Keep
        // the built-in folder taxonomy navigable in that case, while continuing
        // to load all customers and documents from their authoritative tables.
        var relativeDrivePath = normalizedPath["This PC\\".Length..];
        var driveDisplayName = relativeDrivePath.Split('\\', 2)[0];
        var persistedDrive = await dbContext.VirtualDrives
            .AsNoTracking()
            .Where(x => x.IsEnabled && x.DisplayName == driveDisplayName)
            .Select(x => new { x.Code, x.DisplayName })
            .FirstOrDefaultAsync(cancellationToken);

        if (persistedDrive is not null)
        {
            var persistedPaths = directFolders
                .Select(x => x.VirtualPath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var defaultFolders = VirtualFileCenterSeed.DefaultFolders
                .Where(x => string.Equals(x.DriveCode, persistedDrive.Code, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        x.VirtualPath[..x.VirtualPath.LastIndexOf('\\')],
                        normalizedPath,
                        StringComparison.OrdinalIgnoreCase)
                    && !persistedPaths.Contains(x.VirtualPath))
                .Select(seed => CreateNode(
                    $"vf-{VirtualFileCenterSeed.StableGuid($"vfc-folder:{seed.VirtualPath}"):N}",
                    $"drive-{persistedDrive.Code.ToLowerInvariant()}",
                    seed.Name,
                    seed.DisplayName,
                    ResolveVirtualFolderIcon(seed.FolderType),
                    seed.VirtualPath,
                    seed.FolderType,
                    ResolveDriveModule(seed.DriveCode),
                    hasChildren: true,
                    seed.SortOrder,
                    seed.EntityType,
                    VirtualFileCenterSeed.StableGuid($"vfc-folder:{seed.VirtualPath}")));

            var combined = directFolders
                .Select(folder => CreateNode(
                    $"vf-{folder.Id:N}",
                    folder.ParentId is null ? GetParentNodeId(normalizedPath) : $"vf-{folder.ParentId.Value:N}",
                    folder.Name,
                    string.IsNullOrWhiteSpace(folder.DisplayName) ? folder.Name : folder.DisplayName,
                    ResolveVirtualFolderIcon(folder.FolderType),
                    folder.VirtualPath,
                    folder.FolderType,
                    ResolveDriveModule(folder.DriveCode),
                    folders.Any(child => child.VirtualPath.StartsWith(folder.VirtualPath + "\\", StringComparison.OrdinalIgnoreCase)),
                    folder.SortOrder,
                    folder.EntityType,
                    folder.EntityId))
                .Concat(defaultFolders)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.DisplayName)
                .ToList();

            if (combined.Count > 0)
            {
                return combined;
            }
        }

        if (directFolders.Count == 0)
        {
            return null;
        }

        return directFolders.Select(folder => CreateNode(
                $"vf-{folder.Id:N}",
                folder.ParentId is null ? GetParentNodeId(normalizedPath) : $"vf-{folder.ParentId.Value:N}",
                folder.Name,
                string.IsNullOrWhiteSpace(folder.DisplayName) ? folder.Name : folder.DisplayName,
                ResolveVirtualFolderIcon(folder.FolderType),
                folder.VirtualPath,
                folder.FolderType,
                ResolveDriveModule(folder.DriveCode),
                folders.Any(child => child.VirtualPath.StartsWith(folder.VirtualPath + "\\", StringComparison.OrdinalIgnoreCase)),
                folder.SortOrder,
                folder.EntityType,
                folder.EntityId))
            .ToList();
    }

    private static bool TryGetAccountingDocumentType(string path, out BusinessDocumentType[] documentTypes)
    {
        documentTypes = path switch
        {
            @"This PC\D: ACCOUNTING\Hóa đơn" => [BusinessDocumentType.Invoice],
            @"This PC\D: ACCOUNTING\Phiếu nhập" => [BusinessDocumentType.ImportVoucher],
            @"This PC\D: ACCOUNTING\Phiếu xuất" => [BusinessDocumentType.ExportVoucher],
            _ => []
        };
        return documentTypes.Length > 0;
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildAccountingDocumentNodes(
        string virtualPath,
        IReadOnlyList<BusinessDocumentDto> documents,
        IReadOnlyCollection<BusinessDocumentType> documentTypes)
    {
        return documents
            .Where(x => documentTypes.Contains(x.DocumentType))
            .OrderByDescending(x => x.DocumentDate)
            .ThenBy(x => x.Code)
            .Select((document, index) => CreateNode(
                $"doc-{document.Id:N}",
                null,
                document.Code,
                string.IsNullOrWhiteSpace(document.Title) ? document.Code : document.Title,
                "document.ico",
                $"{virtualPath}\\{document.Id:N}{document.FileExtension}",
                VirtualNodeType.Document,
                document.Module,
                false,
                index + 1,
                document.EntityType,
                document.EntityId,
                document.Id,
                document.UpdatedAt,
                physicalPath: document.PhysicalFilePath,
                fileSize: document.FileSize))
            .ToList();
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>> BuildStockIssueNodesAsync(
        string virtualPath,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var issues = await dbContext.StockIssues
            .AsNoTracking()
            .OrderByDescending(x => x.IssueDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.IssueNo,
                x.IssueDate,
                x.Description,
                x.TotalAmount,
                x.IsApproved,
                x.PostingStatus
            })
            .ToListAsync(cancellationToken);

        return issues.Select((issue, index) => new VirtualExplorerNodeDto
        {
            Id = $"stock-issue-{issue.Id}",
            Name = issue.IssueNo,
            DisplayName = string.IsNullOrWhiteSpace(issue.Description)
                ? issue.IssueNo
                : $"{issue.IssueNo} - {issue.Description}",
            Icon = "document.ico",
            VirtualPath = $"{virtualPath}\\{issue.Id}",
            NodeType = VirtualNodeType.Document,
            Module = BusinessModule.Accounting,
            DateModified = issue.IssueDate,
            HasChildren = false,
            SortOrder = index + 1,
            Description = issue.Description,
            Status = string.Equals(issue.PostingStatus, "Reversed", StringComparison.OrdinalIgnoreCase)
                ? "Đã đảo"
                : string.Equals(issue.PostingStatus, "Posted", StringComparison.OrdinalIgnoreCase)
                    ? "Đã ghi sổ"
                    : issue.IsApproved ? "Đã duyệt" : "Chờ duyệt",
            Amount = issue.TotalAmount,
            Currency = "VND"
        }).ToList();
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>> BuildSalesOrderNodesAsync(
        string virtualPath,
        bool posOnly,
        CancellationToken cancellationToken,
        string? searchTerm = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        var nodes = new List<VirtualExplorerNodeDto>();

        var cafeOrdersQuery = dbContext.BreakfastCafeOrders.AsNoTracking();
        if (normalizedSearch is not null)
        {
            cafeOrdersQuery = cafeOrdersQuery.Where(x => x.OrderNumber.Contains(normalizedSearch));
        }

        var cafeOrders = await cafeOrdersQuery
            .OrderByDescending(x => x.OrderDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                Number = x.OrderNumber,
                x.OrderDate,
                x.Status,
                x.TotalAmount
            })
            .ToListAsync(cancellationToken);

        nodes.AddRange(cafeOrders.Select(order => CreateSalesOrderNode(
            $"pos-order-{order.Id:N}",
            order.Number,
            order.OrderDate,
            order.Status,
            order.TotalAmount,
            "POS",
            virtualPath,
            nodes.Count + 1)));

        if (!posOnly)
        {
            var restaurantOrdersQuery = dbContext.AlacarteOrders.AsNoTracking();
            if (normalizedSearch is not null)
            {
                restaurantOrdersQuery = restaurantOrdersQuery.Where(x => x.OrderNo.Contains(normalizedSearch));
            }

            var restaurantOrders = await restaurantOrdersQuery
                .OrderByDescending(x => x.OrderDate)
                .ThenByDescending(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.OrderNo,
                    x.OrderDate,
                    x.Status,
                    x.TotalAmount
                })
                .ToListAsync(cancellationToken);

            nodes.AddRange(restaurantOrders.Select(order => CreateSalesOrderNode(
                $"restaurant-order-{order.Id:N}",
                order.OrderNo,
                order.OrderDate,
                order.Status,
                order.TotalAmount,
                "Nhà hàng",
                virtualPath,
                nodes.Count + 1)));
        }

        return nodes
            .OrderByDescending(x => x.DateModified)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static VirtualExplorerNodeDto CreateSalesOrderNode(
        string id,
        string orderNumber,
        DateTime orderDate,
        string status,
        decimal totalAmount,
        string source,
        string virtualPath,
        int sortOrder)
    {
        return new VirtualExplorerNodeDto
        {
            Id = id,
            Name = orderNumber,
            DisplayName = $"{orderNumber} · {source}",
            Icon = "document.ico",
            VirtualPath = $"{virtualPath}\\{id}",
            NodeType = VirtualNodeType.Document,
            Module = BusinessModule.Sales,
            DateModified = orderDate,
            HasChildren = false,
            SortOrder = sortOrder,
            Status = status,
            Amount = totalAmount,
            Currency = "VND"
        };
    }

    private static string GetParentNodeId(string normalizedPath)
    {
        var lastSeparator = normalizedPath.LastIndexOf('\\');
        var name = lastSeparator >= 0 ? normalizedPath[(lastSeparator + 1)..] : normalizedPath;
        return name.StartsWith("C:", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("D:", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("E:", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("F:", StringComparison.OrdinalIgnoreCase)
            || name.StartsWith("G:", StringComparison.OrdinalIgnoreCase)
            ? $"drive-{name[0].ToString().ToLowerInvariant()}"
            : "this-pc";
    }

    private static string ResolveVirtualFolderIcon(VirtualNodeType folderType)
        => folderType switch
        {
            VirtualNodeType.Report => "report.ico",
            VirtualNodeType.SystemTool => "settings.ico",
            VirtualNodeType.Document => "document.ico",
            _ => EmptyFolderIcon
        };

    private static BusinessModule ResolveDriveModule(string driveCode)
        => driveCode.ToUpperInvariant() switch
        {
            "D" => BusinessModule.Accounting,
            "E" => BusinessModule.Sales,
            "F" => BusinessModule.HR,
            "G" => BusinessModule.Logs,
            _ => BusinessModule.System
        };

    private static ConfiguredDrive MapPersistedDrive(VirtualDrive drive)
    {
        var settings = DriveSettingsByCode.TryGetValue(drive.Code, out var configured)
            ? configured
            : new DriveSetting(null, null, ResolveDriveModule(drive.Code));

        return new ConfiguredDrive(
            drive.Code,
            string.IsNullOrWhiteSpace(drive.DisplayName) ? drive.Name : drive.DisplayName,
            drive.Icon,
            settings.PathKey,
            settings.EnabledKey,
            settings.Module,
            drive.SortOrder);
    }

    private static string ReadStringSetting(
        string key,
        IReadOnlyDictionary<string, SettingDefinitionDto> definitions,
        IReadOnlyDictionary<string, SettingValueDto> values)
    {
        if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value.Value))
        {
            return value.Value.Trim();
        }

        return definitions.TryGetValue(key, out var definition)
            ? definition.DefaultValue?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static bool ReadBooleanSetting(
        string key,
        IReadOnlyDictionary<string, SettingDefinitionDto> definitions,
        IReadOnlyDictionary<string, SettingValueDto> values,
        bool defaultValue)
    {
        var raw = values.TryGetValue(key, out var value)
            ? value.Value
            : definitions.TryGetValue(key, out var definition) ? definition.DefaultValue : null;
        return bool.TryParse(raw, out var parsed) ? parsed : defaultValue;
    }

    /// <summary>
    /// Reads VFC/Network settings with a context created by the factory.
    /// SettingsService is backed by the shell's scoped AppDbContext; using it
    /// here can reproduce EF Core's second-operation error when VFC opens while
    /// the Settings page is seeding or loading that same context.
    /// </summary>
    private async Task<(
        IReadOnlyDictionary<string, SettingDefinitionDto> Definitions,
        IReadOnlyDictionary<string, SettingValueDto> Values)> LoadSystemSettingsAsync(
        CancellationToken cancellationToken)
    {
        var definitions = _settingsRegistry.GetDefaultDefinitions()
            .Where(x => x.Scope == SettingScope.System)
            .ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var persistedDefinitions = await dbContext.SettingDefinitions
            .AsNoTracking()
            .Where(x => x.Scope == SettingScope.System)
            .ToListAsync(cancellationToken);
        foreach (var definition in persistedDefinitions)
        {
            definitions[definition.Key] = new SettingDefinitionDto
            {
                Key = definition.Key,
                DisplayName = definition.DisplayName,
                Description = definition.Description,
                Category = definition.Category,
                Group = definition.Group,
                Scope = definition.Scope,
                ValueType = definition.ValueType,
                DefaultValue = definition.DefaultValue,
                IsRequired = definition.IsRequired,
                IsSensitive = definition.IsSensitive,
                IsReadOnly = definition.IsReadOnly,
                RequiresRestart = definition.RequiresRestart,
                RequiredPermission = definition.RequiredPermission,
                SortOrder = definition.SortOrder
            };
        }

        var persistedValues = await dbContext.SettingValues
            .AsNoTracking()
            .Where(x => x.Scope == SettingScope.System
                && (x.ScopeTarget == null || x.ScopeTarget == string.Empty)
                && !x.IsEncrypted)
            .ToListAsync(cancellationToken);

        var values = persistedValues.ToDictionary(
            x => x.DefinitionKey,
            x => new SettingValueDto
            {
                Key = x.DefinitionKey,
                Scope = x.Scope,
                ScopeTarget = x.ScopeTarget,
                Value = x.Value,
                IsPersisted = true,
                IsEncrypted = x.IsEncrypted,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            },
            StringComparer.OrdinalIgnoreCase);

        return (definitions, values);
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>> BuildNetworkNodesAsync(CancellationToken cancellationToken)
    {
        var (definitionMap, valueMap) = await LoadSystemSettingsAsync(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        var databaseHealth = await DescribeDatabaseAsync(cancellationToken);

        var apiBaseUrl = ReadStringSetting("Network.ApiBaseUrl", definitionMap, valueMap);
        var apiPort = ReadStringSetting("Network.ApiPort", definitionMap, valueMap);
        var enableHttps = ReadBooleanSetting("Network.EnableHttps", definitionMap, valueMap, true);
        var apiHealthTask = DescribeEndpointAsync(apiBaseUrl, apiPort, enableHttps, valueMap.ContainsKey("Network.ApiBaseUrl"), cancellationToken);
        var posHealthTask = DescribeEndpointAsync(ReadStringSetting("POS.ApiEndpoint", definitionMap, valueMap), null, null, valueMap.ContainsKey("POS.ApiEndpoint"), cancellationToken);
        var foHealthTask = DescribeEndpointAsync(ReadStringSetting("FO.ApiEndpoint", definitionMap, valueMap), null, null, valueMap.ContainsKey("FO.ApiEndpoint"), cancellationToken);
        await Task.WhenAll(apiHealthTask, posHealthTask, foHealthTask);
        var apiHealth = await apiHealthTask;
        var posHealth = await posHealthTask;
        var foHealth = await foHealthTask;
        var printerHealth = await DescribePrintersAsync(cancellationToken);
        var sharedFolderHealth = DescribePath(
            ReadStringSetting("Storage.DataRootPath", definitionMap, valueMap),
            "Storage.DataRootPath",
            valueMap.ContainsKey("Storage.DataRootPath"));

        return new List<VirtualExplorerNodeDto>
        {
            CreateNetworkNode("net-sql", "SQL Server", "sql.ico", 10, databaseHealth),
            CreateNetworkNode("net-api", "API Server", "api.ico", 20, apiHealth),
            CreateNetworkNode("net-pos", "POS Terminals", "pos.ico", 30, posHealth),
            CreateNetworkNode("net-fo", "FO Clients", "fo.ico", 40, foHealth),
            CreateNetworkNode("net-share", "Shared Folders", EmptyFolderIcon, 50, sharedFolderHealth),
            CreateNetworkNode("net-printer", "Printers", "printer.ico", 60, printerHealth)
        };
    }

    private async Task<NetworkHealth> DescribePrintersAsync(CancellationToken cancellationToken)
    {
        if (_printerSettingsService is null)
        {
            return new NetworkHealth("Windows printer provider chưa được đăng ký.", "Không khả dụng", true);
        }

        try
        {
            var printers = await _printerSettingsService.GetAvailablePrintersAsync(cancellationToken);
            if (printers.Count == 0)
            {
                return new NetworkHealth(
                    "Windows không trả về máy in nào; kiểm tra Print Spooler và quyền của tài khoản.",
                    "Không phát hiện",
                    true);
            }

            var problemCount = printers.Count(x =>
                x.Status.Equals("Offline", StringComparison.OrdinalIgnoreCase)
                || x.Status.Equals("Error", StringComparison.OrdinalIgnoreCase));
            var sample = string.Join(" · ", printers.Take(4).Select(x => $"{x.Name} ({x.Status})"));
            var remainingCount = printers.Count - Math.Min(printers.Count, 4);
            var suffix = remainingCount > 0 ? $" · +{remainingCount} máy in" : string.Empty;
            var description = $"Windows liệt kê {printers.Count} máy in · {sample}{suffix}";
            return problemCount > 0
                ? new NetworkHealth($"{description} · {problemCount} máy in cần kiểm tra", "Cần kiểm tra", true)
                : new NetworkHealth(description, "Sẵn sàng", false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new NetworkHealth(
                $"Không đọc được danh sách máy in từ Windows ({ex.GetType().Name}).",
                "Không khả dụng",
                true);
        }
    }

    private async Task<NetworkHealth> DescribeDatabaseAsync(CancellationToken cancellationToken)
    {
        var target = "Database provider";
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
            if (dbContext.Database.IsRelational())
            {
                var connection = dbContext.Database.GetDbConnection();
                target = FormatDatabaseTarget(connection.DataSource, connection.Database);
            }
            else
            {
                target = dbContext.Database.ProviderName ?? target;
            }

            var connected = await dbContext.Database.CanConnectAsync(cancellationToken);
            return connected
                ? new NetworkHealth($"Đã kết nối · {target}", "Đã kết nối", false)
                : new NetworkHealth($"Không kết nối được · {target}", "Không khả dụng", true);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is DbUpdateException or InvalidOperationException or System.Data.Common.DbException)
        {
            return new NetworkHealth($"Không kết nối được · {target}", "Không khả dụng", true);
        }
    }

    private static string FormatDatabaseTarget(string? dataSource, string? database)
    {
        var source = string.IsNullOrWhiteSpace(dataSource) ? "SQL Server" : dataSource.Trim();
        return string.IsNullOrWhiteSpace(database) ? source : $"{source} · {database.Trim()}";
    }

    private static VirtualExplorerNodeDto CreateNetworkNode(
        string id,
        string displayName,
        string icon,
        int sortOrder,
        NetworkHealth health)
    {
        return CreateNode(
            id,
            "network",
            displayName,
            displayName,
            icon,
            $"Network\\{displayName}",
            VirtualNodeType.NetworkItem,
            BusinessModule.Network,
            false,
            sortOrder,
            description: health.Description,
            status: health.Status,
            isAlert: health.IsAlert);
    }

    private static async Task<NetworkHealth> DescribeEndpointAsync(
        string? endpoint,
        string? port,
        bool? useHttps,
        bool isPersisted,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return new NetworkHealth("Chưa cấu hình endpoint", "Chưa cấu hình", true);
        }

        var candidate = endpoint.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            var scheme = useHttps is false ? "http" : "https";
            candidate = $"{scheme}://{candidate}";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new NetworkHealth("Endpoint không hợp lệ; giá trị URL được ẩn để bảo vệ thông tin xác thực.", "Không hợp lệ", true);
        }

        var configuredUri = uri;
        if (configuredUri.IsDefaultPort && int.TryParse(port, out var parsedPort) && parsedPort is > 0 and <= 65535)
        {
            var builder = new UriBuilder(configuredUri) { Port = parsedPort };
            configuredUri = builder.Uri;
        }

        var safeEndpoint = FormatEndpointAuthority(configuredUri);

        if (!isPersisted)
        {
            return new NetworkHealth($"Mặc định, chưa lưu trong Settings Engine · {safeEndpoint}", "Mặc định", true);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, configuredUri);
            using var response = await EndpointProbeClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            return response.IsSuccessStatusCode
                ? new NetworkHealth($"Đã kiểm tra · {safeEndpoint} · HTTP {(int)response.StatusCode}", "Đã kết nối", false)
                : new NetworkHealth($"Endpoint phản hồi HTTP {(int)response.StatusCode} · {safeEndpoint}", "Phản hồi lỗi", true);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new NetworkHealth($"Timeout khi kiểm tra · {safeEndpoint}", "Timeout", true);
        }
        catch (HttpRequestException ex)
        {
            return new NetworkHealth($"Không kết nối được · {safeEndpoint} · {ex.GetType().Name}", "Không khả dụng", true);
        }
    }

    private static string FormatEndpointAuthority(Uri endpoint)
    {
        var safeUri = new UriBuilder(endpoint)
        {
            UserName = string.Empty,
            Password = string.Empty,
            Path = string.Empty,
            Query = string.Empty,
            Fragment = string.Empty
        }.Uri;
        return safeUri.GetLeftPart(UriPartial.Authority);
    }

    private static NetworkHealth DescribePath(string? configuredPath, string settingKey, bool isPersisted)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return new NetworkHealth($"Chưa cấu hình {settingKey}", "Chưa cấu hình", true);
        }

        var path = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
        if (!Path.IsPathFullyQualified(path))
        {
            return new NetworkHealth($"Đường dẫn phải là đường dẫn tuyệt đối · {path}", "Không hợp lệ", true);
        }

        return Directory.Exists(path)
            ? isPersisted
                ? new NetworkHealth($"Đã kiểm tra · {path}", "Sẵn sàng", false)
                : new NetworkHealth($"Mặc định, chưa lưu trong Settings Engine · {path}", "Mặc định", true)
            : new NetworkHealth($"Không tìm thấy · {path}", "Không khả dụng", true);
    }

    private async Task<IReadOnlyList<VirtualFileCenterSeed.SupplierSeed>> GetSupplierSeedsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var dbSuppliers = await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierName)
            .Select(x => new VirtualFileCenterSeed.SupplierSeed(
                ToSupplierEntityId(x.Id),
                x.SupplierCode,
                x.SupplierName))
            .ToListAsync(cancellationToken);

        return dbSuppliers
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildSupplierRootFolders(string supplierName, Guid supplierId, IReadOnlyList<BusinessDocumentDto> documents)
    {
        var basePath = $@"This PC\D: ACCOUNTING\Nhà cung cấp\{supplierName}";
        var hasImportDocuments = documents.Any(x =>
            x.EntityType == BusinessEntityType.Supplier &&
            x.EntityId == supplierId &&
            x.DocumentType == BusinessDocumentType.ImportVoucher);

        var hasInvoiceDocuments = documents.Any(x =>
            x.EntityType == BusinessEntityType.Supplier &&
            x.EntityId == supplierId &&
            x.DocumentType == BusinessDocumentType.Invoice);

        var hasPaymentDocuments = documents.Any(x =>
            x.EntityType == BusinessEntityType.Supplier &&
            x.EntityId == supplierId &&
            (x.DocumentType == BusinessDocumentType.PaymentVoucher || x.DocumentType == BusinessDocumentType.AdvancePayment));
        
        var hasDebtDocuments = documents.Any(x =>
            x.EntityType == BusinessEntityType.Supplier &&
            x.EntityId == supplierId &&
            (x.DocumentType == BusinessDocumentType.ImportVoucher
             || x.DocumentType == BusinessDocumentType.PaymentVoucher
             || x.DocumentType == BusinessDocumentType.AdvancePayment
             || x.DocumentType == BusinessDocumentType.DebtStatement));

        return new List<VirtualExplorerNodeDto>
        {
            CreateNode($"{supplierId}-profile", $"supplier-{supplierId}", "01. Hồ sơ nhà cung cấp", "01. Hồ sơ nhà cung cấp", EmptyFolderIcon, basePath + @"\01. Hồ sơ nhà cung cấp", VirtualNodeType.Folder, BusinessModule.Accounting, false, 10, BusinessEntityType.Supplier, supplierId),
            CreateNode($"{supplierId}-import", $"supplier-{supplierId}", ImportFolderName, ImportFolderName, hasImportDocuments ? FilledFolderIcon : EmptyFolderIcon, basePath + @"\" + ImportFolderName, VirtualNodeType.Folder, BusinessModule.Accounting, true, 20, BusinessEntityType.Supplier, supplierId),
            CreateNode($"{supplierId}-invoice", $"supplier-{supplierId}", InvoiceFolderName, InvoiceFolderName, hasInvoiceDocuments ? FilledFolderIcon : EmptyFolderIcon, basePath + @"\" + InvoiceFolderName, VirtualNodeType.Folder, BusinessModule.Accounting, true, 30, BusinessEntityType.Supplier, supplierId),
            CreateNode($"{supplierId}-payment", $"supplier-{supplierId}", PaymentFolderName, PaymentFolderName, hasPaymentDocuments ? FilledFolderIcon : EmptyFolderIcon, basePath + @"\" + PaymentFolderName, VirtualNodeType.Folder, BusinessModule.Accounting, true, 40, BusinessEntityType.Supplier, supplierId),
            CreateNode($"{supplierId}-debt", $"supplier-{supplierId}", DebtFolderName, DebtFolderName, hasDebtDocuments ? FilledFolderIcon : EmptyFolderIcon, basePath + @"\" + DebtFolderName, VirtualNodeType.Folder, BusinessModule.Accounting, true, 50, BusinessEntityType.Supplier, supplierId),
            CreateNode($"{supplierId}-report", $"supplier-{supplierId}", ReportFolderName, ReportFolderName, EmptyFolderIcon, basePath + @"\" + ReportFolderName, VirtualNodeType.Report, BusinessModule.Accounting, false, 60, BusinessEntityType.Supplier, supplierId)
        };
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildDateFolders(Guid supplierId, string basePath, IReadOnlyList<BusinessDocumentDto> documents, params BusinessDocumentType[] documentTypes)
    {
        var years = documents
            .Where(x => x.EntityType == BusinessEntityType.Supplier
                        && x.EntityId == supplierId
                        && documentTypes.Contains(x.DocumentType))
            .Select(x => x.DocumentDate.Year)
            .Distinct()
            .OrderByDescending(x => x)
            .ToList();

        return years
            .Select((year, idx) =>
            {
                var hasYearDocuments = documents.Any(x =>
                    x.EntityType == BusinessEntityType.Supplier &&
                    x.EntityId == supplierId &&
                    documentTypes.Contains(x.DocumentType) &&
                    x.DocumentDate.Year == year);

                return CreateNode(
                    $"{supplierId}-{string.Join("-", documentTypes.Select(x => x.ToString()))}-year-{year}",
                    null,
                    year.ToString("0000"),
                    year.ToString("0000"),
                    hasYearDocuments ? FilledFolderIcon : EmptyFolderIcon,
                    $"{basePath}\\{year:0000}",
                    VirtualNodeType.Folder,
                    BusinessModule.Accounting,
                    true,
                    idx + 1,
                    BusinessEntityType.Supplier,
                    supplierId);
            })
            .ToList();
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildMonthFolders(Guid supplierId, int year, string currentPath, IReadOnlyList<BusinessDocumentDto> documents, params BusinessDocumentType[] documentTypes)
    {
        return Enumerable.Range(1, 12)
            .Select((month, idx) =>
            {
                var hasMonthDocuments = documents.Any(x =>
                    x.EntityType == BusinessEntityType.Supplier &&
                    x.EntityId == supplierId &&
                    documentTypes.Contains(x.DocumentType) &&
                    x.DocumentDate.Year == year &&
                    x.DocumentDate.Month == month);

                return CreateNode(
                    $"{supplierId}-{string.Join("-", documentTypes.Select(x => x.ToString()))}-month-{month}",
                    null,
                    month.ToString("00"),
                    month.ToString("00"),
                    hasMonthDocuments ? FilledFolderIcon : EmptyFolderIcon,
                    $"{currentPath}\\{month:00}",
                    VirtualNodeType.Folder,
                    BusinessModule.Accounting,
                    hasMonthDocuments,
                    idx + 1,
                    BusinessEntityType.Supplier,
                    supplierId);
            })
            .ToList();
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildDebtRootNodes(Guid supplierId, string currentPath, IReadOnlyList<BusinessDocumentDto> documents)
    {
        var statementNodes = documents
            .Where(x => x.EntityType == BusinessEntityType.Supplier
                        && x.EntityId == supplierId
                        && x.DocumentType == BusinessDocumentType.DebtStatement)
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.Code)
            .Select((doc, idx) => CreateNode(
                $"doc-{doc.Id}",
                null,
                doc.Code,
                $"{doc.Code} - {doc.Title}",
                "document.ico",
                $"{currentPath}\\{doc.FileName}",
                VirtualNodeType.Document,
                doc.Module,
                false,
                idx + 1,
                doc.EntityType,
                doc.EntityId,
                doc.Id,
                doc.UpdatedAt,
                physicalPath: doc.PhysicalFilePath,
                fileSize: doc.FileSize))
            .ToList();

        var yearFolders = BuildDateFolders(
                supplierId,
                currentPath,
                documents,
                BusinessDocumentType.ImportVoucher,
                BusinessDocumentType.PaymentVoucher,
                BusinessDocumentType.AdvancePayment)
            .Select((node, idx) =>
            {
                node.SortOrder = statementNodes.Count + idx + 1;
                return node;
            });

        return statementNodes.Concat(yearFolders).ToList();
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildSupplierDocumentRootNodes(
        Guid supplierId,
        string currentPath,
        IReadOnlyList<BusinessDocumentDto> documents,
        string summaryCodePrefix,
        params BusinessDocumentType[] documentTypes)
    {
        var summaryNodes = documents
            .Where(x => x.EntityType == BusinessEntityType.Supplier
                        && x.EntityId == supplierId
                        && x.DocumentType == BusinessDocumentType.Report
                        && x.Code.StartsWith(summaryCodePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Code)
            .Select((doc, idx) => CreateNode(
                $"doc-{doc.Id}",
                null,
                doc.Code,
                $"{doc.Code} - {doc.Title}",
                "document.ico",
                $"{currentPath}\\{doc.FileName}",
                VirtualNodeType.Document,
                doc.Module,
                false,
                idx + 1,
                doc.EntityType,
                doc.EntityId,
                doc.Id,
                doc.UpdatedAt,
                physicalPath: doc.PhysicalFilePath,
                fileSize: doc.FileSize))
            .ToList();

        var yearFolders = BuildDateFolders(supplierId, currentPath, documents, documentTypes)
            .Select((node, idx) =>
            {
                node.SortOrder = summaryNodes.Count + idx + 1;
                return node;
            });

        return summaryNodes.Concat(yearFolders).ToList();
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildDocumentNodesByMonth(Guid supplierId, int year, int month, string currentPath, IReadOnlyList<BusinessDocumentDto> documents, params BusinessDocumentType[] documentTypes)
    {
        var rows = documents
            .Where(x => x.EntityType == BusinessEntityType.Supplier
                        && x.EntityId == supplierId
                        && documentTypes.Contains(x.DocumentType)
                        && x.DocumentDate.Year == year
                        && x.DocumentDate.Month == month)
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.Code)
            .ToList();

        return rows.Select((doc, idx) => CreateNode(
                $"doc-{doc.Id}",
                null,
                doc.Code,
                $"{doc.Code} - {doc.Title}",
                "document.ico",
                $"{currentPath}\\{doc.FileName}",
                VirtualNodeType.Document,
                doc.Module,
                false,
                idx + 1,
                doc.EntityType,
                doc.EntityId,
                doc.Id,
                doc.UpdatedAt,
                physicalPath: doc.PhysicalFilePath,
                fileSize: doc.FileSize))
            .ToList();
    }

    private async Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        if (_documentsCache is not null && (nowUtc - _documentsCacheLoadedAtUtc) <= DocumentsCacheTtl)
        {
            return _documentsCache;
        }

        await _documentsCacheLock.WaitAsync(cancellationToken);
        try
        {
            nowUtc = DateTime.UtcNow;
            if (_documentsCache is not null && (nowUtc - _documentsCacheLoadedAtUtc) <= DocumentsCacheTtl)
            {
                return _documentsCache;
            }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var persistedDocuments = await dbContext.BusinessDocuments
                .AsNoTracking()
                .Select(x => new BusinessDocumentDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    Title = x.Title,
                    Module = x.Module,
                    EntityType = x.EntityType,
                    EntityId = x.EntityId,
                    DocumentType = x.DocumentType,
                    DocumentDate = x.DocumentDate,
                    Amount = x.Amount,
                    Currency = x.Currency,
                    VirtualPath = NormalizeAccountingVirtualPath(x.VirtualPath),
                    PhysicalFilePath = string.IsNullOrWhiteSpace(x.PhysicalFilePath)
                        ? $"dbdoc://{x.Id:N}"
                        : x.PhysicalFilePath,
                    FileName = x.FileName,
                    FileExtension = x.FileExtension,
                    FileSize = x.FileSize,
                    Status = x.Status,
                    CreatedBy = x.CreatedBy,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync(cancellationToken);

        var approvedReceiptDocuments = await BuildApprovedReceiptDocumentsAsync(cancellationToken);

        var merged = persistedDocuments
            .Concat(approvedReceiptDocuments)
            .Concat(await BuildPayableDebtStatementDocumentsAsync(cancellationToken))
            .Concat(await BuildSupplierQuickCheckDocumentsAsync(cancellationToken))
            .GroupBy(x => x.PhysicalFilePath, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .ToList();
        _documentsCache = merged;
        _documentsCacheLoadedAtUtc = DateTime.UtcNow;
        return merged;
        }
        finally
        {
            _documentsCacheLock.Release();
        }
    }

    private static string NormalizeAccountingVirtualPath(string? virtualPath)
    {
        if (string.IsNullOrWhiteSpace(virtualPath))
        {
            return string.Empty;
        }

        var path = virtualPath.Trim();
        if (path.StartsWith(@"This PC\", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        if (path.StartsWith(@"D:\ACCOUNTING\", StringComparison.OrdinalIgnoreCase))
        {
            return @"This PC\D: ACCOUNTING\" + path.Substring(@"D:\ACCOUNTING\".Length);
        }

        return path;
    }

    private async Task<IReadOnlyList<BusinessDocumentDto>> BuildApprovedReceiptDocumentsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var approvedReceipts = await dbContext.StockReceipts
            .AsNoTracking()
            .Where(x => x.IsApproved)
            .OrderByDescending(x => x.ReceiptDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.ReceiptNo,
                x.ReceiptDate,
                x.SupplierName,
                x.TotalAmount
            })
            .ToListAsync(cancellationToken);

        var receiptNos = approvedReceipts
            .Select(x => x.ReceiptNo)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var auditLogByReceiptNo = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => receiptNos.Contains(x.Detail)
                        && (x.Action == "APPROVE_RECEIPT" || x.Action == "CREATE_RECEIPT" || x.Action == "UPDATE_RECEIPT"))
            .GroupBy(x => x.Detail)
            .Select(g => new { ReceiptNo = g.Key, LastAt = g.Max(x => x.CreatedAt) })
            .ToDictionaryAsync(x => x.ReceiptNo, x => x.LastAt, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .Select(x => new { x.Id, x.SupplierCode, x.SupplierName })
            .ToListAsync(cancellationToken);

        return approvedReceipts.Select(x =>
        {
            var safeSupplierName = (x.SupplierName ?? string.Empty).Trim();
            var supplier = suppliers.FirstOrDefault(s =>
                string.Equals((s.SupplierName ?? string.Empty).Trim(), safeSupplierName, StringComparison.OrdinalIgnoreCase));
            var supplierId = supplier is not null
                ? ToSupplierEntityId(supplier.Id)
                : VirtualFileCenterSeed.StableGuid($"supplier:{safeSupplierName.ToUpperInvariant()}");
            var virtualFolder = $@"This PC\D: ACCOUNTING\Nhà cung cấp\{safeSupplierName}\{ImportFolderName}\{x.ReceiptDate:yyyy}\{x.ReceiptDate:MM}";
            var fileName = $"{x.ReceiptNo}.pdf";
            var timestamp = auditLogByReceiptNo.TryGetValue(x.ReceiptNo, out var lastAt)
                ? lastAt
                : x.ReceiptDate;
            return new BusinessDocumentDto
            {
                // Generated from a persisted receipt, therefore its virtual
                // document identity must remain stable across cache reloads.
                Id = GuidUtility.Create(GuidUtility.UrlNamespace, $"stock-receipt:{x.Id}"),
                Code = x.ReceiptNo,
                Title = $"Phiếu nhập {x.ReceiptNo}",
                Module = BusinessModule.Accounting,
                EntityType = BusinessEntityType.Supplier,
                EntityId = supplierId,
                DocumentType = BusinessDocumentType.ImportVoucher,
                DocumentDate = x.ReceiptDate,
                Amount = x.TotalAmount,
                Currency = "VND",
                VirtualPath = virtualFolder,
                PhysicalFilePath = $"storage://accounting/import/{x.ReceiptDate:yyyy}/{x.ReceiptDate:MM}/{x.ReceiptNo}.pdf",
                FileName = fileName,
                FileExtension = ".pdf",
                FileSize = 0,
                Status = DocumentStatus.Active,
                CreatedBy = "System",
                CreatedAt = timestamp,
                UpdatedAt = timestamp
            };
        }).ToList();
    }

    private async Task<IReadOnlyList<BusinessDocumentDto>> BuildPayableDebtStatementDocumentsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierName)
            .Select(x => new { x.Id, x.SupplierCode, x.SupplierName })
            .ToListAsync(cancellationToken);

        var payableTypes = new[]
        {
            BusinessDocumentType.ImportVoucher,
            BusinessDocumentType.Invoice,
            BusinessDocumentType.PaymentVoucher,
            BusinessDocumentType.AdvancePayment
        };

        var documents = await dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Supplier && payableTypes.Contains(x.DocumentType))
            .Select(x => new
            {
                x.EntityId,
                x.Code,
                x.DocumentType,
                x.DocumentDate,
                x.Amount,
                x.Status
            })
            .ToListAsync(cancellationToken);

        var approvedReceipts = await dbContext.StockReceipts
            .AsNoTracking()
            .Where(x => x.IsApproved)
            .Select(x => new
            {
                x.ReceiptNo,
                x.ReceiptDate,
                x.SupplierCode,
                x.SupplierName,
                x.TotalAmount
            })
            .ToListAsync(cancellationToken);

        var rows = new List<BusinessDocumentDto>();
        foreach (var supplier in suppliers)
        {
            var supplierEntityId = ToSupplierEntityId(supplier.Id);
            var supplierCodeKey = NormalizeKey(supplier.SupplierCode);
            var supplierNameKey = NormalizeKey(supplier.SupplierName);
            var importCodesInDocuments = documents
                .Where(x => x.EntityId == supplierEntityId && x.DocumentType == BusinessDocumentType.ImportVoucher)
                .Select(x => NormalizeKey(x.Code))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var supplierTransactions = documents
                .Where(x => x.EntityId == supplierEntityId)
                .Select(x => new
                {
                    x.DocumentDate,
                    Payable = x.DocumentType is BusinessDocumentType.ImportVoucher or BusinessDocumentType.Invoice ? x.Amount : 0m,
                    Paid = x.DocumentType is BusinessDocumentType.PaymentVoucher or BusinessDocumentType.AdvancePayment
                        ? x.Status is DocumentStatus.Active or DocumentStatus.Locked ? x.Amount : 0m
                        : 0m
                })
                .Concat(approvedReceipts
                    .Where(r =>
                        (string.Equals(NormalizeKey(r.SupplierCode), supplierCodeKey, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(NormalizeKey(r.SupplierName), supplierNameKey, StringComparison.OrdinalIgnoreCase))
                        && !importCodesInDocuments.Contains(NormalizeKey(r.ReceiptNo)))
                    .Select(r => new
                    {
                        DocumentDate = r.ReceiptDate,
                        Payable = r.TotalAmount,
                        Paid = 0m
                    }))
                .ToList();

            if (supplierTransactions.Count == 0)
            {
                continue;
            }

            var closing = supplierTransactions
                .Where(x => x.DocumentDate.Date <= DateTime.Today)
                .Sum(x => x.Payable - x.Paid);

            var safeCode = string.IsNullOrWhiteSpace(supplier.SupplierCode) ? supplier.Id.ToString("0000") : supplier.SupplierCode;
            var statementCode = $"DCN-{safeCode}";
            var virtualFolder = $@"This PC\D: ACCOUNTING\Nhà cung cấp\{supplier.SupplierName}\{DebtFolderName}";

            rows.Add(new BusinessDocumentDto
            {
                Id = GuidUtility.Create(GuidUtility.UrlNamespace, $"payable-debt-statement:{supplier.Id}"),
                Code = statementCode,
                Title = $"Đối chiếu công nợ - còn lại {Math.Max(0, closing):N0} VND",
                Module = BusinessModule.Accounting,
                EntityType = BusinessEntityType.Supplier,
                EntityId = supplierEntityId,
                DocumentType = BusinessDocumentType.DebtStatement,
                DocumentDate = DateTime.Today,
                Amount = Math.Max(0, closing),
                Currency = "VND",
                VirtualPath = virtualFolder,
                PhysicalFilePath = $"vfc://payable-debt-statement/{supplier.Id}",
                FileName = $"{statementCode}.pdf",
                FileExtension = ".pdf",
                FileSize = 0,
                Status = DocumentStatus.Active,
                CreatedBy = "System",
                CreatedAt = DateTime.Today,
                UpdatedAt = DateTime.Now
            });
        }

        return rows;
    }

    private async Task<IReadOnlyList<BusinessDocumentDto>> BuildSupplierQuickCheckDocumentsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var suppliers = await dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierName)
            .Select(x => new { x.Id, x.SupplierCode, x.SupplierName })
            .ToListAsync(cancellationToken);

        var documents = await dbContext.BusinessDocuments
            .AsNoTracking()
            .Where(x => x.EntityType == BusinessEntityType.Supplier)
            .Select(x => new
            {
                x.EntityId,
                x.DocumentType,
                x.DocumentDate,
                x.Amount,
                x.Status
            })
            .ToListAsync(cancellationToken);

        var approvedReceipts = await dbContext.StockReceipts
            .AsNoTracking()
            .Where(x => x.IsApproved)
            .Select(x => new
            {
                x.ReceiptDate,
                x.SupplierCode,
                x.SupplierName,
                x.TotalAmount
            })
            .ToListAsync(cancellationToken);

        var rows = new List<BusinessDocumentDto>();
        foreach (var supplier in suppliers)
        {
            var supplierEntityId = ToSupplierEntityId(supplier.Id);
            var supplierCodeKey = NormalizeKey(supplier.SupplierCode);
            var supplierNameKey = NormalizeKey(supplier.SupplierName);
            var safeCode = string.IsNullOrWhiteSpace(supplier.SupplierCode) ? supplier.Id.ToString("0000") : supplier.SupplierCode;

            var importTotal = documents
                .Where(x => x.EntityId == supplierEntityId && x.DocumentType is BusinessDocumentType.ImportVoucher or BusinessDocumentType.Invoice)
                .Sum(x => x.Amount)
                + approvedReceipts
                    .Where(r =>
                        string.Equals(NormalizeKey(r.SupplierCode), supplierCodeKey, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(NormalizeKey(r.SupplierName), supplierNameKey, StringComparison.OrdinalIgnoreCase))
                    .Sum(x => x.TotalAmount);

            if (importTotal > 0)
            {
                rows.Add(CreateSupplierQuickCheckDocument(
                    supplierEntityId,
                    supplier.SupplierName,
                    $"THPN-{safeCode}",
                    "Tổng hợp phiếu nhập",
                    importTotal,
                    ImportFolderName,
                    $"supplier-import-summary:{supplier.Id}",
                    $"vfc://supplier-import-summary/{supplier.Id}"));
            }

            var paymentTotal = documents
                .Where(x => x.EntityId == supplierEntityId
                            && x.DocumentType is BusinessDocumentType.PaymentVoucher or BusinessDocumentType.AdvancePayment
                            && x.Status is DocumentStatus.Active or DocumentStatus.Locked)
                .Sum(x => x.Amount);

            if (paymentTotal > 0)
            {
                rows.Add(CreateSupplierQuickCheckDocument(
                    supplierEntityId,
                    supplier.SupplierName,
                    $"THTT-{safeCode}",
                    "Tổng hợp thanh toán - tạm ứng",
                    paymentTotal,
                    PaymentFolderName,
                    $"supplier-payment-summary:{supplier.Id}",
                    $"vfc://supplier-payment-summary/{supplier.Id}"));
            }
        }

        return rows;
    }

    private static BusinessDocumentDto CreateSupplierQuickCheckDocument(
        Guid supplierEntityId,
        string supplierName,
        string code,
        string title,
        decimal amount,
        string folderName,
        string idSeed,
        string physicalPath)
    {
        return new BusinessDocumentDto
        {
            Id = GuidUtility.Create(GuidUtility.UrlNamespace, idSeed),
            Code = code,
            Title = $"{title} - tổng {amount:N0} VND",
            Module = BusinessModule.Accounting,
            EntityType = BusinessEntityType.Supplier,
            EntityId = supplierEntityId,
            DocumentType = BusinessDocumentType.Report,
            DocumentDate = DateTime.Today,
            Amount = amount,
            Currency = "VND",
            VirtualPath = $@"This PC\D: ACCOUNTING\Nhà cung cấp\{supplierName}\{folderName}",
            PhysicalFilePath = physicalPath,
            FileName = $"{code}.pdf",
            FileExtension = ".pdf",
            FileSize = 0,
            Status = DocumentStatus.Active,
            CreatedBy = "System",
            CreatedAt = DateTime.Today,
            UpdatedAt = DateTime.Now
        };
    }

    private async Task<IReadOnlyList<VirtualExplorerNodeDto>> BuildCustomerFoldersAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        var customers = await dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerName)
            .Select(x => new
            {
                x.Id,
                x.CustomerCode,
                x.CustomerName
            })
            .ToListAsync(cancellationToken);

        var customerIds = customers
            .Select(x => GuidUtility.Create(GuidUtility.UrlNamespace, $"customer:{x.Id}"))
            .ToHashSet();
        var documents = await GetDocumentsAsync(cancellationToken);
        var customersWithDocuments = documents
            .Where(x => x.EntityType == BusinessEntityType.Customer && customerIds.Contains(x.EntityId))
            .Select(x => x.EntityId)
            .ToHashSet();

        return customers
            .Select((c, idx) =>
            {
                var customerId = GuidUtility.Create(GuidUtility.UrlNamespace, $"customer:{c.Id}");
                var segment = string.IsNullOrWhiteSpace(c.CustomerCode) ? $"customer-{c.Id}" : c.CustomerCode;
                return CreateNode(
                    $"customer-{customerId:N}",
                    "accounting-customers",
                    string.IsNullOrWhiteSpace(c.CustomerCode) ? c.CustomerName : c.CustomerCode,
                    c.CustomerName,
                    "customer.ico",
                    $@"This PC\D: ACCOUNTING\Khách hàng\{segment}",
                    VirtualNodeType.EntityFolder,
                    BusinessModule.Accounting,
                    customersWithDocuments.Contains(customerId),
                    idx + 1,
                    BusinessEntityType.Customer,
                    customerId);
            })
            .ToList();
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildCustomerDocumentNodes(
        string virtualPath,
        Guid customerId,
        IReadOnlyList<BusinessDocumentDto> documents)
    {
        return documents
            .Select((document, index) => CreateNode(
                $"doc-{document.Id:N}",
                $"customer-{customerId:N}",
                string.IsNullOrWhiteSpace(document.FileName) ? document.Code : document.FileName,
                string.IsNullOrWhiteSpace(document.Title) ? document.Code : document.Title,
                "document.ico",
                $"{virtualPath}\\{document.Id:N}{document.FileExtension}",
                VirtualNodeType.Document,
                document.Module,
                false,
                index + 1,
                document.EntityType,
                document.EntityId,
                document.Id,
                document.UpdatedAt,
                physicalPath: document.PhysicalFilePath,
                fileSize: document.FileSize))
            .ToList();
    }

    private static bool TryResolveSupplierPath(string path, out string supplierName, out string tailPath)
    {
        supplierName = string.Empty;
        tailPath = string.Empty;

        const string prefix = @"This PC\D: ACCOUNTING\Nhà cung cấp\";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remaining = path.Substring(prefix.Length);
        var parts = remaining.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        supplierName = parts[0];
        tailPath = string.Join('\\', parts.Skip(1));
        return true;
    }

    private static bool TryResolveCustomerPath(string path, out string customerName, out string tailPath)
    {
        customerName = string.Empty;
        tailPath = string.Empty;

        const string prefix = @"This PC\D: ACCOUNTING\Khách hàng\";
        if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var remaining = path.Substring(prefix.Length);
        var parts = remaining.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        customerName = parts[0];
        tailPath = string.Join('\\', parts.Skip(1));
        return true;
    }

    private static bool IsDynamicBusinessDataPath(string path)
    {
        if (TryGetAccountingDocumentType(path, out _)
            || TryResolveSupplierPath(path, out _, out _)
            || TryResolveCustomerPath(path, out _, out _))
        {
            return true;
        }

        return path.Equals(@"This PC\D: ACCOUNTING\Nhà cung cấp", StringComparison.OrdinalIgnoreCase)
            || path.Equals(@"This PC\D: ACCOUNTING\Khách hàng", StringComparison.OrdinalIgnoreCase)
            || path.Equals(@"This PC\E: SALES\POS", StringComparison.OrdinalIgnoreCase)
            || path.Equals(@"This PC\E: SALES\Đơn hàng", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveDatePath(string path, out int? year, out int? month, out int? day, out BusinessDocumentType documentType, out bool includeAdvancePayment)
    {
        year = null;
        month = null;
        day = null;
        documentType = BusinessDocumentType.Invoice;
        includeAdvancePayment = false;

        var parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 6)
        {
            return false;
        }

        var folderName = parts[4];
        if (folderName.Equals(InvoiceFolderName, StringComparison.OrdinalIgnoreCase))
        {
            documentType = BusinessDocumentType.Invoice;
        }
        else if (folderName.Equals(ImportFolderName, StringComparison.OrdinalIgnoreCase))
        {
            documentType = BusinessDocumentType.ImportVoucher;
        }
        else if (folderName.Equals(PaymentFolderName, StringComparison.OrdinalIgnoreCase)
                 || folderName.Equals(PaymentFolderLegacyName, StringComparison.OrdinalIgnoreCase))
        {
            documentType = BusinessDocumentType.PaymentVoucher;
            includeAdvancePayment = true;
        }
        else if (folderName.Equals(DebtFolderName, StringComparison.OrdinalIgnoreCase))
        {
            documentType = BusinessDocumentType.ImportVoucher;
            includeAdvancePayment = true;
        }
        else
        {
            return false;
        }

        if (!int.TryParse(parts[5], out var y))
        {
            return false;
        }
        year = y;

        if (parts.Length == 6 || !int.TryParse(parts[6], out var m))
        {
            return true;
        }
        month = m;

        if (parts.Length == 7 || !int.TryParse(parts[7], out var d))
        {
            return true;
        }
        day = d;
        return true;
    }

    private static VirtualExplorerNodeDto CreateNode(
        string id,
        string? parentId,
        string name,
        string displayName,
        string icon,
        string virtualPath,
        VirtualNodeType nodeType,
        BusinessModule module,
        bool hasChildren,
        int sortOrder,
        BusinessEntityType? entityType = null,
        Guid? entityId = null,
        Guid? documentId = null,
        DateTime? dateModified = null,
        string? description = null,
        string? status = null,
        string? physicalPath = null,
        long fileSize = 0,
        double? usedPercent = null,
        bool hasUsageMetric = false,
        bool isAlert = false)
    {
        return new VirtualExplorerNodeDto
        {
            Id = id,
            ParentId = parentId,
            Name = name,
            DisplayName = displayName,
            Icon = icon,
            IconGlyph = ResolveGlyph(nodeType, module, name),
            VirtualPath = virtualPath,
            NodeType = nodeType,
            Module = module,
            EntityType = entityType,
            EntityId = entityId,
            DocumentId = documentId,
            DateModified = dateModified,
            HasChildren = hasChildren,
            SortOrder = sortOrder,
            Description = description,
            Status = status,
            PhysicalPath = physicalPath,
            FileSize = fileSize,
            UsedPercent = usedPercent,
            HasUsageMetric = hasUsageMetric,
            IsAlert = isAlert
        };
    }

    private sealed record ConfiguredDrive(
        string Code,
        string DisplayName,
        string Icon,
        string? PathKey,
        string? EnabledKey,
        BusinessModule Module,
        int SortOrder);

    private sealed record DriveSetting(
        string? PathKey,
        string? EnabledKey,
        BusinessModule Module);

    private sealed record DriveHealth(
        string Description,
        string Status,
        double? UsedPercent,
        bool HasUsageMetric,
        bool IsAlert);

    private sealed record NetworkHealth(
        string Description,
        string Status,
        bool IsAlert);

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.0} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024d * 1024):0.0} MB";
        return $"{bytes / (1024d * 1024 * 1024):0.0} GB";
    }

    private static string ResolveGlyph(VirtualNodeType nodeType, BusinessModule module, string name)
    {
        return nodeType switch
        {
            VirtualNodeType.Drive => "\uE7F1",
            VirtualNodeType.Document => "\uE8A5",
            VirtualNodeType.Report => "\uE8B7",
            VirtualNodeType.NetworkItem => "\uE968",
            VirtualNodeType.SystemTool => "\uE8B7",
            VirtualNodeType.Folder => "\uE8B7",
            VirtualNodeType.EntityFolder => "\uE8B7",
            _ => module switch
            {
                BusinessModule.Accounting => "\uE8B7",
                BusinessModule.Sales => "\uE8B7",
                BusinessModule.HR => "\uE8B7",
                BusinessModule.Logs => "\uE8B7",
                BusinessModule.Network => "\uE968",
                _ => "\uE8B7"
            }
        };
    }

    private static string NormalizePath(string path)
    {
        return (path ?? string.Empty).Trim().Replace('/', '\\');
    }

    private static bool Contains(string source, string keyword)
    {
        return source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeKey(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static Guid ToSupplierEntityId(int supplierId)
    {
        return GuidUtility.Create(GuidUtility.UrlNamespace, $"supplier:{supplierId}");
    }
}

