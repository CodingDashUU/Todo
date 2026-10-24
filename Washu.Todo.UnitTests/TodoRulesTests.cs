namespace Washu.Todo.UnitTests;

using FluentAssertions;
using Washu.Framework.Constants;

public class TodoRulesTests
{
    [Fact]
    public void TaskNameRules_HaveExpectedBoundariesAndAllowedCharacters()
    {
        TodoRules.TaskName.MinimumLength.Should().Be(3);
        TodoRules.TaskName.MaximumLength.Should().Be(50);
        TodoRules.TaskName.AllowedSymbols.Should().Be("!@$%&(){}[]/.,><");

        TodoRules.TaskName.AllowedCharacters.Should().Contain(Characters.Lowercase);
        TodoRules.TaskName.AllowedCharacters.Should().Contain(Characters.Uppercase);
        TodoRules.TaskName.AllowedCharacters.Should().Contain(Characters.Numbers);
        TodoRules.TaskName.AllowedCharacters.Should().Contain(Characters.Whitespace.ToString());
        TodoRules.TaskName.AllowedCharacters.Should().Contain("!");
        TodoRules.TaskName.AllowedCharacters.Should().Contain(">");
    }

    [Fact]
    public void ListNameRules_HaveExpectedBoundariesAndAllowedCharacters()
    {
        TodoRules.ListName.MinimumLength.Should().Be(3);
        TodoRules.ListName.MaximumLength.Should().Be(30);
        TodoRules.ListName.AllowedSymbols.Should().Be("!#$&()/?");

        TodoRules.ListName.AllowedCharacters.Should().Contain(Characters.Lowercase);
        TodoRules.ListName.AllowedCharacters.Should().Contain(Characters.Uppercase);
        TodoRules.ListName.AllowedCharacters.Should().Contain(Characters.Numbers);
        TodoRules.ListName.AllowedCharacters.Should().Contain(Characters.Whitespace.ToString());
        TodoRules.ListName.AllowedCharacters.Should().Contain("#");
        TodoRules.ListName.AllowedCharacters.Should().Contain("?");
    }
}
