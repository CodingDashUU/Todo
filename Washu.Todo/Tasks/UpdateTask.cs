namespace Washu.Todo.Tasks;

using FluentValidation;
using Framework;
using Framework.Identity;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence;
using TodoList;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;

[UniqueViolationMessage("Task with the given name already exists")]
public sealed record UpdateTaskCommand(
    TaskEntry Task,
    string NewTaskName,
    DateTimeOffset NewGoalDate,
    TodoList List) : ICommand<Result>;
public sealed class UpdateTaskHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<UpdateTaskCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateTaskCommand command, CancellationToken ctx = new())
    {
        if (await user.GetUserIdAsync() is not { } userId) return UserErrors.UserIdNotFound;
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists.ForUser(userId).ByTodoList(command.List.Id, ctx);
        var task = await  dbContext.Tasks.ForTodoList(command.List.Id).ByTask(command.Task.Id, ctx);
        if (list is null)
            return TodoErrors.ListNotFound;
        if (task is null) return TodoErrors.TaskNotFound;
        var taskNameChanged = command.NewTaskName != task.Name;
        var goalDateChanged = command.NewGoalDate != task.GoalDate;
        if (taskNameChanged) task.Name = command.NewTaskName;
        if (goalDateChanged) task.ChangeGoalDate(command.NewGoalDate);
        if (taskNameChanged || goalDateChanged)
            dbContext.UpdateEntity(list, command.List.RowVersion);
        else return Result.Unit;
        await dbContext.SaveChangesAsync(ctx);
        return Result.Unit;
    }
}

public class UpdateTaskValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskValidator()
    {
        RuleFor(m => m.NewTaskName)
            .SetValidator(new TaskNameValidator())
            .Unless(m => m.Task.Name == m.NewTaskName);
        RuleFor(m => m.NewGoalDate)
            .SetValidator(new GoalDateValidator())
            .Unless(m => m.Task.GoalDate == m.NewGoalDate);
        RuleFor(m => m)
            .Must(m => m.Task.GoalDate != m.NewGoalDate || m.Task.Name != m.NewTaskName)
            .WithMessage("You have not edited anything. Please edit one of the fields above");
    }
}