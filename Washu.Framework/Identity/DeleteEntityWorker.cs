namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

public class DeleteEntityWorker(
    IDbContextFactory<ApplicationDbContext> dbFactory, 
    IServiceScopeFactory scopeFactory,
    ILogger<DeleteEntityWorker> logger) : BackgroundService
{
    private readonly bool _informationLoggingEnabled = logger.IsEnabled(LogLevel.Information);
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(1);
    private const string WorkerName = nameof(DeleteEntityWorker);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Delete Background worker is starting");
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_informationLoggingEnabled)
                logger.LogInformation("Worker running at: {Time}", DateTimeOffset.UtcNow);
            await using var dbContext = await dbFactory.CreateDbContextAsync(stoppingToken);
            var workerLastRun = await dbContext
                .WorkersLastRun.SingleOrDefaultAsync(w => w.WorkerName == WorkerName,stoppingToken);
            var lastRun = workerLastRun?.LastRun ?? DateTimeOffset.MinValue;
            if (DateTimeOffset.UtcNow - lastRun < _runInterval) continue;
            try
            {
                if (_informationLoggingEnabled)
                    logger.LogInformation("Interval reached, executing tasks now at: {Time}", DateTimeOffset.UtcNow);
                var startTime = Stopwatch.GetTimestamp();
                using var scope = scopeFactory.CreateScope();
                
                var tasks = scope.ServiceProvider.GetRequiredService<IEnumerable<IDeleteEntityTask>>();

                foreach (var task in tasks)
                {
                    var result = await task.ExecuteAsync(dbContext, stoppingToken);
                    if (_informationLoggingEnabled)
                        logger.LogInformation("{EntityName} deleted: {RowsDeleted}", result.EntityName, result.RowsDeleted);
                }
                if (workerLastRun is null)
                    dbContext.WorkersLastRun.Add(new WorkerLastRun { WorkerName = WorkerName, LastRun = DateTimeOffset.UtcNow });
                else
                    workerLastRun.LastRun = DateTimeOffset.UtcNow;

                await dbContext.SaveChangesAsync(stoppingToken);
                var elapsed = Stopwatch.GetElapsedTime(startTime);
                if (_informationLoggingEnabled)
                    logger.LogInformation("Finished in: {TimeElapsed}ms", elapsed.Milliseconds);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Delete Background worker stopped");
            }
        }
    }
}