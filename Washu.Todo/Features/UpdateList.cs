namespace Washu.Todo.Features;

using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public sealed record UpdateListCommand(Guid ListId, Guid ListVersionId, Guid UserId, string NewListName);
public class UpdateListHandler(IDbContextFactory<TodoDbContext> factory)
{
    public async Task<Message> HandleAsync(UpdateListCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var list = await dbContext.TodoLists.FirstOrDefaultAsync(l => l.Id == command.ListId && l.UserId == command.UserId);
        if (list is null)
            return Message.Error("Update List Error", "Todo list does not exist");
        if (list.VersionId != command.ListVersionId)
            return Message.Error("Toggle Task Error", "Your todo list was already modified, please try again");
        if (dbContext.TodoLists.Any(l => l.UserId == command.UserId && l.Name == command.NewListName))
            return Message.Error("Update List Error", "A todo list with the given name already exists");
        list.Name =  command.NewListName;
        list.LastModified = DateTimeOffset.UtcNow;
        list.ChangeVersionId();
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            return Message.Error("Update List Error", "There was an unknown error while updating your list");
        }

        return Message.Success(title: "Update List", details: "Successfully updated list");
    }
}