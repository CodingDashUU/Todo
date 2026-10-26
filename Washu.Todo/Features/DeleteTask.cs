namespace Washu.Todo.Features;

using Framework.Extensions;
using Framework.Identity;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Notifications;
using Washu.Todo.Identity;

public sealed record DeleteTaskCommand(Guid TaskId, Guid ListId, uint ListRowVersion);

public sealed class DeleteTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    ICurrentUser user
    )
{
    public async Task<Message> HandleAsync(DeleteTaskCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var list = await dbContext.TodoLists
            .ForCurrentUser(user)
            .ByTodoList(command.ListId)
            .SingleOrDefaultAsync();
        var task = await dbContext.Tasks
            .ForTodoList(command.ListId)
            .ByTask(command.TaskId)
            .SingleOrDefaultAsync();
        if (list is null)
            return Message.Error("Toggle Task Error", "Todo list does not exist");
        dbContext.SetRowVersion(list, command.ListRowVersion);
        if (task is null) return Message.Error(title: "Toggle Task Error", details: "Task does not exist");
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
        catch (Exception)
        {
            return Message.Error("Task Deletion Error", "There was an unknown error while deleting your task");
        }
        return Message.Success("Task Deletion Success", "Successfully deleted task");
    }
}