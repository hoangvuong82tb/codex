namespace ASMILE.BO.Application.Interfaces;

public interface ITableAllocationEngine
{
    IReadOnlyList<TableAllocationResult> Allocate(int guestCount, int tableStandard);
}

public interface IMenuCalculationEngine
{
    decimal Calculate(string? pricingRule, int guestCount, int realTableCount, decimal defaultQuantity, decimal? manualQuantity = null);
}

public interface IRoomOrderMenuPlanningEngine
{
    IReadOnlyList<RoomOrderMenuPlanLine> BuildPlan(RoomOrderMenuPlanningRequest request);
}

public sealed class TableAllocationResult
{
    public int TableNo { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int GuestCount { get; init; }
    public int SortOrder { get; init; }
    public bool IsVirtual { get; init; }
    public string? Note { get; init; }
}

public sealed class RoomOrderMenuPlanningRequest
{
    public IReadOnlyList<RoomOrderMenuTableInput> Tables { get; init; } = Array.Empty<RoomOrderMenuTableInput>();
    public string ApplyScope { get; init; } = "AllServing";
    public string ServingRule { get; init; } = "PerGuest";
    public int TableStandard { get; init; } = 10;
    public decimal BaseQuantity { get; init; } = 1m;
    public decimal ExtraUnitQuantity { get; init; } = 1m;
    public decimal ManualQuantity { get; init; } = 1m;
    public Guid? SelectedTableId { get; init; }
}

public sealed class RoomOrderMenuTableInput
{
    public Guid TableId { get; init; }
    public string TableName { get; init; } = string.Empty;
    public int GuestCount { get; init; }
    public string Status { get; init; } = string.Empty;
}

public sealed class RoomOrderMenuPlanLine
{
    public Guid TableId { get; init; }
    public string TableName { get; init; } = string.Empty;
    public int GuestCount { get; init; }
    public decimal SaveQuantity { get; init; }
    public decimal DisplayQuantity { get; init; }
    public decimal ExtraQuantity { get; init; }
    public string DisplayRuleText { get; init; } = string.Empty;
    public string? KitchenNote { get; init; }
}
