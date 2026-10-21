namespace Washu.Framework.Identity;

using Extensions;
using Microsoft.AspNetCore.Http;

public interface ICurrentUser
{
    Guid? UserId { get; }
}
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId => accessor.HttpContext?.User.UserId;
}