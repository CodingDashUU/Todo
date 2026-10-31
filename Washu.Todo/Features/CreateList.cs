namespace Washu.Todo.Features;

using Framework.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Notifications;
using Washu.Todo.Identity;
using TodoList = TodoList;

public sealed record CreateListCommand(string ListName);
public sealed class CreateListHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    ICurrentUser user
    )
{
    public async Task<Message> HandleAsync(CreateListCommand command)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        dbContext.TodoLists.Add(new TodoList(user.UserId!.Value, command.ListName));
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