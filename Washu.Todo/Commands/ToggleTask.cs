namespace Washu.Todo.Commands;

using Framework.Identity;
using Framework.Identity.Entities;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public class ToggleTask
{
    public class Command(IDbContextFactory<TodoDbContext> factory)
    {
        public async Task<(Message, TodoList?)> ExecuteAsync(TodoList selectedList, TaskEntry entry, ApplicationUser user)
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var list = await dbContext.GetListAsync(selectedList.Id);
            if (list is null)
                return (
                    Message.Error("Toggle Task Error", "Todo list does not exist"), 
                    null);
            if (list.VersionId != selectedList.VersionId)
                return (Message.Error("Toggle Task Error", "Your todo list was already modified, please try again"), null);
            var item = list.Tasks.FirstOrDefault(t => t.Id == entry.Id);
            if (item is null) return (Message.Error(title: "Toggle Task Error", details: "Task does not exist"), null);
            // 3. Toggle it
            item.Toggle();
            list.LastModified = DateTimeOffset.UtcNow;
            list.ChangeVersionId();
            dbContext.TodoLists.Update(list);
            try
            {
                await dbContext.SaveChangesAsync();
            }
            catch (Exception)
            {
                return (Message.Error("Toggle Task Error", "There was an unknown error while toggling your task"), null);
            }

            return (Message.Success(title: "Toggle Task", details: "Successfully toggled task"), list);
        }
    }
}