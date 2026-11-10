using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Entities.SoftDeleteEntity;

public interface IDeleteEntityTask
{
    Task<(string EntityName, int RowsDeleted)> ExecuteAsync(ApplicationDbContext dbContext, CancellationToken ctx = default);
}
