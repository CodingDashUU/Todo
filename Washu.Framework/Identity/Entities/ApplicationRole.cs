namespace Washu.Framework.Identity.Entities;

public class ApplicationRole(string name)
{
    private ApplicationRole() : this(string.Empty) {}
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Name { get; private init; } = name;
    public string NormalizedName { get; private init; } = name.ToUpperInvariant();
}