namespace Washu.Framework.Identity.Users;

public interface ICarriesUserId
{
    Guid UserId { get; }
}