namespace Washu.Framework.Identity;

public interface IOwnedByUser
{
    Guid UserId { get; init; }
}