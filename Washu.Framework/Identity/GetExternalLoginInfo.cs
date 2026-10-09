namespace Washu.Framework.Identity;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

public sealed class GetExternalLoginInfoHandler(
    IHttpContextAccessor httpContextAccessor,
    IAuthenticationSchemeProvider schemeProvider
    )
{
    public async Task<ExternalLoginInfo?> HandleAsync()
    {
        var context = httpContextAccessor.HttpContext 
                                   ?? throw new InvalidOperationException("An active HttpContext is required.");
        var auth = await context.AuthenticateAsync(IdentityConstants.ExternalScheme);
        var items = auth.Properties?.Items;

        if (auth.Principal is null || items is null || !items.TryGetValue("LoginProvider", out var provider)) return null;
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
}