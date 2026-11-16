namespace Washu.Todo.Domain;

using Framework.Identity.Users;
using Washu.Framework.Entities;

public class TodoList(Guid userId, string listName) : IOwnedByUser, IAuditableEntity, ISoftDeletableEntity, INamedEntity
{
    public Guid Id { get; private init; } = Guid.CreateVersion7();
    public Guid UserId { get; init; } = userId;
    public ApplicationUser User { get; private init; } = null!;
    public string Name { get; set; } = listName;
    public List<TaskEntry> Tasks { get; private init; } = [];
    public DateTimeOffset CreationDate { get; private init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedDate { get; private init; }
    public uint RowVersion { get; private init; }
    
    private TodoList() : this(Guid.Empty, string.Empty) { }
    public static readonly TodoList None = new()
    {
        Id = Guid.Empty,
        Name = "None",
        Tasks = []
    };
    public bool IsDeleted { get; private set; }

    public void SoftDelete()
    {
        Name = $"deleted~{Name}~{Random.Shared.Next(1000, 10000)}";
        IsDeleted = true;
    }
}