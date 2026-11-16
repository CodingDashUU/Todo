namespace Washu.Todo.Application;

using Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Presentation;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Persistence;

public sealed record DeleteListCommand(Guid ListId) : ICommand<Message>;

public sealed class DeleteListHandler(
    IDbContextFactory<TodoDbContext> factory,
    GetUserService getUserService
    ) : ICommandHandler<DeleteListCommand, Message>
{
    public async ValueTask<Message> Handle(DeleteListCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var todoList = await dbContext.TodoLists
            .ForUser(await getUserService.GetUserIdAsync())
            .ByTodoList(command.ListId)
            .FirstOrDefaultAsync(ctx);
        if (todoList is null) return Message.Error("List Deletion Error", "List was not found");
        todoList.SoftDelete();
        var rowsChanged = await dbContext.SaveChangesAsync(ctx);
        return rowsChanged != 1 
            ? Message.Error("List Deletion Error", "List was not found") 
            : Message.Success("List Deletion Success", "Successfully deleted list");
    }
}