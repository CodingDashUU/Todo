namespace Washu.Framework.IntegrationTests;

using System.Security.Claims;
using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;
using ZiggyCreatures.Caching.Fusion;

[Collection(PostgreSqlTestCollection.Name)]
public class HasPermissionHandlerTests(ApplicationPostgreSqlFixture fixture)
{
    private static HttpContextAccessor CreateHttpContext(Guid? userId)
    {
        var context = new DefaultHttpContext();
        if (userId.HasValue)
        {
            var identity = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString())
            ]);
            context.User = new ClaimsPrincipal(identity);
        }
        else
            context.User = new ClaimsPrincipal(new ClaimsIdentity());

        var accessor = new HttpContextAccessor { HttpContext = context };
        return accessor;
    }

    [Fact]
    public async Task HandleAsync_WhenHttpContextIsNull_ReturnsFalse()
    {
        var factory = fixture.CreateDbContextFactory();
        using var cache = new FusionCache(new FusionCacheOptions());
        var accessor = new HttpContextAccessor { HttpContext = null };
        var handler = new HasPermissionHandler(factory, accessor, cache);

        var result = await handler.HandleAsync(new HasPermissionQuery("AnyPermission"));

        result.Should().BeFalse("Unauthenticated request with null HttpContext should not have permissions");
    }

    [Fact]
    public async Task HandleAsync_WhenUserIdClaimMissing_ReturnsFalse()
    {
        var factory = fixture.CreateDbContextFactory();
        using var cache = new FusionCache(new FusionCacheOptions());
        var accessor = CreateHttpContext(userId: null);
        var handler = new HasPermissionHandler(factory, accessor, cache);

        var result = await handler.HandleAsync(new HasPermissionQuery("AnyPermission"));

        result.Should().BeFalse("Unauthenticated request without UserId claim should not have permissions");
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasNoRoles_ReturnsFalse()
    {
        var factory = fixture.CreateDbContextFactory();
        await using var db = await factory.CreateDbContextAsync();
        using var cache = new FusionCache(new FusionCacheOptions());

        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var accessor = CreateHttpContext(user.Id);
        var handler = new HasPermissionHandler(factory, accessor, cache);

        var result = await handler.HandleAsync(new HasPermissionQuery("CanDoAnything"));

        result.Should().BeFalse("User with no assigned roles should have no permissions");
    }

    [Fact]
    public async Task HandleAsync_WhenUserHasRoleWithPermission_ReturnsTrue()
    {
        var factory = fixture.CreateDbContextFactory();
        await using var db = await factory.CreateDbContextAsync();
        using var cache = new FusionCache(new FusionCacheOptions());

        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");
        db.Users.Add(user);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        var permName = $"Perm_{Guid.CreateVersion7():N}";
        db.RolePermissions.Add(new RolePermission(permName, role.Id));
        await db.SaveChangesAsync();

        var accessor = CreateHttpContext(user.Id);
        var handler = new HasPermissionHandler(factory, accessor, cache);

        var hasPermission = await handler.HandleAsync(new HasPermissionQuery(permName));
        var hasOtherPermission = await handler.HandleAsync(new HasPermissionQuery("NonExistentPermission"));

        hasPermission.Should().BeTrue("User has the specified role permission");
        hasOtherPermission.Should().BeFalse("User does not have non-assigned permission");
    }

    [Fact]
    public async Task HandleAsync_CombinesPermissionsFromMultipleRoles()
    {
        var factory = fixture.CreateDbContextFactory();
        await using var db = await factory.CreateDbContextAsync();
        using var cache = new FusionCache(new FusionCacheOptions());

        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        var role1 = new ApplicationRole($"Role1_{Guid.CreateVersion7():N}");
        var role2 = new ApplicationRole($"Role2_{Guid.CreateVersion7():N}");
        db.Users.Add(user);
        db.Roles.AddRange(role1, role2);
        await db.SaveChangesAsync();

        db.UserRoles.AddRange(
            new UserRole { UserId = user.Id, RoleId = role1.Id },
            new UserRole { UserId = user.Id, RoleId = role2.Id }
        );
        var perm1 = $"PermA_{Guid.CreateVersion7():N}";
        var perm2 = $"PermB_{Guid.CreateVersion7():N}";
        db.RolePermissions.AddRange(
            new RolePermission(perm1, role1.Id),
            new RolePermission(perm2, role2.Id)
        );
        await db.SaveChangesAsync();

        var accessor = CreateHttpContext(user.Id);
        var handler = new HasPermissionHandler(factory, accessor, cache);

        (await handler.HandleAsync(new HasPermissionQuery(perm1))).Should().BeTrue();
        (await handler.HandleAsync(new HasPermissionQuery(perm2))).Should().BeTrue();
        (await handler.HandleAsync(new HasPermissionQuery("UnknownPerm"))).Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_CachesPermissions_AndServesFromCacheOnSubsequentCalls()
    {
        var factory = fixture.CreateDbContextFactory();
        await using var db = await factory.CreateDbContextAsync();
        using var cache = new FusionCache(new FusionCacheOptions());

        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");
        db.Users.Add(user);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        var permName = $"Perm_{Guid.CreateVersion7():N}";
        db.RolePermissions.Add(new RolePermission(permName, role.Id));
        await db.SaveChangesAsync();

        var accessor = CreateHttpContext(user.Id);
        var handler = new HasPermissionHandler(factory, accessor, cache);

        // First call populates cache
        var firstResult = await handler.HandleAsync(new HasPermissionQuery(permName));
        firstResult.Should().BeTrue();

        // Remove permission directly from DB to prove second call hits cache
        await using (var deleteContext = await factory.CreateDbContextAsync())
        {
            var p = await deleteContext.RolePermissions.FirstAsync(rp => rp.Name == permName);
            deleteContext.RolePermissions.Remove(p);
            await deleteContext.SaveChangesAsync();
        }

        var secondResult = await handler.HandleAsync(new HasPermissionQuery(permName));
        secondResult.Should().BeTrue("Subsequent check should be served from FusionCache without re-querying deleted DB record");
    }
}
