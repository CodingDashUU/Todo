namespace Washu.Framework.Identity.Entities;

using System;

public sealed class ApplicationUser(string userName, string email, string googleSubject) : IAuditableEntity
{
    private ApplicationUser() : this(string.Empty, string.Empty, string.Empty) {}
    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public string Username { get; set; } = userName;

    public string Email { get; private init; } = email;
    public Guid SecurityStamp { get; private init; } = Guid.CreateVersion7();
    public string GoogleSubject { get; private init; } = googleSubject;
    public uint RowVersion { get; private init; }
    public DateTimeOffset CreationDate { get; private init; }
    public DateTimeOffset? LastModifiedDate { get; private init; }
    public DateTimeOffset? LastSignedInDate { get; private init; }
}