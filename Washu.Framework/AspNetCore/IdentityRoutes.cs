namespace Washu.Framework.AspNetCore;

public static class IdentityRoutes
{
    private const string Base = "/api/auth";
    public const string SignOut = $"{Base}/sign-out";
    public const string SignOutFromAllSessions = $"{Base}/sign-out-from-all-sessions";
    public const string ChallengeGoogle = $"{Base}/google";
    public const string ExternalCallback = $"{Base}/external/callback";
}