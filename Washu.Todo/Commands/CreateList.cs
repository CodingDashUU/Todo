namespace Washu.Todo.Commands;

using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;
using TodoList = TodoList;

public sealed record CreateListCommand(Guid UserId, string ListName);
public sealed class CreateListHandler(IDbContextFactory<TodoDbContext> dbContextFactory)
{
    public async Task<Message> HandleAsync(CreateListCommand command)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        if (await dbContext.TodoLists.AnyAsync(l => l.Name == command.ListName && l.UserId == command.UserId))
            return Message.Error("List Creation Error", "Todo List with the given name already exists");
        dbContext.TodoLists.Add(new TodoList(command.UserId, command.ListName));
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (Exception)
        {
            return Message.Error("List Creation Error", "There was an unknown error while creating your list");
        }
        return Message.Success("List Creation Success", $"Successfully created list: {command.ListName}");
    }
}