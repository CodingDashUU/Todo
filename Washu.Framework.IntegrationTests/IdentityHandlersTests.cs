namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Notifications;
using Xunit;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class IdentityHandlersTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task AddRolesHandler_ShouldInsertNewRolesAndIgnoreDuplicates()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new AddRolesHandler(factory);

        var role1 = $"Role_{Guid.NewGuid():N}";
        var role2 = $"Role_{Guid.NewGuid():N}";

        // First pass: add roles
        await handler.HandleAsync(new AddRolesCommand([role1, role2]));

        // Second pass: include existing role and duplicates with different casing
        await handler.HandleAsync(new AddRolesCommand([role1.ToLowerInvariant(), role1.ToUpperInvariant(), role2.ToLowerInvariant(), role2.ToUpperInvariant()]));

        await using var context = fixture.CreateDbContext();
        var countRole1 = await context.Roles.CountAsync(r => r.Name == role1);
        var countRole2 = await context.Roles.CountAsync(r => r.Name == role2);

        countRole1.Should().Be(1, $"There obviously should not be a duplicate role for {role1}");
        countRole2.Should().Be(1, $"There obviously should not be a duplicate role for {role2}");
    }

    [Fact]
    public async Task FindUserHandler_ShouldReturnUserWhenExists()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new FindUserHandler(factory);

        var username = $"FindUser_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username, $"{username}@example.com", $"sub-{Guid.NewGuid():N}");

        await using (var context = fixture.CreateDbContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var foundUser = await handler.HandleAsync(new FindUserQuery(user.Id));
        foundUser.Should().NotBeNull($"The user, {foundUser.Username} should be in the database at this point");
        foundUser.Id.Should().Be(user.Id, "The Id of the user received should be the same as the user in the database");
        foundUser.Username.Should().Be(username);

        var nonExistent = await handler.HandleAsync(new FindUserQuery(Guid.NewGuid()));
        nonExistent.Should().BeNull();
    }

    [Fact]
    public async Task ChangeUsernameHandler_ShouldUpdateUsernameSuccessfully()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new ChangeUsernameHandler(factory);

        var user = new ApplicationUser($"OldName_{Guid.NewGuid():N}", $"{Guid.NewGuid():N}@example.com", $"sub-{Guid.NewGuid():N}");
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var newUsername = $"NewName_{Guid.NewGuid():N}";
        var result = await handler.HandleAsync(new ChangeUsernameCommand(user.Id, user.VersionId, newUsername));
        result.Should().Be(ChangeUsernameResult.Success);
        await using (var context = fixture.CreateDbContext())
        {
            var updatedUser = await context.Users.FindAsync(user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser.Username.Should().Be(newUsername);
        }
    }

    [Fact]
    public async Task ChangeUsernameHandler_ShouldFailWhenUsernameAlreadyTaken()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new ChangeUsernameHandler(factory);

        var existingUsername = $"Taken_{Guid.NewGuid():N}";
        var user1 = new ApplicationUser(existingUsername, $"{Guid.NewGuid():N}@example.com", $"sub-{Guid.NewGuid():N}");
        var user2 = new ApplicationUser($"Other_{Guid.NewGuid():N}", $"{Guid.NewGuid():N}@example.com", $"sub-{Guid.NewGuid():N}");

        await using (var context = fixture.CreateDbContext())
        {
            context.Users.AddRange(user1, user2);
            await context.SaveChangesAsync();
        }

        var result = await handler.HandleAsync(new ChangeUsernameCommand(user2.Id, user2.VersionId, existingUsername));
        result.Should().Be(ChangeUsernameResult.UsernameTaken, "The handler should fail");
    }
    
    [Fact]
    public async Task ChangeUsernameHandler_ShouldFailWhenUserIsNotFound()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new ChangeUsernameHandler(factory);
        
        var user = new ApplicationUser($"DeletedUser_{Guid.NewGuid():N}", $"{Guid.NewGuid():N}@example.com", $"{Guid.NewGuid():N}");

        await using (var context = fixture.CreateDbContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync();
        }
        var result = await handler.HandleAsync(new ChangeUsernameCommand(user.Id, user.VersionId, $"NewName_{Guid.NewGuid():N}"));
        result.Should().Be(ChangeUsernameResult.AccountNotFound, "The handler should fail");
    }

    [Fact] public async Task ChangeUsernameHandler_ShouldFailWhenUserAlreadyModified()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new ChangeUsernameHandler(factory);

        var username1 = $"User_{Guid.NewGuid():N}";
        var user = new ApplicationUser(username1, $"{Guid.NewGuid():N}@example.com", $"sub-{Guid.NewGuid():N}");

        await using (var context = fixture.CreateDbContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        var username2 = $"User2_{Guid.NewGuid():N}";
        await handler.HandleAsync(new ChangeUsernameCommand(user.Id, user.VersionId, username2));
        var username3 = $"User3_{Guid.NewGuid():N}";
        var result = await handler.HandleAsync(new ChangeUsernameCommand(user.Id, user.VersionId, username3));
        result.Should().Be(ChangeUsernameResult.UserAlreadyModified, "The handler should fail");
    }
    [Fact]
    public async Task DeleteUserHandler_ShouldRemoveUserFromDatabase()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new DeleteUserHandler(factory);

        var user = new ApplicationUser($"DelUser_{Guid.NewGuid():N}", $"{Guid.NewGuid():N}@example.com", $"sub-{Guid.NewGuid():N}");
        await using (var context = fixture.CreateDbContext())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        var result = await handler.HandleAsync(new DeleteUserCommand(user.Id));
        result.Should().Be(DeleteUserResult.Success, "The handler should succeed");

        await using (var context = fixture.CreateDbContext())
        {
            var exists = await context.Users.AnyAsync(u => u.Id == user.Id);
            exists.Should().BeFalse($"The user, {user.Username} should be deleted");
        }
    }

    [Fact]
    public async Task DeleteUserHandler_ShouldReturnError_WhenUserDoesNotExist()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new DeleteUserHandler(factory);

        var nonExistentId = Guid.NewGuid();
        var result = await handler.HandleAsync(new DeleteUserCommand(nonExistentId));

        result.Should().Be(DeleteUserResult.AccountNotFound, "The handler should report error when account does not exist");
    }

    [Fact]
    public async Task DeleteUserHandler_ShouldCascadeDeleteUserRoles_WhenUserHasRoles()
    {
        var factory = fixture.CreateDbContextFactory();
        var handler = new DeleteUserHandler(factory);

        var user = new ApplicationUser($"CascadeDelUser_{Guid.NewGuid():N}", $"{Guid.NewGuid():N}@example.com", $"sub-{Guid.NewGuid():N}");
        var role = new ApplicationRole($"CascadeRole_{Guid.NewGuid():N}");

        await using (var context = fixture.CreateDbContext())
        {
            context.Users.Add(user);
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();
        }

        await handler.HandleAsync(new DeleteUserCommand(user.Id));

        await using (var context = fixture.CreateDbContext())
        {
            var userExists = await context.Users.AnyAsync(u => u.Id == user.Id);
            userExists.Should().BeFalse("The user should be deleted from the database");

            var userRoleExists = await context.UserRoles.AnyAsync(ur => ur.UserId == user.Id);
            userRoleExists.Should().BeFalse("Associated UserRoles should be cascade deleted");
        }
    }
}
