namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public sealed record SignInCommand(ApplicationUser User, bool IsPersistent, string? AuthenticationMethod = null);

public sealed class SignInHandler(
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    IHttpContextAccessor httpContextAccessor
    )
{
    public async Task HandleAsync(SignInCommand command)
    {
        var context = httpContextAccessor.HttpContext ??
                      throw new InvalidOperationException("An active HttpContext is required");

        var authenticationProperties = new AuthenticationProperties { IsPersistent = command.IsPersistent };
        var userPrincipal = await claimsFactory.CreateAsync(command.User);

        if (command.AuthenticationMethod is not null
            && userPrincipal.Identity is ClaimsIdentity identity)
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, command.AuthenticationMethod));
        
        await context.SignInAsync(
            IdentityConstants.ApplicationScheme,
            userPrincipal,
            authenticationProperties);
        
        context.User = userPrincipal;
        await context.SignOutAsync(IdentityConstants.ExternalScheme);
    }
}