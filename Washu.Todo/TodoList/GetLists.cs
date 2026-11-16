namespace Washu.Todo.Application;

using Components;
using Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Todo.Extensions;
using Washu.Todo.Persistence;

public sealed record GetListsQuery(Guid UserId, LoadDataConfig Config) : IQuery<GetListsResponse>;
public class GetListsHandler(IDbContextFactory<TodoDbContext> factory) : IQueryHandler<GetListsQuery, GetListsResponse>
{
    public async ValueTask<GetListsResponse> Handle(GetListsQuery query, CancellationToken ctx = new())
    {
        var response = new GetListsResponse();
        await using var context = await factory.CreateDbContextAsync(ctx);
        var dbQuery = await context.TodoLists
            .AsNoTracking()
            .ForUser(query.UserId)
            .OrderByDescending(e => e.LastModifiedDate.HasValue)
            .ThenByDescending(e => e.LastModifiedDate)
            .ThenByDescending(e => e.CreationDate)
            .AsQueryable()
            .ApplyDataGridDropDownFiltersAsync(query.Config, async queryable =>
            {
                response.Count = await queryable.CountAsync(ctx);
            });
        response.Lists = response.Count == 0 ? [] : await dbQuery.ToListAsync(ctx);
        return response;
    }  
}

public sealed class GetListsResponse
{
    public int Count { get; set; }
    public List<TodoList> Lists { get; set; } = [];

}