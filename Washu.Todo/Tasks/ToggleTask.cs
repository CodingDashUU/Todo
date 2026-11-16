namespace Washu.Todo.Application;

using Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Presentation;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Persistence;

public sealed record ToggleTaskCommand(Guid TaskId, Guid ListId, uint ListRowVersion) : ICommand<Message>;

public sealed class ToggleTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    GetUserService getUserService
    ) : ICommandHandler<ToggleTaskCommand, Message>
{
    public async ValueTask<Message> Handle(ToggleTaskCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists
            .ForUser(await getUserService.GetUserIdAsync())
            .ByTodoList(command.ListId)
            .SingleOrDefaultAsync(cancellationToken: ctx);
        var task = await dbContext.Tasks
            .ForTodoList(command.ListId)
            .ByTask(command.TaskId)
            .SingleOrDefaultAsync(cancellationToken: ctx);
        if (list is null)
            return Message.Error("Toggle Task Error", "Todo list does not exist");
        if (task is null) return Message.Error(title: "Toggle Task Error", details: "Task does not exist");
        dbContext.UpdateEntity(list, command.ListRowVersion);
        task.Toggle();
        try
        {
            await dbContext.SaveChangesAsync(ctx);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Message.Error("Toggle Task Error", "Your todo list was already modified, please try again");
        }

        return Message.Success(title: "Toggle Task", details: "Successfully toggled task");
    }
}