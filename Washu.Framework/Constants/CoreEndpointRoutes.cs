namespace Washu.Framework.Constants;

public static class CoreEndpointRoutes
{
    public const string Theme = "/theme";
    public static class External
    {
        private const string Base = "/api/auth";
        public const string ChallengeGoogle = $"{Base}/google";
        public const string ExternalCallback = $"{Base}/external/callback";
        public const string CompleteRegistration = $"{Base}/complete";
    }
}