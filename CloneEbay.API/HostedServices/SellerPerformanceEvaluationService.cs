using CloneEbay.Application.Sellers;

namespace CloneEbay.API.HostedServices;

/// <summary>
/// Spec A.3: checks daily, but only actually (re-)evaluates sellers when the
/// current UTC day is the 20th — <see cref="ISellerPerformanceService.EvaluateDueSellersAsync"/>
/// itself guards on that, this loop just supplies the daily heartbeat.
/// </summary>
public sealed class SellerPerformanceEvaluationService(
    IServiceScopeFactory scopeFactory,
    ILogger<SellerPerformanceEvaluationService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<ISellerPerformanceService>();
                var evaluated = await service.EvaluateDueSellersAsync(stoppingToken);
                if (evaluated > 0) logger.LogInformation("Re-evaluated performance level for {SellerCount} sellers.", evaluated);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not evaluate seller performance levels.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}
