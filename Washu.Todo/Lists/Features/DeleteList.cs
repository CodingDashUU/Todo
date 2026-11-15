namespace Washu.Todo.Lists.Features;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Persistence;
using Washu.Todo.Extensions;
public sealed record DeleteListCommand(Guid ListId) : ICommand<Message>;

public sealed class DeleteListHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<DeleteListCommand, Message>
{
    public async ValueTask<Message> Handle(DeleteListCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var todoList = await dbContext.TodoLists
            .ForCurrentUser(user)
            .ByTodoList(command.ListId)
            .FirstOrDefaultAsync(ctx);
        if (todoList is null) return Message.Error("List Deletion Error", "List was not found");
        dbContext.Remove(todoList);
        var rowsChanged = await dbContext.SaveChangesAsync(ctx);
        return rowsChanged != 1 
            ? Message.Error("List Deletion Error", "List was not found") 
            : Message.Success("List Deletion Success", "Successfully deleted list");
    }
}