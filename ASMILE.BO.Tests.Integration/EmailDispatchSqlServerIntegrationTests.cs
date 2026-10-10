using System.Net;
using System.Net.Sockets;
using System.Text;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Options;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Tests.Integration;

[Collection(SqlServerIntegrationCollection.Name)]
public sealed class EmailDispatchSqlServerIntegrationTests
{
    [Fact]
    public async Task Concurrent_dispatchers_send_an_outbox_message_once()
    {
        var databaseName = $"ASMILE_BO_EMAIL_DISPATCH_{Guid.NewGuid():N}";
        var connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Integrated Security=True;TrustServerCertificate=True;Connect Timeout=30;";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
        var messageId = Guid.NewGuid();
        using var smtp = new LocalSmtpServer();
        CancellationTokenSource? serverCancellation = null;
        Task? serverTask = null;

        await using var setupDb = new AppDbContext(options);
        try
        {
            await setupDb.Database.EnsureDeletedAsync();
            await setupDb.Database.MigrateAsync();
            setupDb.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                Id = messageId,
                Recipient = "guest@example.test",
                Subject = "Dispatch race integration test",
                Body = "Test message",
                CreatedAtUtc = DateTime.UtcNow
            });
            await setupDb.SaveChangesAsync();

            serverCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            serverTask = smtp.RunAsync(serverCancellation.Token);

            var emailOptions = Options.Create(new EmailOptions
            {
                Enabled = true,
                Host = IPAddress.Loopback.ToString(),
                Port = smtp.Port,
                EnableSsl = false,
                FromAddress = "noreply@example.test",
                TimeoutMilliseconds = 10_000
            });
            await using var firstDb = new AppDbContext(options);
            await using var secondDb = new AppDbContext(options);
            var firstDispatcher = new EmailDispatchService(firstDb, emailOptions, NullLogger<EmailDispatchService>.Instance);
            var secondDispatcher = new EmailDispatchService(secondDb, emailOptions, NullLogger<EmailDispatchService>.Instance);

            var firstDispatch = firstDispatcher.DispatchPendingAsync();
            await smtp.FirstConnectionAccepted.WaitAsync(TimeSpan.FromSeconds(10));
            var secondDispatch = secondDispatcher.DispatchPendingAsync();
            await Task.Delay(250);
            smtp.ReleaseHandshake();

            var results = await Task.WhenAll(firstDispatch, secondDispatch).WaitAsync(TimeSpan.FromSeconds(15));
            serverCancellation.Cancel();
            await serverTask;

            results.Sum().Should().Be(1);
            smtp.ConnectionCount.Should().Be(1);
            var message = await setupDb.EmailOutboxMessages.AsNoTracking().SingleAsync(x => x.Id == messageId);
            message.Status.Should().Be("Sent");
        }
        finally
        {
            smtp.ReleaseHandshake();
            serverCancellation?.Cancel();
            try
            {
                if (serverTask is not null)
                {
                    await serverTask;
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the test shuts down its local SMTP server.
            }
            catch (ObjectDisposedException)
            {
                // The listener is disposed during test cleanup.
            }

            await setupDb.Database.EnsureDeletedAsync();
            SqlConnection.ClearAllPools();
        }
    }

    private sealed class LocalSmtpServer : IDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly TaskCompletionSource _firstConnectionAccepted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _handshakeReleased = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _connectionCount;

        public LocalSmtpServer()
        {
            _listener.Start();
        }

        public int Port => ((IPEndPoint)_listener.LocalEndpoint).Port;
        public int ConnectionCount => Volatile.Read(ref _connectionCount);
        public Task FirstConnectionAccepted => _firstConnectionAccepted.Task;

        public void ReleaseHandshake() => _handshakeReleased.TrySetResult();

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var clients = new List<Task>();
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                    Interlocked.Increment(ref _connectionCount);
                    _firstConnectionAccepted.TrySetResult();
                    clients.Add(HandleClientAsync(client, cancellationToken));
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal test shutdown.
            }
            finally
            {
                ReleaseHandshake();
                if (clients.Count > 0)
                {
                    await Task.WhenAll(clients);
                }
            }
        }

        public void Dispose()
        {
            ReleaseHandshake();
            _listener.Stop();
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
        {
            using (client)
            await using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true))
            using (var writer = new StreamWriter(stream, Encoding.ASCII, leaveOpen: true) { AutoFlush = true, NewLine = "\r\n" })
            {
                await _handshakeReleased.Task.WaitAsync(cancellationToken);
                await writer.WriteLineAsync("220 localhost test SMTP");
                var readingMessageBody = false;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (line is null)
                    {
                        return;
                    }

                    if (readingMessageBody)
                    {
                        if (line == ".")
                        {
                            readingMessageBody = false;
                            await writer.WriteLineAsync("250 message accepted");
                        }

                        continue;
                    }

                    if (line.StartsWith("DATA", StringComparison.OrdinalIgnoreCase))
                    {
                        readingMessageBody = true;
                        await writer.WriteLineAsync("354 end with dot");
                    }
                    else if (line.StartsWith("QUIT", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("221 bye");
                        return;
                    }
                    else
                    {
                        await writer.WriteLineAsync("250 localhost");
                    }
                }
            }
        }
    }
}
