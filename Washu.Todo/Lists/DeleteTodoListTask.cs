using Microsoft.EntityFrameworkCore;
using Washu.Framework.Entities.SoftDeleteEntity;
using Washu.Framework.Identity.Persistence;
using Washu.Todo.Lists.Features;

namespace Washu.Todo.Lists;

public sealed class DeleteTodoListTask : IDeleteEntityTask
{
    public async Task<(string EntityName, int RowsDeleted)> ExecuteAsync(ApplicationDbContext dbContext,
        CancellationToken ctx = default)
    {
        var rowsDeleted = await dbContext.Set<TodoList>().IgnoreQueryFilters().Where(u => u.IsDeleted).ExecuteDeleteAsync(ctx);
        return (nameof(TodoList), rowsDeleted);
    }
}