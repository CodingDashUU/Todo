using FluentValidation;
using Washu.Framework.Constants;

namespace Washu.Todo.Lists;

public static class ListName
{
    public const byte MinimumLength = 3;
    public const byte MaximumLength = 30;
    public const string AllowedSymbols = "!#$&()/?";
    public static readonly string AllowedCharacters = $"{CharacterSet.Alphanumeric}{CharacterSet.Whitespace}{AllowedSymbols}";
}
public class ListNameValidator : AbstractValidator<string>
{
    private static readonly HashSet<char> AllowedChars = [.. ListName.AllowedCharacters];

    public ListNameValidator() =>
        RuleFor(n => n)
            .NotEmpty().WithMessage("List name is required")
            .Length(ListName.MinimumLength, ListName.MaximumLength).WithMessage($"List name must be between {ListName.MinimumLength} and {ListName.MaximumLength} characters")
            .Must(s => s.All(AllowedChars.Contains)).WithMessage($"List name can only contain alphanumeric, whitespace and {ListName.AllowedSymbols} characters");
}