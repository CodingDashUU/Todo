using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Persistence;
using Washu.Todo.Lists;
using Washu.Todo.Lists.Features;
using Washu.Todo.Tasks;

namespace Washu.Todo.Persistence;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    
    public DbSet<TaskEntry> Tasks => Set<TaskEntry>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TodoList>(entity =>
        {
            entity.HasSoftDeleteQueryFilter();
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);
            entity.ToTable(table =>
            {
                table.HasMaxLengthCheckConstraint(p => p.Name, 64);
                table.HasAllowedCharactersCheckConstraint(p => p.Name, ListName.AllowedCharacters);
                table.HasDateLessThanOrEqualToNullableDateCheckConstraint(p => p.CreationDate, p => p.LastModifiedDate);
            });
            entity.Property(t => t.RowVersion).IsRowVersion();
            
            entity.HasMany(x => x.Tasks)
                .WithOne()
                .HasForeignKey(x => x.TodoListId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasUniqueIndex(t => new { t.UserId, t.Name }).HasSoftDeleteFilter();
        });
        builder.Entity<TaskEntry>(entity =>
        {
            entity.HasUniqueIndex(t => new { t.TodoListId, t.Name });
            entity.ToTable(table =>
            {
                table.HasMaxLengthCheckConstraint(p => p.Name, 64);
                table.HasAllowedCharactersCheckConstraint(p => p.Name, TaskName.AllowedCharacters);
                table.HasDateLessThanOrEqualToOtherDateCheckConstraint(p => p.DateCreated, p => p.GoalDate);
                table.HasDateLessThanOrEqualToNullableDateCheckConstraint(p => p.DateCreated, p => p.DateCompleted);
            });
        });
    }
}