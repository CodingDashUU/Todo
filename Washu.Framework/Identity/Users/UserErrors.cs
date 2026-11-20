namespace Washu.Framework.Identity.Users;

public static class UserErrors
{
    public static readonly Error UserIdNotFound = Error.HandlerError(Codes.UserIdNotFound, "Could not find user in session");
    public static readonly Error UserNotFound = Error.HandlerError(Codes.UserNotFound, "Account could not be found");

    public static class Codes
    {
        public const string UserNotFound = "User.NotFound";
        public const string UserIdNotFound = "User.IdNotFound";
    }
}