namespace Washu.Framework.Extensions;

using Blazor;
using Microsoft.AspNetCore.WebUtilities;

public static class QueryHelpersExtensions
{
    extension(QueryHelpers)
    {
        public static string SafeReturnUrl(string? returnUrl) =>
            !string.IsNullOrWhiteSpace(returnUrl) &&
            returnUrl[0] == '/' &&
            (returnUrl.Length == 1 || (returnUrl[1] != '/' && returnUrl[1] != '\\'))
                ? returnUrl
                : ApplicationRoutes.Home;
    }
}