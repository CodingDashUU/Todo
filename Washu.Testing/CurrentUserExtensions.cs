namespace Washu.Testing;

using Framework.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public static class CurrentUserExtensions
{
    extension(CurrentUser)
    {
        public static CurrentUser CreateMock(Guid? userId)
        {
            var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
            if (userId.HasValue)
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            var claimsPrincipal = new ClaimsPrincipal(identity);
            var httpAccessor = new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
            return new CurrentUser(httpAccessor);
        }
    }
}