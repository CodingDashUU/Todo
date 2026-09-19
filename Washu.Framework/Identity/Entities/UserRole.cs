namespace Washu.Framework.Identity.Entities;

public class UserRole
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Guid RoleId { get; set; }
    public ApplicationRole Role { get; set; } = null!;
}