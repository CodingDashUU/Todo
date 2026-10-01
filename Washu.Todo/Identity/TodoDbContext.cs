namespace Washu.Todo.Identity;

using Framework.Extensions;
using Framework.Identity;
using Framework.Identity.Entities;
using Microsoft.EntityFrameworkCore;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<TodoList> TodoLists => Set<TodoList>();
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
                table.HasDateLessThanOrEqualToOtherDateCheckConstraint(p => p.CreationDate, p => p.LastModified);
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
            entity.ToTable(table =>
            {
                table.HasMaxLengthCheckConstraint(p => p.Name, 64);
                table.HasAllowedCharactersCheckConstraint(p => p.Name, TodoRules.TaskName.AllowedCharacters);
                table.HasDateLessThanOrEqualToOtherDateCheckConstraint(p => p.DateCreated, p => p.GoalDate);
                table.HasDateLessThanOrEqualToNullableDateCheckConstraint(p => p.DateCreated, p => p.DateCompleted);
                
            });
        });
    }
    public async Task<TodoList?> GetListAsync(Guid id) => await TodoLists.Include(l => l.Tasks.OrderBy(t => t.DateCreated)).FirstOrDefaultAsync(l => l.Id == id);
    public async Task<List<TodoList>> GetListsByUserIdAsync(Guid userId) => await TodoLists.Where(l => l.UserId == userId).Include(l => l.Tasks.OrderBy(t => t.DateCreated)).ToListAsync();
}