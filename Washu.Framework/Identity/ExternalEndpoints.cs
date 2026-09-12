namespace Washu.Framework.Identity;

using Constants;
using Entities;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Notifications;
using System.Security.Claims;

public class ExternalEndpoints<TDbContext> (
    ApplicationSignInManager<TDbContext> signInManager,
    IDbContextFactory<TDbContext> dbContextFactory)
where TDbContext : ApplicationDbContext
{
    public void Map(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("")
            .RequireRateLimiting(RateLimiterPolicy.AuthLimiter);
        group.MapGet(CoreEndpointRoutes.External.ChallengeGoogle, ChallengeExternal);
        group.MapGet(CoreEndpointRoutes.External.ExternalCallback, ExternalCallback);
        group.MapPost(CoreEndpointRoutes.External.CompleteRegistration, CompleteRegistration);
    }

    private ChallengeHttpResult ChallengeExternal([FromQuery] bool persistCookie, [FromQuery] string returnUrl)
    {
        var properties =
            signInManager.ConfigureExternalAuthenticationProperties(
                GoogleDefaults.AuthenticationScheme,
                $"{CoreEndpointRoutes.External.ExternalCallback}?PersistCookie={persistCookie}&&ReturnUrl={returnUrl}");

        return TypedResults.Challenge(
            properties,
            [GoogleDefaults.AuthenticationScheme]);
    }

    private async Task<IResult> ExternalCallback([FromQuery] bool persistCookie, [FromQuery] string returnUrl)
    {
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null) return Results.Redirect($"{ApplicationRoutes.SignIn}?PersistCookie={persistCookie}&&ReturnUrl={returnUrl}");
        // 1. Sign in if Google link exists
        var result = await signInManager.ExternalLoginSignInAsync(
            info.ProviderKey, 
            isPersistent: persistCookie);
        return Results.Redirect(result.Succeeded ? SafeReturnUrl(returnUrl) :
            $"{ApplicationRoutes.Register}?PersistCookie={persistCookie}&&ReturnUrl={returnUrl}");
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
            return TypedResults.Redirect($"{ApplicationRoutes.Register}?messageId={id}&&ReturnUrl={returnUrl}");
        }
        var info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            var id = store.Store(
                Message.Error("Registration Error", "You need to continue with a sign-in provider to register"));

            return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}&&ReturnUrl={returnUrl}");
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email is null)
        {
            var id = store.Store(
                Message.Error("Registration Error", "Email was not provided or not found"));
            return TypedResults.Redirect($"{ApplicationRoutes.SignIn}?messageId={id}&&ReturnUrl={returnUrl}");
        }
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        var user = new ApplicationUser(request.Username, email, info.ProviderKey);
        if (await dbContext.Users
                .AnyAsync(u => u.NormalizedUsername == user.NormalizedUsername))
        {
            var id = store.Store(Message.Error("Login Error", "User already exists") );
            return TypedResults.Redirect($"{ApplicationRoutes.Register}?messageId={id}&&ReturnUrl={returnUrl}");
        }
        await dbContext.Users.AddAsync(user);
        // Adding user to role
        var normalizedRole = InitialUserRoles.User.ToUpperInvariant();
        var role = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.NormalizedName == normalizedRole);

        if (role is null) 
        {
            var id = store.Store(Message.Error("Login Error", "There was an issue whle logging you in") );
            return TypedResults.Redirect($"{ApplicationRoutes.Register}?messageId={id}&&ReturnUrl={returnUrl}");
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
        await dbContext.SaveChangesAsync();
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