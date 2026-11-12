using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Washu.Framework.BackgroundJobs;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Entities.SoftDeleteEntity;

public class DeleteEntityWorker(
    IDbContextFactory<ApplicationDbContext> dbFactory, 
    IServiceScopeFactory scopeFactory,
    ILogger<DeleteEntityWorker> logger) : BackgroundService
{
    private const string WorkerName = nameof(DeleteEntityWorker);
    private List<IDeleteEntityTask> _tasks = [];
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Delete Background worker is starting");
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(30));
            using (var scope = scopeFactory.CreateScope()) _tasks =
                [.. scope.ServiceProvider.GetRequiredService<IEnumerable<IDeleteEntityTask>>()];
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var now = DateTimeOffset.UtcNow;
                logger.LogInformation("Worker running at: {Time}", now);
                await using var dbContext = await dbFactory.CreateDbContextAsync(stoppingToken);
                var startTime = Stopwatch.GetTimestamp();

                foreach (var task in _tasks)
                {
                    var lastRun = await dbContext.WorkersLastRun
                        .Where(w => w.WorkerName == WorkerName && w.TaskName == task.TaskName)
                        .FirstOrDefaultAsync(stoppingToken);
                    
                    if (lastRun is not null)
                    {
                        if (now - lastRun.LastRun < task.RunInterval) continue;
                        logger.LogInformation("Interval reached, executing {TaskName} now at: {Time}", task.TaskName, now);
                        var rowsDeleted = await task.ExecuteAsync(dbContext, stoppingToken);
                        logger.LogInformation("{EntityName} deleted: {RowsDeleted}", task.EntityName, rowsDeleted);
                        lastRun.LastRun = now;
                    }
                    else
                        dbContext.WorkersLastRun.Add(new WorkerLastRun { WorkerName = WorkerName, TaskName = task.TaskName, LastRun = now });
                }

                await dbContext.SaveChangesAsync(stoppingToken);
                var elapsed = Stopwatch.GetElapsedTime(startTime);
                logger.LogInformation("Finished in: {TimeElapsed}ms", elapsed.Milliseconds);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Delete Background worker stopped");
        }
    }
}