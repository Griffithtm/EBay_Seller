using CloneEbay.Application.Orders;

namespace CloneEbay.API.HostedServices;

public sealed class OrderAutoProgressService(IServiceScopeFactory scopeFactory, ILogger<OrderAutoProgressService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Background loop that silently processes orders every 2 minutes
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(2));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var progressed = await service.AutoProgressOrdersAsync(stoppingToken);
                if (progressed > 0)
                {
                    logger.LogInformation("Background order worker: Automatically progressed {Count} orders.", progressed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error during background order progression.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
