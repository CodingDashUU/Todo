namespace Washu.Todo.Tasks;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence;
using Washu.Framework.Extensions;
using Framework.Identity.Users;
using Extensions;
using FluentValidation;
using Framework;
using Framework.Identity;
using TodoList;

[UniqueViolationMessage("Task with the given name already exists")]
public sealed record CreateTaskCommand(TodoList List, string TaskName, DateTimeOffset GoalDate) : ICommand<Result>;

public sealed class CreateTaskHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    CurrentUser user
    ) : ICommandHandler<CreateTaskCommand, Result>
{
    public async ValueTask<Result> Handle(CreateTaskCommand command, CancellationToken ctx = new())
    {
        if (await user.GetUserIdAsync() is not { } userId) return UserErrors.UserIdNotFound;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ctx);
        var list = await dbContext.TodoLists
            .ForUser(userId)
            .ByTodoList(command.List.Id, ctx);
        if (list is null)
            return TodoErrors.ListNotFound;
        var count = await dbContext.Tasks.ForTodoList(command.List.Id).CountAsync(ctx);
        if (count >= 1000) 
            return TodoErrors.ListTooLong;
        dbContext.UpdateEntity(list, command.List.RowVersion);
        dbContext.Tasks.Add(new TaskEntry(command.TaskName, command.GoalDate, list.Id));
        await dbContext.SaveChangesAsync(ctx);
        return Result.Unit;
    }
}
public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.TaskName)
            .SetValidator(new TaskNameValidator());
        RuleFor(x => x.GoalDate)
            .SetValidator(new GoalDateValidator());
    }
}
