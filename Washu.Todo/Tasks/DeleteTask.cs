namespace Washu.Todo.Tasks;

using Framework;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;

[RequireValidUser]
public sealed record DeleteTaskCommand(Guid TaskId, Guid ListId, uint ListRowVersion) : ICommand<Result>;

public sealed class DeleteTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<DeleteTaskCommand, Result>
{
    public async ValueTask<Result> Handle(DeleteTaskCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        if ((await user.GetUserAsync()).UserId is not { } userId) return new UserIdNotFoundError();
        var list = await dbContext.TodoLists
            .ForUser(userId)
            .ByTodoList(command.ListId, ctx);
        var task = await dbContext.Tasks
            .ForTodoList(command.ListId)
            .ByTask(command.TaskId, ctx);
        if (list is null)
            return new NotFoundError(nameof(list));
        if (task is null) return new NotFoundError(nameof(task));
        dbContext.UpdateEntity(list, command.ListRowVersion);
        dbContext.Tasks.Remove(task);
        
        await dbContext.SaveChangesAsync(ctx);
        return Result.Unit;
    }
}
