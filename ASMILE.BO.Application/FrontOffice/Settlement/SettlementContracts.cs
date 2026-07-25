using ASMILE.BO.Application.FrontOffice.Folio;
using ASMILE.BO.Application.FrontOffice.Foundation.Cqrs;
using ASMILE.BO.Core.FrontOffice.Settlement;

namespace ASMILE.BO.Application.FrontOffice.Settlement;

// === DTOs ===
public sealed record HotelInvoiceDto(Guid HotelInvoiceId, string InvoiceNumber, Guid HotelFolioId, Guid HotelStayId, InvoiceStatus Status, decimal Subtotal, decimal TaxAmount, decimal TotalAmount, decimal PaidAmount, decimal OutstandingAmount, string? GuestName, string? RoomNumber, string? StayDates, DateTime CreatedAtUtc, DateTime? PrintedAtUtc, byte[]? RowVersion);
public sealed record SettlementResultDto(bool Success, string Message, Guid? InvoiceId, decimal OutstandingBefore, decimal OutstandingAfter);

// === Commands ===
public sealed record SettleFolioCommand(Guid HotelFolioId, byte[] FolioExpectedRowVersion, decimal TaxAmount, decimal DiscountAmount, bool ManagerOverride) : ICommand<SettlementResultDto>;
public sealed record CloseFolioCommand(Guid HotelFolioId, byte[] FolioExpectedRowVersion) : ICommand<HotelFolioDto>;
public sealed record GenerateInvoiceCommand(Guid HotelFolioId, byte[] FolioExpectedRowVersion) : ICommand<HotelInvoiceDto>;
public sealed record PrintInvoiceCommand(Guid HotelInvoiceId, byte[] ExpectedRowVersion) : ICommand<HotelInvoiceDto>;

// === Queries ===
public sealed record GetInvoiceQuery(Guid HotelInvoiceId) : IQuery<HotelInvoiceDto>;
public sealed record GetInvoiceByFolioQuery(Guid HotelFolioId) : IQuery<HotelInvoiceDto>;

// === Repository ===
public interface IHotelInvoiceRepository
{
    Task<HotelInvoiceDto?> GetDtoAsync(Guid propertyId, Guid invoiceId, CancellationToken ct);
    Task<HotelInvoiceDto?> GetByFolioAsync(Guid propertyId, Guid folioId, CancellationToken ct);
    Task<HotelInvoice?> GetAsync(Guid propertyId, Guid invoiceId, CancellationToken ct);
    Task AddAsync(HotelInvoice invoice, CancellationToken ct);
}

// === Number Generator ===
public interface IHotelInvoiceNumberGenerator { Task<string> GenerateAsync(Guid propertyId, DateOnly businessDate, CancellationToken ct = default); }
