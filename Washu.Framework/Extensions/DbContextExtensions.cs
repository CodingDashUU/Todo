namespace Washu.Framework.Extensions;

using Identity;
using Microsoft.EntityFrameworkCore;

public static class DbContextExtensions
{
    extension(DbContext context)
    {
        public void SetRowVersion<TEntity>(TEntity entity, uint newRowVersion)
            where TEntity : IAuditableEntity => 
            context.Entry(entity).Property(nameof(IAuditableEntity.RowVersion)).OriginalValue = newRowVersion;
    }
}