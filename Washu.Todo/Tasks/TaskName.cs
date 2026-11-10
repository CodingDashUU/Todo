using FluentValidation;
using Washu.Framework.Constants;

namespace Washu.Todo.Tasks;
public static class TaskName
{
    public const byte MinimumLength = 3;
    public const byte MaximumLength = 50;
    public const string AllowedSymbols = "!@$%&(){}[]/.,><";
    public static readonly string AllowedCharacters = $"{CharacterSet.Alphanumeric}{CharacterSet.Whitespace}{AllowedSymbols}";
}
public class TaskNameValidator : AbstractValidator<string>
{
    private static readonly HashSet<char> AllowedChars = [.. TaskName.AllowedCharacters];

    public TaskNameValidator() =>
        RuleFor(n => n)
            .NotEmpty().WithMessage("Task name is required")
            .Length(TaskName.MinimumLength, TaskName.MaximumLength).WithMessage($"Task name must be between {TaskName.MinimumLength} and {TaskName.MaximumLength} characters")
            .Must(s => s.All(AllowedChars.Contains)).WithMessage($"Task name can only contain alphanumeric, whitespace and {TaskName.AllowedSymbols} characters");
}