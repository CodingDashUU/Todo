namespace Washu.Todo;

using Framework.Identity;
using Framework.Identity.Entities;
using Identity;

public class TodoList(Guid userId, string listName) : IOwnedByUser, IAuditableEntity
{
    public Guid Id { get; private init; } = Guid.CreateVersion7();
    public Guid UserId { get; init; } = userId;
    public ApplicationUser User { get; private init; } = null!;
    public string Name { get; set; } = listName;
    public List<TaskEntry> Tasks { get; private init; } = [];
    public DateTimeOffset CreationDate { get; private init; }
    public DateTimeOffset? LastModifiedDate { get; private init; }
    public uint RowVersion { get; private init; }
    
    private TodoList() : this(Guid.Empty, string.Empty) { }
    public static readonly TodoList None = new()
    {
        Id = Guid.Empty,
        Name = "None",
        Tasks = []
    };
}