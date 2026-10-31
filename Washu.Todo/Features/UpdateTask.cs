namespace Washu.Todo.Features;

using Framework.Extensions;
using Framework.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Notifications;
using Washu.Todo.Identity;

public sealed record UpdateTaskCommand(Guid TaskId, string NewTaskName, DateTimeOffset NewGoalDate, Guid ListId, uint ListRowVersion);

public sealed class UpdateTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    ICurrentUser user
    )
{
    public async Task<Message> HandleAsync(UpdateTaskCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var list = await dbContext.TodoLists.ForCurrentUser(user).ByTodoList(command.ListId).SingleOrDefaultAsync();
        var task = await  dbContext.Tasks.ForTodoList(command.ListId).ByTask(command.TaskId).SingleOrDefaultAsync();
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
            await dbContext.SaveChangesAsync();
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