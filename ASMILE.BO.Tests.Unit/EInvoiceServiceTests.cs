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
    public async Task ProviderConfiguration_SavesProtectedSecretAndNeverReturnsPlainSecret()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = new EInvoiceService(
            db,
            new EInvoiceXmlGenerator(db),
            [new StubEInvoiceProvider("MISA")],
            new PrefixSecretProtector());

        var id = await service.SaveProviderConfigurationAsync(new InvoiceProviderConfigurationRequestDto
        {
            ProviderCode = "MISA",
            DisplayName = "MISA staging",
            EndpointUrl = "https://staging.example.invalid/einvoice",
            Username = "tenant-user",
            Secret = "secret-value",
            IsDefault = true
        });

        var stored = await db.InvoiceProviderConfigurations.AsNoTracking().SingleAsync(x => x.Id == id);
        stored.ProtectedSecret.Should().Be("protected:secret-value");
        var visible = await service.GetProviderConfigurationsAsync();
        visible.Should().ContainSingle(x => x.Id == id && x.HasSecret && x.ProviderCode == "MISA");
        visible.Single().GetType().GetProperty("Secret").Should().BeNull();
    }

    [Fact]
    public async Task ProviderConfiguration_RejectsPublicHttpEndpoint()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);

        var act = () => service.SaveProviderConfigurationAsync(new InvoiceProviderConfigurationRequestDto
        {
            ProviderCode = "MISA",
            DisplayName = "MISA insecure",
            EndpointUrl = "http://provider.example.invalid/einvoice",
            Secret = "secret-value"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*HTTPS*");
    }

    [Fact]
    public async Task ProviderConfiguration_AllowsLoopbackHttpForLocalHarness()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);

        var id = await service.SaveProviderConfigurationAsync(new InvoiceProviderConfigurationRequestDto
        {
            ProviderCode = "MISA",
            DisplayName = "MISA local harness",
            EndpointUrl = "http://localhost:5080/einvoice"
        });

        id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task ProviderReadiness_IsUnavailableWhenOnlyFailClosedProviderIsRegistered()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.InvoiceProviderConfigurations.Add(new ASMILE.BO.Core.Entities.InvoiceProviderConfiguration
        {
            ProviderCode = "MISA",
            DisplayName = "MISA production",
            EndpointUrl = "https://einvoice.example.invalid/api",
            TenantCode = "tenant",
            Username = "service-user",
            ProtectedSecret = "protected-secret",
            IsActive = true,
            IsDefault = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new UnavailableEInvoiceProvider("MISA"));

        var readiness = await service.GetProviderReadinessAsync();

        readiness.IsReady.Should().BeFalse();
        readiness.HasActiveConfiguration.Should().BeTrue();
        readiness.HasRealAdapter.Should().BeFalse();
        readiness.Status.Should().Be("Unavailable");
        readiness.Message.Should().Contain("adapter thật");
    }

    [Fact]
    public async Task ProviderReadiness_IsReadyOnlyWhenAdapterAndConfigurationAreComplete()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.InvoiceProviderConfigurations.Add(new ASMILE.BO.Core.Entities.InvoiceProviderConfiguration
        {
            ProviderCode = "MISA",
            DisplayName = "MISA production",
            EndpointUrl = "https://einvoice.example.invalid/api",
            TenantCode = "tenant",
            Username = "service-user",
            ProtectedSecret = "protected-secret",
            IsActive = true,
            IsDefault = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db, new StubEInvoiceProvider("MISA"));

        var readiness = await service.GetProviderReadinessAsync();

        readiness.IsReady.Should().BeTrue();
        readiness.ProviderCode.Should().Be("MISA");
        readiness.HasValidEndpoint.Should().BeTrue();
        readiness.HasTenant.Should().BeTrue();
        readiness.HasUsername.Should().BeTrue();
        readiness.HasSecret.Should().BeTrue();
        readiness.Status.Should().Be("Ready");
    }

    [Fact]
    public async Task CreateDraft_UsesActiveDefaultProviderConfigurationWhenProviderIsNotSpecified()
    {
        await using var db = TestDbContextFactory.CreateContext();
        db.InvoiceProviderConfigurations.Add(new ASMILE.BO.Core.Entities.InvoiceProviderConfiguration
        {
            ProviderCode = "VNPT",
            DisplayName = "VNPT staging",
            EndpointUrl = "https://staging.example.invalid/einvoice",
            IsActive = true,
            IsDefault = true
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-DEFAULT-PROVIDER-001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách dùng provider mặc định",
            ProviderCode = string.Empty,
            Lines = [new InvoiceLineDraftDto
            {
                ItemCode = "DV-DEFAULT",
                ItemName = "Dịch vụ",
                UnitName = "Lần",
                Quantity = 1,
                UnitPrice = 100_000m,
                TaxRate = 10m
            }]
        });

        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        invoice.ProviderCode.Should().Be("VNPT");
        (await service.GetDefaultProviderCodeAsync()).Should().Be("VNPT");
    }

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

    [Fact]
    public async Task ProcessIssueQueue_WhenProviderIsUnavailable_FailsClosedAndPersistsFailure()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-UNAVAILABLE-001",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách chưa cấu hình provider",
            ProviderCode = "MISA",
            Lines = [new InvoiceLineDraftDto { ItemCode = "DV-FAIL", ItemName = "Dịch vụ", UnitName = "Lần", Quantity = 1, UnitPrice = 100_000m, TaxRate = 10m }]
        });
        await service.QueueIssueAsync(invoiceId);

        var unavailableService = CreateService(db, new UnavailableEInvoiceProvider("MISA"));

        var act = () => unavailableService.ProcessNextQueuedIssueAsync();

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Chưa cấu hình kết nối hóa đơn điện tử thật cho provider MISA.");

        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        invoice.Status.Should().Be("Draft");
        var log = await db.InvoiceIssueLogs.AsNoTracking().SingleAsync(x => x.InvoiceId == invoiceId);
        log.Status.Should().Be("Failed");
        log.ErrorMessage.Should().Be("Chưa cấu hình kết nối hóa đơn điện tử thật cho provider MISA.");
        log.CompletedAt.Should().NotBeNull();

        var failures = await unavailableService.GetFailedIssueLogsAsync();
        failures.Should().ContainSingle(x =>
            x.LogId == log.Id
            && x.InvoiceId == invoiceId
            && x.InvoiceNo == "HD-UNAVAILABLE-001"
            && x.ProviderCode == "MISA"
            && x.ErrorMessage == log.ErrorMessage);
    }

    [Fact]
    public async Task ProcessIssueQueue_PrefersRealAdapterWhenFailClosedSentinelIsRegisteredFirst()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-REAL-ADAPTER-001",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách có adapter thật",
            ProviderCode = "MISA",
            Lines = [new InvoiceLineDraftDto { ItemCode = "DV-REAL", ItemName = "Dịch vụ", UnitName = "Lần", Quantity = 1, UnitPrice = 100_000m, TaxRate = 10m }]
        });
        await service.QueueIssueAsync(invoiceId);

        var configuredService = CreateService(
            db,
            new UnavailableEInvoiceProvider("MISA"),
            new StubEInvoiceProvider("MISA"));

        var result = await configuredService.ProcessNextQueuedIssueAsync();

        result.Status.Should().Be("Issued");
        result.ProviderInvoiceId.Should().Be($"MISA-{invoiceId:N}");
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        invoice.Status.Should().Be("Issued");
        invoice.ProviderInvoiceId.Should().Be($"MISA-{invoiceId:N}");
    }

    [Fact]
    public async Task QueueIssue_WhenProviderIsUnavailable_DoesNotCreateSignedQueueEntry()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db, new UnavailableEInvoiceProvider("MISA"));
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-UNAVAILABLE-002",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách chưa cấu hình provider",
            ProviderCode = "MISA",
            Lines = [new InvoiceLineDraftDto { ItemCode = "DV-FAIL-2", ItemName = "Dịch vụ", UnitName = "Lần", Quantity = 1, UnitPrice = 100_000m, TaxRate = 10m }]
        });

        var act = () => service.QueueIssueAsync(invoiceId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Chưa cấu hình kết nối hóa đơn điện tử thật cho provider MISA.");

        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        invoice.Status.Should().Be("Draft");
        invoice.XmlPayload.Should().BeNull();
        db.InvoiceIssueLogs.Should().BeEmpty();
    }

    [Fact]
    public async Task CancelAsync_CancelsDraftAndPersistsReasonInIssueLog()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-000003",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách hủy hóa đơn",
            ProviderCode = "MISA",
            Lines = [new InvoiceLineDraftDto { ItemCode = "DV003", ItemName = "Dịch vụ", UnitName = "Lần", Quantity = 1, UnitPrice = 100_000m, TaxRate = 10m }]
        });

        var result = await service.CancelAsync(invoiceId, "Khách đổi thông tin xuất hóa đơn");

        result.Status.Should().Be("Cancelled");
        var invoice = await db.Invoices.AsNoTracking().SingleAsync(x => x.Id == invoiceId);
        invoice.Status.Should().Be("Cancelled");
        db.InvoiceIssueLogs.Should().ContainSingle(x =>
            x.InvoiceId == invoiceId
            && x.Status == "Cancelled"
            && x.ErrorMessage == "Khách đổi thông tin xuất hóa đơn");
    }

    [Fact]
    public async Task CancelAsync_RefusesAlreadyIssuedInvoice()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-000004",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách đã phát hành",
            ProviderCode = "VIETTEL",
            Lines = [new InvoiceLineDraftDto { ItemCode = "DV004", ItemName = "Dịch vụ", UnitName = "Lần", Quantity = 1, UnitPrice = 100_000m, TaxRate = 10m }]
        });
        await service.QueueIssueAsync(invoiceId);
        await service.ProcessNextQueuedIssueAsync();

        var act = () => service.CancelAsync(invoiceId, "Không được hủy trực tiếp");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Hóa đơn đã phát hành hoặc đã có mã provider*");
    }

    [Fact]
    public async Task GetInvoicesAsync_ReturnsPersistedInvoiceLines()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-000005",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách có chi tiết",
            ProviderCode = "MISA",
            Lines = [new InvoiceLineDraftDto
            {
                ItemCode = "ROOM-01",
                ItemName = "Tiền phòng 405",
                UnitName = "Đêm",
                Quantity = 2,
                UnitPrice = 750_000m,
                TaxRate = 10m,
                JournalAccountCode = "5111"
            }]
        });

        var invoice = (await service.GetInvoicesAsync()).Single(x => x.Id == invoiceId);

        invoice.Lines.Should().ContainSingle();
        invoice.Lines[0].ItemName.Should().Be("Tiền phòng 405");
        invoice.Lines[0].LineAmount.Should().Be(1_500_000m);
        invoice.Lines[0].JournalAccountCode.Should().Be("5111");
    }

    [Fact]
    public async Task GetInvoicesAsync_ReturnsPersistedReceivableSummary()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var receivableId = Guid.NewGuid();
        db.ReceivableTransactions.Add(new ASMILE.BO.Core.Entities.ReceivableTransaction
        {
            Id = receivableId,
            CustomerId = Guid.NewGuid(),
            DocumentNo = "AR-0001",
            DocumentDate = new DateTime(2026, 5, 22),
            DebitAmount = 1_100_000m,
            CreditAmount = 400_000m,
            BalanceAmount = 700_000m,
            Status = "Open"
        });
        await db.SaveChangesAsync();

        var invoiceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-AR-0001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách có công nợ",
            ProviderCode = "MISA",
            ReceivableTransactionId = receivableId,
            Lines = [new InvoiceLineDraftDto
            {
                ItemCode = "ROOM-AR",
                ItemName = "Tiền phòng",
                UnitName = "Đêm",
                Quantity = 1,
                UnitPrice = 1_000_000m,
                TaxRate = 10m
            }]
        });

        var invoice = (await service.GetInvoicesAsync()).Single(x => x.Id == invoiceId);

        invoice.ReceivableTransactionId.Should().Be(receivableId);
        invoice.ReceivableDebitAmount.Should().Be(1_100_000m);
        invoice.ReceivableCreditAmount.Should().Be(400_000m);
        invoice.ReceivableBalanceAmount.Should().Be(700_000m);
        invoice.ReceivableStatus.Should().Be("Open");
    }

    [Fact]
    public async Task CreateAdjustmentDraftAsync_PersistsLinkedNegativeDecrease()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);
        var sourceId = await service.CreateDraftAsync(new InvoiceDraftDto
        {
            InvoiceNo = "HD-000006",
            InvoiceSeries = "C26TAA",
            TemplateCode = "1/001",
            InvoiceDate = new DateTime(2026, 5, 22),
            BuyerName = "Khách cần điều chỉnh",
            ProviderCode = "VIETTEL",
            Lines = [new InvoiceLineDraftDto
            {
                ItemCode = "ROOM-405",
                ItemName = "Tiền phòng 405",
                UnitName = "Đêm",
                Quantity = 1,
                UnitPrice = 1_000_000m,
                TaxRate = 10m
            }]
        });
        await service.QueueIssueAsync(sourceId);
        await service.ProcessNextQueuedIssueAsync();

        var adjustmentId = await service.CreateAdjustmentDraftAsync(new InvoiceAdjustmentDraftDto
        {
            SourceInvoiceId = sourceId,
            AdjustmentType = "Decrease",
            Reason = "Điều chỉnh giảm tiền phòng theo biên bản",
            Description = "Điều chỉnh giảm tiền phòng 405",
            ItemCode = "ROOM-405-ADJ",
            UnitName = "Lần",
            Amount = 100_000m,
            TaxRate = 10m
        });

        var adjustment = await db.Invoices
            .Include(x => x.Lines)
            .AsNoTracking()
            .SingleAsync(x => x.Id == adjustmentId);

        adjustment.Status.Should().Be("Adjusted");
        adjustment.AdjustsInvoiceId.Should().Be(sourceId);
        adjustment.AdjustmentType.Should().Be("Decrease");
        adjustment.TotalAmount.Should().Be(-110_000m);
        adjustment.Lines.Should().ContainSingle();
        var adjustmentLine = adjustment.Lines.Single();
        adjustmentLine.LineAmount.Should().Be(-100_000m);
        adjustmentLine.TaxAmount.Should().Be(-10_000m);
    }

    private static EInvoiceService CreateService(ASMILE.BO.Infrastructure.Persistence.AppDbContext db, params IEInvoiceProvider[]? providers)
    {
        IEInvoiceXmlGenerator generator = new EInvoiceXmlGenerator(db);
        return new EInvoiceService(db, generator, providers is { Length: > 0 }
            ? providers
            :
            [
                new StubEInvoiceProvider("VIETTEL"),
                new StubEInvoiceProvider("VNPT"),
                new StubEInvoiceProvider("MISA")
            ]);
    }

    private sealed class StubEInvoiceProvider : IEInvoiceProvider
    {
        public StubEInvoiceProvider(string providerCode)
        {
            ProviderCode = providerCode;
        }

        public string ProviderCode { get; }

        public Task<InvoiceIssueResultDto> IssueAsync(
            Guid invoiceId,
            string xmlPayload,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new InvoiceIssueResultDto
            {
                InvoiceId = invoiceId,
                ProviderCode = ProviderCode,
                Status = "Issued",
                ProviderInvoiceId = $"{ProviderCode}-{invoiceId:N}",
                Message = "Provider test double."
            });

        public Task<InvoiceIssueResultDto> QueryStatusAsync(
            Guid invoiceId,
            string? providerInvoiceId,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new InvoiceIssueResultDto
            {
                InvoiceId = invoiceId,
                ProviderCode = ProviderCode,
                Status = string.IsNullOrWhiteSpace(providerInvoiceId) ? "Signed" : "Issued",
                ProviderInvoiceId = providerInvoiceId,
                Message = "Provider test double."
            });
    }

    private sealed class PrefixSecretProtector : ISecretProtector
    {
        public string Protect(string? plainText) => string.IsNullOrEmpty(plainText) ? string.Empty : "protected:" + plainText;
        public string Unprotect(string? protectedText) => protectedText?.Replace("protected:", string.Empty, StringComparison.Ordinal) ?? string.Empty;
        public bool IsProtected(string? value) => value?.StartsWith("protected:", StringComparison.Ordinal) == true;
    }
}
