namespace Washu.Framework.Blazor;

using Constants;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

public sealed class AntiforgeryExceptionHandler : IExceptionHandler
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
        
        context.Response.Redirect(ApplicationRoutes.FormExpired);
        return ValueTask.FromResult(true);
    }
}