using CloneEbay.Application.Reports;

namespace CloneEbay.API.HostedServices;

/// <summary>
/// Spec A.8 #3: the reference project defines MarkRunCompleted but nothing ever calls it —
/// this executor is new, built to actually run due ReportSchedule rows. Note: it regenerates
/// and stores the report file on each run, but this codebase has no email infrastructure yet,
/// so "gửi email tới DeliveryEmail" is not wired up (documented gap, not silently faked).
/// </summary>
public sealed class ReportScheduleExecutionService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReportScheduleExecutionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IReportService>();
                var executed = await service.RunDueSchedulesAsync(stoppingToken);
                if (executed > 0) logger.LogInformation("Executed {Count} due report schedules.", executed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Could not execute due report schedules.");
            }
        }
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }
}
