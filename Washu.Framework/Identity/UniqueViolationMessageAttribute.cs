namespace Washu.Framework.Identity;

[AttributeUsage(AttributeTargets.Class)]
public sealed class UniqueViolationMessageAttribute(string message) : Attribute
{
    public string Message { get; set; } = message;
}