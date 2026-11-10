using Microsoft.EntityFrameworkCore;
using Washu.Framework.Entities.SoftDeleteEntity;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Identity.Users.DeleteUser;

public sealed class DeleteUserTask : IDeleteEntityTask
{
    public async Task<(string EntityName, int RowsDeleted)> ExecuteAsync(ApplicationDbContext dbContext,
        CancellationToken ctx = default)
    {
        var rowsDeleted = await dbContext.Users.IgnoreQueryFilters().Where(u => u.IsDeleted).ExecuteDeleteAsync(ctx);
        return (nameof(ApplicationUser), rowsDeleted);
    }
}