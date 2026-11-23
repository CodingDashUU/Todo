namespace Washu.Framework.Identity.Authentication;

using AspNetCore;
using Blazor;
using Extensions;
using Mediator;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Notifications;
using System.Diagnostics;
using System.Security.Claims;
using Users;

public static class ExternalEndpoints
{
    public static void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
        group.MapGet(IdentityRoutes.ChallengeGoogle,
            async (ISender sender, [FromQuery] bool persistCookie, [FromQuery] string returnUrl = "/") =>
            {
                var result = await sender.Send(new ChallengeExternalCommand(returnUrl, persistCookie));
                return result switch
                {
                    AlreadySignedIn(var safeReturnUrl) => Results.Redirect(QueryHelpers.SafeReturnUrl(safeReturnUrl)),
                    ShouldChallenge(var authenticationProperties) => Results.Challenge(
                        authenticationProperties,
                        [GoogleDefaults.AuthenticationScheme]),
                    null => throw new InvalidOperationException($"{nameof(ChallengeExternalResult)} should not be null")
                };
            });
        group.MapGet(IdentityRoutes.GoogleExternalCallback, async (ISender sender, MessageStore store,
            [FromQuery] bool persistCookie, [FromQuery] string returnUrl) =>
        {
            var info = await sender.Send(new GetExternalLoginInfoQuery());
            if (info is null)
            {
                var errorUrl = QueryHelpers.AddQueryString(
                    ApplicationRoutes.SignIn,
                    new Dictionary<string, string?>
                    {
                        ["PersistCookie"] = persistCookie.ToString(), ["ReturnUrl"] = returnUrl
                    });
                return Results.Redirect(errorUrl);
            }

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (email is null)
                return RedirectError(Message.Error("Registration Error", "Email was not provided or not found"));
            var existingUser = await sender.Send(new FindUserByProviderKeyQuery(info.ProviderKey));
            if (existingUser is not null)
            {
                await sender.Send(new SignInCommand(existingUser, persistCookie, info.LoginProvider));
                return TypedResults.Redirect(QueryHelpers.SafeReturnUrl(returnUrl));
            }

            var result = await sender.Send(new CreateUserCommand(email, info.ProviderKey));
            return result switch
            {
                Error e => RedirectError(Message.Error("Login Error", e.Message)),
                ApplicationUser u => await SignInAndRedirect(u),
                null => throw new UnreachableException()
            };

            async Task<RedirectHttpResult> SignInAndRedirect(ApplicationUser user)
            {
                await sender.Send(new SignInCommand(user, persistCookie));
                return TypedResults.Redirect(QueryHelpers.SafeReturnUrl(returnUrl));
            }

            RedirectHttpResult RedirectError(Message message) => TypedResults.Redirect(QueryHelpers.AddQueryString(
                ApplicationRoutes.SignIn,
                new Dictionary<string, string?>
                {
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["MessageId"] =
                        store.Store(message),
                    ["ReturnUrl"] = returnUrl
                }));
        });
    }
}