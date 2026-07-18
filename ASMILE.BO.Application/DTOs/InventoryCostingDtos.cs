namespace ASMILE.BO.Application.DTOs;

public sealed class InventoryCostResult
{
    public bool Success { get; set; }
    public decimal TotalCost { get; set; }
    public List<InventoryCostLineResult> Lines { get; set; } = [];
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public static InventoryCostResult Failed(params string[] errors)
        => new() { Success = false, Errors = errors.ToList() };

    public static InventoryCostResult Succeeded(IEnumerable<InventoryCostLineResult> lines, IEnumerable<string>? warnings = null)
    {
        var resultLines = lines.ToList();
        return new InventoryCostResult
        {
            Success = true,
            Lines = resultLines,
            TotalCost = resultLines.Sum(x => x.TotalCost),
            Warnings = warnings?.ToList() ?? []
        };
    }
}

public sealed class InventoryCostLineResult
{
    public int ItemId { get; set; }
    public int WarehouseId { get; set; }
    public Guid? SourceDetailId { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string CostMethod { get; set; } = "MovingAverage";
}
