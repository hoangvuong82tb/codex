namespace ASMILE.BO.Core.Entities;

public sealed class DiningRoom : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string RoomType { get; set; } = "Standard";
    public int Capacity { get; set; }
    public string? Description { get; set; }
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<DiningTable> Tables { get; set; } = [];
    public ICollection<TableReservation> Reservations { get; set; } = [];
}

public sealed class DiningTable : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid DiningRoomId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SeatCount { get; set; }
    public string Status { get; set; } = "Empty";
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DiningRoom? Room { get; set; }
    public ICollection<TableReservation> Reservations { get; set; } = [];
    public ICollection<AlacarteOrder> Orders { get; set; } = [];
}

public sealed class TableReservation : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ReservationNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime ReservationDateTime { get; set; }
    public int GuestCount { get; set; }
    public Guid? DiningRoomId { get; set; }
    public Guid? DiningTableId { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public DiningRoom? Room { get; set; }
    public DiningTable? Table { get; set; }
}

public sealed class AlacarteOrder : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid? DiningTableId { get; set; }
    public Guid? DiningRoomId { get; set; }
    public Guid? RestaurantQuoteId { get; set; }
    public int? CustomerId { get; set; }
    public Guid? ReservationId { get; set; }
    public int ActualGuestCount { get; set; }
    public string OrderType { get; set; } = "Basic";
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "Draft";
    public string PaymentStatus { get; set; } = "Unpaid";
    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal ServiceChargeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime? KitchenSentAt { get; set; }
    public string? Note { get; set; }
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DiningTable? Table { get; set; }
    public DiningRoom? Room { get; set; }
    public RestaurantQuote? Quote { get; set; }
    public Customer? Customer { get; set; }
    public TableReservation? Reservation { get; set; }
    public ICollection<AlacarteOrderLine> Lines { get; set; } = [];
    public ICollection<RestaurantOrderTable> OrderTables { get; set; } = [];
    public ICollection<RestaurantServingGroup> ServingGroups { get; set; } = [];
    public ICollection<RestaurantOrderExtraService> ExtraServices { get; set; } = [];
    public ICollection<RestaurantPreparationTicket> PreparationTickets { get; set; } = [];
    public ICollection<RestaurantDeposit> Deposits { get; set; } = [];
}

public sealed class AlacarteOrderLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid AlacarteOrderId { get; set; }
    public Guid? RestaurantOrderTableId { get; set; }
    public Guid MenuItemId { get; set; }
    public int LineNo { get; set; }
    public string MenuItemCode { get; set; } = string.Empty;
    public string MenuItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public decimal VatAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Amount { get; set; }
    public decimal SupplementQuantity { get; set; }
    public string? KitchenNote { get; set; }
    public string? SupplementNote { get; set; }
    public string LineStatus { get; set; } = "Ordered";
    public string KitchenStatus { get; set; } = "New";
    public string LineType { get; set; } = "Normal";
    public string PricingRule { get; set; } = "Manual";
    public bool IsSupplement { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public AlacarteOrder? Order { get; set; }
    public RestaurantOrderTable? OrderTable { get; set; }
    public MenuItem? MenuItem { get; set; }
}

public sealed class RestaurantExtraService : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ServiceType { get; set; } = "Other";
    public string UnitName { get; set; } = "lần";
    public decimal DefaultPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public string? ImagePath { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RestaurantOrderExtraService : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid AlacarteOrderId { get; set; }
    public Guid? RestaurantExtraServiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public bool IsVatIncluded { get; set; } = true;
    public decimal Amount { get; set; }
    public AlacarteOrder? Order { get; set; }
    public RestaurantExtraService? ExtraService { get; set; }
}

public sealed class RestaurantContract : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string? EventDescription { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime? SignedDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public ICollection<RestaurantDeposit> Deposits { get; set; } = [];
}

public sealed class RestaurantDeposit : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string DepositNo { get; set; } = string.Empty;
    public Guid? AlacarteOrderId { get; set; }
    public Guid? RestaurantContractId { get; set; }
    public Guid? TableReservationId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid? CashFundId { get; set; }
    public Guid? BankAccountId { get; set; }
    public string? Receiver { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Confirmed";
    public AlacarteOrder? Order { get; set; }
    public RestaurantContract? Contract { get; set; }
    public TableReservation? Reservation { get; set; }
}

