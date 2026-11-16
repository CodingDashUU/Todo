namespace Washu.Framework.Extensions;

using Identity;
using Identity.Roles;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

public static class EndpointConventionBuilderExtensions
{
    extension(IEndpointConventionBuilder builder)
    {
        public IEndpointConventionBuilder RequirePermission(string permission) => builder.AddEndpointFilter(async (context, next) =>
        {
            var mediator = context.HttpContext.RequestServices.GetRequiredService<IMediator>();
            if (!await mediator.Send(new HasPermissionQuery(permission)))
                return TypedResults.Forbid();
            return await next(context);
        });
    }
}