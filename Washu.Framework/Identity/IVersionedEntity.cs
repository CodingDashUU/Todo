namespace Washu.Framework.Identity;

public interface IVersionedEntity
{
    Guid VersionId { get; }
}