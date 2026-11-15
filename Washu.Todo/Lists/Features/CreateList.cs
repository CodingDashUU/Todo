using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Persistence;

namespace Washu.Todo.Lists.Features;

using Mediator;

public sealed record CreateListCommand(string ListName) : ICommand<Message>;
public sealed class CreateListHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    CurrentUser user
    ) : ICommandHandler<CreateListCommand, Message>
{
    public async ValueTask<Message> Handle(CreateListCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ctx);
        dbContext.TodoLists.Add(new Features.TodoList(user.UserId!.Value, command.ListName));
        try
        {
            await dbContext.SaveChangesAsync(ctx);
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