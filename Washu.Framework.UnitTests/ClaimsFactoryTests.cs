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
        var options = Options.Create(new IdentityOptions());
        var factory = new ApplicationClaimsPrincipalFactory(options);

        var user = new ApplicationUser(
            $"User_{Guid.CreateVersion7():N}", 
            $"{Guid.CreateVersion7():N}@example.com",
            $"{Guid.CreateVersion7()}");
        
        var principal = await factory.CreateAsync(user);
        
        principal.Should().NotBeNull();
        principal.UserId.Should().Be(user.Id);
        principal.FindFirst(
            options.Value.ClaimsIdentity.SecurityStampClaimType)?.Value.Should().Be(user.SecurityStamp.ToString());
    }
}