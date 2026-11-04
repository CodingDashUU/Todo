namespace Washu.Framework.Identity;

using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class DeleteEntityBackgroundWorker(
    IDbContextFactory<ApplicationDbContext> dbFactory, 
    IServiceScopeFactory scopeFactory,
    ILogger<DeleteEntityBackgroundWorker> logger) : BackgroundService
{
    private readonly bool _informationLoggingEnabled = logger.IsEnabled(LogLevel.Information);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Delete Background worker is starting");
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                if (_informationLoggingEnabled)
                    logger.LogInformation("Worker running at: {Time}", DateTimeOffset.UtcNow);
                var startTime = Stopwatch.GetTimestamp();
                using var scope = scopeFactory.CreateScope();
                await using var dbContext = await dbFactory.CreateDbContextAsync(stoppingToken);
                
                var tasks = scope.ServiceProvider.GetRequiredService<IEnumerable<IDeleteEntityTask>>();

                foreach (var task in tasks)
                {
                    var result = await task.ExecuteAsync(dbContext, stoppingToken);
                    if (_informationLoggingEnabled)
                        logger.LogInformation("{EntityName} deleted: {RowsDeleted}", result.EntityName, result.RowsDeleted);
                }
                var elapsed = Stopwatch.GetElapsedTime(startTime);
                if (_informationLoggingEnabled)
                    logger.LogInformation("Finished in: {TimeElapsed}ms", elapsed.Milliseconds);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Delete Background worker stopped");
        }
    }
}