namespace Washu.Framework.Identity;

public interface IAuditableEntity
{
    Guid VersionId { get; }
    DateTimeOffset CreationDate { get; }
    DateTimeOffset? LastModifiedDate { get; }
}