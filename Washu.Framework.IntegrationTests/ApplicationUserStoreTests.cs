namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;

[Collection(PostgreSqlTestCollection.Name)]
public class ApplicationUserStoreTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task GetOrAddUserAsync_CachesUserOnSecondCall()
    {
        var factory = fixture.CreateDbContextFactory();
        await using var dbContext = await factory.CreateDbContextAsync();
        using var cache = new MemoryCache(new MemoryCacheOptions());
    
        var handler = new FindUserHandler(factory);
        var userStore = new ApplicationUserStore(cache, handler);

        var existingUser = new ApplicationUser($"User_{Guid.CreateVersion7():N}", "test@example.com", "sub_123");
        dbContext.Users.Add(existingUser);
        await dbContext.SaveChangesAsync();
        var watch = Stopwatch.StartNew();
        var user1 = await userStore.GetOrAddUserAsync(existingUser.Id);
        watch.Stop();
        dbContext.ChangeTracker.Clear();
        var watch2 = Stopwatch.StartNew();
        var user2 = await userStore.GetOrAddUserAsync(existingUser.Id);
        watch2.Stop();
        
        watch2.ElapsedMilliseconds.Should().BeLessThan(watch.ElapsedMilliseconds, "The time to takes to access data from a cache should be faster than the time it takes to access data from a database");
        user1.Should().NotBeNull($"The user, {existingUser.Id} exists");
        user2.Should().NotBeNull($"The user, {existingUser.Id} exists");
        user2.Id.Should().Be(existingUser.Id, "The Id of the user received should be the same as the user created");
    }
}