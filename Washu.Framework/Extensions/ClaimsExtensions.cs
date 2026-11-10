using Washu.Framework.Identity.Users;

namespace Washu.Framework.Extensions;

using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public static class ClaimsExtensions
{
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public Guid? UserId {
            get
            {
                if (claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) is not { } userId
                    || !Guid.TryParse(userId, out var userGuid)) return null;
                return userGuid;
            }
        }
        public Guid? SecurityStamp {
            get
            {
                if (claimsPrincipal.FindFirstValue("SecurityStamp") is not { } securityStamp
                    || !Guid.TryParse(securityStamp, out var securityStampGuid)) return null;
                return securityStampGuid;
            }
        }
        public static ClaimsPrincipal Create(ApplicationUser user)
        {
            var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);

            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            identity.AddClaim(new Claim(
                "SecurityStamp", 
                user.SecurityStamp.ToString()));
            return new ClaimsPrincipal(identity);
        }
    }
}