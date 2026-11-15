using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;
using Washu.Todo.Lists.Features;
using Washu.Todo.Persistence;

namespace Washu.Todo.Tasks.Features;

using Mediator;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string NewTaskName,
    DateTimeOffset NewGoalDate,
    Guid ListId,
    uint ListRowVersion) : ICommand<Message>;
public sealed class UpdateTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<UpdateTaskCommand, Message>
{
    public async ValueTask<Message> Handle(UpdateTaskCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists.ForCurrentUser(user).ByTodoList(command.ListId).SingleOrDefaultAsync(ctx);
        var task = await  dbContext.Tasks.ForTodoList(command.ListId).ByTask(command.TaskId).SingleOrDefaultAsync(ctx);
        if (list is null)
            return Message.Error("Toggle Task Error", "Todo list does not exist");
        if (task is null) return Message.Error(title: "Toggle Task Error", details: "Task does not exist");
        var taskNameChanged = command.NewTaskName != task.Name;
        var goalDateChanged = command.NewGoalDate != task.GoalDate;
        if (taskNameChanged) task.Name = command.NewTaskName;
        if (goalDateChanged) task.ChangeGoalDate(command.NewGoalDate);
        if (taskNameChanged || goalDateChanged)
        {
            dbContext.SetRowVersion(list, command.ListRowVersion);
            dbContext.TodoLists.Update(list);
        }
        else return Message.Info(
                title: "Task Modification Info",
                details: "Task has not been modified");
        try
        {
            await dbContext.SaveChangesAsync(ctx);
            return Message.Success(
                    title: "Task Modification",
                    details: "Successfully modified the task");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Message.Error("Task Update Error", "Your todo list was already modified, please try again");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Message.Error("Task Creation Error", "Task with the given task name already exists");
        }
    }
}