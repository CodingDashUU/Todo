namespace Washu.Todo.Identity;

using Framework.Extensions;
using Framework.Identity;
using Framework.Identity.Entities;
using Microsoft.EntityFrameworkCore;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    
    public DbSet<TaskEntry> Tasks => Set<TaskEntry>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TodoList>(entity =>
        {
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId);
            entity.ToTable(table =>
            {
                table.HasMaxLengthCheckConstraint(p => p.Name, 64);
                table.HasAllowedCharactersCheckConstraint(p => p.Name, TodoRules.ListName.AllowedCharacters);
                table.HasDateLessThanOrEqualToNullableDateCheckConstraint(p => p.CreationDate, p => p.LastModifiedDate);
            });
            entity.Property(t => t.RowVersion).IsRowVersion();
            
            entity.HasMany(x => x.Tasks)
                .WithOne()
                .HasForeignKey(x => x.TodoListId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<TaskEntry>(entity =>
        {
            entity.HasIndex(t => t.Name);
            entity.HasIndex(t => t.TodoListId);
            entity.HasIndex(t => t.DateCreated);
            entity.HasIndex(t => t.DateCompleted);
            entity.HasIndex(t => t.IsCompleted);
            entity.HasIndex(t => t.GoalDate);
            entity.ToTable(table =>
            {
                table.HasMaxLengthCheckConstraint(p => p.Name, 64);
                table.HasAllowedCharactersCheckConstraint(p => p.Name, TodoRules.TaskName.AllowedCharacters);
                table.HasDateLessThanOrEqualToOtherDateCheckConstraint(p => p.DateCreated, p => p.GoalDate);
                table.HasDateLessThanOrEqualToNullableDateCheckConstraint(p => p.DateCreated, p => p.DateCompleted);
                
            });
        });
    }
}