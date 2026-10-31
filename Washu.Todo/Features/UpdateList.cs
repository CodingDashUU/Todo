namespace Washu.Todo.Features;

using Framework.Extensions;
using Framework.Identity;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public sealed record UpdateListCommand(Guid ListId, uint ListRowVersion, string NewListName);
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
        if (todoListQuery.Any(l => l.Name == command.NewListName && l.Id != command.ListId))
            return Message.Error("Update List Error", "A todo list with the given name already exists");
        dbContext.SetRowVersion(list, command.ListRowVersion);
        list.Name =  command.NewListName;
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation
                                           })
        {
            return Message.Error("Update List Error", "Todo List with the given name already exists");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Message.Error("Update List Error", "Your todo list was already modified, please try again");
        }

        return Message.Success(title: "Update List", details: "Successfully updated list");
    }
}