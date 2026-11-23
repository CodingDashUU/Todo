namespace Washu.Framework.Identity.Authentication;

using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Washu.Framework.AspNetCore;
using Washu.Framework.Blazor;

public sealed class SignOutEndpoints
{
    public static void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet(IdentityRoutes.SignOut, async (HttpContext context, ISender sender, [FromQuery] string? messageId) =>
            {
                await sender.Send(new SignOutCommand(context));
                var redirectUrl = string.IsNullOrWhiteSpace(messageId)
                    ? ApplicationRoutes.SignIn
                    : $"{ApplicationRoutes.SignIn}?messageId={messageId}";
                return TypedResults.Redirect(redirectUrl);
            })
            .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
        app.MapGet(IdentityRoutes.SignOutFromAllSessions, async (HttpContext context,  ISender sender,[FromQuery] string? messageId) =>
            {
                await sender.Send(new SignOutFromAllSessionsCommand(context));
                var targetUrl = string.IsNullOrWhiteSpace(messageId)
                    ? ApplicationRoutes.SignIn
                    : $"{ApplicationRoutes.SignIn}?messageId={messageId}";

                return TypedResults.Redirect(targetUrl);
            })
            .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
    }
}