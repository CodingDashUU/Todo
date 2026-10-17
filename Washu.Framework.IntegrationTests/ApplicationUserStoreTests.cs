namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using ZiggyCreatures.Caching.Fusion;

[Collection(PostgreSqlTestCollection.Name)]
public class ApplicationUserStoreTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task GetOrAddUserAsync_CachesUserOnSecondCall()
    {
        var factory = fixture.CreateDbContextFactory();
        await using var dbContext = await factory.CreateDbContextAsync();
        using var cache = new FusionCache(new FusionCacheOptions());
    
        var handler = new FindUserHandler(factory);
        var userStore = new ApplicationUserStore(cache, handler);

        var existingUser = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        dbContext.Users.Add(existingUser);
        await dbContext.SaveChangesAsync();

        var user1 = await userStore.GetOrSetUserAsync(existingUser.Id);
        user1.Should().NotBeNull($"the user, {existingUser.Id} exists in the database");

        // Verify user is in cache
        var user = cache.TryGet<ApplicationUser>($"user:{existingUser.Id}");
        user.HasValue.Should().BeTrue("the user should be stored in the memory cache after the first call");
        user.Value.Should().BeSameAs(user1, "the cached instance should be reference identical to user1");

        // Remove from database to prove second call is purely served from memory cache
        dbContext.Users.Remove(existingUser);
        await dbContext.SaveChangesAsync();

        var user2 = await userStore.GetOrSetUserAsync(existingUser.Id);
        user2.Should().NotBeNull("the user should still be served from cache even if removed from the database");
        user2.Should().BeSameAs(user1, "the second retrieval should return the exact cached instance from memory without querying database");
        user2.Id.Should().Be(existingUser.Id, "the Id of the cached user should match the original user Id");
    }

    [Fact]
    public async Task RemoveUser_EvictsUserFromCache()
    {
        var factory = fixture.CreateDbContextFactory();
        await using var dbContext = await factory.CreateDbContextAsync();
        using var cache = new FusionCache(new FusionCacheOptions());

        var handler = new FindUserHandler(factory);
        var userStore = new ApplicationUserStore(cache, handler);

        var existingUser = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        dbContext.Users.Add(existingUser);
        await dbContext.SaveChangesAsync();

        var user = await userStore.GetOrSetUserAsync(existingUser.Id);
        user.Should().NotBeNull();
        cache.TryGet<ApplicationUser>($"user:{existingUser.Id}").HasValue.Should().BeTrue("user should be cached");

        // Remove from database and evict from cache
        dbContext.Users.Remove(existingUser);
        await dbContext.SaveChangesAsync();

        await userStore.RemoveUserAsync(existingUser.Id);

        cache.TryGet<ApplicationUser>($"user:{existingUser.Id}").HasValue.Should().BeFalse("user should be evicted from cache after RemoveUser call");

        var userAfterEviction = await userStore.GetOrSetUserAsync(existingUser.Id);
        userAfterEviction.Should().BeNull("querying user after eviction and database deletion should return null");
    }

    [Fact]
    public async Task GetOrAddUserAsync_ReturnsNull_WhenUserDoesNotExist()
    {
        using var cache = new FusionCache(new FusionCacheOptions());
        var factory = fixture.CreateDbContextFactory();
        var handler = new FindUserHandler(factory);
        var userStore = new ApplicationUserStore(cache, handler);

        var nonExistentId = Guid.NewGuid();
        var user = await userStore.GetOrSetUserAsync(nonExistentId);

        user.Should().BeNull("a non-existent user Id should return null");
    }
}