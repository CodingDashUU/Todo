using Microsoft.AspNetCore.Http;
using Washu.Framework.Extensions;

namespace Washu.Framework.Identity.Users;

public class CurrentUser(IHttpContextAccessor accessor)
{
    public Guid? UserId => accessor.HttpContext?.User.UserId;
}