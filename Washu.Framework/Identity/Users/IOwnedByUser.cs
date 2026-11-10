namespace Washu.Framework.Identity.Users;

public interface IOwnedByUser
{
    Guid UserId { get; }
    ApplicationUser User { get; }
}