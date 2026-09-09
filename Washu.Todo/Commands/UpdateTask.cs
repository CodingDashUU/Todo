namespace Washu.Todo.Commands;

using FluentValidation;
using Framework.Notifications;
using Identity;
using Microsoft.EntityFrameworkCore;

public static class UpdateTask
{
    public class Model
    {
        public string NewTaskName { get; set; } = string.Empty;
        public DateTimeOffset NewGoalDate { get; set; }
    }

    public class Validator : AbstractValidator<Model>
    {
        public Validator(TaskEntry oldEntry)
        {
            RuleFor(m => m.NewTaskName)
                .SetValidator(new TodoValidators.TaskName())
                .Unless(m => oldEntry.Name == m.NewTaskName);
            RuleFor(m => m.NewGoalDate)
                .SetValidator(new TodoValidators.GoalDate())
                .Unless(m => oldEntry.GoalDate.ToUniversalTime() == m.NewGoalDate.ToUniversalTime());
        }
            
    }
        public class Command(IDbContextFactory<TodoDbContext> factory)
    {
        public async Task<(Message, TodoList?)> ExecuteAsync(Model model, TodoList selectedList, TaskEntry entry)
        {
            await using var dbContext = await factory.CreateDbContextAsync();
            var list = await dbContext.TodoLists
                .FirstOrDefaultAsync(l => l.Id == selectedList.Id);
            if (list is null) return (Message.Error(
                title: "Task Modification Error", 
                details: "List does not exist"), null);
            if (list.VersionId != selectedList.VersionId) 
                return (Message.Error("Task Update Error", "Your todo list was already modified, please try again"), null);
            var item = list.Tasks.FirstOrDefault(t => t.Id == entry.Id);
            if (item is null) return (Message.Error(
                title: "Task Modification Error", 
                details: "Task does not exist"), null);
            var taskNameChanged = model.NewTaskName != entry.Name;
            var goalDateChanged = model.NewGoalDate != entry.GoalDate;
            if (taskNameChanged)
            {
                if (list.Tasks.Any(x => x.Name == model.NewTaskName))
                    return (Message.Error(
                            title: "Task Modification Error", 
                            details: "Task with the provided task name already exists"), 
                        null);
                item.Name = model.NewTaskName;
            }
            if (goalDateChanged) item.ChangeGoalDate(model.NewGoalDate);
            if (taskNameChanged || goalDateChanged) list.LastModified = DateTimeOffset.UtcNow;
            else return (Message.Info(
                    title: "Task Modification Info", 
                    details: "Task has not been modified"), 
                null);
            list.ChangeVersionId();
            dbContext.TodoLists.Update(list);
            try
            {
                await dbContext.SaveChangesAsync();
                return (Message.Success(
                        title: "Task Modification",
                        details: "Successfully modified the task"),
                    list);
            }
            catch (Exception)
            {
                return (Message.Error("Task Update Error", "There was an unknown error while updating your task"), null);
            }
        }
    }
}