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
using Identity.Entities;
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
            group.MapGet(IdentityRoutes.ExternalCallback, ExternalCallback);
        }
    }
    private static async Task<IResult> ChallengeExternal(
        HttpContext context, 
        FindUserHandler handler,
        [FromQuery] bool persistCookie, 
        [FromQuery] string returnUrl = "/")
    {
        if (context.User is { Identity.IsAuthenticated: true, UserId: {} userGuid}
            && await handler.HandleAsync(new FindUserQuery(userGuid)) is not null)
            return TypedResults.Redirect(SafeReturnUrl(returnUrl));
        var callbackUrl = QueryHelpers.AddQueryString(
            IdentityRoutes.ExternalCallback,
            new Dictionary<string, string?>
            {
                ["PersistCookie"] = persistCookie.ToString(),
                ["ReturnUrl"] = returnUrl
            });
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl, Items =
        {
            ["LoginProvider"] = GoogleDefaults.AuthenticationScheme
        } };

        return TypedResults.Challenge(
            properties,
            [GoogleDefaults.AuthenticationScheme]);
    }

    private static async Task<IResult> ExternalCallback (
        SignInHandler signInHandler,
        GetExternalLoginInfoHandler getExternalLoginInfoHandler,
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        [FromQuery] bool persistCookie, 
        MessageStore store,
        [FromQuery] string returnUrl)
    {
        var info = await getExternalLoginInfoHandler.HandleAsync();
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

        var existingUser = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.GoogleSubject == info.ProviderKey);

        if (existingUser is not null)
        {
            await signInHandler.HandleAsync(new SignInCommand(existingUser, persistCookie, info.LoginProvider));
            return TypedResults.Redirect(SafeReturnUrl(returnUrl));
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
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }
        if (await dbContext.Users.AnyAsync(u => u.GoogleSubject == info.ProviderKey || u.Email == email))
        {
            var id = store.Store(Message.Error("Registration Error", "This account or email is already registered. Please sign in."));
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.SignIn,
                new Dictionary<string, string?>
                {
                    ["MessageId"] = id,
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }

        var username = await GenerateValidUsernameAsync(dbContextFactory, email);
        var user = new ApplicationUser(username, email, info.ProviderKey);
        await dbContext.Users.AddAsync(user);
        var role = await dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == InitialUserRoles.User);
        if (role is null) 
        {
            var id = store.Store(Message.Error("Login Error", "There was an issue while logging you in") );
            var url = QueryHelpers.AddQueryString(
                ApplicationRoutes.SignIn,
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
                ApplicationRoutes.SignIn,
                new Dictionary<string, string?>
                {
                    ["MessageId"] = id,
                    ["PersistCookie"] = persistCookie.ToString(),
                    ["ReturnUrl"] = returnUrl
                });
            return TypedResults.Redirect(url);
        }
        await signInHandler.HandleAsync(new SignInCommand(user, persistCookie));
            
        return TypedResults.Redirect(SafeReturnUrl(returnUrl));  
    }
    
    private static string SafeReturnUrl(string? returnUrl) =>
        !string.IsNullOrWhiteSpace(returnUrl) &&
        returnUrl[0] == '/' &&
        (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'))
            ? returnUrl
            : ApplicationRoutes.Home;
    
    private static async Task<string> GenerateValidUsernameAsync(
        IDbContextFactory<ApplicationDbContext> dbContextFactory,
        string email) 
    {
        var raw = email.Split('@')[0];
        var clean = new string(raw.Where(UsernameRules.ValidCharacters.Contains).ToArray());
        clean = clean.TrimStart('_', '-');
        
        if (string.IsNullOrEmpty(clean) || !char.IsAsciiLetter(clean[0])) clean = $"u_{clean}".TrimEnd('_', '-');
        
        if (clean.Length < UsernameRules.MinUsernameLength)
            clean = $"{clean}_{Random.Shared.Next(100, 999)}";
        
        const int maxBaseLength = UsernameRules.MaxUsernameLength - 5;
        if (clean.Length > maxBaseLength) clean = clean[..maxBaseLength];

        var candidate = clean;
        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        while (await dbContext.Users.AnyAsync(u => u.Username == candidate)) candidate = $"{clean}_{Random.Shared.Next(100, 9999)}";

        return candidate;
    }
    
}