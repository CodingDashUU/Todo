namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

[Collection(PostgreSqlTestCollection.Name)]
public class RolePermissionTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CanInsertAndRetrieveRolePermission()
    {
        await using var context = fixture.CreateDbContext();

        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var permName = $"Perm_{Guid.CreateVersion7():N}";
        var permission = new RolePermission(permName, role.Id);
        context.RolePermissions.Add(permission);
        await context.SaveChangesAsync();

        var retrieved = await context.RolePermissions
            .Include(rp => rp.Role)
            .FirstOrDefaultAsync(rp => rp.Id == permission.Id);

        retrieved.Should().NotBeNull();
        retrieved!.Name.Should().Be(permName);
        retrieved.RoleId.Should().Be(role.Id);
        retrieved.Role.Should().NotBeNull();
        retrieved.Role.Name.Should().Be(role.Name);
    }

    [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicatePermissionName()
    {
        await using var context = fixture.CreateDbContext();

        var role1 = new ApplicationRole($"Role1_{Guid.CreateVersion7():N}");
        var role2 = new ApplicationRole($"Role2_{Guid.CreateVersion7():N}");
        context.Roles.AddRange(role1, role2);
        await context.SaveChangesAsync();

        var baseName = $"Permission_{Guid.CreateVersion7():N}";
        var perm1 = new RolePermission(baseName.ToUpperInvariant(), role1.Id);
        context.RolePermissions.Add(perm1);
        await context.SaveChangesAsync();

        var perm2 = new RolePermission(baseName.ToLowerInvariant(), role2.Id);
        context.RolePermissions.Add(perm2);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("Duplicate permission name should violate unique constraint"))
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    [Fact]
    public async Task MaxLengthCheckConstraint_RejectsNameExceeding64Characters()
    {
        await using var context = fixture.CreateDbContext();

        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var tooLongName = new string('P', 65);
        var perm = new RolePermission(tooLongName, role.Id);
        context.RolePermissions.Add(perm);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("Permission name exceeding 64 characters should violate check constraint"))
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task ForeignKeyConstraint_RejectsNonExistentRoleId()
    {
        await using var context = fixture.CreateDbContext();

        var nonExistentRoleId = Guid.CreateVersion7();
        var perm = new RolePermission($"Perm_{Guid.CreateVersion7():N}", nonExistentRoleId);
        context.RolePermissions.Add(perm);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("Non-existent RoleId should violate foreign key constraint"))
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task CascadeDelete_WhenRoleDeleted_DeletesAssociatedRolePermissions()
    {
        await using var context = fixture.CreateDbContext();

        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        var perm1 = new RolePermission($"Perm1_{Guid.CreateVersion7():N}", role.Id);
        var perm2 = new RolePermission($"Perm2_{Guid.CreateVersion7():N}", role.Id);
        context.RolePermissions.AddRange(perm1, perm2);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        context.Roles.Remove(role);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var remainingPerms = await context.RolePermissions.CountAsync(rp => rp.RoleId == role.Id);
        remainingPerms.Should().Be(0, "All RolePermissions associated with the deleted role must be cascade-deleted");
    }
}
