using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Lists.Features;
using Washu.Todo.Persistence;

namespace Washu.Todo.Tasks.Features;

public sealed record CreateTaskCommand(Guid ListId, uint ListRowVersion, string TaskName, DateTimeOffset GoalDate);

public sealed class CreateTaskHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    CurrentUser user
    )
{
    public async Task<Message> HandleAsync(CreateTaskCommand command)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var list = await dbContext.TodoLists
            .ForCurrentUser(user)
            .ByTodoList(command.ListId)
            .SingleOrDefaultAsync();
        if (list is null)
            return Message.Error("Task Creation Error", "List does not exist");
        if (await dbContext.Tasks.ForTodoList(command.ListId).CountAsync<TaskEntry>() >= 1000) 
            return Message.Error("Task Creation Error", 
                "A single todo list cannot exceed 1000 tasks. Please either delete any existing task or create a new todo list");
        dbContext.SetRowVersion(list, command.ListRowVersion);
        dbContext.Tasks.Add(new TaskEntry(command.TaskName, command.GoalDate, list.Id));
        dbContext.TodoLists.Update(list);
        try
        {
            await dbContext.SaveChangesAsync();
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