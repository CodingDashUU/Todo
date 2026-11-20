using Microsoft.EntityFrameworkCore;
using Washu.Framework.Constants;
using Washu.Framework.Extensions;

namespace Washu.Framework.Identity.Persistence;

using Roles;
using System.Reflection;
using Users;

public class ApplicationDbContext(DbContextOptions options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<ApplicationRole> Roles => Set<ApplicationRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        if (Assembly.GetEntryAssembly() is {} assembly)
            modelBuilder.ApplyConfigurationsFromAssembly(assembly);
    }
}