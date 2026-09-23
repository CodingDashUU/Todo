namespace Washu.Framework.Extensions;

using System.Security.Claims;

public static class ClaimsExtensions
{
    extension(ClaimsPrincipal claimsPrincipal)
    {
        public Guid? FindUserId()
        {
            if(claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier) is not { } userId
                || !Guid.TryParse(userId, out var userGuid)) return null;
            return userGuid;
        }
    }
}