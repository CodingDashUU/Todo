using Washu.Framework.Identity.Permissions;

namespace Washu.Framework.Identity.Roles;

public sealed class ApplicationRole(string name)
{
    private ApplicationRole() : this(string.Empty) {}
    public Guid Id { get; private init; } = Guid.CreateVersion7();
    public string Name { get; private init; } = name;
    public HashSet<RolePermission> Permissions { get; private init; } = [];
}