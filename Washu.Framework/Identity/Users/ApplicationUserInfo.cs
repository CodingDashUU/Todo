namespace Washu.Framework.Identity.Users;

public sealed class ApplicationUserInfo
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "UNKNOWN";
    public string Email { get; init; } = "UNKNOWN";
    public DateTimeOffset CreationDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSignedInDate { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSignedOutDate { get; init; } = DateTimeOffset.UtcNow;
    public uint RowVersion { get; init; }

    public static ApplicationUserInfo FromUser(ApplicationUser user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        CreationDate = user.CreationDate,
        LastModifiedDate = user.LastModifiedDate,
        LastSignedInDate = user.LastSignedInDate,
        LastSignedOutDate = user.LastSignedOutDate,
        RowVersion = user.RowVersion
    };
}