public sealed class RestaurantQuote : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string QuoteNo { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public int TableStandard { get; set; } = 10;
    public Guid? DiningRoomId { get; set; }
    public Guid? DiningTableId { get; set; }
    public Guid? TableReservationId { get; set; }
    public Guid? ApprovedOrderId { get; set; }
    public Guid? DepositId { get; set; }
    public Guid? DepositCashTransactionId { get; set; }
    public string? DepositCashTransactionNo { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public string PaymentDeadline { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Chuyển khoản";
    public decimal DiscountAmount { get; set; }
    public string Promotion { get; set; } = string.Empty;
    public int ValidityDays { get; set; } = 7;
    public int CancelDeadlineDays { get; set; } = 3;
    public decimal CancelFeePercent { get; set; } = 30;
    public string BankAccount { get; set; } = string.Empty;
    public string Status { get; set; } = "Nháp";
    public string Note { get; set; } = string.Empty;
    public string PreparedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DiningRoom? DiningRoom { get; set; }
    public DiningTable? DiningTable { get; set; }
    public TableReservation? Reservation { get; set; }
    public AlacarteOrder? ApprovedOrder { get; set; }
    public RestaurantDeposit? Deposit { get; set; }
    public ICollection<RestaurantQuoteTable> QuoteTables { get; set; } = [];
    public ICollection<RestaurantQuoteLine> Lines { get; set; } = [];
}

public sealed class RestaurantQuoteLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid RestaurantQuoteId { get; set; }
    public Guid? MenuItemId { get; set; }
    public int LineNo { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LineType { get; set; } = "Món ăn";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public string PricingRule { get; set; } = "Manual";
    public decimal CalculatedQuantity { get; set; }
    public decimal Total { get; set; }
    public RestaurantQuote? Quote { get; set; }
    public MenuItem? MenuItem { get; set; }
}

public sealed class RestaurantQuoteTable : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid RestaurantQuoteId { get; set; }
    public int TableNo { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int SortOrder { get; set; }
    public bool IsVirtual { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public RestaurantQuote? Quote { get; set; }
}

public sealed class RestaurantOrderTable : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid AlacarteOrderId { get; set; }
    public Guid? RestaurantQuoteTableId { get; set; }
    public Guid? DiningTableId { get; set; }
    public int TableNo { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int GuestCount { get; set; }
    public int SortOrder { get; set; }
    public bool IsVirtual { get; set; }
    public string Status { get; set; } = "Serving";
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public AlacarteOrder? Order { get; set; }
    public RestaurantQuoteTable? QuoteTable { get; set; }
    public ICollection<AlacarteOrderLine> Lines { get; set; } = [];
    public ICollection<RestaurantServingGroupTable> ServingGroupTables { get; set; } = [];
}

public sealed class RestaurantServingGroup : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid AlacarteOrderId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ScopeType { get; set; } = "Serving";
    public string ServingRule { get; set; } = "PerTable";
    public int SortOrder { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public AlacarteOrder? Order { get; set; }
    public ICollection<RestaurantServingGroupTable> Tables { get; set; } = [];
}

public sealed class RestaurantServingGroupTable : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid RestaurantServingGroupId { get; set; }
    public Guid RestaurantOrderTableId { get; set; }
    public Guid? DiningTableId { get; set; }
    public int GuestCount { get; set; }
    public int ExtraGuestCount { get; set; }
    public bool IsServing { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public RestaurantServingGroup? ServingGroup { get; set; }
    public RestaurantOrderTable? OrderTable { get; set; }
}

public sealed class RestaurantPreparationTicket : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid AlacarteOrderId { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Station { get; set; } = "Kitchen";
    public string TicketType { get; set; } = "Send";
    public string Status { get; set; } = "Sent";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string? SentBy { get; set; }
    public string? Note { get; set; }
    public AlacarteOrder? Order { get; set; }
    public ICollection<RestaurantPreparationTicketLine> Lines { get; set; } = [];
}

public sealed class RestaurantPreparationTicketLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid RestaurantPreparationTicketId { get; set; }
    public Guid? AlacarteOrderLineId { get; set; }
    public int LineNo { get; set; }
    public string MenuItemName { get; set; } = string.Empty;
    public decimal Qty { get; set; }
    public string? KitchenNote { get; set; }
    public RestaurantPreparationTicket? Ticket { get; set; }
    public AlacarteOrderLine? OrderLine { get; set; }
}

public sealed class BanquetRoom : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int MaxTables { get; set; }
    public string? Description { get; set; }
    public string? LayoutJson { get; set; }
    public string Status { get; set; } = "Available";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BanquetEvent> Events { get; set; } = [];
}

public sealed class BanquetEvent : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string EventNo { get; set; } = string.Empty;
    public string EventType { get; set; } = "Other";
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GuestCount { get; set; }
    public int TableCount { get; set; }
    public int ExtraGuestCount { get; set; }
    public int ExtraTableCount { get; set; }
    public string? BrideName { get; set; }
    public string? GroomName { get; set; }
    public string? ConferenceName { get; set; }
    public string? BirthdayPersonName { get; set; }
    public DateTime? BirthdayDate { get; set; }
    public string? MeetingTopic { get; set; }
    public string? PartyTheme { get; set; }
    public string? OtherEventName { get; set; }
    public Guid? BanquetRoomId { get; set; }
    public string Status { get; set; } = "Draft";
    public bool IsFinanciallyLocked { get; set; }
    public DateTime? FinanciallyLockedAt { get; set; }
    public string? FinanciallyLockedBy { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public BanquetRoom? Room { get; set; }
    public ICollection<BanquetEventMenu> Menus { get; set; } = [];
    public ICollection<BanquetServiceLine> Services { get; set; } = [];
    public ICollection<BanquetContract> Contracts { get; set; } = [];
    public ICollection<BanquetDeposit> Deposits { get; set; } = [];
    public ICollection<BanquetChecklist> ChecklistItems { get; set; } = [];
    public ICollection<BanquetFloorPlan> FloorPlans { get; set; } = [];
    public ICollection<BanquetTimeline> TimelineItems { get; set; } = [];
    public ICollection<BanquetQuantificationBatch> QuantificationBatches { get; set; } = [];
    public ICollection<BanquetPurchaseRequest> PurchaseRequests { get; set; } = [];
    public byte[]? RowVersion { get; set; }
}

public sealed class BanquetMenuPackage : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PricingMode { get; set; } = "PerTable";
    public decimal Price { get; set; }
    public decimal VatRate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }
    public ICollection<BanquetMenuPackageLine> Lines { get; set; } = [];
}

