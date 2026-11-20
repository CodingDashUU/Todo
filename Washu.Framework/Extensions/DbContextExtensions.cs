namespace Washu.Framework.Extensions;

using Entities;
using Identity;
using Microsoft.EntityFrameworkCore;

public static class DbContextExtensions
{
    extension(DbContext context)
    {
        public void UpdateEntity<TEntity>(TEntity entity, uint callerRowVersion)
            where TEntity : IAuditableEntity
        {
            context.Entry(entity).Property(nameof(IAuditableEntity.RowVersion)).OriginalValue = callerRowVersion;
            context.Entry(entity).Property(nameof(IAuditableEntity.LastModifiedDate)).CurrentValue = DateTimeOffset.UtcNow;
            context.Update(entity);
        }
        public void UpdateRowVersion<TEntity>(TEntity entity, uint callerRowVersion)
            where TEntity : IAuditableEntity =>
            context.Entry(entity).Property(nameof(IAuditableEntity.RowVersion)).OriginalValue = callerRowVersion;
    }
}