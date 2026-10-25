namespace Washu.Framework.IntegrationTests;

using Fixtures;
using FluentAssertions;
using Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class ApplicationUserTests(ApplicationPostgreSqlFixture fixture)
{
    [Fact]
    public async Task CanInsertAndRetrieveApplicationUser()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var username = $"User_{Guid.CreateVersion7():N}";
        var email = $"{username.ToLowerInvariant()}@example.com";
        var googleSubject = $"{Guid.CreateVersion7():N}";
        
        var user = new ApplicationUser(username, email, googleSubject);
        context.Users.Add(user);
        var now = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        var retrieved = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user.Id);
        retrieved.Should().NotBeNull($"The user, {user.Username} should be in the database at this point");
        retrieved.Username.Should().Be(username, "The name of the user received should match the name of the user created");
        retrieved.Email.Should().Be(email, "The email of the user received should match the email of the user created");
        retrieved.GoogleSubject.Should().Be(googleSubject,  "The googleSubject of the user received should match the googleSubject of the user created");
        retrieved.CreationDate.Should().BeCloseTo(now, TimeSpan.FromSeconds(10),
            "The interceptor should have updated the creation date");
        retrieved.LastModifiedDate.Should().BeNull();
        retrieved.LastSignedInDate.Should().BeNull();
    }
    [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicateEmail()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

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
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation, "The email should already exist in the database, even with different casing");
    }
    [Fact]
    public async Task CitextUniqueConstraint_RejectsInsensitiveDuplicateUsername()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

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

        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation, "The username should already exist in the database, even with different casing");
    }
        [Fact]
    public async Task CitextUniqueConstraint_RejectsDuplicateGoogleSubject()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var baseEmail = $"{Guid.CreateVersion7():N}@example.com";
        var baseName = $"User_{Guid.CreateVersion7():N}";
        var sub = $"test_{Guid.CreateVersion7():N}";
        // Email
        var user1 = new ApplicationUser(
            baseName.ToUpperInvariant(), 
            baseEmail.ToUpperInvariant(), 
            sub);
        context.Users.Add(user1);
        await context.SaveChangesAsync();
        
        // GoogleSubject
        var user2 = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}", 
            $"{Guid.CreateVersion7():N}@example.com", 
            sub);
        
        context.Users.Add(user2);
        
        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.UniqueViolation, "The googleSubject should already exist in the database, even with different casing");
    }
    [Fact]
    public async Task EmailCheckConstraint_RejectsInvalidEmailFormat()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var invalidUser = new ApplicationUser(
            $"ValidName_{Guid.CreateVersion7():N}",
            "invalid-email-without-at-and-domain",
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(invalidUser);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "The email violates the ValidEmailCheckConstraint");
    }
    
    [Fact]
    public async Task UsernameValidCharacterCheckConstraint_RejectInvalidUsername()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var user = new ApplicationUser(
            $"User_With_Inv@lid_(H@$@(%13212_{Guid.CreateVersion7()}", 
            $"{Guid.CreateVersion7():N}@example.com", 
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(user);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "The username violates the ValidCharacterCheckConstraint");
    }
    
    [Fact]
    public async Task UsernameValidFirstCharacterCheckConstraint_RejectInvalidUsername()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var user = new ApplicationUser(
            $"&_User_{Guid.CreateVersion7()}", 
            $"{Guid.CreateVersion7():N}@example.com",
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(user);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "The username violates the ValidFirstCharacterCheckConstraint");
    }

    [Fact]
    public async Task UsernameMaxLengthCheckConstraint_RejectExceedingMaxLength()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var tooLongUsername = $"U{new string('a', 128)}";
        var user = new ApplicationUser(
            tooLongUsername,
            $"{Guid.CreateVersion7():N}@example.com",
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(user);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "The username violates the 128 character MaxLength check constraint");
    }

    [Fact]
    public async Task EmailMaxLengthCheckConstraint_RejectExceedingMaxLength()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var tooLongEmail = $"a{new string('b', 245)}@example.com";
        var user = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}",
            tooLongEmail,
            $"{Guid.CreateVersion7():N}");

        context.Users.Add(user);

        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "The email violates the 255 character MaxLength check constraint");
    }

    [Fact]
    public async Task GoogleSubjectMaxLengthCheckConstraint_RejectExceedingMaxLength()
    {
        await using var context = await fixture.CreateDbContextFactory().CreateDbContextAsync();

        var tooLongSubject = new string('s', 256);
        var user = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}",
            $"{Guid.CreateVersion7():N}@example.com",
            tooLongSubject);

        context.Users.Add(user);
        var saveAction = async () => await context.SaveChangesAsync();
        (await saveAction.Should().ThrowAsync<DbUpdateException>("The database should fail to update"))
            .WithInnerException<PostgresException>("The reason should be a Postgres error specifically")
            .And
            .SqlState.Should().Be(PostgresErrorCodes.StringDataRightTruncation, "The GoogleSubject violates the 255 character MaxLength check constraint");
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
    [Fact]
    public async Task CreationDateLTELastSignedInDateCheckConstraint_RejectLastSignedInDateLessThanCreationDate()
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
                .Where(u => u.Username == username).ExecuteUpdateAsync(s => s.SetProperty(u => u.LastSignedInDate, DateTimeOffset.UtcNow.AddDays(-1)));
            (await saveAction.Should().ThrowAsync<PostgresException>("The action must throw this specific exception because we updated the property directly instead of calling SaveChangesAsync"))
                .And
                .SqlState.Should().Be(PostgresErrorCodes.CheckViolation, "the LastSignedInDate is 1 day less than the CreationDate");
        }
    }
}
