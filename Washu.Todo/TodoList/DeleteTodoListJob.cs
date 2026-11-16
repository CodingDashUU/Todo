namespace Washu.Todo.Application;

using Microsoft.EntityFrameworkCore;
using TickerQ.Utilities.Base;
using TickerQ.Utilities.Interfaces;
using Washu.Todo.Persistence;

public class DeleteTodoListJob(
    IDbContextFactory<TodoDbContext> dbFactory,
    ILogger<DeleteTodoListJob> logger) : ITickerFunction
{
    [TickerFunction(nameof(DeleteTodoListJob))]
    public async Task ExecuteAsync(TickerFunctionContext context,
        CancellationToken cancellationToken = new())
    {
        await using var dbContext = await dbFactory.CreateDbContextAsync(cancellationToken);
        var rowsChanged = await dbContext.TodoLists.IgnoreQueryFilters().Where(u => u.IsDeleted).ExecuteDeleteAsync(cancellationToken);
        logger.LogInformation("Todo Lists deleted: {TodoListsDeleted}", rowsChanged);
    }
}