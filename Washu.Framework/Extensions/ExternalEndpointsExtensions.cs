using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Extensions;

using Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Claims;
using AspNetCore;
using Blazor;
using Identity.Login;
using Identity.Users;
using Mediator;
using Microsoft.AspNetCore.Http.HttpResults;
using Notifications;

public static class ExternalEndpointsExtensions
{
    extension(IEndpointRouteBuilder app)
    {
        public void MapExternalEndpoints()
        {
            var group = app.MapGroup("")
                .RequireRateLimiting(RateLimiterPolicy.StandardRateLimiter);
            group.MapGet(IdentityRoutes.ChallengeGoogle, ChallengeExternal);
            group.MapGet(IdentityRoutes.GoogleExternalCallback, ExternalCallback);
        }
    }
    private static async Task<IResult> ChallengeExternal(
        IMediator mediator, 
        [FromQuery] bool persistCookie, 
        [FromQuery] string returnUrl = "/")
    {
        var result = await mediator.Send(new ChallengeExternalCommand(returnUrl, persistCookie));
        return result switch
        {
            AlreadySignedIn(var safeReturnUrl) => TypedResults.Redirect(QueryHelpers.SafeReturnUrl(safeReturnUrl)),
            ShouldChallenge(var authenticationProperties) => TypedResults.Challenge(
                authenticationProperties,
                [GoogleDefaults.AuthenticationScheme]),
            null => throw new InvalidOperationException($"{nameof(ChallengeExternalResult)} should not be null")
        };
    }

    private static async Task<IResult> ExternalCallback (
        IMediator mediator,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        [FromQuery] bool persistCookie, 
        MessageStore store,
        [FromQuery] string returnUrl)
    {
        var info = await mediator.Send(new GetExternalLoginInfoQuery());
        if (info is null)
        {
            var errorUrl = QueryHelpers.AddQueryString(
                ApplicationRoutes.SignIn,
                new Dictionary<string, string?>
                {
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["ReturnUrl"] = returnUrl
                });
            return Results.Redirect(errorUrl);
        }
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email is null) return RedirectError(Message.Error("Registration Error", "Email was not provided or not found"));
        var existingUser = await mediator.Send(new FindUserByProviderKeyQuery(info.ProviderKey));
        if (existingUser is not null)
        {
            await mediator.Send(new SignInCommand(existingUser, persistCookie, info.LoginProvider));
            return TypedResults.Redirect(QueryHelpers.SafeReturnUrl(returnUrl));
        }
        var result = await mediator.Send(new CreateUserCommand(email, info.ProviderKey));
        return result switch
        {
            RoleNotFound => RedirectError(Message.Error("Login Error", "There was an issue while logging you in")),
            UserAlreadyExists => RedirectError(Message.Error("Identity Error", "That username or sign-in identity is already in use.")),
            CreateUserSuccess(var user) => await SignInAndRedirect(user),
            null => throw new InvalidOperationException($"{nameof(CreateUserResult)} cannot be null")
        };

        async Task<RedirectHttpResult> SignInAndRedirect(ApplicationUser user)
        {
            await mediator.Send(new SignInCommand(user, persistCookie));
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
    }
    
}