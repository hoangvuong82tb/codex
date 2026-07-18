using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Interfaces.Services;
using System.Globalization;
using System.Text;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class MockInventoryMovementService : IInventoryMovementService
{
    private readonly IAuditLogService _auditLogService;
    private readonly List<InventoryMovementDto> _movements;
    private readonly Dictionary<string, decimal> _minThresholdByProduct = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _supplierByProduct = new(StringComparer.OrdinalIgnoreCase);

    public MockInventoryMovementService(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
        _movements = BuildSeedMovements();
        InitializeMeta();
    }

    public async Task<InventoryDashboardDto> GetDashboardAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var balances = await GetStockBalancesAsync(filter, cancellationToken);
        var movements = ApplyFilter(_movements, filter).ToList();
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
            TotalItems = balances.Select(x => x.ProductId).Distinct().Count(),
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

    public Task<IReadOnlyList<InventoryStockBalanceDto>> GetStockBalancesAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_movements, filter);
        var balances = query.GroupBy(x => new { x.ProductId, x.ProductCode, x.ProductName, x.ItemGroup, x.WarehouseId, x.WarehouseCode, x.WarehouseName, x.Unit })
            .Select(g =>
            {
                var rows = g.OrderBy(x => x.MovementDate).ThenBy(x => x.Id).ToList();
                var opening = rows.FirstOrDefault()?.BalanceQuantity - rows.FirstOrDefault()?.QuantityIn + rows.FirstOrDefault()?.QuantityOut ?? 0;
                var totalIn = rows.Sum(x => x.QuantityIn);
                var totalOut = rows.Sum(x => x.QuantityOut);
                var closing = opening + totalIn - totalOut;
                var avg = rows.Any() ? rows.Average(x => x.UnitPrice) : 0;
                var min = _minThresholdByProduct.TryGetValue(g.Key.ProductId, out var minValue) ? minValue : 10;
                return new InventoryStockBalanceDto
                {
                    ProductId = g.Key.ProductId,
                    ProductCode = g.Key.ProductCode,
                    ProductName = g.Key.ProductName,
                    ItemGroup = g.Key.ItemGroup,
                    WarehouseId = g.Key.WarehouseId,
                    WarehouseCode = g.Key.WarehouseCode,
                    WarehouseName = g.Key.WarehouseName,
                    OpeningQuantity = opening,
                    TotalIn = totalIn,
                    TotalOut = totalOut,
                    ClosingQuantity = closing,
                    MinStockThreshold = min,
                    Unit = g.Key.Unit,
                    AverageCost = avg,
                    StockValue = closing * avg,
                    LastMovementDate = rows.Max(x => x.MovementDate),
                    Status = BuildStatus(closing, min, totalIn, totalOut)
                };
            })
            .Where(x => !filter.LowStockOnly || x.ClosingQuantity < x.MinStockThreshold)
            .Where(x => !filter.NegativeStockOnly || x.ClosingQuantity < 0)
            .OrderBy(x => x.ProductCode)
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryStockBalanceDto>>(balances);
    }

    public Task<IReadOnlyList<InventoryMovementDto>> GetMovementsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilter(_movements, filter).OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.Id).ToList();
        return Task.FromResult<IReadOnlyList<InventoryMovementDto>>(query);
    }

    public Task<IReadOnlyList<InventoryMovementDto>> GetMovementsByProductAsync(string productId, string? warehouseId, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var query = _movements.Where(x => x.ProductId == productId);
        if (!string.IsNullOrWhiteSpace(warehouseId)) query = query.Where(x => x.WarehouseId == warehouseId);
        if (fromDate.HasValue) query = query.Where(x => x.MovementDate.Date >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.MovementDate.Date <= toDate.Value.Date);
        return Task.FromResult<IReadOnlyList<InventoryMovementDto>>(query.OrderByDescending(x => x.MovementDate).ThenByDescending(x => x.Id).ToList());
    }

    public Task<IReadOnlyList<InventoryDocumentDto>> GetDocumentsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var docs = ApplyFilter(_movements, filter)
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
                    TotalItems = g.Select(x => x.ProductId).Distinct().Count(),
                    TotalQuantity = g.Sum(x => x.QuantityIn + x.QuantityOut),
                    TotalAmount = g.Sum(x => x.Amount),
                    Status = "Đã duyệt"
                };
            })
            .OrderByDescending(x => x.DocumentDate)
            .ToList();

        return Task.FromResult<IReadOnlyList<InventoryDocumentDto>>(docs);
    }

    public Task<IReadOnlyList<InventoryMovementDto>> GetDocumentLinesAsync(string documentNo, CancellationToken cancellationToken = default)
    {
        var rows = _movements.Where(x => x.DocumentNo.Equals(documentNo, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.ProductCode)
            .ToList();
        return Task.FromResult<IReadOnlyList<InventoryMovementDto>>(rows);
    }

    public async Task<IReadOnlyList<InventoryLowStockAlertDto>> GetLowStockAlertsAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var balances = await GetStockBalancesAsync(filter, cancellationToken);
        var alerts = balances
            .Where(x => x.ClosingQuantity < x.MinStockThreshold || x.ClosingQuantity < 0)
            .Select(x =>
            {
                var missing = x.ClosingQuantity < x.MinStockThreshold ? x.MinStockThreshold - x.ClosingQuantity : 0;
                var supplier = _supplierByProduct.TryGetValue(x.ProductId, out var name) ? name : "NCC chưa xác định";
                return new InventoryLowStockAlertDto
                {
                    ProductId = x.ProductId,
                    ProductCode = x.ProductCode,
                    ProductName = x.ProductName,
                    WarehouseId = x.WarehouseId,
                    WarehouseCode = x.WarehouseCode,
                    CurrentStock = x.ClosingQuantity,
                    MinStockThreshold = x.MinStockThreshold,
                    MissingQuantity = Math.Max(0, missing),
                    SuggestedSupplier = supplier,
                    SuggestedPurchaseQty = Math.Max(0, missing * 1.2m),
                    Severity = x.ClosingQuantity < 0 ? "Critical" : "Warning"
                };
            })
            .OrderByDescending(x => x.Severity)
            .ThenBy(x => x.CurrentStock)
            .ToList();

        return alerts;
    }

    public async Task RecalculateStockAsync(CancellationToken cancellationToken = default)
    {
        foreach (var group in _movements.GroupBy(x => new { x.ProductId, x.WarehouseId }))
        {
            decimal balance = 0;
            foreach (var row in group.OrderBy(x => x.MovementDate).ThenBy(x => x.Id))
            {
                balance += row.QuantityIn - row.QuantityOut;
                row.BalanceQuantity = balance;
            }
        }

        await _auditLogService.LogAsync("InventoryRecalculate", "Inventory.Movement - Recalculate stock balances", "inventory", cancellationToken);
    }

    public async Task CreateAdjustmentAsync(string productId, string warehouseId, decimal actualQuantity, string reason, CancellationToken cancellationToken = default)
    {
        var scoped = _movements
            .Where(x => x.ProductId == productId && x.WarehouseId == warehouseId)
            .OrderBy(x => x.MovementDate)
            .ThenBy(x => x.Id)
            .ToList();

        var current = scoped.LastOrDefault()?.BalanceQuantity ?? 0;
        var delta = actualQuantity - current;
        if (delta == 0)
        {
            return;
        }

        var sample = scoped.LastOrDefault() ?? _movements.First(x => x.ProductId == productId);
        _movements.Add(new InventoryMovementDto
        {
            Id = Guid.NewGuid().ToString("N"),
            ProductId = sample.ProductId,
            ProductCode = sample.ProductCode,
            ProductName = sample.ProductName,
            ItemGroup = sample.ItemGroup,
            WarehouseId = warehouseId,
            WarehouseCode = sample.WarehouseCode,
            WarehouseName = sample.WarehouseName,
            DocumentNo = $"ADJ{DateTime.Now:yyMMddHHmmss}",
            DocumentType = delta > 0 ? InventoryDocumentType.AdjustmentIn : InventoryDocumentType.AdjustmentOut,
            MovementDate = DateTime.Now,
            QuantityIn = delta > 0 ? delta : 0,
            QuantityOut = delta < 0 ? Math.Abs(delta) : 0,
            Unit = sample.Unit,
            UnitPrice = sample.UnitPrice,
            Amount = Math.Abs(delta) * sample.UnitPrice,
            BalanceQuantity = actualQuantity,
            Description = string.IsNullOrWhiteSpace(reason) ? "Điều chỉnh tồn kho" : reason,
            Note = "Phiếu điều chỉnh từ Inventory Command Center",
            CreatedBy = "administrator"
        });

        await RecalculateStockAsync(cancellationToken);
        await _auditLogService.LogAsync("InventoryAdjustment", $"Product={sample.ProductCode}; Warehouse={sample.WarehouseCode}; Actual={actualQuantity}", "inventory", cancellationToken);
    }

    public async Task PrintStockCardAsync(string productId, string? warehouseId, CancellationToken cancellationToken = default)
    {
        await _auditLogService.LogAsync("InventoryPrintStockCard", $"Product={productId}; Warehouse={warehouseId}", "inventory", cancellationToken);
    }

    public async Task<string> ExportCsvAsync(InventoryMovementFilterDto filter, CancellationToken cancellationToken = default)
    {
        var rows = await GetStockBalancesAsync(filter, cancellationToken);
        var root = Path.Combine(AppContext.BaseDirectory, "exports", "inventory");
        Directory.CreateDirectory(root);
        var file = Path.Combine(root, $"inventory_report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.AppendLine("MaHang,TenHang,Nhom,Kho,DVT,DauKy,Nhap,Xuat,TonCuoi,GiaVonTB,GiaTriTon,TrangThai");
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(',',
                Csv(row.ProductCode),
                Csv(row.ProductName),
                Csv(row.ItemGroup),
                Csv(row.WarehouseCode),
                Csv(row.Unit),
                row.OpeningQuantity.ToString("0.##", CultureInfo.InvariantCulture),
                row.TotalIn.ToString("0.##", CultureInfo.InvariantCulture),
                row.TotalOut.ToString("0.##", CultureInfo.InvariantCulture),
                row.ClosingQuantity.ToString("0.##", CultureInfo.InvariantCulture),
                row.AverageCost.ToString("0.##", CultureInfo.InvariantCulture),
                row.StockValue.ToString("0.##", CultureInfo.InvariantCulture),
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
        var random = new Random();
        var pick = _movements[random.Next(_movements.Count)];
        var qty = random.Next(5, 20);
        _movements.Add(new InventoryMovementDto
        {
            Id = Guid.NewGuid().ToString("N"),
            ProductId = pick.ProductId,
            ProductCode = pick.ProductCode,
            ProductName = pick.ProductName,
            ItemGroup = pick.ItemGroup,
            WarehouseId = pick.WarehouseId,
            WarehouseCode = pick.WarehouseCode,
            WarehouseName = pick.WarehouseName,
            DocumentNo = $"IMPXL{DateTime.Now:yyMMddHHmmss}",
            DocumentType = InventoryDocumentType.ImportVoucher,
            MovementDate = DateTime.Now,
            QuantityIn = qty,
            QuantityOut = 0,
            Unit = pick.Unit,
            UnitPrice = pick.UnitPrice,
            Amount = qty * pick.UnitPrice,
            BalanceQuantity = pick.BalanceQuantity + qty,
            Description = "Nhập dữ liệu mô phỏng từ Excel",
            CreatedBy = "administrator",
            Note = "Import mock"
        });
        await RecalculateStockAsync(cancellationToken);
        await _auditLogService.LogAsync("InventoryImportExcel", "Import mock from excel", "inventory", cancellationToken);
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
        if (closing < 0) return "?m kho";
        if (closing < min) return "Tồn thấp";
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

    private void InitializeMeta()
    {
        var productIds = _movements.Select(x => x.ProductId).Distinct().OrderBy(x => x).ToList();
        var suppliers = new[] { "Công ty Minh Phát", "NCC An Khang", "NCC Đại Phúc", "Công ty Ánh Sao" };
        var index = 0;
        foreach (var id in productIds)
        {
            _minThresholdByProduct[id] = 10 + (index % 5) * 5;
            _supplierByProduct[id] = suppliers[index % suppliers.Length];
            index++;
        }
    }

    private static List<InventoryMovementDto> BuildSeedMovements()
    {
        var warehouses = new[]
        {
            new { Id = "w1", Code = "W1", Name = "Kho Trung tâm" },
            new { Id = "w2", Code = "W2", Name = "Kho Buồng phòng" },
            new { Id = "w3", Code = "W3", Name = "Kho Online" },
            new { Id = "w4", Code = "W4", Name = "Kho Dự phòng" }
        };

        var products = new[]
        {
            new { Id = "p01", Code = "HH-001", Name = "Nước lau sàn", Group = "Hóa phẩm", Unit = "Can", Price = 185000m },
            new { Id = "p02", Code = "HH-002", Name = "Khăn tắm cao cấp", Group = "Linen", Unit = "Cái", Price = 120000m },
            new { Id = "p03", Code = "HH-003", Name = "Ga giường Queen", Group = "Linen", Unit = "Bộ", Price = 420000m },
            new { Id = "p04", Code = "HH-004", Name = "Bàn chải đánh răng", Group = "Amenities", Unit = "Hộp", Price = 65000m },
            new { Id = "p05", Code = "HH-005", Name = "Dầu gội chai 30ml", Group = "Amenities", Unit = "Thùng", Price = 980000m },
            new { Id = "p06", Code = "HH-006", Name = "Sữa tắm chai 30ml", Group = "Amenities", Unit = "Thùng", Price = 940000m },
            new { Id = "p07", Code = "HH-007", Name = "Giấy vệ sinh cuộn", Group = "Vật tư tiêu hao", Unit = "Bao", Price = 210000m },
            new { Id = "p08", Code = "HH-008", Name = "Nước rửa tay", Group = "Hóa phẩm", Unit = "Can", Price = 160000m },
            new { Id = "p09", Code = "HH-009", Name = "Ly thủy tinh 300ml", Group = "Dụng cụ", Unit = "Thùng", Price = 510000m },
            new { Id = "p10", Code = "HH-010", Name = "Bóng đèn LED", Group = "Kỹ thuật", Unit = "Hộp", Price = 320000m },
            new { Id = "p11", Code = "HH-011", Name = "Đèn ngủ mini", Group = "Kỹ thuật", Unit = "Cái", Price = 245000m },
            new { Id = "p12", Code = "HH-012", Name = "Nệm topper", Group = "Linen", Unit = "Cái", Price = 680000m },
            new { Id = "p13", Code = "HH-013", Name = "Túi rác 45L", Group = "Vật tư tiêu hao", Unit = "Cuộn", Price = 43000m },
            new { Id = "p14", Code = "HH-014", Name = "Nước giặt công nghiệp", Group = "Hóa phẩm", Unit = "Can", Price = 355000m },
            new { Id = "p15", Code = "HH-015", Name = "Máy sấy tóc mini", Group = "Thiết bị", Unit = "Cái", Price = 540000m },
            new { Id = "p16", Code = "HH-016", Name = "Tách trà sứ", Group = "Dụng cụ", Unit = "Hộp", Price = 280000m },
            new { Id = "p17", Code = "HH-017", Name = "Khóa từ cửa", Group = "Kỹ thuật", Unit = "Bộ", Price = 1350000m },
            new { Id = "p18", Code = "HH-018", Name = "Nước xịt khử mùi", Group = "Hóa phẩm", Unit = "Can", Price = 230000m },
            new { Id = "p19", Code = "HH-019", Name = "Áo choàng tắm", Group = "Linen", Unit = "Cái", Price = 365000m },
            new { Id = "p20", Code = "HH-020", Name = "Gối đầu tiêu chuẩn", Group = "Linen", Unit = "Cái", Price = 210000m }
        };

        var list = new List<InventoryMovementDto>();
        var now = DateTime.Today;
        var id = 1;
        var random = new Random(2026);

        foreach (var p in products)
        {
            var wh = warehouses[id % warehouses.Length];
            var opening = random.Next(8, 120);
            list.Add(new InventoryMovementDto
            {
                Id = $"m{id++:000}",
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                ItemGroup = p.Group,
                WarehouseId = wh.Id,
                WarehouseCode = wh.Code,
                WarehouseName = wh.Name,
                DocumentNo = $"OPEN-{p.Code}",
                DocumentType = InventoryDocumentType.OpeningStock,
                MovementDate = now.AddDays(-45),
                QuantityIn = opening,
                QuantityOut = 0,
                Unit = p.Unit,
                UnitPrice = p.Price,
                Amount = opening * p.Price,
                BalanceQuantity = opening,
                Description = "Tồn đầu kỳ",
                CreatedBy = "system"
            });

            var import1 = random.Next(10, 60);
            list.Add(new InventoryMovementDto
            {
                Id = $"m{id++:000}",
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                ItemGroup = p.Group,
                WarehouseId = wh.Id,
                WarehouseCode = wh.Code,
                WarehouseName = wh.Name,
                DocumentNo = $"PN{260000 + id}",
                DocumentType = InventoryDocumentType.ImportVoucher,
                MovementDate = now.AddDays(-28 + random.Next(0, 5)),
                QuantityIn = import1,
                QuantityOut = 0,
                Unit = p.Unit,
                UnitPrice = p.Price * (1m + (decimal)random.NextDouble() * 0.05m),
                Amount = import1 * p.Price,
                BalanceQuantity = opening + import1,
                Description = "Nhập bổ sung từ nhà cung cấp",
                CreatedBy = "kho.user"
            });

            var export1 = random.Next(5, 70);
            list.Add(new InventoryMovementDto
            {
                Id = $"m{id++:000}",
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                ItemGroup = p.Group,
                WarehouseId = wh.Id,
                WarehouseCode = wh.Code,
                WarehouseName = wh.Name,
                DocumentNo = $"PX{260000 + id}",
                DocumentType = InventoryDocumentType.ExportVoucher,
                MovementDate = now.AddDays(-15 + random.Next(0, 4)),
                QuantityIn = 0,
                QuantityOut = export1,
                Unit = p.Unit,
                UnitPrice = p.Price * (1m + (decimal)random.NextDouble() * 0.08m),
                Amount = export1 * p.Price,
                BalanceQuantity = opening + import1 - export1,
                Description = "Xuất cấp bộ phận sử dụng",
                CreatedBy = "kho.user"
            });
        }

        // add transfer and stocktake samples
        for (var i = 0; i < 12; i++)
        {
            var p = products[i % products.Length];
            var fromWh = warehouses[i % warehouses.Length];
            var toWh = warehouses[(i + 1) % warehouses.Length];
            var qty = 3 + i;
            list.Add(new InventoryMovementDto
            {
                Id = $"m{id++:000}",
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                ItemGroup = p.Group,
                WarehouseId = fromWh.Id,
                WarehouseCode = fromWh.Code,
                WarehouseName = fromWh.Name,
                DocumentNo = $"TR{260000 + id}",
                DocumentType = InventoryDocumentType.TransferOut,
                MovementDate = now.AddDays(-8 + (i % 4)),
                QuantityIn = 0,
                QuantityOut = qty,
                Unit = p.Unit,
                UnitPrice = p.Price,
                Amount = qty * p.Price,
                BalanceQuantity = 0,
                Description = "Chuyển kho đi",
                CreatedBy = "admin"
            });

            list.Add(new InventoryMovementDto
            {
                Id = $"m{id++:000}",
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                ItemGroup = p.Group,
                WarehouseId = toWh.Id,
                WarehouseCode = toWh.Code,
                WarehouseName = toWh.Name,
                DocumentNo = $"TR{260000 + id - 1}",
                DocumentType = InventoryDocumentType.TransferIn,
                MovementDate = now.AddDays(-8 + (i % 4)),
                QuantityIn = qty,
                QuantityOut = 0,
                Unit = p.Unit,
                UnitPrice = p.Price,
                Amount = qty * p.Price,
                BalanceQuantity = 0,
                Description = "Chuyển kho đến",
                CreatedBy = "admin"
            });
        }

        // force some low/negative stocks by extra export
        var riskProducts = products.Take(4).ToArray();
        foreach (var p in riskProducts)
        {
            var wh = warehouses[0];
            var qtyOut = 150;
            list.Add(new InventoryMovementDto
            {
                Id = $"m{id++:000}",
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                ItemGroup = p.Group,
                WarehouseId = wh.Id,
                WarehouseCode = wh.Code,
                WarehouseName = wh.Name,
                DocumentNo = $"PXR{260000 + id}",
                DocumentType = InventoryDocumentType.ExportVoucher,
                MovementDate = now.AddDays(-2),
                QuantityIn = 0,
                QuantityOut = qtyOut,
                Unit = p.Unit,
                UnitPrice = p.Price,
                Amount = qtyOut * p.Price,
                BalanceQuantity = 0,
                Description = "Xuất đột biến",
                CreatedBy = "kho.manager"
            });
        }

        // stocktake adjustments samples
        for (var i = 0; i < 6; i++)
        {
            var p = products[(i + 5) % products.Length];
            var wh = warehouses[(i + 2) % warehouses.Length];
            var qty = 2 + i;
            list.Add(new InventoryMovementDto
            {
                Id = $"m{id++:000}",
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                ItemGroup = p.Group,
                WarehouseId = wh.Id,
                WarehouseCode = wh.Code,
                WarehouseName = wh.Name,
                DocumentNo = $"KK{260000 + id}",
                DocumentType = i % 2 == 0 ? InventoryDocumentType.AdjustmentIn : InventoryDocumentType.AdjustmentOut,
                MovementDate = now.AddDays(-1),
                QuantityIn = i % 2 == 0 ? qty : 0,
                QuantityOut = i % 2 != 0 ? qty : 0,
                Unit = p.Unit,
                UnitPrice = p.Price,
                Amount = qty * p.Price,
                BalanceQuantity = 0,
                Description = "Điều chỉnh kiểm kê",
                CreatedBy = "audit.user"
            });
        }

        // recalc balances
        foreach (var group in list.GroupBy(x => new { x.ProductId, x.WarehouseId }))
        {
            decimal bal = 0;
            foreach (var row in group.OrderBy(x => x.MovementDate).ThenBy(x => x.Id))
            {
                bal += row.QuantityIn - row.QuantityOut;
                row.BalanceQuantity = bal;
            }
        }

        return list;
    }
}
