using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class EmailDispatchHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<EmailOptions> _options;
    private readonly ILogger<EmailDispatchHostedService> _logger;

    public EmailDispatchHostedService(IServiceScopeFactory scopeFactory, IOptionsMonitor<EmailOptions> options, ILogger<EmailDispatchHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_options.CurrentValue.Enabled)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<IEmailDispatchService>();
                    await service.DispatchPendingAsync(cancellationToken: stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Email outbox dispatch cycle failed.");
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(_options.CurrentValue.PollSeconds, 10, 3600)), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
