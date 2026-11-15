using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Identity.Roles;

using Mediator;

public sealed record AddRolesCommand(List<string> RoleNames) : ICommand;

public sealed class AddRolesHandler(
    IDbContextFactory<ApplicationDbContext> dbFactory, 
    ILogger<AddRolesHandler> logger) : ICommandHandler<AddRolesCommand>
{
    public async ValueTask<Unit> Handle(AddRolesCommand command, CancellationToken ctx = default)
    {
        if (command.RoleNames.IsEmpty) return Unit.Value;

        var inputRoles = command.RoleNames
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (inputRoles.IsEmpty) return Unit.Value;

        await using var db = await dbFactory.CreateDbContextAsync(ctx);
        var existingRoleNames = await db.Roles
            .Where(r => inputRoles.Contains(r.Name))
            .Select(r => r.Name)
            .ToListAsync(ctx);

        var existingSet = new HashSet<string>(existingRoleNames, StringComparer.OrdinalIgnoreCase);
        var newRoles = inputRoles
            .Where(role => !existingSet.Contains(role))
            .Select(role => new ApplicationRole(role))
            .ToArray();
        if (newRoles.IsEmpty) return Unit.Value;
        db.Roles.AddRange(newRoles);
        await db.SaveChangesAsync(ctx);
        if (logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Added roles: {@Roles}", newRoles.Select(r => r.Name));

        return Unit.Value;
    }
}