public sealed class BanquetMenuGroup : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class BanquetMenuPackageLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetMenuPackageId { get; set; }
    public Guid MenuItemId { get; set; }
    public string CourseType { get; set; } = "Main";
    public string? PricingRule { get; set; }
    public int LineNo { get; set; }
    public decimal Qty { get; set; } = 1m;
    public string? Note { get; set; }
    public BanquetMenuPackage? Package { get; set; }
    public MenuItem? MenuItem { get; set; }
}

public sealed class BanquetEventMenu : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetEventId { get; set; }
    public Guid? BanquetMenuPackageId { get; set; }
    public Guid? MenuItemId { get; set; }
    public string? ItemName { get; set; }
    public decimal Qty { get; set; } = 1m;
    public string? Note { get; set; }
    public string PricingMode { get; set; } = "PerTable";
    public int TableCount { get; set; }
    public int GuestCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public BanquetEvent? Event { get; set; }
    public BanquetMenuPackage? Package { get; set; }
    public MenuItem? MenuItem { get; set; }
}

public sealed class BanquetServiceLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetEventId { get; set; }
    public string ServiceType { get; set; } = "Other";
    public string Description { get; set; } = string.Empty;
    public string UnitName { get; set; } = "lần";
    public decimal Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal Amount { get; set; }
    public decimal CostPrice { get; set; }
    public decimal CostAmount { get; set; }
    public int? SupplierId { get; set; }
    public string? Note { get; set; }
    public BanquetEvent? Event { get; set; }
    public Supplier? Supplier { get; set; }
}

public sealed class BanquetContract : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string ContractNo { get; set; } = string.Empty;
    public Guid BanquetEventId { get; set; }
    public DateTime? SignDate { get; set; }
    public int? CustomerId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = "Draft";
    public string? Note { get; set; }
    public BanquetEvent? Event { get; set; }
    public Customer? Customer { get; set; }
    public ICollection<BanquetDeposit> Deposits { get; set; } = [];
    public byte[]? RowVersion { get; set; }
}

public sealed class BanquetDeposit : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string DepositNo { get; set; } = string.Empty;
    public Guid BanquetEventId { get; set; }
    public Guid? BanquetContractId { get; set; }
    public DateTime DepositDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public Guid? CashFundId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid? CashTransactionId { get; set; }
    public string? CashTransactionNo { get; set; }
    public string? IdempotencyKey { get; set; }
    public decimal RefundAmount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? RefundedBy { get; set; }
    public string? RefundReason { get; set; }
    public Guid? RefundCashTransactionId { get; set; }
    public string? RefundCashTransactionNo { get; set; }
    public string? Receiver { get; set; }
    public string? Note { get; set; }
    public string Status { get; set; } = "Confirmed";
    public BanquetEvent? Event { get; set; }
    public BanquetContract? Contract { get; set; }
    public byte[]? RowVersion { get; set; }
}

public sealed class BanquetChecklist : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetEventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsDone { get; set; }
    public DateTime? DoneAt { get; set; }
    public string? DoneBy { get; set; }
    public int SortOrder { get; set; }
    public string? Note { get; set; }
    public BanquetEvent? Event { get; set; }
}

