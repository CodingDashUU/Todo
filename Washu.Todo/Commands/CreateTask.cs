namespace Washu.Todo.Commands;

using FluentValidation;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;
public sealed record CreateTaskCommand(Guid ListId, Guid ListVersionId, string TaskName, DateTimeOffset GoalDate);

public sealed class CreateTaskHandler(IDbContextFactory<TodoDbContext> dbContextFactory)
{
    public async Task<Message> HandleAsync(CreateTaskCommand command)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var list = await dbContext.GetListAsync(command.ListId);
        if (list is null)
            return Message.Error("Task Creation Error", "List does not exist");
        if (list.Tasks.Any(x => x.Name == command.TaskName))
            return Message.Error("Task Creation Error", "Task with the given task name already exists");
        if (list.VersionId != command.ListVersionId) return Message.Error("Task Creation Error", "Your todo list was already modified, please try again");
        dbContext.Set<TaskEntry>().Add(new TaskEntry(command.TaskName, command.GoalDate, Guid.CreateVersion7(), list.Id));
        list.LastModified = DateTimeOffset.UtcNow;
        list.ChangeVersionId();
        try
        {
            await dbContext.SaveChangesAsync();
            return Message.Success("Task Creation", "Successfully Created Task");
        }
        catch (Exception)
        {
            return Message.Error("Task Creation Error", "There was an unknown error while creating your task");
        }
    }
}