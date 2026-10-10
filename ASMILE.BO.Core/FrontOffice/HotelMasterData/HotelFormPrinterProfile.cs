using ASMILE.BO.Core.FrontOffice.Foundation.Identifiers;

namespace ASMILE.BO.Core.FrontOffice.HotelMasterData;

/// <summary>Property-scoped document template and printer routing profile.</summary>
public sealed class HotelFormPrinterProfile
{
    private HotelFormPrinterProfile() { }
    public Guid HotelFormPrinterProfileId { get; private set; }
    public Guid PropertyId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string DocumentType { get; private set; } = string.Empty;
    public string TemplateReference { get; private set; } = string.Empty;
    public string PrinterName { get; private set; } = string.Empty;
    public string PaperSize { get; private set; } = string.Empty;
    public int Copies { get; private set; }
    public bool Duplex { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime EffectiveTo { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public byte[]? RowVersion { get; private set; }
    public static HotelFormPrinterProfile Create(PropertyId propertyId, string code, string name, string documentType, string templateReference, string printerName, string paperSize, int copies, bool duplex, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, documentType, templateReference, printerName, paperSize, copies, from, to); return new HotelFormPrinterProfile { HotelFormPrinterProfileId = Uuid7.NewGuid(), PropertyId = propertyId.Value, Code = code.Trim(), Name = name.Trim(), DocumentType = documentType.Trim(), TemplateReference = templateReference.Trim(), PrinterName = printerName.Trim(), PaperSize = paperSize.Trim(), Copies = copies, Duplex = duplex, EffectiveFrom = from.Date, EffectiveTo = to.Date, Description = Normalize(description), IsActive = true, CreatedAtUtc = nowUtc, UpdatedAtUtc = nowUtc, CreatedBy = actorId?.Value, UpdatedBy = actorId?.Value }; }
    public void Update(string code, string name, string documentType, string templateReference, string printerName, string paperSize, int copies, bool duplex, DateTime from, DateTime to, string? description, DateTime nowUtc, ActorId? actorId)
    { Validate(code, name, documentType, templateReference, printerName, paperSize, copies, from, to); Code = code.Trim(); Name = name.Trim(); DocumentType = documentType.Trim(); TemplateReference = templateReference.Trim(); PrinterName = printerName.Trim(); PaperSize = paperSize.Trim(); Copies = copies; Duplex = duplex; EffectiveFrom = from.Date; EffectiveTo = to.Date; Description = Normalize(description); Touch(nowUtc, actorId); }
    public void Activate(DateTime nowUtc, ActorId? actorId) { IsActive = true; Touch(nowUtc, actorId); }
    public void Deactivate(DateTime nowUtc, ActorId? actorId) { IsActive = false; Touch(nowUtc, actorId); }
    private void Touch(DateTime nowUtc, ActorId? actorId) { UpdatedAtUtc = nowUtc; UpdatedBy = actorId?.Value; }
    private static void Validate(string code, string name, string documentType, string template, string printer, string paper, int copies, DateTime from, DateTime to) { if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("HOTEL_FORM_PRINTER_PROFILE_CODE_REQUIRED"); if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(documentType) || string.IsNullOrWhiteSpace(template) || string.IsNullOrWhiteSpace(printer) || string.IsNullOrWhiteSpace(paper)) throw new ArgumentException("HOTEL_FORM_PRINTER_PROFILE_FIELDS_REQUIRED"); if (to.Date < from.Date) throw new ArgumentException("HOTEL_FORM_PRINTER_PROFILE_EFFECTIVE_RANGE_INVALID"); if (copies < 1 || copies > 20) throw new ArgumentException("HOTEL_FORM_PRINTER_PROFILE_COPIES_INVALID"); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
