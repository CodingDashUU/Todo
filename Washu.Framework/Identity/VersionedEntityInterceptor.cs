namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class VersionedEntityInterceptor : SaveChangesInterceptor
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
            .Where(e => e is { Entity: IVersionedEntity, State: EntityState.Modified or EntityState.Added });

        foreach (var entry in entries) entry.Property(nameof(IVersionedEntity.VersionId)).CurrentValue = Guid.CreateVersion7();
    }
}