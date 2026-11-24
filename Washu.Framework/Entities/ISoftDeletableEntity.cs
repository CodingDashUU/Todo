namespace Washu.Framework.Entities;

public interface ISoftDeletableEntity
{
    DateTimeOffset? DeletedAt { get; }
    void SoftDelete();
}