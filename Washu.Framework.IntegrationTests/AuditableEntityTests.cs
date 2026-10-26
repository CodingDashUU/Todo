namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

[Collection(PostgreSqlTestCollection.Name)]
public class AuditableEntityTests(ApplicationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task AddEntity_ShouldUpdateCreationDateToNow()
    {
        var username = $"User_{Guid.NewGuid()}";
        var email = $"Email_{Guid.NewGuid()}@example.com";
        var googleSubject = $"sub-{Guid.NewGuid()}";
        var entity = new ApplicationUser(username, email, googleSubject);
        var dbContextFactory = fixture.CreateDbContextFactory();
        var now = DateTimeOffset.Now;
        await using (var adderDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            adderDbContext.Add(entity);
            await adderDbContext.SaveChangesAsync();
        }

        await using (var verifyDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            entity = await verifyDbContext.Users.SingleOrDefaultAsync(u => u.Id == entity.Id);
            entity.Should().NotBeNull();
            entity.CreationDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));
        }
    }
    [Fact]
    public async Task ModifyEntity_ShouldUpdateLastModifiedToNow()
    {
        var username = $"User_{Guid.NewGuid()}";
        var email = $"Email_{Guid.NewGuid()}@example.com";
        var googleSubject = $"sub-{Guid.NewGuid()}";
        var entity = new ApplicationUser(username, email, googleSubject);
        var dbContextFactory = fixture.CreateDbContextFactory();
        Guid versionId;
        await using (var adderDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            adderDbContext.Add(entity);
            await adderDbContext.SaveChangesAsync();
        }
        var now = DateTimeOffset.Now;
        await using (var modifyDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            entity = await modifyDbContext.Users.SingleOrDefaultAsync(u => u.Id == entity.Id);
            entity.Should().NotBeNull();
            versionId = entity.VersionId;
            entity.Username = $"User_{Guid.NewGuid()}";
            await modifyDbContext.SaveChangesAsync();
        }
        await using (var verifyDbContext = await dbContextFactory.CreateDbContextAsync())
        {
            entity = await verifyDbContext.Users.SingleOrDefaultAsync(u => u.Id == entity.Id);
            entity.Should().NotBeNull();
            entity.VersionId.Should().NotBe(versionId);
            entity.LastModifiedDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(5));
        }
    }
    [Fact]
    public async Task CreationDateLTELastModifiedDateCheckConstraint_RejectLastModifiedDateLessThanCreationDate()
    {
        var factory = fixture.CreateDbContextFactory();
        var username = $"User_{Guid.CreateVersion7():N}";
        await using (var context = await factory.CreateDbContextAsync())
        {
            var user = new ApplicationUser(
                username,
                $"{Guid.CreateVersion7():N}@example.com",
                $"{Guid.CreateVersion7():N}");

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        await using (var context = await factory.CreateDbContextAsync())
        {
            var saveAction = async () => await context.Users
                .Where(u => u.Username == username).ExecuteUpdateAsync(s => s.SetProperty(u => u.LastModifiedDate, DateTimeOffset.UtcNow.AddDays(-1)));
            (await saveAction.Should().ThrowAsync<PostgresException>("The action must throw this specific exception because we updated the property directly instead of calling SaveChangesAsync"))
                .And
                .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "the LastModifiedDate is 1 day less than the CreationDate");
        }
    }
}