namespace Washu.Framework.Identity.Roles;

public static class InitialUserRoles
{
    public const string User = "User";
    public const string Admin = "Admin";
    public static readonly List<string> Roles = [User, Admin];
}