namespace Washu.Todo.Commands;

using FluentValidation;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public static class CreateTask
{
    public class Model
    {
        public string TaskName { get; set; } = string.Empty;
        public DateTimeOffset GoalDate { get; set; }
    }
    
    public class Validator : AbstractValidator<Model>
    {
            public Validator(TodoList list)
            {
                RuleFor(x => x.TaskName)
                    .SetValidator(new TodoValidators.TaskName())
                    .Must(m => list.Tasks.All(x => x.Name != m)).WithMessage("Task with the given task name already exists");
                RuleFor(x => x.GoalDate)
                    .SetValidator(new TodoValidators.GoalDate());
            }
    }

    public class Command(IDbContextFactory<TodoDbContext> dbContextFactory)
    {
        public async Task<(Message, TodoList?)> ExecuteAsync(TodoList selectedList, Model model)
        {
            await using var dbContext = await dbContextFactory.CreateDbContextAsync();
            var list = await dbContext.GetListAsync(selectedList.Id);
            if (list is null)
                return (
                    Message.Error("Task Creation Error", "List does not exist"), 
                    list);
            if (list.VersionId != selectedList.VersionId) return (Message.Error("Task Creation Error", "Your todo list was already modified, please try again"), null);
            dbContext.Set<TaskEntry>().Add(new TaskEntry(model.TaskName, model.GoalDate, Guid.CreateVersion7(), list.Id));
            list.LastModified = DateTimeOffset.UtcNow;
            list.ChangeVersionId();
            try
            {
                await dbContext.SaveChangesAsync();
                return (Message.Success("Task Creation", "Successfully Created Task"), list);
            }
            catch (Exception)
            {
                return (Message.Error("Task Creation Error", "There was an unknown error while creating your task"), null);
            }
        }
    }
}
