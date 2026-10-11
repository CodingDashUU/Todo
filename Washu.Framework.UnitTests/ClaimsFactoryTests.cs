namespace Washu.Framework.UnitTests;

using Extensions;
using FluentAssertions;
using Identity;
using Identity.Entities;
using System.Security.Claims;

public class ClaimsFactoryTests
{
    [Fact]
    public async Task CreateAsync_AddsCustomClaims_ToClaimsPrincipal()
    {
        var factory = new ApplicationClaimsPrincipalFactory();

        var user = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}", 
            $"{Guid.CreateVersion7():N}@example.com",
            $"{Guid.CreateVersion7():N}");
        
        var principal = await factory.CreateAsync(user);
        
        principal.Should().NotBeNull("The factory should never return null");
        principal.UserId.Should().Be(user.Id, "The NameIdentifier value should match the userid of the user passed in");
        principal.SecurityStamp.Should().Be(user.SecurityStamp, "The SecurityStamp value should match the SecurityStamp of the user passed in");
    }

    [Fact]
    public void ClaimsExtensions_ReturnsNull_WhenClaimsMissing()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        principal.UserId.Should().BeNull("UserId should be null when NameIdentifier claim is absent");
        principal.SecurityStamp.Should().BeNull("SecurityStamp should be null when SecurityStamp claim is absent");
    }

    [Fact]
    public void ClaimsExtensions_ReturnsNull_WhenClaimsAreNotValidGuids()
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid-value"),
            new Claim("SecurityStamp", "also-not-a-guid")
        ]);
        var principal = new ClaimsPrincipal(identity);

        principal.UserId.Should().BeNull("UserId should be null when NameIdentifier is not a parsable Guid");
        principal.SecurityStamp.Should().BeNull("SecurityStamp should be null when SecurityStamp is not a parsable Guid");
    }

    [Fact]
    public void ClaimsExtensions_ExtractsGuids_WhenClaimsAreValid()
    {
        var expectedUserId = Guid.CreateVersion7();
        var expectedSecurityStamp = Guid.CreateVersion7();

        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim("SecurityStamp", expectedSecurityStamp.ToString())
        ]);
        var principal = new ClaimsPrincipal(identity);

        principal.UserId.Should().Be(expectedUserId, "UserId should match parsed NameIdentifier claim");
        principal.SecurityStamp.Should().Be(expectedSecurityStamp, "SecurityStamp should match parsed SecurityStamp claim");
    }
}