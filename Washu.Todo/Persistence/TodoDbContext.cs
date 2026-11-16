using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Persistence;

namespace Washu.Todo.Persistence;

using Domain;
using System.Reflection;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    
    public DbSet<TaskEntry> Tasks => Set<TaskEntry>();
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}