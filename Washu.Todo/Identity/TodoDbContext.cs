namespace Washu.Todo.Identity;

using Framework.Identity;
using Microsoft.EntityFrameworkCore;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<TodoList> TodoLists { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TodoList>(entity =>
        {
            entity.HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(t => t.RowVersion).IsRowVersion();
            
            entity.HasMany(x => x.Tasks)
                .WithOne()
                .HasForeignKey(x => x.TodoListId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
    public async Task<TodoList?> GetListAsync(Guid id) => await TodoLists.Include(l => l.Tasks.OrderBy(t => t.DateCreated)).FirstOrDefaultAsync(l => l.Id == id);
    public async Task<List<TodoList>> GetListsByUserIdAsync(Guid userId) => await TodoLists.Where(l => l.UserId == userId).Include(l => l.Tasks.OrderBy(t => t.DateCreated)).ToListAsync();
}