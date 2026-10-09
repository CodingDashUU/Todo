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
        var additionalClaims = new List<Claim>();
        if (command.AuthenticationMethod is not null) additionalClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, command.AuthenticationMethod));

        var authenticationProperties = new AuthenticationProperties { IsPersistent = command.IsPersistent };
        var userPrincipal = await claimsFactory.CreateAsync(command.User);

        if (userPrincipal.Identity is ClaimsIdentity identity)
            foreach (var claim in additionalClaims)
                identity.AddClaim(claim);
        
        await context.SignInAsync(
            IdentityConstants.ApplicationScheme,
            userPrincipal,
            authenticationProperties);
        
        context.User = userPrincipal;
        await context.SignOutAsync(IdentityConstants.ExternalScheme);
    }
}