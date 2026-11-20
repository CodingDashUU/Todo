namespace Washu.Todo;

using Framework;
using Framework.Identity;
using Framework.Identity.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;
using System.Reflection;

public class DatabaseHandlerBehaviour<TMessage, TResponse>(
    ILogger<DatabaseHandlerBehaviour<TMessage, TResponse>> logger,
    CurrentUser currentUser
    ) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(message, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pgEx)
        {
            logger.LogWarning(
                pgEx,
                "Unique constraint violation on {CommandType} ({Constraint})", 
                typeof(TMessage).Name, 
                pgEx.ConstraintName);
            var err = typeof(TMessage).GetCustomAttribute<UniqueViolationMessageAttribute>()?.Message ??
                      "This value is already being used. Try another one";
            var alreadyExistsError = Error.ValidationError("Generic.AlreadyExists", "A validation error has occured", [err]);
            if (ReflectionHelper.IsResult<TResponse>()) return ReflectionHelper.CreateErrorInstance<TResponse>(alreadyExistsError );
            if (!ReflectionHelper.IsGenericResult<TResponse>()) throw;
            return ReflectionHelper.CreateGenericErrorInstance<TResponse>(alreadyExistsError);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Concurrency conflict for User {UserId} on {CommandType}: {@Command}",
                await currentUser.GetUserIdAsync(),
                typeof(TMessage).Name,
                message
            );
            var concurrencyError = GenericErrors.ConcurrencyError;
            if (ReflectionHelper.IsResult<TResponse>()) return ReflectionHelper.CreateErrorInstance<TResponse>(concurrencyError);
            if (!ReflectionHelper.IsGenericResult<TResponse>()) throw;
            return ReflectionHelper.CreateGenericErrorInstance<TResponse>(concurrencyError);
        }
    }
}

public static class ReflectionHelper
{
    public static T CreateErrorInstance<T>(Error error) => (T)(object)new Result(error);

    public static T CreateGenericErrorInstance<T>(Error error)
    {
        var successType = typeof(T).GetGenericArguments()[0];
        var genericResultType = typeof(Result<>).MakeGenericType(successType);
        return (T)Activator.CreateInstance(genericResultType, null, error)!;
    }

    public static bool IsResult<T>() => typeof(T) == typeof(Result);
    public static bool IsGenericResult<T>() => typeof(T).IsGenericType ||
                                               typeof(T).GetGenericTypeDefinition() == typeof(Result<>);
}