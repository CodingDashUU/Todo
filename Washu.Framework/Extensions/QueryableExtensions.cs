namespace Washu.Framework.Extensions;

using Identity;

public static class QueryableExtensions
{
    extension<T>(IQueryable<T> query)
    where T : IOwnedByUser
    {
        public IQueryable<T> ForCurrentUser(CurrentUser user) => 
            !user.UserId.HasValue 
                ? throw new InvalidOperationException("UserId was not found in the session data") 
                : query.Where(x => x.UserId == user.UserId);
        public IQueryable<T> ForUser(Guid userId) => 
            query.Where(x => x.UserId == userId);
    }
}