namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class AuditedEntityInterceptor : SaveChangesInterceptor
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
            .Where(e => e is { Entity: IAuditedEntity, State: EntityState.Modified or EntityState.Added });

        foreach (var entry in entries)
        {
            if (entry.State is EntityState.Added)
                entry.Property(nameof(IAuditedEntity.CreationDate)).CurrentValue = DateTimeOffset.UtcNow;
            entry.Property(nameof(IAuditedEntity.LastModifiedDate)).CurrentValue = DateTimeOffset.UtcNow;
        }
    }
}