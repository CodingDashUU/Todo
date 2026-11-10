namespace Washu.Framework.Identity.Users;

public sealed class ApplicationUserInfo
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "UNKNOWN";
    public string Email { get; set; } = "UNKNOWN";
    public DateTimeOffset CreationDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastModifiedDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSignedInDate { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastSignedOutDate { get; set; } = DateTimeOffset.UtcNow;
    public uint RowVersion { get; set; }

    public static ApplicationUserInfo FromUser(ApplicationUser user) => new ApplicationUserInfo
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