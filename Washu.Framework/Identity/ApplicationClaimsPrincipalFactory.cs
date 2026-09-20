namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

public sealed class ApplicationClaimsPrincipalFactory(
    IOptions<IdentityOptions> optionsAccessor) 
    : IUserClaimsPrincipalFactory<ApplicationUser>
{
    public Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
        identity.AddClaim(new Claim(
            optionsAccessor.Value.ClaimsIdentity.SecurityStampClaimType, 
            user.SecurityStamp.ToString()));
        
        return Task.FromResult(new ClaimsPrincipal(identity));
    }
}