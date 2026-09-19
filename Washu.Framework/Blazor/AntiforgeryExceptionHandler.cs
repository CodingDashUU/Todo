namespace Washu.Framework.Blazor;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Notifications;

public sealed class AntiforgeryExceptionHandler(MessageStore store) : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException
            {
                InnerException: AntiforgeryValidationException
            }) return ValueTask.FromResult(false);
        var id = store.Store(Message.Error("Form Error",
                "There was a problem while processing your request. Please try again"));
        context.Response.Redirect($"{context.Request.Headers.Referer.FirstOrDefault() ?? ApplicationRoutes.Home}?messageId={id}");
        return ValueTask.FromResult(true);
    }
}