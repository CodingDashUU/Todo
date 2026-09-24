namespace Washu.Framework.Identity;

using AspNetCore;
using Blazor;
using Entities;
using Extensions;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Notifications;
using Npgsql;
using System.Security.Claims;

public class ExternalEndpoints<TDbContext> (
    ApplicationSignInManager<TDbContext> signInManager,
    ApplicationUserManager<TDbContext> userManager,
    IDbContextFactory<TDbContext> dbContextFactory)
where TDbContext : ApplicationDbContext
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .RequireRateLimiting(RateLimiterPolicy.AuthLimiter);
        group.MapGet(IdentityRoutes.ChallengeGoogle, ChallengeExternal);
        group.MapGet(IdentityRoutes.ExternalCallback, ExternalCallback);
        group.MapPost(IdentityRoutes.CompleteRegistration, CompleteRegistration);
    }

    private async Task<IResult> ChallengeExternal(HttpContext context, [FromQuery] bool persistCookie, [FromQuery] string returnUrl = "/")
    {
        if (context.User.Identity is { IsAuthenticated: true }
            && context.User.FindUserId() is { } userGuid
            && await userManager.FindByIdAsync(userGuid) is not null)
            return TypedResults.Redirect(SafeReturnUrl(returnUrl));
        var callbackUrl = QueryHelpers.AddQueryString(
            IdentityRoutes.ExternalCallback,
            new Dictionary<string, string?>
            {
                ["PersistCookie"] = persistCookie.ToString(),
                ["ReturnUrl"] = returnUrl
            });
        var properties =
            signInManager.ConfigureExternalAuthenticationProperties(
                GoogleDefaults.AuthenticationScheme,
                callbackUrl);

        return TypedResults.Challenge(
            properties,
            [GoogleDefaults.AuthenticationScheme]);
    }

    private async Task<IResult> ExternalCallback([FromQuery] bool persistCookie, [FromQuery] string returnUrl)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
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
        var url = QueryHelpers.AddQueryString(
            ApplicationRoutes.Register,
            new Dictionary<string, string?>
            {
                ["PersistCookie"] = persistCookie.ToString(),
                ["ReturnUrl"] = returnUrl
            });
        var result = await signInManager.ExternalLoginSignInAsync(
            info.ProviderKey, 
            isPersistent: persistCookie);
        return Results.Redirect(result.Succeeded ? SafeReturnUrl(returnUrl) :
            url);
    }

    private async Task<RedirectHttpResult> CompleteRegistration(
        [FromForm] Registration.Model request,
        [FromQuery] bool persistCookie,
        [FromQuery] string returnUrl,
        HttpContext context,
        MessageStore store)
    {
        var validator = new Registration.Validator();
        var result = await validator.ValidateAsync(request);
        if (!result.IsValid)
        {
            var id = store.Store([.. result.Errors.Select(e => Message.Error("Invalid Registration Details",$"{e.PropertyName}: {e.ErrorMessage}"))]);
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.Register,
                new Dictionary<string, string?>
                {
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["MessageId"] = id,
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            var id = store.Store(
                Message.Error("Registration Error", "You need to continue with a sign-in provider to register"));
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.SignIn,
                new Dictionary<string, string?>
                {
                    ["MessageId"] = id,
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email is null)
        {
            var id = store.Store(
                Message.Error("Registration Error", "Email was not provided or not found"));
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.SignIn,
                new Dictionary<string, string?>
                {
                    ["MessageId"] = id,
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = new ApplicationUser(request.Username, email, info.ProviderKey);
        if (await dbContext.Users
                .AnyAsync(u => u.Username == user.Username))
        {
            var id = store.Store(Message.Error("Login Error", "User already exists") );
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.Register,
                new Dictionary<string, string?>
                {
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["MessageId"] = id,
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }
        await dbContext.Users.AddAsync(user);
        // Adding user to role
        var role = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == InitialUserRoles.User);

        if (role is null) 
        {
            var id = store.Store(Message.Error("Login Error", "There was an issue while logging you in") );
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.Register,
                new Dictionary<string, string?>
                {
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["MessageId"] = id,
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }
        if (!await dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id))
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            await dbContext.UserRoles.AddAsync(userRole);
        }
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException{ SqlState: PostgresErrorCodes.UniqueViolation })
        {
            var id = store.Store(Message.Error("Identity Error",
                "That username or sign-in identity is already in use."));
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.Register,
                new Dictionary<string, string?>
                {
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["MessageId"] = id,
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }
        await signInManager.SignInAsync(user, isPersistent: persistCookie);
            
        return TypedResults.Redirect(SafeReturnUrl(returnUrl));        
    }
    private static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) &&
        returnUrl[0] == '/' &&
        (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'))
            ? returnUrl
            : ApplicationRoutes.Home;
    
}