namespace Washu.Todo.Features;

using Framework.Extensions;
using Framework.Identity;
using Microsoft.EntityFrameworkCore;
using Washu.Framework.Notifications;
using Washu.Todo.Identity;

public sealed record DeleteListCommand(Guid ListId);

public sealed class DeleteListHandler(
    IDbContextFactory<TodoDbContext> factory,
    ICurrentUser user
    )
{
    public async Task<Message> HandleAsync(DeleteListCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var rowsDeleted = await dbContext.TodoLists
            .ForCurrentUser(user)
            .ByTodoList(command.ListId)
            .ExecuteDeleteAsync();
        return rowsDeleted == 0
            ? Message.Error("List Deletion Error", "List was not found")
            : Message.Success("List Deletion Success", "Successfully deleted list");
    }
}