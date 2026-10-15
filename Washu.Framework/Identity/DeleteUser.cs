namespace Washu.Framework.Identity;

using Microsoft.EntityFrameworkCore;
using Notifications;

public sealed record DeleteUserCommand(Guid UserId);

public sealed class DeleteUserHandler(IDbContextFactory<ApplicationDbContext> factory)
{
    public async Task<DeleteUserResult> HandleAsync(DeleteUserCommand command, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var rowsDeleted = await dbContext.Users
            .Where(u => u.Id == command.UserId)
            .ExecuteDeleteAsync(ct);
        return rowsDeleted == 0 
            ? DeleteUserResult.AccountNotFound
            : DeleteUserResult.Success;
    }
}

public enum DeleteUserResult : byte { Success, AccountNotFound }

