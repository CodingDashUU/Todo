namespace Washu.Todo.Application;

using Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Presentation;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Persistence;

public sealed record UpdateListCommand(Guid ListId, uint ListRowVersion, string NewListName) : ICommand<Message>;
public sealed class UpdateListHandler(
    IDbContextFactory<TodoDbContext> factory,
    GetUserService getUserService
    ) : ICommandHandler<UpdateListCommand, Message>
{
    public async ValueTask<Message> Handle(UpdateListCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var todoListQuery = dbContext.TodoLists
            .ForUser(await getUserService.GetUserIdAsync());
        var list = await todoListQuery.ByTodoList(command.ListId).SingleOrDefaultAsync(ctx);
        if (list is null)
            return Message.Error("Update List Error", "Todo list does not exist");
        if (todoListQuery.Any(l => l.Name == command.NewListName && l.Id != command.ListId))
            return Message.Error("Update List Error", "A todo list with the given name already exists");
        dbContext.UpdateEntity(list, command.ListRowVersion);
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