namespace Washu.Todo.TodoList;

using Microsoft.EntityFrameworkCore;
using Persistence;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;

public class DeleteTodoListJob(
    IDbContextFactory<TodoDbContext> dbFactory,
    ILogger<DeleteTodoListJob> logger) : ITickerFunction
{
    [TickerFunction(nameof(DeleteTodoListJob))]
    public async Task ExecuteAsync(TickerFunctionContext context,
        CancellationToken cancellationToken = new())
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rowsChanged = await dbContext.TodoLists
            .IgnoreQueryFilters()
            .Where(u => DateTimeOffset.UtcNow - u.DeletedAt > TimeSpan.FromDays(30))
            .ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Todo Lists deleted: {TodoListsDeleted}", rowsChanged);
    }
}