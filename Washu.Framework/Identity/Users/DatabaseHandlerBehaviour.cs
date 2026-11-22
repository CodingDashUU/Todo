namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Reflection;
using Washu.Framework;
using Washu.Framework.Extensions;
using Washu.Framework.Identity;
using Microsoft.Extensions.Logging;

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
            return ResultFactory.Error<TResponse>(new ValidationError([err]));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            logger.LogWarning(
                ex,
                "Concurrency conflict for User {UserId} on {CommandType}: {@Command}",
                currentUser.User.UserId,
                typeof(TMessage).Name,
                message
            );
            return ResultFactory.Error<TResponse>(new ConcurrencyError());
        }
    }
}