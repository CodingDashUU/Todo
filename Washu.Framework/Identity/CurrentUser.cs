namespace Washu.Framework.Identity;

using Extensions;
using Microsoft.AspNetCore.Http;

public class CurrentUser(IHttpContextAccessor accessor)
{
    public Guid? UserId => accessor.HttpContext?.User.UserId;
}