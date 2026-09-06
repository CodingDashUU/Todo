namespace Washu.Framework.Identity;

public class UserLogin
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string LoginProvider { get; set; }
    public required string ProviderKey { get; set; }
    public required Guid UserId { get; set; }
    public ApplicationUser User { get; set; }
}