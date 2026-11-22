namespace Washu.Todo.TodoList;

using Framework;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;

[RequireValidUser]
public sealed record DeleteListCommand(Guid ListId) : ICommand<Result>;

public sealed class DeleteListHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<DeleteListCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteListCommand command, CancellationToken ctx = new())
    {
        if ((await user.GetUserAsync()).UserId is not { } userId) return new UserIdNotFoundError();
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists
            .ForUser(userId)
            .ByTodoList(command.ListId, ctx);
        if (list is null)
            return new NotFoundError(nameof(list));
        list.SoftDelete();
        await dbContext.SaveChangesAsync(ctx);
        return Result.Unit;
    }
}
