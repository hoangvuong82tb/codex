using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class EInvoiceServiceTests
{
    [Fact]
    public async Task CreateDraft_GeneratesXmlAndQueuesIssue()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);

        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-000001",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Công ty khách hàng",
            BuyerTaxCode = "0100000000",
            ProviderCode = "MISA",
            Lines =
            [
                new InvoiceLineDraftDto
                {
                    ItemCode = "DV001",
                    ItemName = "Dịch vụ tư vấn",
                    UnitName = "Lần",
                    Quantity = 2,
                    UnitPrice = 1_000_000m,
                    TaxRate = 10m
                }
            ]
        });

        var xml = await service.GenerateXmlAsync(invoiceId);
        var queued = await service.QueueIssueAsync(invoiceId);

        xml.Should().Contain("HD-000001");
        queued.Status.Should().Be("Signed");
        db.InvoiceIssueLogs.Should().ContainSingle(x => x.InvoiceId == invoiceId && x.Status == "Queued");
    }

    [Fact]
    public async Task ProcessIssueQueue_MarksInvoiceIssuedAndCreatesVatOutput()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-000002",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Công ty khách hàng",
            ProviderCode = "VIETTEL",
            Lines = [new InvoiceLineDraftDto { ItemCode = "HH001", ItemName = "Hàng hóa", UnitName = "Cái", Quantity = 1, UnitPrice = 500_000m, TaxRate = 10m }]
        });
        await service.QueueIssueAsync(invoiceId);

        var result = await service.ProcessNextQueuedIssueAsync();

        result.Status.Should().Be("Issued");
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        invoice.Status.Should().Be("Issued");
        invoice.ProviderInvoiceId.Should().StartWith("VIETTEL-");
        db.VatTransactions.Should().ContainSingle(x => x.SourceType == "EInvoice" && x.SourceId == invoiceId && x.Direction == "Output");
    }

    private static EInvoiceService CreateService(ASMILE.BO.Infrastructure.Persistence.AppDbContext db)
    {
        IEInvoiceXmlGenerator generator = new EInvoiceXmlGenerator(db);
        IEInvoiceProvider[] providers =
        [
            new StubEInvoiceProvider("VIETTEL"),
            new StubEInvoiceProvider("VNPT"),
            new StubEInvoiceProvider("MISA")
        ];
        return new EInvoiceService(db, generator, providers);
    }
}
