namespace Washu.Todo.Lists.Features;

using Components;
using Extensions;
using Framework.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Persistence;

public sealed record GetListsQuery(Guid UserId, LoadDataConfig Config);
public class GetListsHandler(IDbContextFactory<TodoDbContext> factory)
{
    public async Task<GetListsResponse> HandleAsync(GetListsQuery query, CancellationToken ctx = new())
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