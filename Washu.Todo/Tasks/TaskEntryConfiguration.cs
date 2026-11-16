namespace Washu.Todo.Persistence;

using Domain;
using Framework.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class TaskEntryConfiguration : IEntityTypeConfiguration<TaskEntry>
{
    public void Configure(EntityTypeBuilder<TaskEntry> entity)
    {
        entity.HasUniqueIndex(t => new { t.TodoListId, t.Name });
        entity.ToTable(table =>
        {
            table.HasMaxLengthCheckConstraint(p => p.Name, 64);
            table.HasAllowedCharactersCheckConstraint(p => p.Name, TaskName.AllowedCharacters);
            table.HasDateLessThanOrEqualToOtherDateCheckConstraint(p => p.DateCreated, p => p.GoalDate);
            table.HasDateLessThanOrEqualToNullableDateCheckConstraint(p => p.DateCreated, p => p.DateCompleted);
        });
    }
}