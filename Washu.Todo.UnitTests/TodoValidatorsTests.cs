namespace Washu.Todo.UnitTests;

using FluentAssertions;

public class TodoValidatorsTests
{
    private readonly ListNameValidator _listNameValidator = new();
    private readonly TaskNameValidator _taskNameValidator = new();
    private readonly GoalDateValidator _goalDateValidator = new();

    [Theory]
    [InlineData("Valid List")]
    [InlineData("List 123")]
    [InlineData("My#List")]
    [InlineData("Work & Personal")]
    [InlineData("Plan (2026)")]
    [InlineData("Task/List?")]
    [InlineData("abc")] // min length 3
    public void ListNameValidator_ValidNames_ShouldPass(string name)
    {
        var result = _listNameValidator.Validate(name);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ListNameValidator_EmptyOrWhitespace_ShouldFail(string name)
    {
        var result = _listNameValidator.Validate(name);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "List name is required");
    }

    [Theory]
    [InlineData("ab")] // length 2 (< 3)
    [InlineData("1234567890123456789012345678901")] // length 31 (> 30)
    public void ListNameValidator_InvalidLength_ShouldFail(string name)
    {
        var result = _listNameValidator.Validate(name);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == $"List name must be between {TodoRules.ListName.MinimumLength} and {TodoRules.ListName.MaximumLength} characters");
    }

    [Theory]
    [InlineData("List*Star")]
    [InlineData("List^Caret")]
    [InlineData("List<Tag>")]
    [InlineData("List=Equal")]
    public void ListNameValidator_DisallowedCharacters_ShouldFail(string name)
    {
        var result = _listNameValidator.Validate(name);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("List name can only contain alphanumeric"));
    }

    [Theory]
    [InlineData("Valid Task")]
    [InlineData("Task!")]
    [InlineData("Cost $100")]
    [InlineData("Complete 50%")]
    [InlineData("Foo & Bar")]
    [InlineData("Func()")]
    [InlineData("Array[]")]
    [InlineData("Object{}")]
    [InlineData("a/b.c,d")]
    [InlineData("x > y < z")]
    [InlineData("abc")] // min length 3
    public void TaskNameValidator_ValidNames_ShouldPass(string name)
    {
        var result = _taskNameValidator.Validate(name);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void TaskNameValidator_EmptyOrWhitespace_ShouldFail(string name)
    {
        var result = _taskNameValidator.Validate(name);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Task name is required");
    }

    [Theory]
    [InlineData("ab")] // length 2 (< 3)
    [InlineData("123456789012345678901234567890123456789012345678901")] // length 51 (> 50)
    public void TaskNameValidator_InvalidLength_ShouldFail(string name)
    {
        var result = _taskNameValidator.Validate(name);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == $"Task name must be between {TodoRules.TaskName.MinimumLength} and {TodoRules.TaskName.MaximumLength} characters");
    }

    [Theory]
    [InlineData("Task#Sharp")]
    [InlineData("Task^Power")]
    [InlineData("Task*Multiply")]
    [InlineData("Task?Question")]
    public void TaskNameValidator_DisallowedCharacters_ShouldFail(string name)
    {
        var result = _taskNameValidator.Validate(name);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Task name can only contain alphanumeric"));
    }

    [Fact]
    public void GoalDateValidator_FutureDate_ShouldPass()
    {
        var futureDate = DateTimeOffset.UtcNow.AddMinutes(10);
        var result = _goalDateValidator.Validate(futureDate);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GoalDateValidator_PastDate_ShouldFail()
    {
        var pastDate = DateTimeOffset.UtcNow.AddMinutes(-10);
        var result = _goalDateValidator.Validate(pastDate);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Goal date must be from the future");
    }

    [Fact]
    public void GoalDateValidator_DefaultDate_ShouldFail()
    {
        var defaultDate = default(DateTimeOffset);
        var result = _goalDateValidator.Validate(defaultDate);
        result.IsValid.Should().BeFalse();
    }
}
