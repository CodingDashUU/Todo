namespace Washu.Todo.Commands;

using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public sealed record DeleteTaskCommand(Guid TaskId, Guid ListId, Guid ListVersionId);

public sealed class DeleteTaskHandler(IDbContextFactory<TodoDbContext> factory)
{
    public async Task<(Message, TodoList?)> HandleAsync(DeleteTaskCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var list = await dbContext.GetListAsync(command.ListId);
        if (list is null) return (Message.Error("Task Deletion Error", "Todo list was not found"), null);
        if (list.VersionId != command.ListVersionId) return (Message.Error("Task Deletion Error", "Your todo list was already modified, please try again"), null);
        var task = list.Tasks.SingleOrDefault(t => t.Id == command.TaskId);
        if (task is null) return (Message.Error("Task Deletion Error", "Task was not found"), null);
        dbContext.Set<TaskEntry>().Remove(task);
        list.LastModified = DateTimeOffset.UtcNow;
        list.ChangeVersionId();
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            return (Message.Error("Task Deletion Error", "There was an unknown error while deleting your task"), null);
        }
        return (Message.Success("Task Deletion Success", "Successfully deleted task"), list);
    }
}