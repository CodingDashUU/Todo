namespace Washu.Framework.IntegrationTests;

using Entities;
using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Roles;
using Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using TickerQ.Utilities.Base;
using Xunit;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class SoftDeleteAndJobIntegrationTests(ApplicationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task SoftDeleteInterceptor_SetsIsDeleted_AndHidesUserFromDefaultQueryFilter()
    {
        var factory = fixture.CreateDbContextFactory();
        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        await using (var deleteContext = await factory.CreateDbContextAsync())
        {
            var userToDelete = await deleteContext.Users.FirstAsync(u => u.Id == user.Id);
            deleteContext.Users.Remove(userToDelete);
            await deleteContext.SaveChangesAsync();
        }

        await using (var verifyContext = await factory.CreateDbContextAsync())
        {
            // Default query filter hides soft-deleted user
            var defaultQueryExists = await verifyContext.Users.AnyAsync(u => u.Id == user.Id);
            defaultQueryExists.Should().BeFalse("Soft-deleted user must be filtered out by default query filter");

            // IgnoreQueryFilters shows the user still exists with IsDeleted = true
            var ignoredFilterUser = await verifyContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == user.Id);
            ignoredFilterUser.Should().NotBeNull("User should not be physically removed by SaveChangesAsync");
            ignoredFilterUser.IsDeleted.Should().BeTrue($"SoftDeletableEntityInterceptor must set {nameof(ISoftDeletableEntity.IsDeleted)} to UTC now");
        }
    }

    [Fact]
    public async Task HasUserSoftDeleteQueryFilter_HidesUserRoles_WhenUserIsSoftDeleted()
    {
        var factory = fixture.CreateDbContextFactory();
        var user = new ApplicationUser($"User_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Users.Add(user);
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            await context.SaveChangesAsync();
        }

        // Soft delete user
        await using (var context = await factory.CreateDbContextAsync())
        {
            var userToDelete = await context.Users.FirstAsync(u => u.Id == user.Id);
            context.Users.Remove(userToDelete);
            await context.SaveChangesAsync();
        }

        await using (var verifyContext = await factory.CreateDbContextAsync())
        {
            // UserRole query with filter applied should hide the association
            var roleExistsWithFilter = await verifyContext.UserRoles.AnyAsync(ur => ur.UserId == user.Id);
            roleExistsWithFilter.Should().BeFalse("UserRole query filter should hide associations of soft-deleted users");

            // Ignoring query filters reveals the association still exists
            var roleExistsIgnoringFilter = await verifyContext.UserRoles.IgnoreQueryFilters().AnyAsync(ur => ur.UserId == user.Id);
            roleExistsIgnoringFilter.Should().BeTrue("UserRole association should remain until purged");
        }
    }

    [Fact]
    public async Task DeleteUserTask_HardDeletesSoftDeletedUsers_AndLeavesActiveUsersIntact()
    {
        var factory = fixture.CreateDbContextFactory();
        var activeUser = new ApplicationUser($"Active_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");
        var softDeletedUser = new ApplicationUser($"SoftDel_{Guid.CreateVersion7():N}", $"{Guid.CreateVersion7():N}@example.com", $"sub_{Guid.CreateVersion7():N}");

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Users.AddRange(activeUser, softDeletedUser);
            await context.SaveChangesAsync();
        }

        // Soft delete one user
        await using (var context = await factory.CreateDbContextAsync())
        {
            var u = await context.Users.FirstAsync(x => x.Id == softDeletedUser.Id);
            context.Users.Remove(u);
            await context.SaveChangesAsync();
        }

        // Execute background cleanup task
        var task = new DeleteUserJob(factory, new NullLogger<DeleteUserJob>());
        await task.ExecuteAsync(new TickerFunctionContext());
        // Verify softDeletedUser is completely erased from table
        await using (var verifyContext = await factory.CreateDbContextAsync())
        {
            var purgedUser = await verifyContext.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == softDeletedUser.Id);
            purgedUser.Should().BeNull("Soft-deleted user should be permanently purged by DeleteUserTask");

            var stillActive = await verifyContext.Users.FirstOrDefaultAsync(u => u.Id == activeUser.Id);
            stillActive.Should().NotBeNull("Active user must not be deleted by cleanup task");
        }
    }
}
