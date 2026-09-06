namespace Washu.Framework.Identity;

using System.Collections.Concurrent;

public sealed class PermissionManager
{
    // Thread-safe lookup: Key = RoleName (e.g., "Admin"), Value = Set of Permissions
    private readonly ConcurrentDictionary<string, HashSet<string>> _rolePermissions = 
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Thread-safe update method called when an admin updates role permissions in the DB.
    /// </summary>
    public void UpdateRolePermissions(string roleName, IEnumerable<string> permissions) => _rolePermissions[roleName] = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a distinct list of all permissions granted across a collection of assigned user roles.
    /// </summary>
    public HashSet<string> GetPermissionsForRoles(IEnumerable<string> userRoles)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in userRoles)
            if (_rolePermissions.TryGetValue(role, out var rolePerms))
                permissions.UnionWith(rolePerms);

        return permissions;
    }
}