namespace Washu.Framework.Identity.Login;

using Extensions;
using Mediator;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Persistence;

public sealed record ChallengeExternalCommand(string ReturnUrl, bool ShouldPersistCookie) : ICommand<ChallengeExternalResult>;

public sealed class ChallengeExternalHandler(
    IHttpContextAccessor httpContextAccessor,
    IDbContextFactory<ApplicationDbContext> factory
    ) : ICommandHandler<ChallengeExternalCommand, ChallengeExternalResult>
{
    public async ValueTask<ChallengeExternalResult> Handle(ChallengeExternalCommand command, CancellationToken cancellationToken)
    {
        var context = httpContextAccessor.HttpContext;
        var safeReturnUrl = QueryHelpers.SafeReturnUrl(command.ReturnUrl);
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        if (context?.User is { Identity.IsAuthenticated: true, UserId: {} userGuid}
            && await dbContext.Users.AnyAsync(u => u.Id == userGuid, cancellationToken))
            return new AlreadySignedIn(safeReturnUrl);
        var callbackUrl = QueryHelpers.AddQueryString(
            IdentityRoutes.GoogleExternalCallback,
            new Dictionary<string, string?>
            {
                ["PersistCookie"] = command.ShouldPersistCookie.ToString(),
                ["ReturnUrl"] = safeReturnUrl
            });
        var properties = new AuthenticationProperties { RedirectUri = callbackUrl, Items =
        {
            ["LoginProvider"] = GoogleDefaults.AuthenticationScheme
        } };

        return new ShouldChallenge(properties);
    }
}

public sealed record AlreadySignedIn(string ReturnUrl);
public sealed record ShouldChallenge(AuthenticationProperties Properties);
public readonly union ChallengeExternalResult(AlreadySignedIn, ShouldChallenge);