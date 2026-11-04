namespace Washu.Framework.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class SoftDeletableEntityInterceptor  : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        UpdateFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
    
    private static void UpdateFields(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e is { Entity: ISoftDeletableEntity, State: EntityState.Deleted });

        foreach (var entry in entries)
        {
            entry.Property(nameof(ISoftDeletableEntity.IsDeleted)).CurrentValue = true;
            entry.State = EntityState.Unchanged;
        }
    }
}