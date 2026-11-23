namespace Washu.Framework.Identity.Users;

using Blazor;
using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications;
using Npgsql;
using Persistence;
using Roles;

public sealed record CreateUserCommand(string Email, string GoogleSubject) : ICommand<Result<ApplicationUser>>;
public sealed class CreateUserHandler(
    IDbContextFactory<ApplicationDbContext> factory,
    ILogger<CreateUserHandler> logger,
    ISender sender
    ) : ICommandHandler<CreateUserCommand, Result<ApplicationUser>>
{
    public async ValueTask<Result<ApplicationUser>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken); 
        var username = await sender.Send(
            new GenerateUniqueUsernameCommand(command.Email), cancellationToken);
        var user = new ApplicationUser(username, command.Email, command.GoogleSubject);
        dbContext.Users.Add(user);
        
        var role = await dbContext.Roles
            .AsNoTracking()
            .SingleOrDefaultAsync(r => r.Name == InitialUserRoles.User, cancellationToken: cancellationToken);
        if (role is null) 
        {
            logger.LogError("Role {Role} could not be found", InitialUserRoles.User);
            return new InternalError();
        }
        
        if (!await dbContext.UserRoles
                .AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id, cancellationToken: cancellationToken))
        {
            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id
            };
            await dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return user;
    }
}