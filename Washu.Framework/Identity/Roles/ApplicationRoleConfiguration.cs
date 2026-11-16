namespace Washu.Framework.Identity.Roles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Washu.Framework.Extensions;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> entity)
    {
        entity.Property(u => u.Name).HasColumnType("citext");
        entity.HasUniqueIndex(u => u.Name);
        entity.ToTable(t =>
        {
            t.HasMaxLengthCheckConstraint(p => p.Name, 128);
        });
    }
}