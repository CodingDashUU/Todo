namespace Washu.Framework.Identity;

public class ApplicationRole(string name)
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Name { get; set; } = name;
    public string NormalizedName { get; set; } = name.ToUpperInvariant();
}