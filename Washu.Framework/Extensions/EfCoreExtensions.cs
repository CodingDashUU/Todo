namespace Washu.Framework.Extensions;

using Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

public static class EfCoreExtensions
{
    extension<TEntity>(TableBuilder<TEntity> entity) where TEntity : class
    {
        public void HasMaxLengthCheckConstraint(Expression<Func<TEntity, string>> property, ushort maxLength)
        {
            var propertyInfo = property.GetPropertyInfo();
            var entityName = typeof(TEntity).Name;
            var propertyName = propertyInfo.Name;
            entity.HasCheckConstraint(
                $"CK_{entityName}_{propertyName}_MaxLength",
                $"length(\"{propertyName}\") <= {maxLength}"
            );
        }
    }
}
