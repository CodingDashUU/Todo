using Washu.Framework.Identity.Persistence;

namespace Washu.Framework.Entities.SoftDeleteEntity;

public interface IDeleteEntityTask
{
    string EntityName { get; }
    TimeSpan RunInterval { get; }
    string TaskName => $"Delete{EntityName}";
    Task<int> ExecuteAsync(ApplicationDbContext dbContext, CancellationToken ctx = default);
}
