namespace Washu.Framework.Identity;

using Entities;

public interface IOwnedByUser
{
    Guid UserId { get; }
    ApplicationUser User { get; }
}