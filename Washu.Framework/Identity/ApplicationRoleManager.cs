namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Notifications;

public class ApplicationRoleManager<TContext>(IDbContextFactory<TContext> dbFactory) 
    where TContext : ApplicationDbContext
{
    public async Task AddRoleAsync(string roleName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        var exists = await db.Roles.AnyAsync(r => r.Name == roleName, ct);
        if (!exists)
        {
            db.Roles.Add(new ApplicationRole(roleName));
            await db.SaveChangesAsync(ct);
        }
    }
}