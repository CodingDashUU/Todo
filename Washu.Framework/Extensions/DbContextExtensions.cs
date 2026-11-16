namespace Washu.Framework.Extensions;

using Entities;
using Identity;
using Microsoft.EntityFrameworkCore;

public static class DbContextExtensions
{
    extension(DbContext context)
    {
        public void UpdateEntity<TEntity>(TEntity entity, uint newRowVersion)
            where TEntity : IAuditableEntity
        {
            context.Entry(entity).Property(nameof(IAuditableEntity.RowVersion)).OriginalValue = newRowVersion;
            context.Entry(entity).Property(nameof(IAuditableEntity.LastModifiedDate)).CurrentValue = DateTimeOffset.UtcNow;
            context.Update(entity);
        }
    }
}