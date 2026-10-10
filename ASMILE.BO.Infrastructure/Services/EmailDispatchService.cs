using System.Net;
using System.Net.Mail;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EmailDispatchService : IEmailDispatchService
{
    private readonly AppDbContext _dbContext;
    private readonly EmailOptions _options;
    private readonly ILogger<EmailDispatchService> _logger;

    public EmailDispatchService(AppDbContext dbContext, IOptions<EmailOptions> options, ILogger<EmailDispatchService> logger)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailDispatchResultDto> QueueAsync(EmailDispatchRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var recipient = NormalizeRecipient(request.Recipient);
        if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Trim().Length > 255)
        {
            throw new InvalidOperationException("Email subject is required and must not exceed 255 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 100_000)
        {
            throw new InvalidOperationException("Email body is required and must not exceed 100,000 characters.");
        }

        var message = new EmailOutboxMessage
        {
            Recipient = recipient,
            Subject = request.Subject.Trim(),
            Body = request.Body,
            IsBodyHtml = request.IsBodyHtml,
            SourceType = string.IsNullOrWhiteSpace(request.SourceType) ? null : request.SourceType.Trim(),
            SourceId = request.SourceId,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.EmailOutboxMessages.Add(message);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Map(message);
    }

    public async Task<int> DispatchPendingAsync(int batchSize = 20, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            return 0;
        }

        var usesSqlServerLock = string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal);
        var ownsSqlServerLock = await TryAcquireSqlServerDispatchLockAsync(cancellationToken);
        if (usesSqlServerLock && !ownsSqlServerLock)
        {
            // Another process owns the dispatch lease; the hosted service will
            // retry on its next poll without producing an error or sending in parallel.
            return 0;
        }

        try
        {
            var now = DateTime.UtcNow;
            var messages = await _dbContext.EmailOutboxMessages
                .Where(x => (x.Status == "Pending" || x.Status == "Failed")
                    && (x.NextAttemptAtUtc == null || x.NextAttemptAtUtc <= now))
                .OrderBy(x => x.CreatedAtUtc)
                .Take(Math.Clamp(batchSize, 1, 100))
                .ToListAsync(cancellationToken);

            var sent = 0;
            foreach (var message in messages)
            {
                try
                {
                    await SendAsync(message, cancellationToken);
                    message.MarkSent(DateTime.UtcNow);
                    sent++;
                }
                catch (Exception ex) when (ex is SmtpException or InvalidOperationException or FormatException)
                {
                    message.MarkFailed(ex.Message, DateTime.UtcNow.AddMinutes(Math.Min(60, Math.Max(1, message.AttemptCount + 1) * 5)));
                    _logger.LogWarning(ex, "Email delivery failed for outbox message {EmailMessageId}", message.Id);
                }
            }

            if (messages.Count > 0)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return sent;
        }
        finally
        {
            if (ownsSqlServerLock)
            {
                await ReleaseSqlServerDispatchLockAsync();
            }
        }
    }

    private async Task<bool> TryAcquireSqlServerDispatchLockAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return false;
        }

        await _dbContext.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                DECLARE @lockResult int;
                EXEC @lockResult = sys.sp_getapplock
                    @Resource = N'ASMILE.BO.EmailOutbox.Dispatch',
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Session',
                    @LockTimeout = 1000,
                    @DbPrincipal = N'public';
                SELECT @lockResult;
                """;
            command.CommandTimeout = 15;

            var result = await command.ExecuteScalarAsync(cancellationToken);
            var lockResult = Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
            if (lockResult == -1)
            {
                await _dbContext.Database.CloseConnectionAsync();
                return false;
            }

            if (lockResult < 0)
            {
                throw new InvalidOperationException($"Could not acquire the SQL Server email dispatch lock (sp_getapplock={lockResult}).");
            }

            return true;
        }
        catch
        {
            await _dbContext.Database.CloseConnectionAsync();
            throw;
        }
    }

    private async Task ReleaseSqlServerDispatchLockAsync()
    {
        try
        {
            var connection = _dbContext.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = """
                EXEC sys.sp_releaseapplock
                    @Resource = N'ASMILE.BO.EmailOutbox.Dispatch',
                    @LockOwner = N'Session',
                    @DbPrincipal = N'public';
                """;
            command.CommandTimeout = 15;
            await command.ExecuteNonQueryAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Closing the connection also releases a session-owned application lock.
            _logger.LogWarning(ex, "Could not explicitly release the SQL Server email dispatch lock; the connection will be closed.");
        }
        finally
        {
            await _dbContext.Database.CloseConnectionAsync();
        }
    }

    private async Task SendAsync(EmailOutboxMessage message, CancellationToken cancellationToken)
    {
        using var smtp = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = Math.Clamp(_options.TimeoutMilliseconds, 1_000, 120_000)
        };
        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            smtp.Credentials = new NetworkCredential(_options.Username, _options.Password ?? string.Empty);
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsBodyHtml
        };
        mail.To.Add(new MailAddress(message.Recipient));
        await smtp.SendMailAsync(mail, cancellationToken);
    }

    private static string NormalizeRecipient(string value)
    {
        var recipient = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(recipient) || !MailAddress.TryCreate(recipient, out var address))
        {
            throw new InvalidOperationException("A valid recipient email is required.");
        }

        return address.Address;
    }

    private static EmailDispatchResultDto Map(EmailOutboxMessage message) => new()
    {
        Id = message.Id,
        Status = message.Status,
        AttemptCount = message.AttemptCount,
        CreatedAtUtc = message.CreatedAtUtc,
        SentAtUtc = message.SentAtUtc,
        LastError = message.LastError
    };
}
