using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
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

    private readonly AppDbContext _dbContext;
    private readonly SemaphoreSlim _documentsCacheLock = new(1, 1);
    private IReadOnlyList<BusinessDocumentDto>? _documentsCache;
    private DateTime _documentsCacheLoadedAtUtc;
    private static readonly TimeSpan DocumentsCacheTtl = TimeSpan.FromSeconds(30);
    public VirtualExplorerRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
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
            return BuildDriveNodes();
        }

        if (normalizedPath.Equals("Network", StringComparison.OrdinalIgnoreCase))
        {
            return BuildNetworkNodes();
        }

        if (normalizedPath.Equals(@"This PC\D: ACCOUNTING", StringComparison.OrdinalIgnoreCase))
        {
            return BuildAccountingNodes();
        }

        if (normalizedPath.Equals(@"This PC\C: SYSTEM", StringComparison.OrdinalIgnoreCase))
        {
            return BuildSystemNodes();
        }

        if (normalizedPath.Equals(@"This PC\E: SALES", StringComparison.OrdinalIgnoreCase))
        {
            return BuildSalesNodes();
        }

        if (normalizedPath.Equals(@"This PC\F: HR", StringComparison.OrdinalIgnoreCase))
        {
            return BuildHrNodes();
        }

        if (normalizedPath.Equals(@"This PC\G: LOGS", StringComparison.OrdinalIgnoreCase))
        {
            return BuildLogNodes();
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

        if (TryResolveCustomerPath(normalizedPath, out var customerName, out var customerTailPath))
        {
            if (string.IsNullOrWhiteSpace(customerTailPath))
            {
                return BuildCustomerRootFolders(customerName);
            }
        }

        return Array.Empty<VirtualExplorerNodeDto>();
    }

    public async Task<IReadOnlyList<VirtualExplorerNodeDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
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

        var thisPcChildren = await GetChildrenAsync("This PC", cancellationToken);
        results.AddRange(thisPcChildren.Where(x => Contains(x.DisplayName, normalized)));

        var accountingChildren = await GetChildrenAsync(@"This PC\D: ACCOUNTING", cancellationToken);
        results.AddRange(accountingChildren.Where(x => Contains(x.DisplayName, normalized)));

        var suppliers = await GetChildrenAsync(@"This PC\D: ACCOUNTING\Nhà cung cấp", cancellationToken);
        results.AddRange(suppliers.Where(x => Contains(x.DisplayName, normalized)));

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
                HasChildren = false,
                SortOrder = idx + 1
            }));

        return results
            .GroupBy(x => x.VirtualPath, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToList();
    }

    public async Task<VirtualExplorerNodeDto?> GetNodeAsync(string virtualPath, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizePath(virtualPath);
        var roots = await GetRootAsync(cancellationToken);
        var rootNode = roots.FirstOrDefault(x => x.VirtualPath.Equals(normalized, StringComparison.OrdinalIgnoreCase));
        if (rootNode is not null)
        {
            return rootNode;
        }

        var searchResult = await SearchAsync(normalized, cancellationToken);
        return searchResult.FirstOrDefault(x => x.VirtualPath.Equals(normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildDriveNodes()
    {
        return new List<VirtualExplorerNodeDto>
        {
            CreateNode("drive-c", "this-pc", "C: SYSTEM", "C: SYSTEM", "drive-system.ico", @"This PC\C: SYSTEM", VirtualNodeType.Drive, BusinessModule.System, true, 10),
            CreateNode("drive-d", "this-pc", "D: ACCOUNTING", "D: ACCOUNTING", "drive-accounting.ico", @"This PC\D: ACCOUNTING", VirtualNodeType.Drive, BusinessModule.Accounting, true, 20),
            CreateNode("drive-e", "this-pc", "E: SALES", "E: SALES", "drive-sales.ico", @"This PC\E: SALES", VirtualNodeType.Drive, BusinessModule.Sales, true, 30),
            CreateNode("drive-f", "this-pc", "F: HR", "F: HR", "drive-hr.ico", @"This PC\F: HR", VirtualNodeType.Drive, BusinessModule.HR, true, 40),
            CreateNode("drive-g", "this-pc", "G: LOGS", "G: LOGS", "drive-logs.ico", @"This PC\G: LOGS", VirtualNodeType.Drive, BusinessModule.Logs, true, 50),
            CreateNode("drive-net", "this-pc", "Network", "Network", "network.ico", "Network", VirtualNodeType.NetworkItem, BusinessModule.Network, true, 60)
        };
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildAccountingNodes()
    {
        return new List<VirtualExplorerNodeDto>
        {
            CreateNode("accounting-suppliers", "drive-d", "Nhà cung cấp", "Nhà cung cấp", FilledFolderIcon, @"This PC\D: ACCOUNTING\Nhà cung cấp", VirtualNodeType.Folder, BusinessModule.Accounting, true, 10),
            CreateNode("accounting-customers", "drive-d", "Khách hàng", "Khách hàng", FilledFolderIcon, @"This PC\D: ACCOUNTING\Khách hàng", VirtualNodeType.Folder, BusinessModule.Accounting, true, 20),
            CreateNode("accounting-invoices", "drive-d", "Hóa đơn", "Hóa đơn", "invoice.ico", @"This PC\D: ACCOUNTING\Hóa đơn", VirtualNodeType.Folder, BusinessModule.Accounting, false, 30),
            CreateNode("accounting-receipts", "drive-d", "Phiếu nhập", "Phiếu nhập", "import.ico", @"This PC\D: ACCOUNTING\Phiếu nhập", VirtualNodeType.Folder, BusinessModule.Accounting, false, 40),
            CreateNode("accounting-issues", "drive-d", "Phiếu xuất", "Phiếu xuất", "export.ico", @"This PC\D: ACCOUNTING\Phiếu xuất", VirtualNodeType.Folder, BusinessModule.Accounting, false, 50),
            CreateNode("accounting-debt", "drive-d", "Công nợ", "Công nợ", "debt.ico", @"This PC\D: ACCOUNTING\Công nợ", VirtualNodeType.Folder, BusinessModule.Accounting, false, 60),
            CreateNode("accounting-report", "drive-d", "Báo cáo kế toán", "Báo cáo kế toán", "report.ico", @"This PC\D: ACCOUNTING\Báo cáo kế toán", VirtualNodeType.Report, BusinessModule.Accounting, false, 70)
        };
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildSystemNodes()
    {
        return new List<VirtualExplorerNodeDto>
        {
            CreateNode("system-schema", "drive-c", "Database Schemas", "Database Schemas", "db.ico", @"This PC\C: SYSTEM\Database Schemas", VirtualNodeType.SystemTool, BusinessModule.System, false, 10),
            CreateNode("system-tables", "drive-c", "Tables", "Tables", "table.ico", @"This PC\C: SYSTEM\Tables", VirtualNodeType.SystemTool, BusinessModule.System, false, 20),
            CreateNode("system-views", "drive-c", "Views", "Views", "view.ico", @"This PC\C: SYSTEM\Views", VirtualNodeType.SystemTool, BusinessModule.System, false, 30),
            CreateNode("system-proc", "drive-c", "Stored Procedures", "Stored Procedures", "code.ico", @"This PC\C: SYSTEM\Stored Procedures", VirtualNodeType.SystemTool, BusinessModule.System, false, 40),
            CreateNode("system-backup", "drive-c", "Backup / Restore", "Backup / Restore", "backup.ico", @"This PC\C: SYSTEM\Backup / Restore", VirtualNodeType.SystemTool, BusinessModule.System, false, 50),
            CreateNode("system-users", "drive-c", "Users & Roles", "Users & Roles", "user.ico", @"This PC\C: SYSTEM\Users & Roles", VirtualNodeType.SystemTool, BusinessModule.System, false, 60),
            CreateNode("system-settings", "drive-c", "System Settings", "System Settings", "settings.ico", @"This PC\C: SYSTEM\System Settings", VirtualNodeType.SystemTool, BusinessModule.System, false, 70)
        };
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildSalesNodes()
    {
        return new List<VirtualExplorerNodeDto>
        {
            CreateNode("sales-fo", "drive-e", "FO", "FO", "fo.ico", @"This PC\E: SALES\FO", VirtualNodeType.Folder, BusinessModule.Sales, false, 10),
            CreateNode("sales-pos", "drive-e", "POS", "POS", "pos.ico", @"This PC\E: SALES\POS", VirtualNodeType.Folder, BusinessModule.Sales, false, 20),
            CreateNode("sales-orders", "drive-e", "Đơn hàng", "Đơn hàng", "order.ico", @"This PC\E: SALES\Đơn hàng", VirtualNodeType.Folder, BusinessModule.Sales, false, 30),
            CreateNode("sales-shift", "drive-e", "Ca bán hàng", "Ca bán hàng", "shift.ico", @"This PC\E: SALES\Ca bán hàng", VirtualNodeType.Folder, BusinessModule.Sales, false, 40),
            CreateNode("sales-revenue", "drive-e", "Doanh thu", "Doanh thu", "report.ico", @"This PC\E: SALES\Doanh thu", VirtualNodeType.Report, BusinessModule.Sales, false, 50)
        };
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildHrNodes()
    {
        return new List<VirtualExplorerNodeDto>
        {
            CreateNode("hr-people", "drive-f", "Nhân sự", "Nhân sự", "people.ico", @"This PC\F: HR\Nhân sự", VirtualNodeType.Folder, BusinessModule.HR, false, 10),
            CreateNode("hr-attendance", "drive-f", "Chấm công", "Chấm công", "clock.ico", @"This PC\F: HR\Chấm công", VirtualNodeType.Folder, BusinessModule.HR, false, 20),
            CreateNode("hr-payroll", "drive-f", "Lương", "Lương", "salary.ico", @"This PC\F: HR\Lương", VirtualNodeType.Folder, BusinessModule.HR, false, 30),
            CreateNode("hr-shift", "drive-f", "Phân ca", "Phân ca", "shift.ico", @"This PC\F: HR\Phân ca", VirtualNodeType.Folder, BusinessModule.HR, false, 40)
        };
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildLogNodes()
    {
        return new List<VirtualExplorerNodeDto>
        {
            CreateNode("log-audit", "drive-g", "Audit Logs", "Audit Logs", "audit.ico", @"This PC\G: LOGS\Audit Logs", VirtualNodeType.Folder, BusinessModule.Logs, false, 10),
            CreateNode("log-error", "drive-g", "Error Logs", "Error Logs", "error.ico", @"This PC\G: LOGS\Error Logs", VirtualNodeType.Folder, BusinessModule.Logs, false, 20),
            CreateNode("log-sync", "drive-g", "Sync Logs", "Sync Logs", "sync.ico", @"This PC\G: LOGS\Sync Logs", VirtualNodeType.Folder, BusinessModule.Logs, false, 30),
            CreateNode("log-login", "drive-g", "Login Logs", "Login Logs", "login.ico", @"This PC\G: LOGS\Login Logs", VirtualNodeType.Folder, BusinessModule.Logs, false, 40),
            CreateNode("log-backup", "drive-g", "Backup Logs", "Backup Logs", "backup.ico", @"This PC\G: LOGS\Backup Logs", VirtualNodeType.Folder, BusinessModule.Logs, false, 50)
        };
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildNetworkNodes()
    {
        return new List<VirtualExplorerNodeDto>
        {
            CreateNode("net-sql", "network", "SQL Server", "SQL Server", "sql.ico", @"Network\SQL Server", VirtualNodeType.NetworkItem, BusinessModule.Network, false, 10),
            CreateNode("net-api", "network", "API Server", "API Server", "api.ico", @"Network\API Server", VirtualNodeType.NetworkItem, BusinessModule.Network, false, 20),
            CreateNode("net-pos", "network", "POS Terminals", "POS Terminals", "pos.ico", @"Network\POS Terminals", VirtualNodeType.NetworkItem, BusinessModule.Network, false, 30),
            CreateNode("net-fo", "network", "FO Clients", "FO Clients", "fo.ico", @"Network\FO Clients", VirtualNodeType.NetworkItem, BusinessModule.Network, false, 40),
            CreateNode("net-share", "network", "Shared Folders", "Shared Folders", EmptyFolderIcon, @"Network\Shared Folders", VirtualNodeType.NetworkItem, BusinessModule.Network, false, 50),
            CreateNode("net-printer", "network", "Printers", "Printers", "printer.ico", @"Network\Printers", VirtualNodeType.NetworkItem, BusinessModule.Network, false, 60)
        };
    }

    private async Task<IReadOnlyList<VirtualFileCenterSeed.SupplierSeed>> GetSupplierSeedsAsync(CancellationToken cancellationToken)
    {
        var dbSuppliers = await _dbContext.Suppliers
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

        var currentYear = DateTime.Today.Year;
        if (!years.Contains(currentYear))
        {
            years.Insert(0, currentYear);
        }

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
                doc.UpdatedAt))
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
                doc.UpdatedAt))
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
                doc.UpdatedAt))
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

        var seeded = VirtualFileCenterSeed.DefaultDocuments();
        IReadOnlyList<BusinessDocumentDto> persistedDocuments;
        try
        {
            persistedDocuments = await _dbContext.BusinessDocuments
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
        }
        catch
        {
            persistedDocuments = Array.Empty<BusinessDocumentDto>();
        }

        IReadOnlyList<BusinessDocumentDto> approvedReceiptDocuments;
        try
        {
            approvedReceiptDocuments = await BuildApprovedReceiptDocumentsAsync(cancellationToken);
        }
        catch
        {
            approvedReceiptDocuments = Array.Empty<BusinessDocumentDto>();
        }

        var merged = seeded
            .Concat(persistedDocuments)
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
        var approvedReceipts = await _dbContext.StockReceipts
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

        var auditLogByReceiptNo = await _dbContext.AuditLogs
            .AsNoTracking()
            .Where(x => receiptNos.Contains(x.Detail)
                        && (x.Action == "APPROVE_RECEIPT" || x.Action == "CREATE_RECEIPT" || x.Action == "UPDATE_RECEIPT"))
            .GroupBy(x => x.Detail)
            .Select(g => new { ReceiptNo = g.Key, LastAt = g.Max(x => x.CreatedAt) })
            .ToDictionaryAsync(x => x.ReceiptNo, x => x.LastAt, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var suppliers = await _dbContext.Suppliers
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
                Id = Guid.NewGuid(),
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
        var suppliers = await _dbContext.Suppliers
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

        var documents = await _dbContext.BusinessDocuments
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

        var approvedReceipts = await _dbContext.StockReceipts
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
        var suppliers = await _dbContext.Suppliers
            .AsNoTracking()
            .OrderBy(x => x.SupplierName)
            .Select(x => new { x.Id, x.SupplierCode, x.SupplierName })
            .ToListAsync(cancellationToken);

        var documents = await _dbContext.BusinessDocuments
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

        var approvedReceipts = await _dbContext.StockReceipts
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
        var customers = await _dbContext.Customers
            .AsNoTracking()
            .OrderBy(x => x.CustomerName)
            .Select(x => new
            {
                x.CustomerCode,
                x.CustomerName
            })
            .ToListAsync(cancellationToken);

        return customers
            .Select((c, idx) => CreateNode(
                $"customer-{VirtualFileCenterSeed.StableGuid($"customer:{c.CustomerName.ToUpperInvariant()}")}",
                "accounting-customers",
                c.CustomerName,
                c.CustomerName,
                "customer.ico",
                $@"This PC\D: ACCOUNTING\Khách hàng\{c.CustomerName}",
                VirtualNodeType.EntityFolder,
                BusinessModule.Accounting,
                false,
                idx + 1,
                BusinessEntityType.Customer,
                VirtualFileCenterSeed.StableGuid($"customer:{c.CustomerName.ToUpperInvariant()}")))
            .ToList();
    }

    private static IReadOnlyList<VirtualExplorerNodeDto> BuildCustomerRootFolders(string customerName)
    {
        var customerId = VirtualFileCenterSeed.StableGuid($"customer:{customerName.ToUpperInvariant()}");
        var basePath = $@"This PC\D: ACCOUNTING\Khách hàng\{customerName}";

        return new List<VirtualExplorerNodeDto>
        {
            CreateNode($"{customerId}-profile", $"customer-{customerId}", "01. Hồ sơ khách hàng", "01. Hồ sơ khách hàng", EmptyFolderIcon, basePath + @"\01. Hồ sơ khách hàng", VirtualNodeType.Folder, BusinessModule.Accounting, false, 10, BusinessEntityType.Customer, customerId),
            CreateNode($"{customerId}-contract", $"customer-{customerId}", "02. Hợp đồng", "02. Hợp đồng", EmptyFolderIcon, basePath + @"\02. Hợp đồng", VirtualNodeType.Folder, BusinessModule.Accounting, false, 20, BusinessEntityType.Customer, customerId),
            CreateNode($"{customerId}-advance", $"customer-{customerId}", "03. Tạm ứng hợp đồng", "03. Tạm ứng hợp đồng", EmptyFolderIcon, basePath + @"\03. Tạm ứng hợp đồng", VirtualNodeType.Folder, BusinessModule.Accounting, false, 30, BusinessEntityType.Customer, customerId),
            CreateNode($"{customerId}-debt", $"customer-{customerId}", "04. Công nợ", "04. Công nợ", EmptyFolderIcon, basePath + @"\04. Công nợ", VirtualNodeType.Folder, BusinessModule.Accounting, false, 40, BusinessEntityType.Customer, customerId),
            CreateNode($"{customerId}-report", $"customer-{customerId}", "05. In ấn - báo cáo", "05. In ấn - báo cáo", EmptyFolderIcon, basePath + @"\05. In ấn - báo cáo", VirtualNodeType.Report, BusinessModule.Accounting, false, 50, BusinessEntityType.Customer, customerId)
        };
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
        DateTime? dateModified = null)
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
            SortOrder = sortOrder
        };
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

