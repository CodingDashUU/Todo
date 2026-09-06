namespace Washu.Todo.Commands;

using Framework.Identity;
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
            var list = await dbContext.TodoLists
                .FirstOrDefaultAsync(l => l.Id == selectedList.Id && user.Id == l.UserId);
            if (list is null)
                return (new Message
                {
                    Title = "Task Completion Error",
                    Details = "Todo list does not exist"
                }, null);
            if (list.VersionId != selectedList.VersionId)
                return (new Message
                {
                    Title = "Task Completion Error",
                    Details = "Your todo list was already modified, please try again"
                }, null);
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
            catch (DbUpdateException)
            {
                return (new Message(), null);
            }

            return (Message.Success(title: "Toggle Task", details: "Successfully toggled task"), list);
        }
    }
}