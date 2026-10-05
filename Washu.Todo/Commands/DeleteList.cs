namespace Washu.Todo.Commands;

using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public sealed record DeleteListCommand(Guid ListId, Guid UserId);

public sealed class DeleteListHandler(IDbContextFactory<TodoDbContext> factory)
{
    public async Task<Message> HandleAsync(DeleteListCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        try
        {
            var rowsDeleted = await dbContext.TodoLists
                .Where(l => l.Id == command.ListId && l.UserId == command.UserId)
                .ExecuteDeleteAsync();
            return rowsDeleted == 0
                ? Message.Error("List Deletion Error", "List was not found")
                : Message.Success("List Deletion Success", "Successfully deleted list");
        }
        catch (Exception)
        {
            return Message.Error("List Deletion Error", "There was an unknown error while deleting your list");
        }
    }
}