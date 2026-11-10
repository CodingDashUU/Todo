using Washu.Framework.Identity.Users;
using Washu.Todo.Lists.Features;
using Washu.Todo.Persistence;
using Washu.Todo.Tasks;

namespace Washu.Todo.IntegrationTests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

[Collection(TodoPostgreSqlTestCollection.Name)]
public class TodoDbContextConstraintsTests(TodoPostgreSqlFixture fixture)
{
    private static async Task<ApplicationUser> CreateTestUserAsync(TodoDbContext context)
    {
        var guid = Guid.CreateVersion7().ToString("N");
        var user = new ApplicationUser($"User_{guid}", $"{guid}@example.com", $"sub_{guid}");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task UniqueIndex_WithSoftDeleteFilter_AllowsRecreatingListNameAfterSoftDelete()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        var listName = $"Project {Random.Shared.Next(0, 1000)}";

        await using var context = await factory.CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        // 1. Create first list
        var list1 = new TodoList(user.Id, listName);
        context.TodoLists.Add(list1);
        await context.SaveChangesAsync();

        // 2. Attempt duplicate while list1 is active -> UniqueViolation
        var listDuplicate = new TodoList(user.Id, listName);
        context.TodoLists.Add(listDuplicate);
        var actDuplicate = async () => await context.SaveChangesAsync();
        (await actDuplicate.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);

        // Detach failed entry
        context.Entry(listDuplicate).State = EntityState.Detached;

        // 3. Soft delete list1
        context.TodoLists.Remove(list1);
        await context.SaveChangesAsync();

        // 4. Create new list with same name -> Should succeed now
        var list2 = new TodoList(user.Id, listName);
        context.TodoLists.Add(list2);
        var actRecreate = async () => await context.SaveChangesAsync();
        await actRecreate.Should().NotThrowAsync("SoftDelete filter allows recreating list with same name once previous is soft-deleted");

        // Verify both coexist with query filters ignored
        var count = await context.TodoLists.IgnoreQueryFilters().CountAsync(l => l.UserId == user.Id && l.Name == listName);
        count.Should().Be(2);
    }

    [Fact]
    public async Task DateCreatedLTEGoalDateCheckConstraint_RejectsGoalDateEarlierThanDateCreated()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await factory.CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, $"List {Random.Shared.Next(0, 1000)}");
        var task = new TaskEntry("Valid Task", DateTimeOffset.UtcNow.AddDays(2), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        // Attempt direct update setting GoalDate to 1 day before DateCreated
        var saveAction = async () => await context.Tasks
            .Where(t => t.Id == task.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.GoalDate, task.DateCreated.AddDays(-1)));

        (await saveAction.Should().ThrowAsync<PostgresException>())
            .And.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task DateCreatedLTEDateCompletedCheckConstraint_RejectsDateCompletedEarlierThanDateCreated()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await factory.CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, $"List {Random.Shared.Next(0, 1000)}");
        var task = new TaskEntry("Valid Task", DateTimeOffset.UtcNow.AddDays(2), list.Id);
        list.Tasks.Add(task);
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        // Attempt direct update setting DateCompleted to 1 day before DateCreated
        var saveAction = async () => await context.Tasks
            .Where(t => t.Id == task.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.DateCompleted, task.DateCreated.AddDays(-1)));

        (await saveAction.Should().ThrowAsync<PostgresException>())
            .And.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task ListNameMaxLengthCheckConstraint_RejectsNameExceeding64Chars()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await factory.CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var tooLongName = new string('A', 65);
        var invalidList = new TodoList(user.Id, tooLongName);
        context.TodoLists.Add(invalidList);

        var act = async () => await context.SaveChangesAsync();
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task TaskNameMaxLengthCheckConstraint_RejectsNameExceeding64Chars()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await factory.CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, $"List_{Guid.CreateVersion7():N}");
        var tooLongTaskName = new string('B', 65);
        var invalidTask = new TaskEntry(tooLongTaskName, DateTimeOffset.UtcNow.AddDays(1), list.Id);
        list.Tasks.Add(invalidTask);
        context.TodoLists.Add(list);

        var act = async () => await context.SaveChangesAsync();
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task TaskNameUniqueIndex_RejectsDuplicateTaskNameInSameList()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        await using var context = await factory.CreateDbContextAsync();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, $"List {Random.Shared.Next(0, 1000)}");
        var task1 = new TaskEntry("SameTask", DateTimeOffset.UtcNow.AddDays(1), list.Id);
        var task2 = new TaskEntry("SameTask", DateTimeOffset.UtcNow.AddDays(2), list.Id);
        list.Tasks.AddRange(task1, task2);
        context.TodoLists.Add(list);

        var act = async () => await context.SaveChangesAsync();
        (await act.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
}
