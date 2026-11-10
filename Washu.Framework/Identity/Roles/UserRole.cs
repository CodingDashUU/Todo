using Washu.Framework.Identity.Users;

namespace Washu.Framework.Identity.Roles;

public sealed class UserRole : IOwnedByUser
{
    public Guid UserId { get; init; }
    public ApplicationUser User { get; private init; } = null!;
    public Guid RoleId { get; init; }
    public ApplicationRole Role { get; private init; } = null!;
}