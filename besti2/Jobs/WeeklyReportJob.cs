using besti2.Application.Statistics;

namespace besti2.Jobs;

// "Ukerapport": every hour checks if it is Monday morning in Oslo and sends last week's numbers to the businesses
public class WeeklyReportJob(IServiceScopeFactory scopeFactory, ILogger<WeeklyReportJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        try
        {
            do
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var reports = scope.ServiceProvider.GetRequiredService<IWeeklyReportService>();
                    var sent = await reports.SendDueAsync(DateTime.UtcNow, stoppingToken);
                    if (sent > 0)
                    {
                        logger.LogInformation("Sent {Count} weekly reports", sent);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Sending weekly reports failed");
                }
            } while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // The API is stopping
        }
    }
}