public sealed class BanquetFloorPlan : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetEventId { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CanvasWidth { get; set; } = 1000;
    public double CanvasHeight { get; set; } = 700;
    public string? LayoutJson { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public BanquetEvent? Event { get; set; }
    public ICollection<BanquetFloorPlanTable> Tables { get; set; } = [];
}

public sealed class BanquetFloorPlanTable : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetFloorPlanId { get; set; }
    public string TableNo { get; set; } = string.Empty;
    public string TableType { get; set; } = "Round";
    public int SeatCount { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; } = 90;
    public double Height { get; set; } = 90;
    public double Rotation { get; set; }
    public string? Note { get; set; }
    public BanquetFloorPlan? FloorPlan { get; set; }
}

public sealed class BanquetTimeline : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetEventId { get; set; }
    public TimeSpan StartTime { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ResponsiblePerson { get; set; }
    public int SortOrder { get; set; }
    public string Status { get; set; } = "Planned";
    public BanquetEvent? Event { get; set; }
}

public sealed class BanquetQuantificationBatch : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetEventId { get; set; }
    public int VersionNo { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public string Status { get; set; } = "Sent";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string? SentBy { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? ReceivedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletedBy { get; set; }
    public string? Note { get; set; }
    public BanquetEvent? Event { get; set; }
    public ICollection<BanquetQuantificationLine> Lines { get; set; } = [];
    public BanquetPurchaseRequest? PurchaseRequest { get; set; }
}

public sealed class BanquetQuantificationLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetQuantificationBatchId { get; set; }
    public int SortOrder { get; set; }
    public string LineType { get; set; } = "Ingredient";
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? SourceText { get; set; }
    public string? Note { get; set; }
    public BanquetQuantificationBatch? Batch { get; set; }
}

public sealed class BanquetPurchaseRequest : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string RequestNo { get; set; } = string.Empty;
    public Guid BanquetEventId { get; set; }
    public Guid BanquetQuantificationBatchId { get; set; }
    public DateTime RequiredDate { get; set; }
    public string Status { get; set; } = "Requested";
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? RequestedBy { get; set; }
    public string? Note { get; set; }
    public BanquetEvent? Event { get; set; }
    public BanquetQuantificationBatch? QuantificationBatch { get; set; }
    public ICollection<BanquetPurchaseRequestLine> Lines { get; set; } = [];
}

public sealed class BanquetPurchaseRequestLine : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid BanquetPurchaseRequestId { get; set; }
    public int SortOrder { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public string? Note { get; set; }
    public BanquetPurchaseRequest? Request { get; set; }
}

public sealed class BanquetModuleSetting : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string VenueName { get; set; } = "Nhà hàng Tiệc (Banquet)";
    public string VenueCode { get; set; } = "BNQ001";
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public int GuestsPerTable { get; set; } = 10;
    public decimal DefaultDepositRate { get; set; } = 30m;
    public decimal DefaultVatRate { get; set; } = 8m;
    public int HoldDays { get; set; } = 7;
    public int ReminderDays { get; set; } = 3;
    public string LunchStartTime { get; set; } = "11:00";
    public string DinnerStartTime { get; set; } = "18:00";
    public bool AutoGenerateEventNo { get; set; } = true;
    public bool WarnRoomConflict { get; set; } = true;
    public bool RequireContractBeforeConfirm { get; set; }
    public bool EnableDepositReminder { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class QuoteChangeRequest : ICompanyScoped, IBranchScoped
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid QuoteId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int PreviousGuestCount { get; set; }
    public int NewGuestCount { get; set; }
    public int GuestCountDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string RequestedBy { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AppliedBy { get; set; }
    public DateTime? AppliedAt { get; set; }
    public decimal PreviousTotalAmount { get; set; }
    public decimal NewTotalAmount { get; set; }
    public decimal AmountDelta { get; set; }
    public string? AdjustmentPlanJson { get; set; }
    public Guid PreviousOrderId { get; set; }
    public Guid? NewOrderId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public RestaurantQuote? Quote { get; set; }
    public AlacarteOrder? PreviousOrder { get; set; }
    public AlacarteOrder? NewOrder { get; set; }
    public ICollection<QuoteChangeRequestDetail> Details { get; set; } = [];
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public sealed class QuoteChangeRequestDetail
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChangeRequestId { get; set; }
    public Guid? QuoteLineId { get; set; }
    public Guid? OrderLineId { get; set; }
    public string ChangeType { get; set; } = "Unchanged";
    public string ItemName { get; set; } = string.Empty;
    public decimal PreviousQuantity { get; set; }
    public decimal NewQuantity { get; set; }
    public decimal PreviousAmount { get; set; }
    public decimal NewAmount { get; set; }
    public Guid? OrderTableId { get; set; }
    public bool IsAlreadySent { get; set; }
    public string? Note { get; set; }
    public QuoteChangeRequest? ChangeRequest { get; set; }
}

public sealed class QuoteAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Action { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangeRequestId { get; set; }
    public string? Note { get; set; }
    public RestaurantQuote? Quote { get; set; }
    public QuoteChangeRequest? ChangeRequest { get; set; }
}
