namespace Washu.Framework.Identity;

public sealed class CircuitState
{
    public Guid Id { get; } = Guid.CreateVersion7();
}