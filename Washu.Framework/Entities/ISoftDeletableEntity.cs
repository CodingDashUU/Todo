namespace Washu.Framework.Entities;

public interface ISoftDeletableEntity
{
    bool IsDeleted { get; }
    void SoftDelete();
}