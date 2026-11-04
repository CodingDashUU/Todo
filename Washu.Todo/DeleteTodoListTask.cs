namespace Washu.Todo;

using Framework.Identity;
using Microsoft.EntityFrameworkCore;

public sealed class DeleteTodoListTask : IDeleteEntityTask
{
    public async Task<(string EntityName, int RowsDeleted)> ExecuteAsync(ApplicationDbContext dbContext,
        CancellationToken ctx = default)
    {
        var rowsDeleted = await dbContext.Set<TodoList>().IgnoreQueryFilters().Where(u => u.IsDeleted).ExecuteDeleteAsync(ctx);
        return (nameof(TodoList), rowsDeleted);
    }
}