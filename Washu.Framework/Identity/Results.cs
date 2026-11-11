namespace Washu.Framework.Identity;

public readonly struct AccountNotFound
{
    public const string Message = "Account could not be found";
}
public readonly struct UserAlreadyModified
{
    public const string Message = "Account has already been modified by another session. Please try again";
}