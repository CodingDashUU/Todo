namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Claims;

public sealed class AppClaimsPrincipalFactory<TDbContext>(
    IDbContextFactory<TDbContext> dbFactory,
    PermissionManager permissionManager,
    IOptions<IdentityOptions> optionsAccessor) 
    : IUserClaimsPrincipalFactory<ApplicationUser>
    where TDbContext :  ApplicationDbContext
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

        await using var context = await dbFactory.CreateDbContextAsync();

        // 1. Fetch user's assigned role names from your separate UserRoles table
        var userRoles = await context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.Id)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        // 2. Map roles -> permissions via the in-memory singleton (0 DB joins!)
        var permissions = permissionManager.GetPermissionsForRoles(userRoles);

        foreach (var permission in permissions) identity.AddClaim(new Claim("permission", permission));

        return new ClaimsPrincipal(identity);
    }
}