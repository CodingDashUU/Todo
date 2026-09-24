namespace Washu.Framework.Identity.Entities;

public class ApplicationRole(string name)
{
    private ApplicationRole() : this(string.Empty) {}
    public Guid Id { get; } = Guid.CreateVersion7();
    public string Name { get; } = name;
}