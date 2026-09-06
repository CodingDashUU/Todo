namespace Washu.Framework.Identity;

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
    // 1. Get all assigned role names for a user
    public async Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        
        return await db.Set<UserRole>()
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync(ct);
    }

    // 2. Fast boolean check for specific role
    public async Task<bool> IsInRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);
        
        return await db.Set<UserRole>()
            .AsNoTracking()
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName, ct);
    }

    // 3. Assign role to user (Idempotent check)
    public async Task AssignRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var exists = await db.Set<UserRole>()
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.UserId == userId && ur.Role.Name == roleName, ct);

        if (!exists)
        {
            var role = await db.Roles.FirstOrDefaultAsync(ur => ur.Name == roleName, ct);
            if (role is not null)
            {
                await db.UserRoles.AddAsync(new UserRole { UserId = userId, RoleId = role.Id }, ct);
                await db.SaveChangesAsync(ct);
            }
        }
    }
    public async Task<MessageType> AddToRoleAsync(
        Guid userId, 
        string roleName,
        CancellationToken ct = default)
    {
        await using var context = await dbFactory.CreateDbContextAsync(ct);

        var normalizedRole = roleName.ToUpperInvariant();

        // Look up the Role ID directly without loading the full Role graph
        var roleId = await context.Roles
            .Where(r => r.NormalizedName == normalizedRole)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        // Check if the user is already in the role
        var inRole = await context.UserRoles
            .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId, ct);

        if (inRole) return MessageType.Success;

        // Direct join-table insert — zero tracking collisions!
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = roleId
        };

        await context.UserRoles.AddAsync(userRole, ct);
        await context.SaveChangesAsync(ct);
        return MessageType.Success;
    }
    // 4. Remove role using fast EF Core ExecuteDelete (no entity tracking required)
    public async Task RemoveRoleAsync(Guid userId, string roleName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        await db.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.UserId == userId && ur.Role.Name == roleName)
            .ExecuteDeleteAsync(ct);
    }

    // 5. Get all user IDs belonging to a specific role
    public async Task<List<Guid>> GetUserIdsInRoleAsync(string roleName, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        return await db.UserRoles
            .AsNoTracking()
            .Include(ur => ur.Role)
            .Where(ur => ur.Role.Name == roleName)
            .Select(ur => ur.UserId)
            .ToListAsync(ct);
    }
}