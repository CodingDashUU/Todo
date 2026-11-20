namespace Washu.Todo.Tasks;

using FluentValidation;

public class GoalDateValidator : AbstractValidator<DateTimeOffset>
{
    public GoalDateValidator() =>
        RuleFor(d => d)
            .NotEmpty().WithMessage("Goal date is required")
            .Must(IsFromFuture).WithMessage("Goal date must be from the future");
    private static bool IsFromFuture(DateTimeOffset d) => d.ToUniversalTime() > DateTimeOffset.UtcNow;
}