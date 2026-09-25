namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationRoleManager<TContext>(IDbContextFactory<TContext> dbFactory) 
    where TContext : ApplicationDbContext
{
    public async Task AddRolesAsync(List<string> roleNames, CancellationToken ct = default)
    {
        if (roleNames.Count == 0) return;

        var inputRoles = roleNames
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (inputRoles.Count == 0) return;

        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var existingRoleNames = await db.Roles
            .Where(r => inputRoles.Contains(r.Name))
            .Select(r => r.Name)
            .ToListAsync(ct);

        var existingSet = new HashSet<string>(existingRoleNames, StringComparer.OrdinalIgnoreCase);
        var newRoles = inputRoles
            .Where(role => !existingSet.Contains(role))
            .Select(role => new ApplicationRole(role));

        db.Roles.AddRange(newRoles);
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