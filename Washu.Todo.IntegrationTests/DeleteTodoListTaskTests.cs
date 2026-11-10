using Washu.Framework.Identity.Users;
using Washu.Todo.Lists;
using Washu.Todo.Lists.Features;
using Washu.Todo.Persistence;

namespace Washu.Todo.IntegrationTests;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

[Collection(TodoPostgreSqlTestCollection.Name)]
public class DeleteTodoListTaskTests(TodoPostgreSqlFixture fixture)
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
    public async Task DeleteTodoListTask_PermanentlyPurgesSoftDeletedLists_AndPreservesActiveLists()
    {
        var factory = fixture.CreateDbContextFactory<TodoDbContext>();
        Guid activeListId;
        Guid softDeletedListId;

        await using (var context = await factory.CreateDbContextAsync())
        {
            var user = await CreateTestUserAsync(context);

            var activeList = new TodoList(user.Id, $"Active {Random.Shared.Next(1, 1000)}");
            var softDeletedList = new TodoList(user.Id, $"SoftDel {Random.Shared.Next(1, 1000)}");

            context.TodoLists.AddRange(activeList, softDeletedList);
            await context.SaveChangesAsync();

            activeListId = activeList.Id;
            softDeletedListId = softDeletedList.Id;
        }

        // Soft delete the second list
        await using (var context = await factory.CreateDbContextAsync())
        {
            var listToDelete = await context.TodoLists.FirstAsync(l => l.Id == softDeletedListId);
            context.TodoLists.Remove(listToDelete);
            await context.SaveChangesAsync();
        }

        // Verify it is soft-deleted
        await using (var verifyContext = await factory.CreateDbContextAsync())
        {
            var visible = await verifyContext.TodoLists.AnyAsync(l => l.Id == softDeletedListId);
            visible.Should().BeFalse("Soft-deleted list should be filtered out by default");

            var hidden = await verifyContext.TodoLists.IgnoreQueryFilters().FirstAsync(l => l.Id == softDeletedListId);
            hidden.IsDeleted.Should().BeTrue();
        }

        // Execute background cleanup task
        var task = new DeleteTodoListTask();
        (string EntityName, int RowsDeleted) result;
        await using (var context = await factory.CreateDbContextAsync()) result = await task.ExecuteAsync(context);

        result.EntityName.Should().Be(nameof(TodoList));
        result.RowsDeleted.Should().BeGreaterThanOrEqualTo(1);

        // Verify hard deletion in Postgres
        await using (var verifyContext = await factory.CreateDbContextAsync())
        {
            var purgedList = await verifyContext.TodoLists.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == softDeletedListId);
            purgedList.Should().BeNull("Soft-deleted list must be permanently removed from the database by DeleteTodoListTask");

            var stillActive = await verifyContext.TodoLists.FirstOrDefaultAsync(l => l.Id == activeListId);
            stillActive.Should().NotBeNull("Active list must be preserved");
        }
    }
}
