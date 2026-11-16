using Washu.Todo.Persistence;

namespace Washu.Todo.IntegrationTests;

using Application;
using Domain;
using FluentAssertions;
using Framework.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TickerQ.Utilities.Base;
using Xunit;

[Collection(TodoPostgreSqlTestCollection.Name)]
public sealed class DeleteTodoListJobTests(TodoPostgreSqlFixture fixture)
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
    public async Task DeleteTodoListJob_PermanentlyPurgesSoftDeletedLists_AndPreservesActiveLists()
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

        // Execute TickerQ cleanup job directly (only 2 lines changed!)
        var job = new DeleteTodoListJob(factory, NullLogger<DeleteTodoListJob>.Instance);
        await job.ExecuteAsync(new TickerFunctionContext(), CancellationToken.None);

        // Verify hard deletion in Postgres & active list preservation
        await using (var verifyContext = await factory.CreateDbContextAsync())
        {
            var purgedList = await verifyContext.TodoLists.IgnoreQueryFilters().FirstOrDefaultAsync(l => l.Id == softDeletedListId);
            purgedList.Should().BeNull("Soft-deleted list must be permanently removed from PostgreSQL");

            var stillActive = await verifyContext.TodoLists.FirstOrDefaultAsync(l => l.Id == activeListId);
            stillActive.Should().NotBeNull("Active list must be preserved");
        }
    }
}
