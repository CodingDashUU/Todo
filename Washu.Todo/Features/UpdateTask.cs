namespace Washu.Todo.Features;

using Microsoft.EntityFrameworkCore;
using Washu.Framework.Notifications;
using Washu.Todo.Identity;

public sealed record UpdateTaskCommand(Guid TaskId, string NewTaskName, DateTimeOffset NewGoalDate, Guid ListId, Guid ListVersionId);

public sealed class UpdateTaskHandler(IDbContextFactory<TodoDbContext> factory)
{
    public async Task<Message> HandleAsync(UpdateTaskCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var list = await dbContext.TodoLists.FirstOrDefaultAsync(l => l.Id == command.ListId);
        var task = await dbContext.Tasks.FirstOrDefaultAsync(t => t.Id == command.TaskId && t.TodoListId == command.ListId);
        if (list is null)
            return Message.Error("Toggle Task Error", "Todo list does not exist");
        if (list.VersionId != command.ListVersionId)
            return Message.Error("Toggle Task Error", "Your todo list was already modified, please try again");
        if (task is null) return Message.Error(title: "Toggle Task Error", details: "Task does not exist");
        var taskNameChanged = command.NewTaskName != task.Name;
        var goalDateChanged = command.NewGoalDate != task.GoalDate;
        if (taskNameChanged)
        {
            if (await dbContext.Tasks.AnyAsync(t => t.Name == command.NewTaskName && t.TodoListId == command.ListId))
                return Message.Error(
                        title: "Task Modification Error",
                        details: "Task with the provided task name already exists");
            task.Name = command.NewTaskName;
        }
        if (goalDateChanged) task.ChangeGoalDate(command.NewGoalDate);
        if (taskNameChanged || goalDateChanged)
        {
            list.LastModified = DateTimeOffset.UtcNow;
            list.ChangeVersionId();
        }
        else return Message.Info(
                title: "Task Modification Info",
                details: "Task has not been modified");
        dbContext.TodoLists.Update(list);
        try
        {
            await dbContext.SaveChangesAsync();
            return Message.Success(
                    title: "Task Modification",
                    details: "Successfully modified the task");
        }
        catch (Exception)
        {
            return Message.Error("Task Update Error", "There was an unknown error while updating your task");
        }
    }
}