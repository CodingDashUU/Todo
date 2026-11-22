namespace Washu.Framework;

// ── Discriminated unions ───────────────────────────────────────────────────────
// Result      = UnitValue  | Error    (void command)
// Result<T>   = T          | Error    (command/query with a typed return value)
// ──────────────────────────────────────────────────────────────────────────────

public union Result<T>(T, Error)
{
    /// <summary>Implicitly lifts a success value into <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Implicitly lifts an <see cref="Error"/> into <see cref="Result{T}"/>.</summary>
    public static implicit operator Result<T>(Error error) => new(error);

    /// <summary>Returns <c>true</c> when the union holds the success case.</summary>
    public bool IsOk    => this is T;

    /// <summary>Returns <c>true</c> when the union holds the error case.</summary>
    public bool IsError => this is Error;
}

public union Result(UnitValue, Error)
{
    /// <summary>The singleton success value used by void commands.</summary>
    public static readonly Result Unit = new UnitValue();

    /// <summary>Implicitly lifts a <see cref="UnitValue"/> into <see cref="Result"/>.</summary>
    public static implicit operator Result(UnitValue value) => new(value);

    /// <summary>Implicitly lifts an <see cref="Error"/> into <see cref="Result"/>.</summary>
    public static implicit operator Result(Error error) => new(error);

    /// <summary>Returns <c>true</c> when the union holds the success case.</summary>
    public bool IsOk    => this is UnitValue;

    /// <summary>Returns <c>true</c> when the union holds the error case.</summary>
    public bool IsError => this is Error;
}

/// <summary>The unit type used as the success payload for void commands.</summary>
public readonly record struct UnitValue;

// ── Closed error hierarchy ─────────────────────────────────────────────────────
// All concrete errors are sealed records so switch expressions are exhaustive.
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Base type for all domain errors. Closed: only subtypes in this assembly are permitted.</summary>
public closed record Error(string Message);

/// <summary>Another session modified or deleted the resource before this operation completed.</summary>
public sealed record ConcurrencyError()
    : Error("Your data has been modified or deleted by another session");

/// <summary>An unrecognised internal fault occurred.</summary>
public sealed record InternalError()
    : Error("There was an unknown error while processing your request");

/// <summary>One or more FluentValidation rules failed.</summary>
public sealed record ValidationError(List<string> Errors)
    : Error("One or more validation errors have occurred");

/// <summary>
/// The requested resource could not be found.
/// <para>Pass a human-readable <paramref name="DataName"/> (e.g. <c>nameof(list)</c>) to produce
/// a contextual message; omit it for a generic "could not be found" message.</para>
/// </summary>
public sealed record NotFoundError(string DataName = "resource")
    : Error($"The {DataName.ToLowerInvariant()} could not be found");

/// <summary>
/// The authenticated user's identity could not be resolved from the current session.
/// This typically triggers a redirect to sign-in rather than a user-visible error message.
/// </summary>
public sealed record UserIdNotFoundError()
    : Error("Your session could not be verified. Please sign in again");

/// <summary>An attempt to create a resource that already exists was made.</summary>
public sealed record AlreadyExistsError(string? Detail = null)
    : Error(Detail ?? "A resource with the given identifier already exists");

public sealed record UserNotFoundError() : Error("Account could not be found");
