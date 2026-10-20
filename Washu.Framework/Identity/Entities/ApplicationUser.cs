namespace Washu.Framework.Identity.Entities;

using System;

public sealed class ApplicationUser(string userName, string email, string googleSubject)
{
    private ApplicationUser() : this(string.Empty, string.Empty, string.Empty) {}
    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public string Username
    {
        get;
        set
        {
            field = value;
            VersionId = Guid.CreateVersion7();
        }
    } = userName;

    public string Email { get; private init; } = email;
    public Guid VersionId { get; private set; } = Guid.CreateVersion7();
    public Guid SecurityStamp { get; private set; } = Guid.CreateVersion7();
    public string GoogleSubject { get; private init; } = googleSubject;
    public uint RowVersion { get; }
    
    public void ChangeSecurityStamp() => SecurityStamp = Guid.CreateVersion7();
}