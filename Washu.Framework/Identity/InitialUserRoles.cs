namespace Washu.Framework.Identity;

using System.Collections.Immutable;

public static class InitialUserRoles
{
    public const string User = "User";

    public static readonly ImmutableArray<string> Roles = [User];
}