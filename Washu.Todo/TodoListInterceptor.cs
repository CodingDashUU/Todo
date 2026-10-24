namespace Washu.Todo;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class TodoListInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        UpdateFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        UpdateFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateFields(DbContext? context)
    {
        if (context is null) return;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Modified && e.Entity is TodoList);

        foreach (var entry in entries)
        {
            entry.Property(nameof(TodoList.LastModified)).CurrentValue = DateTimeOffset.UtcNow;
            entry.Property(nameof(TodoList.VersionId)).CurrentValue = Guid.CreateVersion7();
        }
    }
}