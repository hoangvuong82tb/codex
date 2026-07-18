namespace ASMILE.BO.Application.DTOs;

public sealed class MenuCategoryDto
{
    public Guid Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string? ParentCategoryName { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public sealed class CreateMenuCategoryRequest
{
    public Guid? Id { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class MenuItemDto
{
    public Guid Id { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public bool HasRecipeManagement { get; set; }
    public bool HasRecipe { get; set; }
    public bool IsCombo { get; set; }
    public bool IsHotelService { get; set; }
    public bool IsActive { get; set; }
    public bool HasAvailabilityWarning { get; set; }
    public bool IsOutOfStock { get; set; }
    public bool IsLowStock { get; set; }
    public string? StockWarning { get; set; }
    public string? Note { get; set; }
    public string? PreparationArea { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class MenuRecipeLineDto
{
    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public int? ItemId { get; set; }
    public Guid? ChildMenuItemId { get; set; }
    public int? DefaultWarehouseId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LossRate { get; set; }
    public bool IsRequired { get; set; }
    public decimal EstimatedUnitCost { get; set; }
    public decimal EstimatedLineCost { get; set; }
    public decimal AvailableQuantity { get; set; }
    public bool MissingCost { get; set; }
    public bool InsufficientStock { get; set; }
    public string? Warning { get; set; }
    public string? Note { get; set; }
}

public sealed class MenuRecipeDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public IReadOnlyList<MenuRecipeLineDto> Lines { get; set; } = Array.Empty<MenuRecipeLineDto>();
}

public sealed class SubstituteIngredientDto
{
    public int RowNo { get; set; }
    public Guid Id { get; set; }
    public Guid RecipeLineId { get; set; }
    public int InventoryItemId { get; set; }
    public string InventoryItemCode { get; set; } = string.Empty;
    public string InventoryItemName { get; set; } = string.Empty;
    public int SubstituteInventoryItemId { get; set; }
    public string SubstituteItemCode { get; set; } = string.Empty;
    public string SubstituteItemName { get; set; } = string.Empty;
    public string SubstituteUnitName { get; set; } = string.Empty;
    public decimal ConversionRate { get; set; }
    public int Priority { get; set; }
    public bool AutoReplace { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateSubstituteIngredientRequest
{
    public Guid RecipeLineId { get; set; }
    public int SubstituteInventoryItemId { get; set; }
    public decimal ConversionRate { get; set; } = 1m;
    public int Priority { get; set; } = 1;
    public bool AutoReplace { get; set; }
    public string? Notes { get; set; }
    public string? UserName { get; set; }
}

public sealed class UpdateSubstituteIngredientRequest
{
    public Guid Id { get; set; }
    public decimal ConversionRate { get; set; } = 1m;
    public int Priority { get; set; } = 1;
    public bool AutoReplace { get; set; }
    public string? Notes { get; set; }
    public string? UserName { get; set; }
}

public sealed class MenuCostPreviewLineDto
{
    public int LineNo { get; set; }
    public int? ItemId { get; set; }
    public Guid? ChildMenuItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LossRate { get; set; }
    public decimal EffectiveQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal LineCost { get; set; }
    public decimal AvailableQuantity { get; set; }
    public bool MissingCost { get; set; }
    public bool InsufficientStock { get; set; }
    public string? Warning { get; set; }
}

public sealed class MenuCostPreviewDto
{
    public Guid MenuItemId { get; set; }
    public string MenuItemCode { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public decimal SellingPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal MarginRate { get; set; }
    public IReadOnlyList<MenuCostPreviewLineDto> Lines { get; set; } = Array.Empty<MenuCostPreviewLineDto>();
    public IReadOnlyList<string> Warnings { get; set; } = Array.Empty<string>();
}

public sealed class IngredientLookupDto
{
    public int ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal ReferenceUnitCost { get; set; }
    public decimal AvailableQuantity { get; set; }
}

public sealed class WarehouseLookupDto
{
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public bool IsConsumptionWarehouse { get; set; }
}

public sealed class IngredientsLookupDto
{
    public IReadOnlyList<IngredientLookupDto> Items { get; set; } = Array.Empty<IngredientLookupDto>();
    public IReadOnlyList<WarehouseLookupDto> Warehouses { get; set; } = Array.Empty<WarehouseLookupDto>();
}

public sealed class CreateMenuItemRequest
{
    public Guid? Id { get; set; }
    public Guid MenuCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public bool HasRecipeManagement { get; set; } = true;
    public bool IsCombo { get; set; }
    public bool IsHotelService { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public int? ItemId { get; set; }
    public string? Note { get; set; }
    public string? PreparationArea { get; set; }
    public string? ImagePath { get; set; }
}

public sealed class SaveMenuRecipeRequest
{
    public Guid? RecipeId { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = "Default";
    public bool IsDefault { get; set; } = true;
    public IReadOnlyList<MenuRecipeLineDto> Lines { get; set; } = Array.Empty<MenuRecipeLineDto>();
}

public sealed class BreakfastCafeOrderLineDto
{
    public Guid Id { get; set; }
    public int LineNo { get; set; }
    public Guid MenuItemId { get; set; }
    public string MenuItemCode { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineDiscountAmount { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
    public decimal LineTotalAmount { get; set; }
    public string KitchenStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
}

public sealed class BreakfastCafePaymentDto
{
    public Guid Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNo { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class BreakfastCafeOrderDto
{
    public Guid Id { get; set; }
    public Guid? PosTableId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string ServiceStatus { get; set; } = string.Empty;
    public string ServiceStatusText { get; set; } = string.Empty;
    public string? AreaName { get; set; }
    public string? TableCode { get; set; }
    public string? TableName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerReference { get; set; }
    public string? RoomReference { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? KitchenSentAt { get; set; }
    public int KitchenTicketCount { get; set; }
    public int KitchenHistoryCount { get; set; }
    public string? Note { get; set; }
    public int LineCount { get; set; }
    public IReadOnlyList<BreakfastCafeOrderLineDto> Lines { get; set; } = Array.Empty<BreakfastCafeOrderLineDto>();
    public IReadOnlyList<BreakfastCafePaymentDto> Payments { get; set; } = Array.Empty<BreakfastCafePaymentDto>();
}

public sealed class PosTableDto
{
    public Guid Id { get; set; }
    public string AreaCode { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public int SortOrder { get; set; }
    public Guid? OpenOrderId { get; set; }
    public string? OpenOrderNumber { get; set; }
    public decimal OpenOrderTotal { get; set; }
    public DateTime? OpenedAt { get; set; }
}

public sealed class PosKitchenTicketDto
{
    public Guid Id { get; set; }
    public Guid BreakfastCafeOrderId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Station { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

public sealed class BreakfastCafeSalesSummaryDto
{
    public DateTime SummaryDate { get; set; }
    public decimal RevenueAmount { get; set; }
    public decimal VatOutputAmount { get; set; }
    public decimal EstimatedCostAmount { get; set; }
    public int OrderCount { get; set; }
    public IReadOnlyList<BreakfastCafeTopItemDto> TopSellingItems { get; set; } = Array.Empty<BreakfastCafeTopItemDto>();
    public IReadOnlyList<BreakfastCafePaymentSummaryDto> PaymentSummaries { get; set; } = Array.Empty<BreakfastCafePaymentSummaryDto>();
}

public sealed class BreakfastCafeTopItemDto
{
    public string MenuItemCode { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal RevenueAmount { get; set; }
}

public sealed class BreakfastCafePaymentSummaryDto
{
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed class CreateBreakfastCafeOrderRequest
{
    public Guid? PosTableId { get; set; }
    public DateTime? OrderDate { get; set; }
    public string? OrderType { get; set; }
    public string? AreaName { get; set; }
    public string? TableCode { get; set; }
    public string? TableName { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerReference { get; set; }
    public string? RoomReference { get; set; }
    public string? Note { get; set; }
    public string? CreatedBy { get; set; }
}

public sealed class UpdateBreakfastCafeOrderLineRequest
{
    public Guid OrderId { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid MenuItemId { get; set; }
    public decimal Quantity { get; set; }
    public string? Note { get; set; }
}

public sealed class SendBreakfastCafeKitchenRequest
{
    public Guid OrderId { get; set; }
    public string? Station { get; set; }
    public string? SentBy { get; set; }
    public string? Note { get; set; }
}

public sealed class PayBreakfastCafeOrderRequest
{
    public Guid OrderId { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid? CashFundId { get; set; }
    public Guid? BankAccountId { get; set; }
    public int? CustomerId { get; set; }
    public string? RoomReference { get; set; }
    public string? ReferenceNo { get; set; }
    public string? CreatedBy { get; set; }
}
