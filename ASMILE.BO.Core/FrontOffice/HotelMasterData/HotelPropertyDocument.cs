using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped legal and operating document metadata.</summary>
public sealed class HotelPropertyDocument
{
    private HotelPropertyDocument() { }
    public Guid HotelPropertyDocumentId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string DocumentType { get; private set; } = string.Empty;
    public string DocumentNumber { get; private set; } = string.Empty;
    public string? IssuedBy { get; private set; }
    public DateTime IssuedDate { get; private set; }
    public DateTime? ExpiryDate { get; private set; }
    public string? FileReference { get; private set; }
    public string? Notes { get; private set; }
    public bool IsRequired { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public static HotelPropertyDocument Create(PropertyId propertyId, string documentType, string documentNumber, string? issuedBy, DateTime issuedDate, DateTime? expiryDate, string? fileReference, string? notes, bool isRequired, DateTime nowUtc, ActorId? actorId)
    { Validate(documentType, documentNumber, issuedDate, expiryDate); return new HotelPropertyDocument { HotelPropertyDocumentId = Uuid7.NewGuid(), PropertyId = propertyId.Value, DocumentType = documentType.Trim(), DocumentNumber = documentNumber.Trim(), IssuedBy = Normalize(issuedBy), IssuedDate = issuedDate.Date, ExpiryDate = expiryDate?.Date, FileReference = Normalize(fileReference), Notes = Normalize(notes), IsRequired = isRequired, IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string documentType, string documentNumber, string? issuedBy, DateTime issuedDate, DateTime? expiryDate, string? fileReference, string? notes, bool isRequired, DateTime nowUtc, ActorId? actorId)
    { Validate(documentType, documentNumber, issuedDate, expiryDate); DocumentType = documentType.Trim(); DocumentNumber = documentNumber.Trim(); IssuedBy = Normalize(issuedBy); IssuedDate = issuedDate.Date; ExpiryDate = expiryDate?.Date; FileReference = Normalize(fileReference); Notes = Normalize(notes); IsRequired = isRequired; Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string type, string number, DateTime issued, DateTime? expiry) { if (string.IsNullOrWhiteSpace(type)) throw new ArgumentException("HOTEL_PROPERTY_DOCUMENT_TYPE_REQUIRED"); if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("HOTEL_PROPERTY_DOCUMENT_NUMBER_REQUIRED"); if (expiry is not null && expiry.Value.Date < issued.Date) throw new ArgumentException("HOTEL_PROPERTY_DOCUMENT_DATE_RANGE_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
