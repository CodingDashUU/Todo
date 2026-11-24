namespace Washu.Framework.Identity.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

public class DeleteUserJob(
    IDbContextFactory<ApplicationDbContext> dbFactory,
    ILogger<DeleteUserJob> logger) : ITickerFunction
{
    [TickerFunction(nameof(DeleteUserJob))]
    public async Task ExecuteAsync(TickerFunctionContext context,
        CancellationToken cancellationToken = new())
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rowsChanged = await dbContext.Users
            .IgnoreQueryFilters()
            .Where(u => DateTimeOffset.UtcNow - u.DeletedAt > TimeSpan.FromDays(30))
            .ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Users deleted: {UsersDeleted}", rowsChanged);
    }
}