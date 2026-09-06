namespace Washu.Framework.Identity;

using FluentValidation;

public static class Registration
{
    public sealed record Model(string Username);
    
    public static class Errors
    {
        public const string NeedToContinueWithSignInProvider =
            "You need to continue with a sign-in provider to register";
    }
    
    public sealed class Validator : AbstractValidator<Model>
    {
        public Validator() =>
            RuleFor(x => x.Username)
                .SetValidator(new Username.Validator());
    }
}