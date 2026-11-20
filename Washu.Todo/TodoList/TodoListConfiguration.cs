namespace Washu.Todo.TodoList;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Washu.Framework.Extensions;

public class TodoListConfiguration : IEntityTypeConfiguration<TodoList>
{
    public void Configure(EntityTypeBuilder<TodoList> entity)
    {
        entity.HasSoftDeleteQueryFilter();
        entity.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId);
        entity.ToTable(table =>
        {
            table.HasMaxLengthCheckConstraint(p => p.Name, 64);
            table.HasAllowedCharactersCheckConstraint(p => p.Name, ListName.AllowedCharacters + "~");
            table.HasDateLessThanOrEqualToNullableDateCheckConstraint(p => p.CreationDate, p => p.LastModifiedDate);
        });
        entity.Property(t => t.RowVersion).IsRowVersion();
            
        entity.HasMany(x => x.Tasks)
            .WithOne()
            .HasForeignKey(x => x.TodoListId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasUniqueIndex(t => new { t.UserId, t.Name });
    }
}