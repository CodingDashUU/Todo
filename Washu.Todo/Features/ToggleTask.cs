namespace Washu.Todo.Features;

using Microsoft.EntityFrameworkCore;
using Washu.Framework.Notifications;
using Washu.Todo.Identity;

public sealed record ToggleTaskCommand(Guid TaskId, Guid ListId, Guid ListVersionId);

public sealed class ToggleTaskHandler(IDbContextFactory<TodoDbContext> factory)
{
    public async Task<Message> HandleAsync(ToggleTaskCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var list = await dbContext.TodoLists.FirstOrDefaultAsync(l => l.Id == command.ListId);
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == command.TaskId && t.TodoListId == command.ListId);
        if (list is null)
            return Message.Error("Toggle Task Error", "Todo list does not exist");
        if (list.VersionId != command.ListVersionId)
            return Message.Error("Toggle Task Error", "Your todo list was already modified, please try again");
        if (task is null) return Message.Error(title: "Toggle Task Error", details: "Task does not exist");
        task.Toggle();
        list.LastModified = DateTimeOffset.UtcNow;
        list.ChangeVersionId();
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            return Message.Error("Toggle Task Error", "There was an unknown error while toggling your task");
        }

        return Message.Success(title: "Toggle Task", details: "Successfully toggled task");
    }
}