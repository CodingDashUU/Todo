namespace Washu.Framework.UnitTests;

using Extensions;
using FluentAssertions;
using Identity;
using Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
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
            $"{Guid.CreateVersion7()}");
        
        var principal = await factory.CreateAsync(user);
        
        principal.Should().NotBeNull("The factory should never return null");
        principal.UserId.Should().Be(user.Id, "The NameIdentifier value should match the userid of the user passed in");
        principal.SecurityStamp.Should().Be(user.SecurityStamp.ToString(), "The SecurityStamp value should match the SecurityStamp of the user passed in");
    }
}