namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

public static class ApplicationClaimsPrincipalFactory
{
    public static ClaimsPrincipal Create(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(
            "SecurityStamp", 
            user.SecurityStamp.ToString()));
        return new ClaimsPrincipal(identity);
    }
}