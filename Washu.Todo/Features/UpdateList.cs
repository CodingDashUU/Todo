namespace Washu.Todo.Features;

using Framework.Extensions;
using Framework.Identity;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public sealed record UpdateListCommand(Guid ListId, Guid ListVersionId, string NewListName);
public sealed class UpdateListHandler(
    IDbContextFactory<TodoDbContext> factory,
    ICurrentUser user
    )
{
    public async Task<Message> HandleAsync(UpdateListCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var todoListQuery = dbContext.TodoLists
            .ForCurrentUser(user);
        var list = await todoListQuery.ByTodoList(command.ListId).SingleOrDefaultAsync();
        if (list is null)
            return Message.Error("Update List Error", "Todo list does not exist");
        if (list.VersionId != command.ListVersionId)
            return Message.Error("Toggle Task Error", "Your todo list was already modified, please try again");
        if (todoListQuery.Any(l => l.Name == command.NewListName && l.Id != command.ListId))
            return Message.Error("Update List Error", "A todo list with the given name already exists");
        list.Name =  command.NewListName;
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