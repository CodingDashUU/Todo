namespace Washu.Framework.Entities.SoftDeleteEntity;

public interface ISoftDeletableEntity
{
    bool IsDeleted { get; }
}