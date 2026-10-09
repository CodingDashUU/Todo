namespace Washu.Framework.Identity;

using Constants;
using FluentValidation;

public static class UsernameRules
{
    public const byte MinUsernameLength = 3;
    public const byte MaxUsernameLength = 50;
    
    public const string ValidCharacters = $"{Characters.Alphanumeric}_-";
}
public sealed class UsernameValidator : AbstractValidator<string>
{
    public UsernameValidator() =>
        RuleFor(u => u)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Name is required")
            .Length(UsernameRules.MinUsernameLength, UsernameRules.MaxUsernameLength).WithMessage($"Name must be between {UsernameRules.MinUsernameLength} and {UsernameRules.MaxUsernameLength} characters")
            .Must(HasValidCharacters).WithMessage("Name may only contain alphanumeric, '_' and/or '-' characters")
            .Must(StartsWithAlphaCharacters).WithMessage("Name must start with a lower-, or uppercase character");
    private static bool HasValidCharacters(string name) => name.All(UsernameRules.ValidCharacters.Contains);

    private static bool StartsWithAlphaCharacters(string name) =>
        $"{Characters.Letters}".Contains(name.First());
}