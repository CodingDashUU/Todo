using Microsoft.EntityFrameworkCore;
using Washu.Framework.Extensions;
using Washu.Framework.Identity.Persistence;

namespace Washu.Todo.Persistence;

using System.Reflection;
using Tasks;
using TodoList;

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : ApplicationDbContext(options)
{
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TaskEntry> Tasks => Set<TaskEntry>();
}