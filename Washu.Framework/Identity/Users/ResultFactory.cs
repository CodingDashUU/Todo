namespace Washu.Framework.Identity.Users;

using Washu.Framework;

/// <summary>
/// Creates <see cref="Result"/> and <see cref="Result{T}"/> error instances from an <see cref="Error"/>
/// without reflection by exploiting the implicit conversion operators defined on the union types.
/// Falls back to Activator only for exotic response types that are neither Result nor Result&lt;T&gt;.
/// </summary>
public static class ResultFactory
{
    /// <summary>
    /// Wraps <paramref name="error"/> in a <typeparamref name="TResponse"/> that is either
    /// <see cref="Result"/>, <see cref="Result{T}"/>, or falls through (rethrowing).
    /// </summary>
    public static TResponse Error<TResponse>(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)(object)(Result)error;

        if (typeof(TResponse).IsGenericType &&
            typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
        {
            // Result<T>(Error) constructor takes a single argument — one of its union cases.
            var successType = typeof(TResponse).GetGenericArguments()[0];
            var genericResultType = typeof(Result<>).MakeGenericType(successType);
            return (TResponse)Activator.CreateInstance(genericResultType, [error])!;
        }

        // TResponse is not a Result type — the behaviour shouldn't be wrapping errors; rethrow.
        throw new InvalidOperationException(
            $"Cannot wrap {error.GetType().Name} in {typeof(TResponse).Name}: " +
            "response type is not Result or Result<T>.");
    }

    public static bool IsResult<T>()        => typeof(T) == typeof(Result);
    public static bool IsGenericResult<T>() => typeof(T).IsGenericType &&
                                               typeof(T).GetGenericTypeDefinition() == typeof(Result<>);
}