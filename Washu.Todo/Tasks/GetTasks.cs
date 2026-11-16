namespace Washu.Todo.Application;

using Components;
using Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Washu.Todo.Extensions;
using Washu.Todo.Persistence;

public sealed record GetTasksQuery(Guid SelectedListId, LoadDataConfig Config) : IQuery<GetTasksResponse>;
public sealed class GetTasksHandler(IDbContextFactory<TodoDbContext> factory) : IQueryHandler<GetTasksQuery, GetTasksResponse>
{
    public async ValueTask<GetTasksResponse> Handle(GetTasksQuery query, CancellationToken ctx = new())
    {
        var response = new GetTasksResponse();
        await using var context = await factory.CreateDbContextAsync(ctx);
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