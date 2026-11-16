namespace Washu.Framework.UnitTests;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Washu.Framework.Identity;

public class CurrentUserTests
{
    [Fact]
    public void UserId_WhenHttpContextIsNull_ReturnsNull()
    {
        var accessor = new HttpContextAccessor { HttpContext = null };
        var currentUser = new CurrentUser(accessor);

        currentUser.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenUserHasNoNameIdentifierClaim_ReturnsNull()
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var currentUser = new CurrentUser(accessor);

        currentUser.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenUserHasInvalidGuidInNameIdentifierClaim_ReturnsNull()
    {
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
        ]);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var currentUser = new CurrentUser(accessor);

        currentUser.UserId.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenUserHasValidNameIdentifierClaim_ReturnsParsedGuid()
    {
        var expectedId = Guid.CreateVersion7();
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, expectedId.ToString())
        ]);
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };
        var accessor = new HttpContextAccessor { HttpContext = context };
        var currentUser = new CurrentUser(accessor);

        currentUser.UserId.Should().Be(expectedId);
    }
}
