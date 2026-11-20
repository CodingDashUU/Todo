namespace Washu.Framework.Identity.Authentication;

using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Users;

public sealed record SignInCommand(ApplicationUser User, bool IsPersistent, string? AuthenticationMethod = null) : ICommand;

public sealed class SignInHandler(
    IHttpContextAccessor httpContextAccessor,
    IPublisher publisher
    ) : ICommandHandler<SignInCommand>
{
    public async ValueTask<Unit> Handle(SignInCommand command, CancellationToken ctx = new())
    {
        var context = httpContextAccessor.HttpContext ??
                      throw new InvalidOperationException("An active HttpContext is required");

        var authenticationProperties = new AuthenticationProperties { IsPersistent = command.IsPersistent };
        var userPrincipal = ClaimsPrincipal.Create(command.User);

        if (command.AuthenticationMethod is not null
            && userPrincipal.Identity is ClaimsIdentity identity)
            identity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, command.AuthenticationMethod));
        
        await context.SignInAsync(
            IdentityConstants.ApplicationScheme,
            userPrincipal,
            authenticationProperties);
        await publisher.Publish(new UserSigningInNotification(context, command.User.Id), ctx);
        context.User = userPrincipal;
        await context.SignOutAsync(IdentityConstants.ExternalScheme);
        return Unit.Value;
    }
}