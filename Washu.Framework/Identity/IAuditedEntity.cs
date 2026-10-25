namespace Washu.Framework.Identity;

public interface IAuditedEntity
{
    DateTimeOffset CreationDate { get; }
    DateTimeOffset LastModifiedDate { get; }
}