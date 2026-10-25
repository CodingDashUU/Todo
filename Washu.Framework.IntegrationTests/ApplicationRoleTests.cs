namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Collection(PostgreSqlTestCollection.Name)]
public class ApplicationRoleTests(ApplicationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task CanInsertAndRetrieveRole()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var role = new ApplicationRole($"User_{Guid.CreateVersion7():N}");
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();
        
        var received = await context.Roles.FirstOrDefaultAsync(r => r.Id == role.Id);
        received.Should().NotBeNull($"The role, {role.Name} should be in the database at this point");
        received!.Name.Should().Be(role.Name, "The name of the role found should match the name of the role we inserted");
    }

    [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicateName()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var name = $"User_{Guid.CreateVersion7():N}";
        var role = new ApplicationRole(name.ToUpperInvariant());
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        
        var sameRole = new ApplicationRole(name.ToLowerInvariant());
        context.Roles.Add(sameRole);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation, "The role with the given name already exists");
    }

    [Fact]
    public async Task MaxLengthCheckConstraint_RejectsNameExceeding128Characters()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var tooLongName = new string('A', 129);
        var role = new ApplicationRole(tooLongName);
        context.Roles.Add(role);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "The role name exceeds the 128 character max length check constraint");
    }

    [Fact]
    public async Task CanDeleteRole_RemovesRoleFromDatabase()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();
        var role = new ApplicationRole($"Role_{Guid.CreateVersion7():N}");
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        context.Roles.Remove(role);
        await context.SaveChangesAsync();

        var exists = await context.Roles.AnyAsync(r => r.Id == role.Id);
        exists.Should().BeFalse("The deleted role should no longer exist in the database");
    }
}