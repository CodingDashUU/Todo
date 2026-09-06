namespace Washu.Todo.Identity;

using Framework.Identity;
using Microsoft.EntityFrameworkCore;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<TodoList> TodoLists { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<TodoList>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Entity<TodoList>()
            .HasKey(t => t.Id);
        builder.Entity<TodoList>()
            .Property(t => t.RowVersion)
            .IsRowVersion();
        builder.Entity<TodoList>()
            .ComplexCollection(u => u.Tasks, t => t.ToJson());
    }
    public async Task<TodoList?> GetListAsync(Guid id) => await TodoLists.FirstOrDefaultAsync(l => l.Id == id);
    public async Task<List<TodoList>> GetListsByUserIdAsync(Guid userId) => await TodoLists.Where(l => l.UserId == userId).ToListAsync();
}