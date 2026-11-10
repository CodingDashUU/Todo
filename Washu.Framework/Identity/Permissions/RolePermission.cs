using Washu.Framework.Identity.Roles;

namespace Washu.Framework.Identity.Permissions;

public class RolePermission(string name, Guid roleId)
{
    public Guid Id { get; private init; } = Guid.CreateVersion7();
    public Guid RoleId { get; private init; } = roleId;
    public ApplicationRole Role { get; private init; } = null!;
    public string Name { get; private init; } = name;
}