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
    public static class Errors
    {
        public const string Required = "Name is required";
        public static readonly string InvalidLength = $"Name must be between {Rules.MinUsernameLength} and {Rules.MaxUsernameLength} characters";
        public const string InvalidCharacters = "Name may only contain lower-, uppercase, '_' and/or '-' characters";
    }
    public class Validator : AbstractValidator<string>
    {
        public Validator() =>
            RuleFor(u => u)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage(Errors.Required)
                .Length(Rules.MinUsernameLength, Rules.MaxUsernameLength).WithMessage(Errors.InvalidLength)
                .Must(HasValidCharacters).WithMessage(Errors.InvalidCharacters);
        private static bool HasValidCharacters(string name) => name.All(Rules.ValidCharacters.Contains);
    }
}