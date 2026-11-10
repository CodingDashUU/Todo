using Washu.Framework.Identity.Roles;
using Washu.Framework.Identity.Users;
using Washu.Framework.Identity.Users.DeleteUser;

namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class SoftDeleteAndTaskIntegrationTests(ApplicationPostgreSqlFixture fixture)
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
            ignoredFilterUser!.IsDeleted.Should().BeTrue("SoftDeletableEntityInterceptor must set IsDeleted to true");
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
    public async Task SoftDeleteFilterOnUniqueIndex_AllowsRecreatingUserWithSameUsernameAndEmail()
    {
        var factory = fixture.CreateDbContextFactory();
        var guid = Guid.CreateVersion7().ToString("N");
        var username = $"ReuseUser_{guid}";
        var email = $"reuse_{guid}@example.com";
        var sub1 = $"sub1_{guid}";
        var sub2 = $"sub2_{guid}";

        var user1 = new ApplicationUser(username, email, sub1);

        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Users.Add(user1);
            await context.SaveChangesAsync();
        }

        // Soft-delete user1
        await using (var context = await factory.CreateDbContextAsync())
        {
            var u1 = await context.Users.FirstAsync(u => u.Id == user1.Id);
            context.Users.Remove(u1);
            await context.SaveChangesAsync();
        }

        // Create new user2 with identical username and email
        var user2 = new ApplicationUser(username, email, sub2);
        await using (var context = await factory.CreateDbContextAsync())
        {
            context.Users.Add(user2);
            var saveAct = async () => await context.SaveChangesAsync();
            await saveAct.Should().NotThrowAsync("SoftDelete filter on unique index permits re-registration of the same username/email");
        }

        await using (var verifyContext = await factory.CreateDbContextAsync())
        {
            var activeUser = await verifyContext.Users.SingleAsync(u => u.Username == username);
            activeUser.Id.Should().Be(user2.Id);
            activeUser.GoogleSubject.Should().Be(sub2);

            var totalUsersWithSameName = await verifyContext.Users.IgnoreQueryFilters().CountAsync(u => u.Username == username);
            totalUsersWithSameName.Should().Be(2, "Both soft-deleted and active users coexist in table");
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
        var task = new DeleteUserTask();
        (string EntityName, int RowsDeleted) result;
        await using (var context = await factory.CreateDbContextAsync()) result = await task.ExecuteAsync(context);

        result.EntityName.Should().Be(nameof(ApplicationUser));
        result.RowsDeleted.Should().BeGreaterThanOrEqualTo(1);

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
