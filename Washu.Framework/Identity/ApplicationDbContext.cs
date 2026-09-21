namespace Washu.Framework.Identity;

using Entities;
using Microsoft.EntityFrameworkCore;
using Extensions;

public class ApplicationDbContext(DbContextOptions options)
    : DbContext(options)
{
    public DbSet<ApplicationUser> Users => Set<ApplicationUser>();
    public DbSet<ApplicationRole> Roles => Set<ApplicationRole>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("citext");
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId);
            entity.HasOne(u => u.Role)
                .WithMany()
                .HasForeignKey(u => u.RoleId);
            entity.HasKey(u => new { u.UserId, u.RoleId });
        });
        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.Username).HasColumnType("citext");
            entity.Property(u => u.Email).HasColumnType("citext");
            entity.Property(u => u.GoogleSubject).HasColumnType("citext");
            entity.HasUniqueIndex(u => u.Username);
            entity.HasUniqueIndex(u => u.Email);
            entity.HasUniqueIndex(u => u.GoogleSubject);
            entity.ToTable(t =>
            {
                t.HasMaxLengthCheckConstraint(p => p.Username, 128);
                t.HasAllowedCharactersCheckConstraint(p => p.Username, Username.Rules.ValidCharacters);
                t.HasMaxLengthCheckConstraint(p => p.Email, 255);
                t.HasEmailCheckConstraint(p => p.Email);
                t.HasMaxLengthCheckConstraint(p => p.GoogleSubject, 255);
            });
        });
        modelBuilder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(u => u.Name).HasColumnType("citext");
            entity.HasUniqueIndex(u => u.Name);
            entity.ToTable(t =>
            {
                t.HasMaxLengthCheckConstraint(p => p.Name, 128);
            });
        });
    }
}