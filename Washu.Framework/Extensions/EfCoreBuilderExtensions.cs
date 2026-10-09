namespace Washu.Framework.Extensions;

using Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

public static class EfCoreBuilderExtensions
{
    extension<TEntity>(EntityTypeBuilder<TEntity> entity) where TEntity : class
    {
        public void HasUniqueIndex(Expression<Func<TEntity, object?>> indexExpression) => entity.HasIndex(indexExpression).IsUnique();
    }
    extension<TEntity>(TableBuilder<TEntity> entity) where TEntity : class
    {
        public void HasMaxLengthCheckConstraint(Expression<Func<TEntity, string>> property, ushort maxLength)
        {
            var propertyName = property.PropertyInfo.Name;
            var entityName = typeof(TEntity).Name;
            entity.HasCheckConstraint(
                $"CK_{entityName}_{propertyName}_MaxLength",
                $"length(\"{propertyName}\") > 0 AND length(\"{propertyName}\") <= {maxLength}"
            );
        }
        public void HasAllowedCharactersCheckConstraint(
            Expression<Func<TEntity, string>> property, 
            string allowedCharacters)
        {
            var propertyName = property.PropertyInfo.Name;
            var entityName = typeof(TEntity).Name;
            var escapedChars = allowedCharacters.Replace("'", "''");
            entity.HasCheckConstraint(
                $"CK_{entityName}_{propertyName}_AllowedChar",
                $"length(\"{propertyName}\") > 0 AND translate(\"{propertyName}\", '{escapedChars}', '') = ''"
            );
        }

        public void HasAllowedFirstCharacterCheckConstraint(
            Expression<Func<TEntity, string>> property,
            string allowedCharacters)
        {
            var propertyName = property.PropertyInfo.Name;
            var entityName = typeof(TEntity).Name;
            var escapedChars = allowedCharacters.Replace("'", "''");
            entity.HasCheckConstraint(
                $"CK_{entityName}_{propertyName}_AllowedFirstChar",
                $"\"{propertyName}\" <> '' AND position(left(\"{propertyName}\", 1) in '{escapedChars}') > 0"
            );
        }
        public void HasEmailCheckConstraint(Expression<Func<TEntity, string>> property)
        {
            var propertyName = property.PropertyInfo.Name;
            var entityName = typeof(TEntity).Name;
            entity.HasCheckConstraint(
                $"CK_{entityName}_{propertyName}_ValidEmail",
                $"\"{propertyName}\" ~* '^[^[:space:]@]+@[^[:space:]@]+\\.[^[:space:]@]+$'"
            );
        }

        public void HasDateLessThanOrEqualToOtherDateCheckConstraint(Expression<Func<TEntity, DateTimeOffset>> firstDate,
            Expression<Func<TEntity, DateTimeOffset>> secondDate)
        {
            var firstDateName = firstDate.PropertyInfo.Name;
            var secondDateName = secondDate.PropertyInfo.Name;
            var entityName = typeof(TEntity).Name;
            entity.HasCheckConstraint(
                $"CK_{entityName}_{firstDateName}_LTE_{secondDateName}", // LTE - Less Than or Equal To
                $"\"{firstDateName}\" <= \"{secondDateName}\""
                );
        }
        public void HasDateLessThanOrEqualToNullableDateCheckConstraint(Expression<Func<TEntity, DateTimeOffset>> firstDate,
            Expression<Func<TEntity, DateTimeOffset?>> secondDate)
        {
            var firstDateName = firstDate.PropertyInfo.Name;
            var secondDateName = secondDate.PropertyInfo.Name;
            var entityName = typeof(TEntity).Name;
            entity.HasCheckConstraint(
                $"CK_{entityName}_{firstDateName}_LTE_N_{secondDateName}", // LTE - Less Than or Equal To; N - Nullable
                $"\"{secondDateName}\" IS NULL OR \"{firstDateName}\" <= \"{secondDateName}\""
            );
        }
    }
}
