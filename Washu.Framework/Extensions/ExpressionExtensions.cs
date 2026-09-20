namespace Washu.Framework.Extensions;

using System.Linq.Expressions;
using System.Reflection;

public static class ExpressionExtensions
{
    extension<TIn, TOut>(Expression<Func<TIn, TOut>> property)
    {
        public PropertyInfo GetPropertyInfo() 
            => property.Body is not MemberExpression { Member: PropertyInfo propertyInfo } 
                ? throw new ArgumentException("The expression must resolve directly to a property", nameof(property)) 
                : propertyInfo;   
    }
}