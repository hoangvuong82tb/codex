using ASMILE.BO.Application.FrontOffice.Settlement;
using ASMILE.BO.Core.FrontOffice.Settlement;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.FrontOffice.Settlement;

public sealed class EfHotelInvoiceRepository(AppDbContext db) : IHotelInvoiceRepository
{
    public async Task<HotelInvoiceDto?> GetDtoAsync(Guid propertyId, Guid invoiceId, CancellationToken ct) => await db.Set<HotelInvoice>().AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelInvoiceId == invoiceId).Select(x => new HotelInvoiceDto(x.HotelInvoiceId, x.InvoiceNumber, x.HotelFolioId, x.HotelStayId, x.Status, x.Subtotal, x.TaxAmount, x.TotalAmount, x.PaidAmount, x.OutstandingAmount, x.GuestName, x.RoomNumber, x.StayDates, x.CreatedAtUtc, x.PrintedAtUtc, x.RowVersion)).FirstOrDefaultAsync(ct);
    public async Task<HotelInvoiceDto?> GetByFolioAsync(Guid propertyId, Guid folioId, CancellationToken ct) => await db.Set<HotelInvoice>().AsNoTracking().Where(x => x.PropertyId == propertyId && x.HotelFolioId == folioId).OrderByDescending(x => x.CreatedAtUtc).Select(x => new HotelInvoiceDto(x.HotelInvoiceId, x.InvoiceNumber, x.HotelFolioId, x.HotelStayId, x.Status, x.Subtotal, x.TaxAmount, x.TotalAmount, x.PaidAmount, x.OutstandingAmount, x.GuestName, x.RoomNumber, x.StayDates, x.CreatedAtUtc, x.PrintedAtUtc, x.RowVersion)).FirstOrDefaultAsync(ct);
    public async Task<HotelInvoice?> GetAsync(Guid propertyId, Guid invoiceId, CancellationToken ct) => await db.Set<HotelInvoice>().FirstOrDefaultAsync(x => x.PropertyId == propertyId && x.HotelInvoiceId == invoiceId, ct);
    public async Task AddAsync(HotelInvoice invoice, CancellationToken ct) { db.Set<HotelInvoice>().Add(invoice); await db.SaveChangesAsync(ct); }
}
