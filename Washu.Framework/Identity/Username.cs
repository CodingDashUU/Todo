namespace Washu.Framework.Identity;

using FluentValidation;

public static class Username
{
    public static class Rules
    {
        public const byte MinUsernameLength = 3;
        public const byte MaxUsernameLength = 20;
    
        public const string ValidCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
    }
    public class Validator : AbstractValidator<string>
    {
        public Validator() =>
            RuleFor(u => u)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("Name is required")
                .Length(Rules.MinUsernameLength, Rules.MaxUsernameLength).WithMessage($"Name must be between {Rules.MinUsernameLength} and {Rules.MaxUsernameLength} characters")
                .Must(HasValidCharacters).WithMessage("Name may only contain lower-, uppercase, '_' and/or '-' characters");
        private static bool HasValidCharacters(string name) => name.All(Rules.ValidCharacters.Contains);
    }
}