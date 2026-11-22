namespace Washu.Todo.TodoList;

using Framework;
using Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Washu.Framework.Extensions;
using Washu.Todo.Components;
using Washu.Todo.Extensions;

[RequireValidUser]
public sealed record GetListsQuery(LoadDataConfig Config) : IQuery<Result<GetListsResponse>>;
public class GetListsHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : IQueryHandler<GetListsQuery, Result<GetListsResponse>>
{
    public async ValueTask<Result<GetListsResponse>> Handle(GetListsQuery query, CancellationToken ctx = new())
    {
        var response = new GetListsResponse();
        if ((await user.GetUserAsync()).UserId is not { } userId) return new UserIdNotFoundError();
        await using var context = await factory.CreateDbContextAsync(ctx);
        var dbQuery = await context.TodoLists
            .AsNoTracking()
            .ForUser(userId)
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
