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

        countRole1.Should().Be(1);
        countRole2.Should().Be(1);
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
        foundUser.Should().NotBeNull();
        foundUser.Id.Should().Be(user.Id);
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
        var result = await handler.HandleAsync(new ChangeUsernameCommand(user.Id, newUsername));
        result.Type.Should().Be(MessageType.Success);
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

        var result = await handler.HandleAsync(new ChangeUsernameCommand(user2.Id, existingUsername));
        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Be("Username provided is already taken");
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
        var result = await handler.HandleAsync(new ChangeUsernameCommand(user.Id, $"NewName_{Guid.NewGuid():N}"));
        result.Type.Should().Be(MessageType.Error);
        result.Details.Should().Be("Account was not found");
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
        result.Type.Should().Be(MessageType.Success);

        await using (var context = fixture.CreateDbContext())
        {
            var exists = await context.Users.AnyAsync(u => u.Id == user.Id);
            exists.Should().BeFalse();
        }
    }
}
