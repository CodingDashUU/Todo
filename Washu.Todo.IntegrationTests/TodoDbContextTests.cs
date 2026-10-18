namespace Washu.Todo.IntegrationTests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Washu.Framework.Identity.Entities;
using Washu.Framework.IntegrationTests.Fixtures;
using Washu.Todo.Identity;

[Collection(TodoPostgreSqlTestCollection.Name)]
public class TodoDbContextTests(PostgreSqlFixture fixture)
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
    public async Task CanInsertAndRetrieveTodoListWithTasks()
    {
        await using var context = fixture.CreateDbContext<TodoDbContext>();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Groceries");
        var task = new TaskEntry("Milk", DateTimeOffset.UtcNow.AddDays(1), Guid.CreateVersion7(), list.Id);
        list.Tasks.Add(task);

        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        await using var verifyContext = fixture.CreateDbContext<TodoDbContext>();
        var retrievedList = await verifyContext.TodoLists
            .Include(l => l.Tasks)
            .FirstOrDefaultAsync(l => l.Id == list.Id);

        retrievedList.Should().NotBeNull();
        retrievedList!.Name.Should().Be("Groceries");
        retrievedList.Tasks.Should().HaveCount(1);
        retrievedList.Tasks[0].Name.Should().Be("Milk");
    }

    [Fact]
    public async Task CascadeDelete_WhenTodoListDeleted_DeletesAssociatedTasks()
    {
        await using var context = fixture.CreateDbContext<TodoDbContext>();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Work");
        var task = new TaskEntry("Report", DateTimeOffset.UtcNow.AddDays(2), Guid.CreateVersion7(), list.Id);
        list.Tasks.Add(task);

        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        context.TodoLists.Remove(list);
        await context.SaveChangesAsync();

        await using var verifyContext = fixture.CreateDbContext<TodoDbContext>();
        var taskExists = await verifyContext.Tasks.AnyAsync(t => t.Id == task.Id);
        taskExists.Should().BeFalse("Associated tasks should be cascaded when TodoList is deleted");
    }

    [Fact]
    public async Task ListNameCheckConstraint_RejectsDisallowedCharacters()
    {
        await using var context = fixture.CreateDbContext<TodoDbContext>();
        var user = await CreateTestUserAsync(context);

        var invalidList = new TodoList(user.Id, "Invalid*Name");
        context.TodoLists.Add(invalidList);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithInnerException<DbUpdateException, PostgresException>()
            .Where(ex => ex.ConstraintName == "CK_TodoList_Name_AllowedChar");
    }

    [Fact]
    public async Task TaskNameCheckConstraint_RejectsDisallowedCharacters()
    {
        await using var context = fixture.CreateDbContext<TodoDbContext>();
        var user = await CreateTestUserAsync(context);

        var list = new TodoList(user.Id, "Daily");
        context.TodoLists.Add(list);
        await context.SaveChangesAsync();

        var invalidTask = new TaskEntry("Invalid^Task", DateTimeOffset.UtcNow.AddDays(1), Guid.CreateVersion7(), list.Id);
        context.Tasks.Add(invalidTask);

        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>()
            .WithInnerException<DbUpdateException, PostgresException>()
            .Where(ex => ex.ConstraintName == "CK_TaskEntry_Name_AllowedChar");
    }
}
