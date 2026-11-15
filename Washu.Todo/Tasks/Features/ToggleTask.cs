using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Lists.Features;
using Washu.Todo.Persistence;

namespace Washu.Todo.Tasks.Features;

using Mediator;

public sealed record ToggleTaskCommand(Guid TaskId, Guid ListId, uint ListRowVersion) : ICommand<Message>;

public sealed class ToggleTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<ToggleTaskCommand, Message>
{
    public async ValueTask<Message> Handle(ToggleTaskCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists
            .ForCurrentUser(user)
            .ByTodoList(command.ListId)
            .SingleOrDefaultAsync(cancellationToken: ctx);
        var task = await dbContext.Tasks
            .ForTodoList(command.ListId)
            .ByTask(command.TaskId)
            .SingleOrDefaultAsync(cancellationToken: ctx);
        if (list is null)
            return Message.Error("Toggle Task Error", "Todo list does not exist");
        if (task is null) return Message.Error(title: "Toggle Task Error", details: "Task does not exist");
        dbContext.SetRowVersion(list, command.ListRowVersion);
        task.Toggle();
        dbContext.TodoLists.Update(list);
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