namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

public class ApplicationSignInManager<TDbContext>(
    IHttpContextAccessor httpContextAccessor,
    IDbContextFactory<TDbContext> dbFactory,
    IAuthenticationSchemeProvider schemeProvider,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory)
where TDbContext : ApplicationDbContext
{
    private const string LoginProviderKey = "LoginProvider";
    private const string XsrfKey = "XsrfId";

    private HttpContext Context => httpContextAccessor.HttpContext 
        ?? throw new InvalidOperationException("An active HttpContext is required.");

    // 1. ConfigureExternalAuthenticationProperties with full XsrfKey support
    public virtual AuthenticationProperties ConfigureExternalAuthenticationProperties(
        string provider, 
        string? redirectUrl, 
        string? userId = null)
    {
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl ?? "/", Items =
        {
            [LoginProviderKey] = provider
        } };

        if (userId is not null) properties.Items[XsrfKey] = userId;

        return properties;
    }

    // 2. GetExternalLoginInfoAsync with full Xsrf validation & Token preservation
    public virtual async Task<ExternalLoginInfo?> GetExternalLoginInfoAsync(string? expectedXsrf = null)
    {
        var auth = await Context.AuthenticateAsync(IdentityConstants.ExternalScheme);
        var items = auth.Properties?.Items;

        if (auth.Principal is null || items is null || !items.TryGetValue(LoginProviderKey, out var provider)) return null;

        if (expectedXsrf is not null)
            if (!items.TryGetValue(XsrfKey, out var userId) || userId != expectedXsrf) return null;

        var providerKey = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier) 
                          ?? auth.Principal.FindFirstValue("sub");

        if (providerKey is null || provider is null) return null;

        var schemes = await schemeProvider.GetAllSchemesAsync();
        var providerDisplayName = schemes.FirstOrDefault(p => p.Name == provider)?.DisplayName ?? provider;

        return new ExternalLoginInfo(auth.Principal, provider, providerKey, providerDisplayName)
        {
            AuthenticationTokens = auth.Properties?.GetTokens(),
            AuthenticationProperties = auth.Properties
        };
    }

    // 3. ExternalLoginSignInAsync backed by PostgreSQL EF Core
    public virtual async Task<SignInResult> ExternalLoginSignInAsync(
        string loginProvider, 
        string providerKey, 
        bool isPersistent, 
        bool bypassTwoFactor = false)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var user = await db.UserLogins
            .AsNoTracking()
            .Where(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey)
            .Select(l => l.User)
            .FirstOrDefaultAsync();

        if (user is null) return SignInResult.Failed;

        if (user.IsBanned) return SignInResult.LockedOut;

        await SignInAsync(user, isPersistent, loginProvider);
        return SignInResult.Success;
    }

    // 4. SignInAsync overload accepting AuthenticationProperties
    public virtual Task SignInAsync(
        ApplicationUser user, 
        AuthenticationProperties authenticationProperties, 
        string? authenticationMethod = null)
    {
        var additionalClaims = new List<Claim>();
        if (authenticationMethod != null) additionalClaims.Add(new Claim(ClaimTypes.AuthenticationMethod, authenticationMethod));

        return SignInWithClaimsAsync(user, authenticationProperties, additionalClaims);
    }

    // 5. SignInAsync simplified overload
    public virtual Task SignInAsync(ApplicationUser user, bool isPersistent, string? authenticationMethod = null) => SignInAsync(user, new AuthenticationProperties { IsPersistent = isPersistent }, authenticationMethod);

    // 6. Full SignInWithClaimsAsync updating HttpContext.User & clearing external cookie
    public virtual async Task SignInWithClaimsAsync(
        ApplicationUser user, 
        AuthenticationProperties? authenticationProperties, 
        IEnumerable<Claim> additionalClaims)
    {
        var userPrincipal = await claimsFactory.CreateAsync(user);

        if (userPrincipal.Identity is ClaimsIdentity identity)
            foreach (var claim in additionalClaims)
                identity.AddClaim(claim);

        authenticationProperties ??= new AuthenticationProperties();

        // Sign into application cookie scheme
        await Context.SignInAsync(
            IdentityConstants.ApplicationScheme,
            userPrincipal,
            authenticationProperties);

        // Immediate update to HttpContext.User for current request execution
        Context.User = userPrincipal;

        // Clean up temporary external login cookie
        await Context.SignOutAsync(IdentityConstants.ExternalScheme);
    }
}