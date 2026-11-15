using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Lists.Features;
using Washu.Todo.Persistence;

namespace Washu.Todo.Lists;

using Mediator;

public sealed record UpdateListCommand(Guid ListId, uint ListRowVersion, string NewListName) : ICommand<Message>;
public sealed class UpdateListHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<UpdateListCommand, Message>
{
    public async ValueTask<Message> Handle(UpdateListCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var todoListQuery = dbContext.TodoLists
            .ForCurrentUser(user);
        var list = await todoListQuery.ByTodoList(command.ListId).SingleOrDefaultAsync(ctx);
        if (list is null)
            return Message.Error("Update List Error", "Todo list does not exist");
        if (todoListQuery.Any(l => l.Name == command.NewListName && l.Id != command.ListId))
            return Message.Error("Update List Error", "A todo list with the given name already exists");
        dbContext.SetRowVersion(list, command.ListRowVersion);
        list.Name =  command.NewListName;
        try
        {
            await dbContext.SaveChangesAsync(ctx);
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