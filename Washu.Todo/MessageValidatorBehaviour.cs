namespace Washu.Todo;

using FluentValidation;
using Framework;
using Mediator;

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
        var validationError = Error.ValidationError("Generic.ValidationError","Validation Error/s", [.. result.Errors.Select(e => e.ErrorMessage)]);
        if (ReflectionHelper.IsResult<TResponse>()) return ReflectionHelper.CreateErrorInstance<TResponse>(validationError);
        if (!ReflectionHelper.IsGenericResult<TResponse>()) return await next(message, cancellationToken);
        return ReflectionHelper.CreateGenericErrorInstance<TResponse>(validationError);
    }
}