namespace ASMILE.BO.Core.Entities;

public sealed class BreakfastCafeOrder : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? PosTableId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string PaymentStatus { get; set; } = "Unpaid";
    public string OrderType { get; set; } = "DineIn";
    public string ServiceStatus { get; set; } = "New";
    public string? AreaName { get; set; }
    public string? TableCode { get; set; }
    public string? TableName { get; set; }
    public string? CustomerReference { get; set; }
    public int? CustomerId { get; set; }
    public string? RoomReference { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? KitchenSentAt { get; set; }
    public Guid? JournalHeaderId { get; set; }
    public string? Note { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public PosTable? PosTable { get; set; }
    public ICollection<BreakfastCafeOrderLine> Lines { get; set; } = [];
    public ICollection<BreakfastCafePayment> Payments { get; set; } = [];
    public ICollection<BreakfastCafeKitchenTicket> KitchenTickets { get; set; } = [];
}

public sealed class BreakfastCafeOrderLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BreakfastCafeOrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public int LineNo { get; set; }
    public string MenuItemCode { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineDiscountAmount { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public decimal VatAmount { get; set; }
    public decimal LineTotalAmount { get; set; }
    public string KitchenStatus { get; set; } = "New";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public BreakfastCafeOrder? Order { get; set; }
    public MenuItem? MenuItem { get; set; }
}

public sealed class PosTable : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string AreaCode { get; set; } = string.Empty;
    public string AreaName { get; set; } = string.Empty;
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string OrderType { get; set; } = "DineIn";
    public string Status { get; set; } = "Empty";
    public int SeatCount { get; set; } = 2;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BreakfastCafeOrder> Orders { get; set; } = [];
}

public sealed class BreakfastCafeKitchenTicket : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BreakfastCafeOrderId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string Station { get; set; } = "KitchenBar";
    public string Status { get; set; } = "Sent";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string? SentBy { get; set; }
    public string? Note { get; set; }
    public BreakfastCafeOrder? Order { get; set; }
}

public sealed class BreakfastCafePayment : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BreakfastCafeOrderId { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? ReferenceNo { get; set; }
    public Guid? CashFundId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? RoomReference { get; set; }
    public int? CustomerId { get; set; }
    public string Status { get; set; } = "Posted";
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public BreakfastCafeOrder? Order { get; set; }
}

public sealed class MenuCategory : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<MenuItem> MenuItems { get; set; } = [];
}

public sealed class MenuItem : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid MenuCategoryId { get; set; }
    public int? ItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitName { get; set; } = "phần";
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public bool HasRecipeManagement { get; set; } = true;
    public bool IsCombo { get; set; }
    public bool IsHotelService { get; set; }
    public string? PreparationArea { get; set; }
    public string? Note { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public MenuCategory? MenuCategory { get; set; }
    public ICollection<MenuItemRecipe> Recipes { get; set; } = [];
}

public sealed class MenuItemRecipe : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = "Default";
    public bool IsDefault { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public MenuItem? MenuItem { get; set; }
    public ICollection<MenuItemRecipeLine> Lines { get; set; } = [];
}

public sealed class MenuItemRecipeLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid MenuItemRecipeId { get; set; }
    public int LineNo { get; set; }
    public int? ItemId { get; set; }
    public Guid? ChildMenuItemId { get; set; }
    public int? DefaultWarehouseId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal LossRate { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public MenuItemRecipe? Recipe { get; set; }
    public ICollection<RecipeIngredientSubstitute> Substitutes { get; set; } = [];
}

public sealed class RecipeIngredientSubstitute : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid RecipeLineId { get; set; }
    public int InventoryItemId { get; set; }
    public int SubstituteInventoryItemId { get; set; }
    public decimal ConversionRate { get; set; } = 1m;
    public int Priority { get; set; } = 1;
    public bool AutoReplace { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public string? ModifiedBy { get; set; }
    public bool IsDeleted { get; set; }
    public MenuItemRecipeLine? RecipeLine { get; set; }
    public Item? InventoryItem { get; set; }
    public Item? SubstituteInventoryItem { get; set; }
}
