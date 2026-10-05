namespace Washu.Todo.Commands;

using FluentValidation;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public sealed record UpdateTaskCommand(Guid TaskId, string NewTaskName, DateTimeOffset NewGoalDate, Guid ListId, Guid ListVersionId);

public sealed class UpdateTaskHandler(IDbContextFactory<TodoDbContext> factory)
{
    public async Task<Message> HandleAsync(UpdateTaskCommand command)
    {
        await using var dbContext = await factory.CreateDbContextAsync();
        var list = await dbContext.GetListAsync(command.ListId);
        if (list is null) return Message.Error(
            title: "Task Modification Error",
            details: "List does not exist");
        if (list.VersionId != command.ListVersionId)
            return Message.Error("Task Update Error", "Your todo list was already modified, please try again");
        var item = list.Tasks.SingleOrDefault(t => t.Id == command.TaskId);
        if (item is null) return Message.Error(
            title: "Task Modification Error",
            details: "Task does not exist");
        var taskNameChanged = command.NewTaskName != item.Name;
        var goalDateChanged = command.NewGoalDate != item.GoalDate;
        if (taskNameChanged)
        {
            if (list.Tasks.Any(x => x.Name == command.NewTaskName))
                return Message.Error(
                        title: "Task Modification Error",
                        details: "Task with the provided task name already exists");
            item.Name = command.NewTaskName;
        }
        if (goalDateChanged) item.ChangeGoalDate(command.NewGoalDate);
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