namespace Washu.Framework.Identity.Roles;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;
using Washu.Framework.Extensions;

public sealed record AddPermissionsCommand(List<PermissionRequest> Permissions) : ICommand;
public record PermissionRequest(string Permission, string Role);

public class AddPermissionsHandler(
    IDbContextFactory<ApplicationDbContext> factory
    ) : ICommandHandler<AddPermissionsCommand>
{
    public async ValueTask<Unit> Handle(AddPermissionsCommand command, CancellationToken cancellationToken)
    {
        if (command.Permissions.IsEmpty) return Unit.Value;
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        
        foreach (var req in command.Permissions)
        {
            if (await dbContext.RolePermissions.AnyAsync(rp => rp.Name == req.Permission, cancellationToken: cancellationToken)) continue;
            var role = dbContext.Roles.First(r  => r.Name == req.Role);
            dbContext.RolePermissions.Add(new RolePermission(req.Permission, role.Id));
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}