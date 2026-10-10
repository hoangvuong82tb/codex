namespace ASMILE.BO.Core.Enums;

public enum BusinessDocumentType
{
    Invoice = 0,
    ImportVoucher = 1,
    ExportVoucher = 2,
    PaymentVoucher = 3,
    AdvancePayment = 4,
    DebtStatement = 5,
    Contract = 6,
    Report = 7,
    Attachment = 8,
    Log = 9,
    // Keep this value appended so existing persisted document-type values remain stable.
    PaymentReceipt = 10
}
