namespace Washu.Todo.Tasks;

using Washu.Framework;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Washu.Todo.Components;
using Washu.Todo.Extensions;

[RequireValidUser]
public sealed record GetTasksQuery(Guid SelectedListId, LoadDataConfig Config) : IQuery<Result<GetTasksResponse>>;
public sealed class GetTasksHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user) : IQueryHandler<GetTasksQuery, Result<GetTasksResponse>>
{
    public async ValueTask<Result<GetTasksResponse>> Handle(GetTasksQuery query, CancellationToken ctx = new())
    {
        var response = new GetTasksResponse();
        if ((await user.GetUserAsync()).UserId is not { } userId)
            return new UserIdNotFoundError();

        await using var context = await factory.CreateDbContextAsync(ctx);
        if (!await context.TodoLists.ForUser(userId).AnyAsync(list => list.Id == query.SelectedListId, ctx))
            return new NotFoundError("todo list");

        var dbQuery = await context.Tasks
            .AsNoTracking()
            .ForTodoList(query.SelectedListId)
            .OrderByDescending(e => e.DateCreated)
            .AsQueryable()
            .ApplyDataGridFiltersAsync(query.Config, async queryable =>
            {
                response.Count = await queryable.CountAsync(ctx);
                response.CompletedTasks = response.Count > 0 ? await queryable.CountAsync(t => t.IsCompleted, ctx) : 0;
            });
            
        response.Tasks = response.Count == 0 ? [] : await dbQuery.ToListAsync(ctx);
        return response;
    }
}

public sealed class GetTasksResponse
{
    public int Count { get; set; }
    public int CompletedTasks { get; set; }
    public List<TaskEntry> Tasks { get; set; } = [];
}
