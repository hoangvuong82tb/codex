using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.Settlement;

public enum InvoiceStatus { Draft, Generated, Printed, Emailed }

public sealed class HotelInvoice
{
    private HotelInvoice() { }
    public Guid HotelInvoiceId { get; private set; }
    public Guid PropertyId { get; private set; }
    public Guid HotelFolioId { get; private set; }
    public Guid HotelStayId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public InvoiceStatus Status { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public string? GuestName { get; private set; }
    public string? RoomNumber { get; private set; }
    public string? StayDates { get; private set; }
    public string InvoiceDataJson { get; private set; } = "{}";
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? PrintedAtUtc { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelInvoice Generate(Guid propertyId, Guid folioId, Guid stayId, string invoiceNumber, decimal subtotal, decimal tax, decimal total, decimal paid, decimal outstanding, string? guestName, string? roomNumber, string? stayDates, string invoiceDataJson, DateTime nowUtc, ActorId? actorId)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber)) throw new ArgumentException("INVOICE_NUMBER_REQUIRED");
        return new HotelInvoice
        {
            HotelInvoiceId = Uuid7.NewGuid(), PropertyId = propertyId, HotelFolioId = folioId,
            HotelStayId = stayId, InvoiceNumber = invoiceNumber.Trim(),
            Status = InvoiceStatus.Generated, Subtotal = subtotal, TaxAmount = tax,
            TotalAmount = total, PaidAmount = paid, OutstandingAmount = outstanding,
            GuestName = guestName, RoomNumber = roomNumber, StayDates = stayDates,
            InvoiceDataJson = invoiceDataJson, CreatedAtUtc = nowUtc, CreatedBy = actorId?.Value
        };
    }

    public void MarkPrinted(DateTime nowUtc) { Status = InvoiceStatus.Printed; PrintedAtUtc = nowUtc; }
}
