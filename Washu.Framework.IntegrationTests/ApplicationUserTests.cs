namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class ApplicationUserTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task CanInsertAndRetrieveApplicationUser()
    {
        await using var context = fixture.CreateDbContext();

        var username = $"User_{Guid.CreateVersion7():N}";
        var email = $"{username.ToLowerInvariant()}@example.com";
        var googleSubject = $"{Guid.CreateVersion7():N}";

        var user = new ApplicationUser(username, email, googleSubject);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var retrieved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        retrieved.Should().NotBeNull();
        retrieved.Username.Should().Be(username);
        retrieved.Email.Should().Be(email);
        retrieved.GoogleSubject.Should().Be(googleSubject);
    }
    [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicateEmail()
    {
        await using var context = fixture.CreateDbContext();

        var baseEmail = $"{Guid.CreateVersion7():N}@example.com";
        var baseName = $"User_{Guid.CreateVersion7():N}";
        var baseSub = $"test_{Guid.CreateVersion7():N}";
        
        var user1 = new ApplicationUser(
            baseName.ToUpperInvariant(), 
            baseEmail.ToUpperInvariant(), 
            baseSub.ToUpperInvariant());
        context.Users.Add(user1);
        await context.SaveChangesAsync();
        
        var user2 = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}", 
            baseEmail.ToLowerInvariant(), 
            $"{Guid.CreateVersion7():N}");
        context.Users.Add(user2);
        
        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
    [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicateUsername()
    {
        await using var context = fixture.CreateDbContext();

        var baseEmail = $"{Guid.CreateVersion7():N}@example.com";
        var baseName = $"User_{Guid.CreateVersion7():N}";
        var baseSub = $"test_{Guid.CreateVersion7():N}";
        var user1 = new ApplicationUser(
            baseName.ToUpperInvariant(), 
            baseEmail.ToUpperInvariant(), 
            baseSub.ToUpperInvariant());
        context.Users.Add(user1);
        await context.SaveChangesAsync();
        
        var user2 = new ApplicationUser(
            baseName.ToLowerInvariant(), 
            $"{Guid.CreateVersion7():N}@example.com", 
            $"{Guid.CreateVersion7():N}");
        context.Users.Add(user2);
        var saveAction = async () => await context.SaveChangesAsync();

        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
        [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicateGoogleSubject()
    {
        await using var context = fixture.CreateDbContext();

        var baseEmail = $"{Guid.CreateVersion7():N}@example.com";
        var baseName = $"User_{Guid.CreateVersion7():N}";
        var baseSub = $"test_{Guid.CreateVersion7():N}";
        // Email
        var user1 = new ApplicationUser(
            baseName.ToUpperInvariant(), 
            baseEmail.ToUpperInvariant(), 
            baseSub.ToUpperInvariant());
        context.Users.Add(user1);
        await context.SaveChangesAsync();
        
        // GoogleSubject
        var user2 = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}", 
            $"{Guid.CreateVersion7():N}@example.com", 
            baseSub.ToLowerInvariant());
        
        context.Users.Add(user2);
        
        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }
    [Fact]
    public async Task EmailCheckConstraint_RejectsInvalidEmailFormat()
    {
        await using var context = fixture.CreateDbContext();

        var invalidUser = new ApplicationUser(
            $"ValidName_{Guid.CreateVersion7():N}",
            "invalid-email-without-at-and-domain",
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(invalidUser);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }
    
    [Fact]
    public async Task UsernameValidCharacterCheckConstraint_RejectInvalidUsername()
    {
        await using var context = fixture.CreateDbContext();

        var user = new ApplicationUser(
            $"User_With_Inv@lid_(H@$@(%13212_{Guid.CreateVersion7()}", 
            $"{Guid.CreateVersion7():N}@example.com", 
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(user);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }
    
    [Fact]
    public async Task UsernameValidFirstCharacterCheckConstraint_RejectInvalidUsername()
    {
        await using var context = fixture.CreateDbContext();

        var user = new ApplicationUser(
            $"&_User_{Guid.CreateVersion7()}", 
            $"{Guid.CreateVersion7():N}@example.com",
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(user);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>())
            .WithInnerException<PostgresException>()
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
    }
}
