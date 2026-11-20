namespace Washu.Framework;

public union Result<T>(T, Error);

public union Result(Result.UnitValue, Error)
{
    public static readonly UnitValue Unit = new();
    public readonly record struct UnitValue;
}

public record Error(ErrorType Type, string Code, string Description)
{
    public static Error HandlerError(string code, string description) => new(ErrorType.HandlerError, code, description); 
    public static Error ValidationError(string code, string description, List<string> errors) => new(ErrorType.ValidationError(errors), code, description); 
}

public readonly record struct HandlerError;
public sealed record ValidationError(List<string> Errors);

public union ErrorType(HandlerError, ValidationError)
{
    public static readonly HandlerError HandlerError = new();
    public static ValidationError ValidationError(List<string> errors) => new(errors);
}