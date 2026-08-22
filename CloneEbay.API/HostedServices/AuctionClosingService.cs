using CloneEbay.Application.Products;

namespace CloneEbay.API.HostedServices;

public sealed class AuctionClosingService(IServiceScopeFactory scopeFactory, ILogger<AuctionClosingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IProductService>();
                var closed = await service.CloseExpiredAuctionsAsync(stoppingToken);
                if (closed > 0) logger.LogInformation("Closed {AuctionCount} expired auctions.", closed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not close expired auctions.");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken)) break;
        }
    }
}
