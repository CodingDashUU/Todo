namespace Washu.Framework.Identity.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Washu.Framework.Extensions;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> entity)
    {
        entity.HasOne(rp => rp.Role)
            .WithMany(r => r.Permissions)
            .HasForeignKey(u => u.RoleId);
        entity.HasKey(u => u.Id);
        entity.Property(rp => rp.Name).HasColumnType("citext");
        entity.HasUniqueIndex(u => u.Name);
        entity.ToTable(t =>
        {
            t.HasMaxLengthCheckConstraint(p => p.Name, 64);
        });
    }
}