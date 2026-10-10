namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Collection(PostgreSqlTestCollection.Name)]
public class ApplicationRoleTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CanInsertAndRetrieveRole()
    {
        await using var context = fixture.CreateDbContext();
        var role = new ApplicationRole($"User_{Guid.CreateVersion7():N}");
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();
        
        var received = await context.Roles.FirstOrDefaultAsync(r => r.Id == role.Id);
        received.Should().NotBeNull();
        received.Name.Should().Be(role.Name);
    }
    [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicateName()
    {
        await using var context = fixture.CreateDbContext();
        var name = $"User_{Guid.CreateVersion7():N}";
        var role = new ApplicationRole(name.ToUpperInvariant());
        context.Roles.Add(role);
        await context.SaveChangesAsync();
        
        var sameRole = new ApplicationRole(name.ToLowerInvariant());
        context.Roles.Add(sameRole);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
    
}