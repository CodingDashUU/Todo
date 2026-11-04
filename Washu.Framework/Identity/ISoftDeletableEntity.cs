namespace Washu.Framework.Identity;

public interface ISoftDeletableEntity
{
    bool IsDeleted { get; }
}