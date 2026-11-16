namespace Washu.Framework.Identity.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Washu.Framework.Constants;
using Washu.Framework.Extensions;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> entity)
    {
        entity.HasSoftDeleteQueryFilter();
        entity.Property(u => u.Username).HasColumnType("citext");
        entity.Property(u => u.Email).HasColumnType("citext");
        entity.Property(u => u.GoogleSubject).HasMaxLength(255);
        entity.HasUniqueIndex(u => u.Username);
        entity.HasUniqueIndex(u => u.Email);
        entity.HasUniqueIndex(u => u.GoogleSubject);
        entity.ToTable(t =>
        {
            t.HasMaxLengthCheckConstraint(p => p.Username, 128);
            t.HasAllowedCharactersCheckConstraint(p => p.Username, UsernameRules.ValidCharacters + "~");
            t.HasAllowedFirstCharacterCheckConstraint(p => p.Username, CharacterSet.Letters);
            t.HasMaxLengthCheckConstraint(p => p.Email, 255);
            t.HasEmailCheckConstraint(p => p.Email);
            t.HasDateLessThanOrEqualToNullableDateCheckConstraint(u => u.CreationDate, u => u.LastModifiedDate);
            t.HasDateLessThanOrEqualToNullableDateCheckConstraint(u => u.CreationDate, u => u.LastSignedInDate);
            t.HasDateLessThanOrEqualToNullableDateCheckConstraint(u => u.CreationDate, u => u.LastSignedOutDate);
        });
        entity.Property(u => u.RowVersion).IsRowVersion();
    }
}