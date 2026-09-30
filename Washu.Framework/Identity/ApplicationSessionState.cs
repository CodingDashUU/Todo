namespace Washu.Framework.Identity;

public sealed class ApplicationSessionState
{
    public Guid Id { get; } = Guid.CreateVersion7();
}