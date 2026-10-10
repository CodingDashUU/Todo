namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Collection(PostgreSqlTestCollection.Name)]
public class UserRoleTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CanAssociateUserWithRole()
    {
        await using var context = fixture.CreateDbContext();

        var user = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}", 
            $"{Guid.CreateVersion7():N}@example.com", 
            $"{Guid.CreateVersion7():N}");
        var role = new ApplicationRole($"User_{Guid.CreateVersion7():N}");

        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();

        var exists = await context.UserRoles
            .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveRole_CascadesDeleteToUserRoles()
    {
        await using var context = fixture.CreateDbContext();
        var role1 = new ApplicationRole($"Admin_{Guid.CreateVersion7():N}");
        var role2 = new ApplicationRole($"User_{Guid.CreateVersion7():N}");
        var guid1 = $"{Guid.CreateVersion7():N}";
        var guid2 = $"{Guid.CreateVersion7():N}";
        var user1 = new ApplicationUser($"User_{guid1}", $"{guid1}@example.com", $"{Guid.CreateVersion7():N}");
        var user2 = new ApplicationUser($"User_{guid2}", $"{guid2}@example.com", $"{Guid.CreateVersion7():N}");
        context.Roles.Add(role1);
        context.Roles.Add(role2);
        context.Users.Add(user1);
        context.Users.Add(user2);
        context.UserRoles.Add(new UserRole { UserId = user1.Id, RoleId = role1.Id });
        context.UserRoles.Add(new UserRole { UserId = user2.Id, RoleId = role2.Id });
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();
        context.Roles.Remove(role1);
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();
        var remainingUserRolesWithDeletedRole = await context.UserRoles.CountAsync(ur => ur.RoleId == role1.Id);
        remainingUserRolesWithDeletedRole.Should().Be(0);
    }
    [Fact]
    public async Task RemoveUser_CascadesDeleteToUserRoles()
    {
        await using var context = fixture.CreateDbContext();
        var role1 = new ApplicationRole($"Admin_{Guid.CreateVersion7():N}");
        var role2 = new ApplicationRole($"User_{Guid.CreateVersion7():N}");
        var guid1 = $"{Guid.CreateVersion7():N}";
        var guid2 = $"{Guid.CreateVersion7():N}";
        var user1 = new ApplicationUser($"User_{guid1}", $"{guid1}@example.com", $"{Guid.CreateVersion7():N}");
        var user2 = new ApplicationUser($"User_{guid2}", $"{guid2}@example.com", $"{Guid.CreateVersion7():N}");
        context.Roles.Add(role1);
        context.Roles.Add(role2);
        context.Users.Add(user1);
        context.Users.Add(user2);
        context.UserRoles.Add(new UserRole { UserId = user1.Id, RoleId = role1.Id });
        context.UserRoles.Add(new UserRole { UserId = user1.Id, RoleId = role2.Id });
        context.UserRoles.Add(new UserRole { UserId = user2.Id, RoleId = role1.Id });
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();
        context.Users.Remove(user1);
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();
        var remainingUserRolesWithDeletedUser = await context.UserRoles.CountAsync(ur => ur.UserId == user1.Id);
        remainingUserRolesWithDeletedUser.Should().Be(0);
    }
    [Fact]
    public async Task UniqueConstraint_RejectDuplicateRole()
    {
        await using var context = fixture.CreateDbContext();

        var user = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}", 
            $"{Guid.CreateVersion7():N}@example.com", 
            $"{Guid.CreateVersion7():N}");
        var role = new ApplicationRole($"User_{Guid.CreateVersion7():N}");

        context.Users.Add(user);
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var userRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        context.UserRoles.Add(userRole);
        await context.SaveChangesAsync();
        
        context.ChangeTracker.Clear();
        var sameUserRole = new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        };
        context.UserRoles.Add(sameUserRole);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
}