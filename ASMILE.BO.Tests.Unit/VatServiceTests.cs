using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class VatServiceTests
{
    [Fact]
    public async Task CreateVatTransactionAsync_SkipsZeroTaxAmount()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var taxCode = await SeedTaxCodeAsync(db);
        var service = new TaxService(db, TestDbContextFactory.AuditLogService(db));

        var act = () => service.CreateVatTransactionAsync(new CreateVatTransactionRequest
        {
            TaxCodeId = taxCode.Id,
            SourceType = "ManualJournal",
            TaxBaseAmount = 100m,
            TaxAmount = 0m,
            Direction = "Input",
            DeclarationPeriod = "2026-05"
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task BuildVatDeclarationDraftAsync_SumsInputAndOutputVat()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var taxCode = await SeedTaxCodeAsync(db);
        var service = new TaxService(db, TestDbContextFactory.AuditLogService(db));
        await service.CreateVatTransactionAsync(new CreateVatTransactionRequest { TaxCodeId = taxCode.Id, SourceType = "Buy", TaxBaseAmount = 100m, TaxAmount = 10m, Direction = "Input", DeclarationPeriod = "2026-05" });
        await service.CreateVatTransactionAsync(new CreateVatTransactionRequest { TaxCodeId = taxCode.Id, SourceType = "Sell", TaxBaseAmount = 200m, TaxAmount = 20m, Direction = "Output", DeclarationPeriod = "2026-05" });

        var draft = await service.BuildVatDeclarationDraftAsync("2026-05");

        draft.InputVatAmount.Should().Be(10m);
        draft.OutputVatAmount.Should().Be(20m);
        draft.NetVatPayable.Should().Be(10m);
    }

    private static async Task<TaxCode> SeedTaxCodeAsync(ASMILE.BO.Infrastructure.Persistence.AppDbContext db)
    {
        var existing = db.TaxCodes.FirstOrDefault(x => x.Code == "VAT10");
        if (existing is not null)
        {
            return existing;
        }

        var taxCode = new TaxCode
        {
            Id = Guid.NewGuid(), Code = "VAT10", Name = "VAT 10%", TaxRate = 10m, TaxType = "VATInput",
            InputAccountCode = "1331", OutputAccountCode = "3331", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.TaxCodes.Add(taxCode);
        await db.SaveChangesAsync();
        return taxCode;
    }
}

