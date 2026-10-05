namespace Washu.Framework.Identity.Entities;

public sealed class UserRole
{
    public Guid UserId { get; init; }
    public ApplicationUser User { get; private init; } = null!;
    public Guid RoleId { get; init; }
    public ApplicationRole Role { get; private init; } = null!;
}