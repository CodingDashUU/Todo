using Microsoft.EntityFrameworkCore;
using Washu.Framework.Entities.SoftDeleteEntity;
using Washu.Framework.Identity.Persistence;
using Washu.Todo.Lists.Features;

namespace Washu.Todo.Lists;

public sealed class DeleteTodoListTask : IDeleteEntityTask
{
    public string EntityName => nameof(TodoList);
    public TimeSpan RunInterval => TimeSpan.FromHours(3); 

    public async Task<int> ExecuteAsync(ApplicationDbContext dbContext,
        CancellationToken ctx = default) =>
        await dbContext.Set<TodoList>().IgnoreQueryFilters().Where(u => u.IsDeleted).ExecuteDeleteAsync(ctx);
}