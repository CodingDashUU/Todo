namespace Washu.Framework.AspNetCore;

public static class CoreEndpointRoutes
{
    public const string Theme = "/theme";
    public static class Identity
    {
        public const string DeleteAccount = "/delete-account";
        public const string SignOut = "/sign-out";
        public const string ChangeUsername = "/change-username";
        public static class External
        {
            private const string Base = "/api/auth";
            public const string ChallengeGoogle = $"{Base}/google";
            public const string ExternalCallback = $"{Base}/external/callback";
            public const string CompleteRegistration = $"{Base}/complete";
        }
    }
}