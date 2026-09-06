namespace Washu.Todo;

using FluentValidation;

public static class TodoValidators
{
    public class ListName : AbstractValidator<string>
    {
        public ListName() =>
            RuleFor(n => n)
                .NotEmpty().WithMessage("List name is required")
                .Length(TodoRules.ListName.MinimumLength, TodoRules.ListName.MaximumLength).WithMessage($"List name must be between {TodoRules.ListName.MinimumLength} and {TodoRules.ListName.MaximumLength} characters")
                .Must(s => ContainsValidCharacters(s, TodoRules.ListName.AllowedCharacters)).WithMessage($"List name can only contain alphanumeric, whitespace and {TodoRules.ListName.AllowedSymbols} characters");
    }

    public class TaskName : AbstractValidator<string>
    {
        public TaskName() =>
            RuleFor(n => n)
                .NotEmpty().WithMessage("Task name is required")
                .Length(TodoRules.TaskName.MinimumLength, TodoRules.TaskName.MaximumLength).WithMessage($"Task name must be between {TodoRules.TaskName.MinimumLength} and {TodoRules.TaskName.MaximumLength} characters")
                .Must(s => ContainsValidCharacters(s, TodoRules.TaskName.AllowedCharacters)).WithMessage($"Task name can only contain alphanumeric, whitespace and {TodoRules.TaskName.AllowedSymbols} characters");
    }

    public class GoalDate : AbstractValidator<DateTimeOffset>
    {
        public GoalDate() =>
            RuleFor(d => d)
                .NotEmpty().WithMessage("Goal date is required")
                .Must(IsFromFuture).WithMessage("Goal date must be from the future");
        private static bool IsFromFuture(DateTimeOffset d) => d.ToUniversalTime() > DateTimeOffset.UtcNow;
    }
    private static bool ContainsValidCharacters(string s, string allowedCharacters) => s.All(allowedCharacters.Contains);
}