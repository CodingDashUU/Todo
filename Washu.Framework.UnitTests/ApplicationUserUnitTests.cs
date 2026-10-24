namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.Identity.Entities;

public class ApplicationUserUnitTests
{
    [Fact]
    public void Constructor_InitializesPropertiesWithValidGuids()
    {
        const string username = "Alice";
        const string email = "alice@example.com";
        const string subject = "google-subject-123";

        var user = new ApplicationUser(username, email, subject);

        user.Id.Should().NotBeEmpty();
        user.Username.Should().Be(username);
        user.Email.Should().Be(email);
        user.GoogleSubject.Should().Be(subject);
        user.VersionId.Should().NotBeEmpty();
        user.SecurityStamp.Should().NotBeEmpty();
    }

    [Fact]
    public void SettingUsername_UpdatesVersionIdToNewGuid()
    {
        var user = new ApplicationUser("InitialName", "user@example.com", "sub-123");
        var initialVersion = user.VersionId;

        user.Username = "UpdatedName";

        user.Username.Should().Be("UpdatedName");
        user.VersionId.Should().NotBe(initialVersion, "Changing the Username property must regenerate VersionId");
    }

    [Fact]
    public void ChangeSecurityStamp_UpdatesSecurityStampToNewGuid()
    {
        var user = new ApplicationUser("Alice", "alice@example.com", "sub-123");
        var initialSecurityStamp = user.SecurityStamp;

        user.ChangeSecurityStamp();

        user.SecurityStamp.Should().NotBe(initialSecurityStamp, "Calling ChangeSecurityStamp must assign a new SecurityStamp");
    }
}
