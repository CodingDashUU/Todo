namespace Washu.Framework.UnitTests;

using FluentAssertions;
using Washu.Framework.Identity;

public class UsernameValidatorTests
{
    private readonly UsernameValidator _validator = new();

    [Theory]
    [InlineData("Alice")]
    [InlineData("bob_123")]
    [InlineData("User-Name")]
    [InlineData("abc")]
    [InlineData("z_1-a")]
    public void Validate_ValidUsernames_ShouldPassValidation(string username)
    {
        var result = _validator.Validate(username);
        result.IsValid.Should().BeTrue($"'{username}' is a valid username");
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_UsernameWithMaxLength50_ShouldPassValidation()
    {
        var valid50CharName = $"A{new string('b', 49)}";
        var result = _validator.Validate(valid50CharName);
        result.IsValid.Should().BeTrue("A 50-character username starting with a letter should pass validation");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyOrWhitespace_ShouldFailWithNotEmptyError(string username)
    {
        var result = _validator.Validate(username);
        result.IsValid.Should().BeFalse("Empty or whitespace username should not be valid");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name is required");
    }

    [Fact]
    public void Validate_Null_ThrowsInvalidOperationException()
    {
        var act = () => _validator.Validate((string)null!);
        act.Should().Throw<InvalidOperationException>("FluentValidation requires root model to be non-null");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void Validate_TooShortUsername_ShouldFailWithLengthError(string username)
    {
        var result = _validator.Validate(username);
        result.IsValid.Should().BeFalse("Username under 3 characters should fail length validation");
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Name must be between 3 and 50 characters"));
    }

    [Fact]
    public void Validate_TooLongUsername_ShouldFailWithLengthError()
    {
        var tooLongName = $"A{new string('b', 50)}"; // 51 characters
        var result = _validator.Validate(tooLongName);
        result.IsValid.Should().BeFalse("Username over 50 characters should fail length validation");
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Name must be between 3 and 50 characters"));
    }

    [Theory]
    [InlineData("1user")]
    [InlineData("_user")]
    [InlineData("-user")]
    [InlineData("9alice")]
    public void Validate_UsernameNotStartingWithLetter_ShouldFailWithFirstCharError(string username)
    {
        var result = _validator.Validate(username);
        result.IsValid.Should().BeFalse($"Username '{username}' not starting with an ASCII letter should fail validation");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name must start with a lower-, or uppercase character");
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user#name")]
    [InlineData("user.name")]
    [InlineData("user!name")]
    public void Validate_UsernameWithDisallowedCharacters_ShouldFailWithCharacterError(string username)
    {
        var result = _validator.Validate(username);
        result.IsValid.Should().BeFalse($"Username '{username}' containing forbidden characters should fail validation");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Name may only contain alphanumeric, '_' and/or '-' characters");
    }
}
