namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<ApplicationRole> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<UserLogin> UserLogins { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);
            entity.HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId);
        });
        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId);
        });
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(Username.Rules.MaxUsernameLength);
            entity.Property(u => u.NormalizedUsername).HasMaxLength(Username.Rules.MaxUsernameLength);
            entity.Property(u => u.Email).HasMaxLength(100);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(100);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.NormalizedUsername).IsUnique();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
        });
        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(u => u.Name).HasMaxLength(30);
            entity.Property(u => u.NormalizedName).HasMaxLength(30);
            entity.HasIndex(u => u.Name).IsUnique();
            entity.HasIndex(u => u.NormalizedName).IsUnique();
        });
    }
}