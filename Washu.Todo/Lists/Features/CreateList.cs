using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Persistence;

namespace Washu.Todo.Lists.Features;

public sealed record CreateListCommand(string ListName);
public sealed class CreateListHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    CurrentUser user
    )
{
    public async Task<Message> HandleAsync(CreateListCommand command)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.TodoLists.Add(new Features.TodoList(user.UserId!.Value, command.ListName));
        try
        {
            await dbContext.SaveChangesAsync();
            return Message.Success("List Creation Success", $"Successfully created list: {command.ListName}");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
                                           {
                                               SqlState: PostgresErrorCodes.UniqueViolation
                                           })
        {
            return Message.Error("List Creation Error", "Todo List with the given name already exists");
        }
    }
}