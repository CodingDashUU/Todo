namespace Washu.Framework.Identity.Users;

using FluentValidation;
using Mediator;
using Washu.Framework;
using Microsoft.Extensions.DependencyInjection;
public sealed class MessageValidatorBehaviour<TMessage, TResponse>(IServiceScopeFactory scopeFactory) : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken
    )
    {
        using var scope = scopeFactory.CreateScope();
        var validator = scope.ServiceProvider.GetService<IValidator<TMessage>>();
        if (validator is null) return await next(message, cancellationToken);
        var result = await validator.ValidateAsync(message, cancellationToken);
        if (result.IsValid) return await next(message, cancellationToken);
        var validationError = new ValidationError([.. result.Errors.Select(e => e.ErrorMessage)]);
        if (!ResultFactory.IsResult<TResponse>() && !ResultFactory.IsGenericResult<TResponse>())
            return await next(message, cancellationToken);
        return ResultFactory.Error<TResponse>(validationError);
    }
}