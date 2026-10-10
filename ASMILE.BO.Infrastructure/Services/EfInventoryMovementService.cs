using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Accounting;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Shared.Storage;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EfInventoryMovementService : IInventoryMovementService
{
    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLogService;
    private readonly ISettingsService _settingsService;

    public EfInventoryMovementService(AppDbContext db, IAuditLogService auditLogService, ISettingsService settingsService)
    {
        _db = db;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
    }

    public async Task<InventoryDashboardDto> GetDashboardAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var balances = await GetStockBalancesAsync(filter, cancellationToken);
        var movements = await GetMovementsAsync(filter, cancellationToken);

        var groupedWarehouse = balances
            .GroupBy(x => new { x.WarehouseCode, x.WarehouseName })
            .Select(g => new InventoryWarehouseSummaryDto
            {
                WarehouseCode = g.Key.WarehouseCode,
                WarehouseName = g.Key.WarehouseName,
                TotalStock = g.Sum(x => x.ClosingQuantity),
                TotalValue = g.Sum(x => x.StockValue)
            })
            .OrderByDescending(x => x.TotalValue)
            .ToList();

        var today = DateTime.Today;
        var start = today.AddDays(-6);
        var daily = Enumerable.Range(0, 7)
            .Select(i => start.AddDays(i))
            .Select(day => new InventoryDailyFlowDto
            {
                Date = day,
                QuantityIn = movements.Where(x => x.MovementDate.Date == day.Date).Sum(x => x.QuantityIn),
                QuantityOut = movements.Where(x => x.MovementDate.Date == day.Date).Sum(x => x.QuantityOut)
            })
            .ToList();

        return new InventoryDashboardDto
        {
            TotalItems = balances.Select(x => x.ProductId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            TotalStock = balances.Sum(x => x.ClosingQuantity),
            TotalInPeriod = balances.Sum(x => x.TotalIn),
            TotalOutPeriod = balances.Sum(x => x.TotalOut),
            TotalStockValue = balances.Sum(x => x.StockValue),
            LowStockCount = balances.Count(x => x.ClosingQuantity < x.MinStockThreshold && x.ClosingQuantity >= 0),
            NegativeStockCount = balances.Count(x => x.ClosingQuantity < 0),
            RecentMovementCount = movements.Count(x => x.MovementDate >= DateTime.Today.AddDays(-7)),
            WarehouseSummaries = groupedWarehouse,
            DailyFlows = daily
        };
    }

    public async Task<IReadOnlyList<InventoryStockBalanceDto>> GetStockBalancesAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var warehouseMap = await _db.Warehouses
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        var itemMap = await _db.Items
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x, cancellationToken);

        var query = _db.StockBalances.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.WarehouseCode))
        {
            var warehouseIds = warehouseMap.Values
                .Where(x => x.WarehouseCode.Equals(filter.WarehouseCode, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Id)
                .ToHashSet();
            query = query.Where(x => warehouseIds.Contains(x.WarehouseId));
        }

        if (!string.IsNullOrWhiteSpace(filter.ItemGroup))
        {
            var itemIds = itemMap.Values
                .Where(x => x.GroupName.Equals(filter.ItemGroup, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Id)
                .ToHashSet();
            query = query.Where(x => itemIds.Contains(x.ItemId));
        }

        var rows = await query.ToListAsync(cancellationToken);
        var defaultMinStockThreshold = await GetLowStockThresholdAsync(cancellationToken);

        var result = rows
            .Select(row =>
            {
                if (!itemMap.TryGetValue(row.ItemId, out var item) || !warehouseMap.TryGetValue(row.WarehouseId, out var wh))
                {
                    return null;
                }

                var min = item.IsMinibarItem && item.MinibarMinStock > 0
                    ? item.MinibarMinStock
                    : defaultMinStockThreshold;
                var max = item.IsMinibarItem && item.MinibarMaxStock >= min
                    ? item.MinibarMaxStock
                    : min;
                var status = BuildStatus(row.ClosingQty, min, row.InQty, row.OutQty);
                return new InventoryStockBalanceDto
                {
                    ProductId = item.Id.ToString(CultureInfo.InvariantCulture),
                    ProductCode = item.ItemCode,
                    ProductName = item.ItemName,
                    ItemGroup = item.GroupName,
                    SupplierCode = item.SupplierCode,
                    SupplierName = item.SupplierName,
                    WarehouseId = wh.Id.ToString(CultureInfo.InvariantCulture),
                    WarehouseCode = wh.WarehouseCode,
                    WarehouseName = wh.WarehouseName,
                    OpeningQuantity = row.OpeningQty,
                    TotalIn = row.InQty,
                    TotalOut = row.OutQty,
                    ClosingQuantity = row.ClosingQty,
                    MinStockThreshold = min,
                    MaxStockThreshold = max,
                    Unit = item.Unit,
                    AverageCost = row.AvgPrice,
                    StockValue = row.ClosingAmount != 0 ? row.ClosingAmount : row.ClosingQty * row.AvgPrice,
                    Status = status
                };
            })
            .Where(x => x is not null)
            .Cast<InventoryStockBalanceDto>()
            .Where(x => string.IsNullOrWhiteSpace(filter.Keyword)
                        || x.ProductCode.Contains(filter.Keyword, StringComparison.OrdinalIgnoreCase)
                        || x.ProductName.Contains(filter.Keyword, StringComparison.OrdinalIgnoreCase))
            .Where(x => !filter.LowStockOnly || x.ClosingQuantity < x.MinStockThreshold)
            .Where(x => !filter.NegativeStockOnly || x.ClosingQuantity < 0)
            .OrderBy(x => x.ProductCode)
            .ToList();

        return result;
    }

    private async Task<decimal> GetLowStockThresholdAsync(CancellationToken cancellationToken)
    {
        // Settings UI persists app-scoped inventory settings under the stable
        // "inventory" target; reading the unscoped value silently ignored the
        // user's saved threshold and always fell back to 20.
        var value = await _settingsService.GetValueAsync("Warehouse.LowStockThreshold", SettingScope.App, "inventory");
        if (decimal.TryParse(value?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var threshold)
            && threshold >= 0)
        {
            return threshold;
        }

        return 20m;
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetMovementsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var rows = await LoadAllMovementsAsync(cancellationToken);
        return ApplyFilter(rows, filter)
            .OrderByDescending(x => x.MovementDate)
            .ThenByDescending(x => x.Id)
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetMovementsByProductAsync(string productId, string? warehouseId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var rows = await LoadAllMovementsAsync(cancellationToken);
        var query = rows.Where(x => x.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            query = query.Where(x => x.WarehouseId == warehouseId);
        }

        if (fromDate.HasValue) query = query.Where(x => x.MovementDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.MovementDate.Date <= toDate.Value.Date);

        return query.OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.Id).ToList();
    }

    public async Task<IReadOnlyList<InventoryDocumentDto>> GetDocumentsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var rows = await GetMovementsAsync(filter, cancellationToken);

        return rows
            .GroupBy(x => x.DocumentNo)
            .Select(g =>
            {
                var first = g.OrderByDescending(x => x.MovementDate).First();
                return new InventoryDocumentDto
                {
                    DocumentNo = g.Key,
                    DocumentDate = first.MovementDate,
                    DocumentType = first.DocumentType,
                    WarehouseCode = first.WarehouseCode,
                    WarehouseName = first.WarehouseName,
                    TotalItems = g.Select(x => x.ProductId).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                    TotalQuantity = g.Sum(x => x.QuantityIn + x.QuantityOut),
                    TotalAmount = g.Sum(x => x.Amount),
                    Status = "Đã duyệt"
                };
            })
            .OrderByDescending(x => x.DocumentDate)
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetDocumentLinesAsync(string documentNo, CancellationToken cancellationToken = default)
    {
        var rows = await LoadAllMovementsAsync(cancellationToken);
        return rows.Where(x => x.DocumentNo.Equals(documentNo, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.ProductCode)
            .ToList();
    }

    public async Task<IReadOnlyList<InventoryLowStockAlertDto>> GetLowStockAlertsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var balances = await GetStockBalancesAsync(filter, cancellationToken);
        return balances
            .Where(x => x.ClosingQuantity < x.MinStockThreshold || x.ClosingQuantity < 0)
            .Select(x =>
            {
                var missing = x.ClosingQuantity < x.MinStockThreshold ? x.MinStockThreshold - x.ClosingQuantity : 0;
                return new InventoryLowStockAlertDto
                {
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    SupplierCode = x.SupplierCode,
                    WarehouseId = x.WarehouseId,
                    WarehouseCode = x.WarehouseCode,
                    CurrentStock = x.ClosingQuantity,
                    MinStockThreshold = x.MinStockThreshold,
                    TargetStockThreshold = x.MaxStockThreshold,
                    MissingQuantity = Math.Max(0, missing),
                    SuggestedSupplier = string.IsNullOrWhiteSpace(x.SupplierName)
                        ? (string.IsNullOrWhiteSpace(x.SupplierCode) ? "Chưa cấu hình nhà cung cấp" : x.SupplierCode)
                        : x.SupplierName,
                    SuggestedPurchaseQty = Math.Max(0, x.MaxStockThreshold - x.ClosingQuantity),
                    Severity = x.ClosingQuantity < 0 ? "Critical" : "Warning"
                };
            })
            .OrderByDescending(x => x.Severity)
            .ThenBy(x => x.CurrentStock)
            .ToList();
    }

    public async Task RecalculateStockAsync(CancellationToken cancellationToken = default)
    {
        var movements = await LoadAllMovementsAsync(cancellationToken);
        var grouped = movements
            .GroupBy(x => new { x.ProductId, x.WarehouseId })
            .ToList();

        var itemCodeToId = await _db.Items.AsNoTracking()
            .ToDictionaryAsync(x => x.ItemCode, x => x.Id, cancellationToken);
        var whCodeToId = await _db.Warehouses.AsNoTracking()
            .ToDictionaryAsync(x => x.WarehouseCode, x => x.Id, cancellationToken);

        var existing = await _db.StockBalances.ToListAsync(cancellationToken);
        _db.StockBalances.RemoveRange(existing);

        foreach (var group in grouped)
        {
            if (!int.TryParse(group.Key.ProductId, out var itemId) && !itemCodeToId.TryGetValue(group.First().ProductCode, out itemId))
            {
                continue;
            }

            if (!int.TryParse(group.Key.WarehouseId, out var warehouseId) && !whCodeToId.TryGetValue(group.First().WarehouseCode, out warehouseId))
            {
                continue;
            }

            var ordered = group.OrderBy(x => x.MovementDate).ThenBy(x => x.Id).ToList();
            var openingQty = 0m;
            var inQty = ordered.Sum(x => x.QuantityIn);
            var outQty = ordered.Sum(x => x.QuantityOut);
            var closingQty = openingQty + inQty - outQty;
            var avg = ordered.Any() ? ordered.Average(x => x.UnitPrice) : 0m;

            _db.StockBalances.Add(new StockBalance
            {
                WarehouseId = warehouseId,
                ItemId = itemId,
                OpeningQty = openingQty,
                OpeningAmount = 0,
                InQty = inQty,
                InAmount = ordered.Sum(x => x.QuantityIn * x.UnitPrice),
                OutQty = outQty,
                OutAmount = ordered.Sum(x => x.QuantityOut * x.UnitPrice),
                ClosingQty = closingQty,
                ClosingAmount = closingQty * avg,
                AvgPrice = avg
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("InventoryRecalculate", "Recalculate stock balances from transaction tables", "inventory", cancellationToken);
    }

    public async Task CreateAdjustmentAsync(string productId, string warehouseId, decimal actualQuantity, string reason, CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(productId, out var itemId) || !int.TryParse(warehouseId, out var whId))
        {
            return;
        }

        var balance = await _db.StockBalances.FirstOrDefaultAsync(x => x.ItemId == itemId && x.WarehouseId == whId, cancellationToken);
        var currentQty = balance?.ClosingQty ?? 0m;
        var delta = actualQuantity - currentQty;
        if (delta == 0) return;

        if (delta > 0)
        {
            var receipt = new StockReceipt
            {
                ReceiptNo = $"ADJIN{DateTime.Now:yyMMddHHmmss}",
                ReceiptDate = DateTime.Now,
                SupplierCode = "ADJUST",
                SupplierName = "Điều chỉnh tồn kho",
                ReceiverName = "system",
                Description = string.IsNullOrWhiteSpace(reason) ? "Điều chỉnh tăng tồn kho" : reason,
                WarehouseId = whId,
                DebitAccountCode = "156",
                CreditAccountCode = "338",
                TotalAmount = Math.Abs(delta) * (balance?.AvgPrice ?? 0m),
                IsApproved = true,
                Details = new List<StockReceiptDetail>
                {
                    new()
                    {
                        LineNo = 1,
                        ItemId = itemId,
                        WarehouseId = whId,
                        Unit = await _db.Items.Where(x => x.Id == itemId).Select(x => x.Unit).FirstOrDefaultAsync(cancellationToken) ?? string.Empty,
                        Quantity = delta,
                        UnitPrice = balance?.AvgPrice ?? 0m,
                        Amount = Math.Abs(delta) * (balance?.AvgPrice ?? 0m)
                    }
                }
            };
            _db.StockReceipts.Add(receipt);
        }
        else
        {
            var issue = new StockIssue
            {
                IssueNo = $"ADJOUT{DateTime.Now:yyMMddHHmmss}",
                IssueDate = DateTime.Now,
                Description = string.IsNullOrWhiteSpace(reason) ? "Điều chỉnh giảm tồn kho" : reason,
                WarehouseId = whId,
                DepartmentCode = "INV-ADJ",
                DebitAccountCode = "632",
                CreditAccountCode = "156",
                TotalAmount = Math.Abs(delta) * (balance?.AvgPrice ?? 0m),
                IsApproved = true,
                Details = new List<StockIssueDetail>
                {
                    new()
                    {
                        LineNo = 1,
                        ItemId = itemId,
                        WarehouseId = whId,
                        Unit = await _db.Items.Where(x => x.Id == itemId).Select(x => x.Unit).FirstOrDefaultAsync(cancellationToken) ?? string.Empty,
                        Quantity = Math.Abs(delta),
                        UnitPrice = balance?.AvgPrice ?? 0m,
                        Amount = Math.Abs(delta) * (balance?.AvgPrice ?? 0m)
                    }
                }
            };
            _db.StockIssues.Add(issue);
        }

        await _db.SaveChangesAsync(cancellationToken);
        await RecalculateStockAsync(cancellationToken);
        await _auditLogService.LogAsync("InventoryAdjustment", $"ItemId={itemId}; WarehouseId={whId}; Actual={actualQuantity}", "inventory", cancellationToken);
    }

    public async Task PrintStockCardAsync(string productId, string? warehouseId, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("InventoryPrintStockCard", $"Product={productId}; Warehouse={warehouseId}", "inventory", cancellationToken);
    }

    public async Task<string> ExportCsvAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var rows = await GetStockBalancesAsync(filter, cancellationToken);
        var root = ApplicationDataPaths.GetUserExportPath("inventory");
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, $"inventory_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("MaHang,TenHang,Nhom,Kho,DVT,GiaBinhQuan,TonDauKy,GiaTriTonDau,NhapTrongKy,GiaTriNhap,XuatTrongKy,GiaTriXuat,TonCuoiKy,GiaTriTonCuoi,TrangThai");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',',
                Csv(row.ProductCode),
                Csv(row.ProductName),
                Csv(row.ItemGroup),
                Csv(row.WarehouseCode),
                Csv(row.Unit),
                row.AverageCost.ToString("0.##", CultureInfo.InvariantCulture),
                row.OpeningQuantity.ToString("0.##", CultureInfo.InvariantCulture),
                row.OpeningValue.ToString("0.##", CultureInfo.InvariantCulture),
                row.TotalIn.ToString("0.##", CultureInfo.InvariantCulture),
                row.InValue.ToString("0.##", CultureInfo.InvariantCulture),
                row.TotalOut.ToString("0.##", CultureInfo.InvariantCulture),
                row.OutValue.ToString("0.##", CultureInfo.InvariantCulture),
                row.ClosingQuantity.ToString("0.##", CultureInfo.InvariantCulture),
                row.ClosingValue.ToString("0.##", CultureInfo.InvariantCulture),
                Csv(row.Status)));
        }

        await File.WriteAllTextAsync(file, sb.ToString(), new UTF8Encoding(true), cancellationToken);
        await _auditLogService.LogAsync("InventoryExport", $"Export file {file}", "inventory", cancellationToken);
        return file;
    }

    public async Task ExportStockReportAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        _ = await ExportCsvAsync(filter, cancellationToken);
    }

    public async Task ImportFromExcelAsync(CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("InventoryImportExcel", "ImportFromExcel đã được chuyển sang ImportEngine persisted ở UI; service cũ giữ để tương thích.", "inventory", cancellationToken);
    }

    private async Task<List<InventoryMovementDto>> LoadAllMovementsAsync(CancellationToken cancellationToken)
    {
        var items = await _db.Items.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);
        var warehouses = await _db.Warehouses.AsNoTracking().ToDictionaryAsync(x => x.Id, cancellationToken);

        var receipts = await _db.StockReceipts
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.PostingStatus != "Reversed")
            .ToListAsync(cancellationToken);

        var issues = await _db.StockIssues
            .AsNoTracking()
            .Include(x => x.Details)
            .Where(x => x.PostingStatus != "Reversed")
            .ToListAsync(cancellationToken);

        var issueCosts = await _db.InventoryCostTransactions
            .AsNoTracking()
            .Where(x => x.SourceType == "StockIssue" && x.SourceDetailId.HasValue)
            .GroupBy(x => x.SourceDetailId!.Value)
            .Select(x => new { SourceDetailId = x.Key, TotalCost = x.Sum(c => c.TotalCost), Quantity = x.Sum(c => c.Quantity) })
            .ToDictionaryAsync(
                x => x.SourceDetailId,
                x => x.Quantity == 0
                    ? (UnitCost: 0m, TotalCost: x.TotalCost)
                    : (UnitCost: x.TotalCost / x.Quantity, TotalCost: x.TotalCost),
                cancellationToken);

        var list = new List<InventoryMovementDto>();

        foreach (var r in receipts)
        {
            foreach (var d in r.Details)
            {
                if (!items.TryGetValue(d.ItemId, out var item)) continue;
                var whId = d.WarehouseId ?? r.WarehouseId;
                if (!warehouses.TryGetValue(whId, out var wh)) continue;

                list.Add(new InventoryMovementDto
                {
                    Id = $"R{r.Id:D6}-{d.Id:D4}",
                    ProductId = item.Id.ToString(CultureInfo.InvariantCulture),
                    ProductCode = item.ItemCode,
                    ProductName = item.ItemName,
                    ItemGroup = item.GroupName,
                    WarehouseId = wh.Id.ToString(CultureInfo.InvariantCulture),
                    WarehouseCode = wh.WarehouseCode,
                    WarehouseName = wh.WarehouseName,
                    DocumentNo = r.ReceiptNo,
                    DocumentType = r.ReceiptNo.StartsWith("ADJIN", StringComparison.OrdinalIgnoreCase) ? InventoryDocumentType.AdjustmentIn : InventoryDocumentType.ImportVoucher,
                    MovementDate = r.ReceiptDate,
                    QuantityIn = d.Quantity,
                    QuantityOut = 0,
                    Unit = string.IsNullOrWhiteSpace(d.Unit) ? item.Unit : d.Unit,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount,
                    Description = r.Description,
                    CreatedBy = "system",
                    Note = string.Empty
                });
            }
        }

        foreach (var i in issues)
        {
            foreach (var d in i.Details)
            {
                if (!items.TryGetValue(d.ItemId, out var item)) continue;
                var whId = d.WarehouseId ?? i.WarehouseId;
                if (!warehouses.TryGetValue(whId, out var wh)) continue;

                var docType = i.IssueNo.StartsWith("ADJOUT", StringComparison.OrdinalIgnoreCase)
                    ? InventoryDocumentType.AdjustmentOut
                    : i.ToWarehouseId.HasValue
                        ? InventoryDocumentType.TransferOut
                        : InventoryDocumentType.ExportVoucher;
                var detailCostKey = PostingSourceIds.FromInt32(d.Id);
                var hasPersistedCost = issueCosts.TryGetValue(detailCostKey, out var issueCost);
                var unitCost = hasPersistedCost ? issueCost.UnitCost : d.UnitPrice;
                var totalCost = hasPersistedCost ? issueCost.TotalCost : d.Amount;

                list.Add(new InventoryMovementDto
                {
                    Id = $"I{i.Id:D6}-{d.Id:D4}",
                    ProductId = item.Id.ToString(CultureInfo.InvariantCulture),
                    ProductCode = item.ItemCode,
                    ProductName = item.ItemName,
                    ItemGroup = item.GroupName,
                    WarehouseId = wh.Id.ToString(CultureInfo.InvariantCulture),
                    WarehouseCode = wh.WarehouseCode,
                    WarehouseName = wh.WarehouseName,
                    DocumentNo = i.IssueNo,
                    DocumentType = docType,
                    MovementDate = i.IssueDate,
                    QuantityIn = 0,
                    QuantityOut = d.Quantity,
                    Unit = string.IsNullOrWhiteSpace(d.Unit) ? item.Unit : d.Unit,
                    UnitPrice = unitCost,
                    Amount = totalCost,
                    Description = i.Description,
                    CreatedBy = "system",
                    Note = string.Empty
                });

                if (i.ToWarehouseId.HasValue && warehouses.TryGetValue(i.ToWarehouseId.Value, out var toWh))
                {
                    list.Add(new InventoryMovementDto
                    {
                        Id = $"T{i.Id:D6}-{d.Id:D4}",
                        ProductId = item.Id.ToString(CultureInfo.InvariantCulture),
                        ProductCode = item.ItemCode,
                        ProductName = item.ItemName,
                        ItemGroup = item.GroupName,
                        WarehouseId = toWh.Id.ToString(CultureInfo.InvariantCulture),
                        WarehouseCode = toWh.WarehouseCode,
                        WarehouseName = toWh.WarehouseName,
                        DocumentNo = i.IssueNo,
                        DocumentType = InventoryDocumentType.TransferIn,
                        MovementDate = i.IssueDate,
                        QuantityIn = d.Quantity,
                        QuantityOut = 0,
                        Unit = string.IsNullOrWhiteSpace(d.Unit) ? item.Unit : d.Unit,
                        UnitPrice = d.UnitPrice,
                        Amount = d.Amount,
                        Description = $"Chuyển kho đến từ {wh.WarehouseCode}",
                        CreatedBy = "system",
                        Note = string.Empty
                    });
                }
            }
        }

        foreach (var group in list.GroupBy(x => new { x.ProductId, x.WarehouseId }))
        {
            decimal running = 0;
            foreach (var row in group.OrderBy(x => x.MovementDate).ThenBy(x => x.Id))
            {
                running += row.QuantityIn - row.QuantityOut;
                row.BalanceQuantity = running;
            }
        }

        return list;
    }

    private static IEnumerable<InventoryMovementDto> ApplyFilter(IEnumerable<InventoryMovementDto> source, InventoryMovementFilterDto filter)
    {
        var query = source;
        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var keyword = filter.Keyword.Trim();
            query = query.Where(x => x.ProductCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.ProductName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || x.DocumentNo.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.WarehouseCode))
        {
            query = query.Where(x => x.WarehouseCode.Equals(filter.WarehouseCode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filter.ItemGroup))
        {
            query = query.Where(x => x.ItemGroup.Equals(filter.ItemGroup, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.FromDate.HasValue) query = query.Where(x => x.MovementDate.Date >= filter.FromDate.Value.Date);
        if (filter.ToDate.HasValue) query = query.Where(x => x.MovementDate.Date <= filter.ToDate.Value.Date);
        if (filter.DocumentType.HasValue) query = query.Where(x => x.DocumentType == filter.DocumentType.Value);
        return query;
    }

    private static string BuildStatus(decimal closing, decimal min, decimal totalIn, decimal totalOut)
    {
        if (closing < 0) return "Âm kho";
        if (min > 0 && closing < min) return "Tồn thấp";
        if (totalIn == 0 && totalOut == 0) return "Không phát sinh";
        return "Bình thường";
    }

    private static string Csv(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
