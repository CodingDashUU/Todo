using Microsoft.EntityFrameworkCore;
using Washu.Framework.Entities.SoftDeleteEntity;
using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Identity.Users.DeleteUser;

public sealed class DeleteUserTask : IDeleteEntityTask
{
    public string EntityName => nameof(ApplicationUser);
    public TimeSpan RunInterval => TimeSpan.FromHours(2);
    
    public async Task<int> ExecuteAsync(ApplicationDbContext dbContext,
        CancellationToken ctx = default) =>
        await dbContext.Users.IgnoreQueryFilters().Where(u => u.IsDeleted).ExecuteDeleteAsync(ctx);
}