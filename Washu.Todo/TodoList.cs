namespace Washu.Todo;

using Commands;
using Framework.Identity;
using Identity;

public class TodoList
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public ApplicationUser User { get; init; }
    public string Name { get; init; }
    public List<TaskEntry> Tasks { get; init; }
    public DateTimeOffset CreationDate { get; init; }
    public DateTimeOffset LastModified { get; set; }
    
    public Guid VersionId { get; private set; }
    public uint RowVersion { get; init; }

    public override string ToString() => Name;
    public static readonly TodoList None = new()
    {
        Id = Guid.Empty,
        Name = "None",
        VersionId = Guid.Empty,
        Tasks = []
    }; 
    public static TodoList Create(Guid userId, CreateList.Model model)
        => new()
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Name = model.ListName,
            CreationDate = DateTimeOffset.UtcNow,
            LastModified = DateTimeOffset.UtcNow,
            VersionId = Guid.CreateVersion7(),
            Tasks = []
        };

    public void ChangeVersionId() => VersionId = Guid.CreateVersion7();
}