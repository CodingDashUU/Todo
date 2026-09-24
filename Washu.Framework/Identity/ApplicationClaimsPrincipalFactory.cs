namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

public sealed class ApplicationClaimsPrincipalFactory<TDbContext>(
    IOptions<IdentityOptions> optionsAccessor,
    ApplicationRoleManager<TDbContext> roleManager) 
    : IUserClaimsPrincipalFactory<ApplicationUser>
where TDbContext : ApplicationDbContext
{
    public async Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
    {
        var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);

        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Username));
        identity.AddClaim(new Claim(
            optionsAccessor.Value.ClaimsIdentity.SecurityStampClaimType, 
            user.SecurityStamp.ToString()));

        var roles = await roleManager.GetRolesForUserAsync(user.Id);
        foreach (var role in roles) identity.AddClaim(new Claim(ClaimTypes.Role, role));
        return new ClaimsPrincipal(identity);
    }
}