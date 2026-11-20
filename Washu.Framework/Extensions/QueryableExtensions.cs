namespace Washu.Framework.Extensions;

using Identity;
using Identity.Users;

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    where T : IOwnedByUser
    {
        public IQueryable<T> ForUser(Guid userId) => 
            query.Where(x => x.UserId == userId);
    }
}