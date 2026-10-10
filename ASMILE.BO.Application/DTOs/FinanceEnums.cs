namespace ASMILE.BO.Application.DTOs;

public enum DebtStatus
{
    Open = 0,
    PartiallyPaid = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4,
    Rejected = 5
}

public enum ReceivableDocumentType
{
    OpeningBalance = 0,
    SalesInvoice = 1,
    ExportVoucher = 2,
    PaymentReceipt = 3,
    Adjustment = 4,
    DebtStatement = 5
}

public enum PayableDocumentType
{
    OpeningBalance = 0,
    PurchaseInvoice = 1,
    ImportVoucher = 2,
    PaymentVoucher = 3,
    AdvancePayment = 4,
    Adjustment = 5,
    DebtStatement = 6
}

public enum InventoryDocumentType
{
    OpeningStock = 0,
    ImportVoucher = 1,
    ExportVoucher = 2,
    AdjustmentIn = 3,
    AdjustmentOut = 4,
    TransferIn = 5,
    TransferOut = 6,
    StockTake = 7
}

public enum LedgerAccountType
{
    Asset = 0,
    Liability = 1,
    Equity = 2,
    Revenue = 3,
    Expense = 4,
    Other = 5
}

public enum LedgerDocumentType
{
    ImportVoucher = 0,
    ExportVoucher = 1,
    PaymentReceipt = 2,
    PaymentVoucher = 3,
    AdvancePayment = 4,
    Adjustment = 5,
    OpeningBalance = 6
}
