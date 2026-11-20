namespace Washu.Todo.TodoList;

using FluentValidation;
using Framework;
using Framework.Identity;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Persistence;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;
using Washu.Framework.Notifications;
using Washu.Todo.Extensions;

[UniqueViolationMessage("Todo list with the given name already exists")]
public sealed record UpdateListCommand(TodoList OldList, string NewListName) : ICommand<Result>;
public sealed class UpdateListHandler(
    IDbContextFactory<TodoDbContext> factory,
    CurrentUser user
    ) : ICommandHandler<UpdateListCommand, Result>
{
    public async ValueTask<Result> Handle(UpdateListCommand command, CancellationToken ctx = new())
    {
        if (await user.GetUserIdAsync() is not { } userId) return UserErrors.UserIdNotFound;
        await using var dbContext = await factory.CreateDbContextAsync(ctx);
        var todoListQuery = dbContext.TodoLists
            .ForUser(userId);
        var list = await todoListQuery.ByTodoList(command.OldList.Id, ctx);
        if (list is null)
            return TodoErrors.ListNotFound;
        if (todoListQuery.Any(l => l.Name == command.NewListName && l.Id != command.OldList.Id))
            return TodoErrors.ListAlreadyExists;
        dbContext.UpdateEntity(list, command.OldList.RowVersion);
        list.Name =  command.NewListName;
        await dbContext.SaveChangesAsync(ctx);

        return Result.Unit;
    }
}

public class UpdateListValidator : AbstractValidator<UpdateListCommand>
{
    public UpdateListValidator()
    {
        RuleFor(m => m.NewListName)
            .SetValidator(new ListNameValidator());
        RuleFor(m => m)
            .Must(m => IsDifferent(m.OldList.Name, m.NewListName)).WithMessage("New name must be different from the old name");
    }
        
    private static bool IsDifferent(string oldName, string newName) => oldName != newName;
}