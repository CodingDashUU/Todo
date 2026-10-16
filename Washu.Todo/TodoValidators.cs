namespace Washu.Todo;

using FluentValidation;

public class ListNameValidator : AbstractValidator<string>
{
    private static readonly HashSet<char> AllowedChars = [.. TodoRules.ListName.AllowedCharacters];

    public ListNameValidator() =>
        RuleFor(n => n)
            .NotEmpty().WithMessage("List name is required")
            .Length(TodoRules.ListName.MinimumLength, TodoRules.ListName.MaximumLength).WithMessage($"List name must be between {TodoRules.ListName.MinimumLength} and {TodoRules.ListName.MaximumLength} characters")
            .Must(s => s.All(AllowedChars.Contains)).WithMessage($"List name can only contain alphanumeric, whitespace and {TodoRules.ListName.AllowedSymbols} characters");
}

public class TaskNameValidator : AbstractValidator<string>
{
    private static readonly HashSet<char> AllowedChars = [.. TodoRules.TaskName.AllowedCharacters];

    public TaskNameValidator() =>
        RuleFor(n => n)
            .NotEmpty().WithMessage("Task name is required")
            .Length(TodoRules.TaskName.MinimumLength, TodoRules.TaskName.MaximumLength).WithMessage($"Task name must be between {TodoRules.TaskName.MinimumLength} and {TodoRules.TaskName.MaximumLength} characters")
            .Must(s => s.All(AllowedChars.Contains)).WithMessage($"Task name can only contain alphanumeric, whitespace and {TodoRules.TaskName.AllowedSymbols} characters");
}

public class GoalDateValidator : AbstractValidator<DateTimeOffset>
{
    public GoalDateValidator() =>
        RuleFor(d => d)
            .NotEmpty().WithMessage("Goal date is required")
            .Must(IsFromFuture).WithMessage("Goal date must be from the future");
    private static bool IsFromFuture(DateTimeOffset d) => d.ToUniversalTime() > DateTimeOffset.UtcNow;
}