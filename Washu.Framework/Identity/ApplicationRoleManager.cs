namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationRoleManager<TContext>(IDbContextFactory<TContext> dbFactory) 
    where TContext : ApplicationDbContext
{
    public async Task AddRolesAsync(string[] roleNames, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        foreach (var role in roleNames)
        {
            var exists = await db.Roles.AnyAsync(r => r.Name == role, ct);
            if (exists) continue;
            db.Roles.Add(new ApplicationRole(role));
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<string[]> GetRolesForUserAsync(Guid userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var roles = await db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .Select(ur => ur.Role.Name)
            .ToArrayAsync(ct);
        return roles;
    }
}