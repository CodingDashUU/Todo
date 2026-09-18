namespace Washu.Framework.AspNetCore;

public static class IdentityRoutes
{
    public const string DeleteAccount = "/delete-account";
    public const string SignOut = "/sign-out";
    public const string ChangeUsername = "/change-username";
    
    private const string Base = "/api/auth";
    public const string ChallengeGoogle = $"{Base}/google";
    public const string ExternalCallback = $"{Base}/external/callback";
    public const string CompleteRegistration = $"{Base}/complete";
}