using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Lists.Features;
using Washu.Todo.Persistence;

namespace Washu.Todo.Tasks.Features;

using Mediator;

public sealed record DeleteTaskCommand(Guid TaskId, Guid ListId, uint ListRowVersion) : ICommand<Message>;

public sealed class DeleteTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<DeleteTaskCommand, Message>
{
    public async ValueTask<Message> Handle(DeleteTaskCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists
            .ForCurrentUser(user)
            .ByTodoList(command.ListId)
            .SingleOrDefaultAsync(ctx);
        var task = await dbContext.Tasks
            .ForTodoList(command.ListId)
            .ByTask(command.TaskId)
            .SingleOrDefaultAsync(ctx);
        if (list is null)
            return Message.Error("Toggle Task Error", "Todo list does not exist");
        if (task is null) return Message.Error(title: "Toggle Task Error", details: "Task does not exist");
        dbContext.SetRowVersion(list, command.ListRowVersion);
        dbContext.Tasks.Remove(task);
        dbContext.TodoLists.Update(list);
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Message.Error("Task Deletion Error", "Your todo list was already modified, please try again");
        }
        return Message.Success("Task Deletion Success", "Successfully deleted task");
    }
}