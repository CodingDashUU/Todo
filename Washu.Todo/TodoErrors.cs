namespace Washu.Todo;

using Framework;

/// <summary>
/// Well-known domain errors specific to the Todo application.
/// Centralises error constants so handlers and tests reference the same instances.
/// </summary>
public static class TodoErrors
{
    /// <summary>Returned when a list already contains the maximum of 1 000 tasks.</summary>
    public static readonly ValidationError ListTooLong =
        new(["A list cannot contain more than 1 000 tasks"]);
}
