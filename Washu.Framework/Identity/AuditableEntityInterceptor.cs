namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class AuditableEntityInterceptor : SaveChangesInterceptor
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
            .Where(e => e is { Entity: IAuditableEntity, State: EntityState.Modified or EntityState.Added });

        foreach (var entry in entries)
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditableEntity.CreationDate)).CurrentValue = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditableEntity.LastModifiedDate)).CurrentValue = DateTimeOffset.UtcNow;
                    break;
            }
    }
}