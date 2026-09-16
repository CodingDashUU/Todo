namespace Washu.Framework.Identity;

using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users { get; set; }
    public DbSet<ApplicationRole> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
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
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Username).HasMaxLength(128);
            entity.Property(u => u.NormalizedUsername).HasMaxLength(128);
            entity.Property(u => u.Email).HasMaxLength(255);
            entity.Property(u => u.NormalizedEmail).HasMaxLength(255);
            entity.Property(u => u.GoogleSubject).HasMaxLength(255);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.NormalizedUsername).IsUnique();
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
            entity.HasIndex(u => u.GoogleSubject).IsUnique();
        });
        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(u => u.Name).HasMaxLength(128);
            entity.Property(u => u.NormalizedName).HasMaxLength(128);
            entity.HasIndex(u => u.Name).IsUnique();
            entity.HasIndex(u => u.NormalizedName).IsUnique();
        });
    }
}