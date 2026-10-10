using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Tests.Unit;

public sealed class EmailDispatchServiceTests
{
    [Fact]
    public async Task QueueAsync_PersistsPendingEmailWithSourceReference()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);

        var sourceId = Guid.NewGuid();
        var result = await service.QueueAsync(new EmailDispatchRequestDto
        {
            Recipient = "guest@example.com",
            Subject = "Hóa đơn HD-000007",
            Body = "Nội dung hóa đơn",
            SourceType = "Invoice",
            SourceId = sourceId
        });

        result.Status.Should().Be("Pending");
        var message = await db.EmailOutboxMessages.FindAsync(result.Id);
        message.Should().NotBeNull();
        message!.Recipient.Should().Be("guest@example.com");
        message.SourceType.Should().Be("Invoice");
        message.SourceId.Should().Be(sourceId);
    }

    [Fact]
    public async Task QueueAsync_RejectsInvalidRecipient()
    {
        await using var db = TestDbContextFactory.CreateContext();
        var service = CreateService(db);

        var act = () => service.QueueAsync(new EmailDispatchRequestDto
        {
            Recipient = "not-an-email",
            Subject = "Test",
            Body = "Body"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("A valid recipient email is required.");
    }

    private static EmailDispatchService CreateService(AppDbContext db)
        => new(db, Options.Create(new EmailOptions()), NullLogger<EmailDispatchService>.Instance);
}
