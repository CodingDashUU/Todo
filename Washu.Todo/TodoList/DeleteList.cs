namespace Washu.Todo.TodoList;

using Framework;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;

public sealed record DeleteListCommand(Guid ListId) : ICommand<Result>;

public sealed class DeleteListHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<DeleteListCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteListCommand command, CancellationToken ctx = new())
    {
        if (await user.GetUserIdAsync() is not { } userId) return UserErrors.UserIdNotFound;
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var todoList = await dbContext.TodoLists
            .ForUser(userId)
            .ByTodoList(command.ListId, ctx);
        if (todoList is null) return TodoErrors.ListNotFound;
        todoList.SoftDelete();
        await dbContext.SaveChangesAsync(ctx);
        return Result.Unit;
    }
}