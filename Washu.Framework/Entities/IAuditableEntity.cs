namespace Washu.Framework.Entities;

public interface IAuditableEntity
{
    uint RowVersion { get; }
    DateTimeOffset CreationDate { get; }
    DateTimeOffset? LastModifiedDate { get; }
}