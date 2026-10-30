namespace Washu.Todo.Features;

using Framework.Extensions;
using Framework.Identity;
using Microsoft.EntityFrameworkCore;
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
        if (await dbContext.TodoLists
                .ForCurrentUser(user)
                .AnyAsync(l => l.Name == command.ListName))
            return Message.Error("List Creation Error", "Todo List with the given name already exists");
        dbContext.TodoLists.Add(new TodoList(user.UserId!.Value, command.ListName));
        await dbContext.SaveChangesAsync();
        return Message.Success("List Creation Success", $"Successfully created list: {command.ListName}");
    }
}