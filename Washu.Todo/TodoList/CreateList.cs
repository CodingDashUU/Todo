namespace Washu.Todo.Application;

using Domain;
using Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Identity.Presentation;
using Washu.Framework.Notifications;
using Washu.Todo.Persistence;

public sealed record CreateListCommand(string ListName) : ICommand<Message>;
public sealed class CreateListHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    GetUserService getUserService
    ) : ICommandHandler<CreateListCommand, Message>
{
    public async ValueTask<Message> Handle(CreateListCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ctx);
        dbContext.TodoLists.Add(new TodoList((await getUserService.GetUserIdAsync())!.Value, command.ListName));
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