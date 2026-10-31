namespace Washu.Framework.Extensions;

using Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public static class EndpointConventionBuilderExtensions
{
    extension(IEndpointConventionBuilder builder)
    {
        public IEndpointConventionBuilder RequirePermission(string permission) => builder.AddEndpointFilter(async (context, next) =>
        {
            var handler = context.HttpContext.RequestServices.GetRequiredService<HasPermissionHandler>();
            if (!await handler.HandleAsync(new HasPermissionQuery(permission)))
                return TypedResults.Forbid();
            return await next(context);
        });
    }
}