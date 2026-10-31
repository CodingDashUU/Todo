namespace Washu.Framework.Identity;

using Entities;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed record AddRolesCommand(List<string> RoleNames);

public sealed class AddRolesHandler(IDbContextFactory<ApplicationDbContext> dbFactory, ILogger<AddRolesHandler> logger)
{
    public async Task HandleAsync(AddRolesCommand command, CancellationToken ct = default)
    {
        if (command.RoleNames.IsEmpty) return;

        var inputRoles = command.RoleNames
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
            .Select(role => new ApplicationRole(role))
            .ToArray();
        if (newRoles.IsNotEmpty)
        {
            db.Roles.AddRange(newRoles);
            await db.SaveChangesAsync(ct);
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Added roles: {@Roles}", newRoles.Select(r => r.Name));   
        }
        
    }
}