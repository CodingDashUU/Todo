namespace Washu.Framework.Identity.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Washu.Framework.Extensions;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
        entity.HasUserSoftDeleteQueryFilter();
        entity.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId);
        entity.HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId);
        entity.HasKey(u => new { u.UserId, u.RoleId });
    }
}