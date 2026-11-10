namespace Washu.Framework.Entities.AuditEntity;

public interface IAuditableEntity
{
    uint RowVersion { get; }
    DateTimeOffset CreationDate { get; }
    DateTimeOffset? LastModifiedDate { get; }
}