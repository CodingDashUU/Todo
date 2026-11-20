namespace Washu.Todo.TodoList;

using Components.Dialogs;
using FluentValidation;
using Framework;
using Framework.Identity;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
[UniqueViolationMessage("Todo list with the given name already exists")]
public sealed record CreateListCommand(string ListName) : ICommand<Result>;
public sealed class CreateListHandler(
    IDbContextFactory<TodoDbContext> dbContextFactory,
    CurrentUser user
    ) : ICommandHandler<CreateListCommand, Result>
{
    public async ValueTask<Result> Handle(CreateListCommand command, CancellationToken ctx = new())
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(ctx);
        if (await user.GetUserIdAsync() is not { } userId) return UserErrors.UserIdNotFound;
        dbContext.TodoLists.Add(new TodoList(userId, command.ListName));
        await dbContext.SaveChangesAsync(ctx);
        return Result.Unit;
    }
}

public class CreateListValidator : AbstractValidator<CreateListCommand>
{
    public CreateListValidator() =>
        RuleFor(m => m.ListName)
            .SetValidator(new ListNameValidator());
}