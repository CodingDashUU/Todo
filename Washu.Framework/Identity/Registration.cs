namespace Washu.Framework.Identity;

using FluentValidation;

public static class Registration
{
    public sealed record Model(string Username);
    
    public sealed class Validator : AbstractValidator<Model>
    {
        public Validator() =>
            RuleFor(x => x.Username)
                .SetValidator(new Username.Validator());
    }
}