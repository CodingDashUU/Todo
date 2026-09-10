namespace Washu.Todo;

using Commands;
using Framework.Identity;
using Framework.Identity.Entities;
using Identity;

public class TodoList(Guid userId, CreateList.Model model)
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; init; } = userId;
    public ApplicationUser User { get; init; }
    public string Name { get; init; } = model.ListName;
    public List<TaskEntry> Tasks { get; init; } = [];
    public DateTimeOffset CreationDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastModified { get; set; } = DateTimeOffset.UtcNow;
    public Guid VersionId { get; private set; } = Guid.CreateVersion7();
    public uint RowVersion { get; init; }

    public override string ToString() => Name;
    public TodoList() : this(Guid.Empty, new CreateList.Model()) {}
    public static readonly TodoList None = new()
    {
        Id = Guid.Empty,
        Name = "None",
        VersionId = Guid.Empty,
        Tasks = []
    }; 
    public void ChangeVersionId() => VersionId = Guid.CreateVersion7();
}