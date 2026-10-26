namespace Washu.Framework.Identity;

public interface IAuditableEntity
{
    uint RowVersion { get; }
    DateTimeOffset CreationDate { get; }
    DateTimeOffset? LastModifiedDate { get; }
}