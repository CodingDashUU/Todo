namespace Washu.Todo.Commands;

using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public static class DeleteList
{
    public class Command(IDbContextFactory<TodoDbContext> factory)
    {
        public async Task<Message> ExecuteAsync(Guid listId, Guid userId)
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            try
            {
                var rowsDeleted = await dbContext.TodoLists.Where(l => l.Id == listId && l.UserId == userId).ExecuteDeleteAsync();
                if (rowsDeleted == 0) return Message.Error("List Deletion Error", "List was not found");
            }
            catch (Exception)
            {
                return Message.Error("List Deletion Error", "There was an unknown error while deleting your list");
            }
            return Message.Success("List Deletion Success", "Successfully deleted list");
            
        }
    }
}