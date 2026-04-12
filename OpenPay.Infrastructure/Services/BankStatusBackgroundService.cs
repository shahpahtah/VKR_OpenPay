using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenPay.Application.Interfaces;

namespace OpenPay.Infrastructure.Services;

public class BankStatusBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BankStatusBackgroundService> _logger;

    public BankStatusBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BankStatusBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновая служба обработки банковских статусов запущена.");

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();

                var processor = scope.ServiceProvider.GetRequiredService<IBankStatusProcessor>();

                await processor.ProcessPendingStatusesAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Фоновая служба обработки банковских статусов остановлена.");
        }
    }
}