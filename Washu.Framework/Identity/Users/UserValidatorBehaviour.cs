namespace Washu.Framework.Identity.Users;

using Mediator;
using System.Reflection;
using Washu.Framework;
using Washu.Framework.Extensions;

public sealed class UserValidatorBehaviour<TMessage, TResponse>(
    CurrentUser currentUser,
    ISender sender
    ) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        if (message.GetType().GetCustomAttribute<RequireValidUserAttribute>() is null ||
            (!ResultFactory.IsResult<TResponse>() && !ResultFactory.IsGenericResult<TResponse>()))
            return await next(message, cancellationToken);
        var user = await currentUser.GetUserAsync();
        
        if (message is ICarriesUserId carriesUserId)
            if (user.UserId != carriesUserId.UserId)
                return ResultFactory.Error<TResponse>(new UserNotFoundError());
        
        if (user is { UserId: { } userId, SecurityStamp: { } securityStamp } &&
            await sender.Send(new FindUserByIdQuery(userId), cancellationToken) is { } applicationUser &&
            applicationUser.SecurityStamp == securityStamp)
            return await next(message, cancellationToken);

        var error = new UserNotFoundError();
        return ResultFactory.Error<TResponse>(error);
    }
}
