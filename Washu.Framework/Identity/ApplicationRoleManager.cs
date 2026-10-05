namespace Washu.Framework.Identity;

using Entities;
using Extensions;
using Microsoft.EntityFrameworkCore;

public sealed class ApplicationRoleManager(IDbContextFactory<ApplicationDbContext> dbFactory)
{
    public async Task AddRolesAsync(List<string> roleNames, CancellationToken ct = default)
    {
        if (roleNames.IsEmpty) return;

        var inputRoles = roleNames
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (inputRoles.IsEmpty) return;

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
}