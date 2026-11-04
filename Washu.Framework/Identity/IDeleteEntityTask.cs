namespace Washu.Framework.Identity;

using Microsoft.EntityFrameworkCore;
using Entities;
public interface IDeleteEntityTask
{
    Task<(string EntityName, int RowsDeleted)> ExecuteAsync(ApplicationDbContext dbContext, CancellationToken ctx = default);
}

public sealed class DeleteUserTask : IDeleteEntityTask
{
    public async Task<(string EntityName, int RowsDeleted)> ExecuteAsync(ApplicationDbContext dbContext,
        CancellationToken ctx = default)
    {
        var rowsDeleted = await dbContext.Users.IgnoreQueryFilters().Where(u => u.IsDeleted).ExecuteDeleteAsync(ctx);
        return (nameof(ApplicationUser), rowsDeleted);
    }
}
