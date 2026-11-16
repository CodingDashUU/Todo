namespace Washu.Todo.Application;

using Domain;
using Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Presentation;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Persistence;

public sealed record CreateTaskCommand(Guid ListId, uint ListRowVersion, string TaskName, DateTimeOffset GoalDate) : ICommand<Message>;

public sealed class CreateTaskHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    GetUserService getUserService
    ) : ICommandHandler<CreateTaskCommand, Message>
{
    public async ValueTask<Message> Handle(CreateTaskCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists
            .ForUser(await getUserService.GetUserIdAsync())
            .ByTodoList(command.ListId)
            .SingleOrDefaultAsync(ctx);
        if (list is null)
            return Message.Error("Task Creation Error", "List does not exist");
        if (await dbContext.Tasks.ForTodoList(command.ListId).CountAsync<TaskEntry>(ctx) >= 1000) 
            return Message.Error("Task Creation Error", 
                "A single todo list cannot exceed 1000 tasks. Please either delete any existing task or create a new todo list");
        dbContext.UpdateEntity(list, command.ListRowVersion);
        dbContext.Tasks.Add(new TaskEntry(command.TaskName, command.GoalDate, list.Id));
        try
        {
            await dbContext.SaveChangesAsync(ctx);
            return Message.Success("Task Creation", "Successfully Created Task");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Message.Error("Task Creation Error", "Your todo list was already modified, please try again");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Message.Error("Task Creation Error", "Task with the given task name already exists");
        }
    }
}