namespace Washu.Todo.Commands;

using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public static class DeleteTask
{
    public class Command(IDbContextFactory<TodoDbContext> factory)
    {
        public async Task<(Message, TodoList?)> ExecuteAsync(Guid taskId, TodoList listToModify)
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var list = await dbContext.GetListAsync(listToModify.Id);
            if (list is null) return (Message.Error("Task Deletion Error", "Todo list was not found"), null);
            if (list.VersionId != listToModify.VersionId) return (Message.Error("Task Deletion Error", "Your todo list was already modified, please try again"), null);
            var task = list.Tasks.SingleOrDefault(t => t.Id == taskId);
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
            return (Message.Success("Task Deletion Success", $"Successfully deleted task"), list);
            
        }
    }
}