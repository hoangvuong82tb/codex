using System.Text.Json;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class ItemImportTemplateHandler : IImportTemplateHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] CodeColumns = ["Mã hàng", "Ma hang", "ItemCode", "Item Code", "Code"];
    private static readonly string[] NameColumns = ["Tên hàng", "Ten hang", "ItemName", "Item Name", "Name"];
    private static readonly string[] UnitColumns = ["Đơn vị tính", "Don vi tinh", "ĐVT", "DVT", "Unit"];
    private static readonly string[] GroupColumns = ["Nhóm hàng", "Nhom hang", "GroupName", "Group"];
    private static readonly string[] SupplierCodeColumns = ["Mã nhà cung cấp", "Ma nha cung cap", "Mã NCC", "Ma NCC", "SupplierCode", "Supplier Code"];
    private static readonly string[] SupplierNameColumns = ["Tên nhà cung cấp", "Ten nha cung cap", "Tên NCC", "Ten NCC", "SupplierName", "Supplier Name"];

    private readonly AppDbContext _dbContext;

    public ItemImportTemplateHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string TargetEntity => "Items";
    public IReadOnlyList<string> RequiredColumns { get; } = ["Mã hàng", "Tên hàng", "Đơn vị tính"];
    public IReadOnlyList<string> KeyColumns { get; } = ["Mã hàng"];

    public async Task ValidateRowAsync(ImportRow row, IReadOnlyDictionary<string, string> values, IReadOnlyList<ImportRow> allRows, CancellationToken cancellationToken = default)
    {
        var itemCode = GetValue(values, CodeColumns);
        var itemName = GetValue(values, NameColumns);
        var unit = GetValue(values, UnitColumns);

        AddRequired(row, "Mã hàng", itemCode, "ITEM_CODE_REQUIRED");
        AddRequired(row, "Tên hàng", itemName, "ITEM_NAME_REQUIRED");
        AddRequired(row, "Đơn vị tính", unit, "ITEM_UNIT_REQUIRED");

        if (!string.IsNullOrWhiteSpace(itemCode))
        {
            var duplicateInFile = allRows
                .Where(x => x.Id != row.Id)
                .Select(x => ParseValues(x.RawJson))
                .Count(x => string.Equals(GetValue(x, CodeColumns), itemCode, StringComparison.OrdinalIgnoreCase));

            if (duplicateInFile > 0)
            {
                AddError(row, "Mã hàng", "DUPLICATE_IN_FILE", $"Mã hàng {itemCode} bị trùng trong file.");
            }

            var existsInDb = await _dbContext.Items.AsNoTracking().AnyAsync(x => x.ItemCode == itemCode, cancellationToken);
            if (existsInDb)
            {
                AddError(row, "Mã hàng", "DUPLICATE_IN_DATABASE", $"Mã hàng {itemCode} đã tồn tại trong dữ liệu.");
            }
        }

        if (!string.IsNullOrWhiteSpace(itemName))
        {
            var existsName = await _dbContext.Items.AsNoTracking().AnyAsync(x => x.ItemName == itemName, cancellationToken);
            if (existsName)
            {
                AddWarning(row, "Tên hàng", "ITEM_NAME_EXISTS", $"Tên hàng {itemName} đã tồn tại, cần kiểm tra trước khi nhập.");
            }
        }

        row.NormalizedJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["ItemCode"] = itemCode,
            ["ItemName"] = itemName,
            ["Unit"] = unit,
            ["GroupName"] = GetValue(values, GroupColumns),
            ["SupplierCode"] = GetValue(values, SupplierCodeColumns),
            ["SupplierName"] = GetValue(values, SupplierNameColumns)
        }, JsonOptions);
    }

    public async Task<int> CommitAsync(ImportSession session, IReadOnlyList<ImportRow> rows, CancellationToken cancellationToken = default)
    {
        var committed = 0;
        foreach (var row in rows.Where(x => x.Status == "Valid"))
        {
            var values = ParseValues(row.NormalizedJson ?? row.RawJson);
            var code = values.GetValueOrDefault("ItemCode", string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var exists = await _dbContext.Items.AnyAsync(x => x.ItemCode == code, cancellationToken);
            if (exists)
            {
                continue;
            }

            _dbContext.Items.Add(new Item
            {
                ItemCode = code,
                ItemName = values.GetValueOrDefault("ItemName", string.Empty).Trim(),
                Unit = values.GetValueOrDefault("Unit", string.Empty).Trim(),
                GroupName = values.GetValueOrDefault("GroupName", string.Empty).Trim(),
                SupplierCode = values.GetValueOrDefault("SupplierCode", string.Empty).Trim(),
                SupplierName = values.GetValueOrDefault("SupplierName", string.Empty).Trim(),
                IsActive = true
            });
            row.Status = "Committed";
            committed++;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return committed;
    }

    internal static Dictionary<string, string> ParseValues(string json)
        => string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static string GetValue(IReadOnlyDictionary<string, string> values, IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            var match = values.FirstOrDefault(x => string.Equals(x.Key, alias, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.Key))
            {
                return match.Value?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static void AddRequired(ImportRow row, string columnName, string value, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(row, columnName, errorCode, $"{columnName} là bắt buộc.");
        }
    }

    private static void AddError(ImportRow row, string columnName, string errorCode, string message)
        => AddCellIssue(row, columnName, "Error", errorCode, message);

    private static void AddWarning(ImportRow row, string columnName, string errorCode, string message)
        => AddCellIssue(row, columnName, "Warning", errorCode, message);

    private static void AddCellIssue(ImportRow row, string columnName, string severity, string errorCode, string message)
    {
        row.CellErrors.Add(new ImportCellError
        {
            Id = Guid.NewGuid(),
            ColumnName = columnName,
            CellAddress = string.Empty,
            Severity = severity,
            ErrorCode = errorCode,
            Message = message
        });
    }
}
