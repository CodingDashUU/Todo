namespace Washu.Todo.Tasks;

using Framework;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;

public sealed record ToggleTaskCommand(Guid TaskId, Guid ListId, uint ListRowVersion) : ICommand<Result>;

public sealed class ToggleTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<ToggleTaskCommand, Result>
{
    public async ValueTask<Result> Handle(ToggleTaskCommand command, CancellationToken ctx = new())
    {
        if (await user.GetUserIdAsync() is not { } userId) return UserErrors.UserIdNotFound;
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists
            .ForUser(userId)
            .ByTodoList(command.ListId, ctx);
        var task = await dbContext.Tasks
            .ForTodoList(command.ListId)
            .ByTask(command.TaskId, ctx);
        if (list is null)
            return TodoErrors.ListNotFound;
        if (task is null) return TodoErrors.TaskNotFound;
        dbContext.UpdateEntity(list, command.ListRowVersion);
        task.Toggle();
        await dbContext.SaveChangesAsync(ctx);

        return Result.Unit;
    }
}