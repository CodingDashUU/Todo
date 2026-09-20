namespace Washu.Framework.Identity.Entities;

using System;

public class ApplicationUser(string userName, string email, string googleSubject)
{
    private ApplicationUser() : this(string.Empty, string.Empty, string.Empty) {}
    public Guid Id { get; private init; } = Guid.CreateVersion7();

    public string Username { get; private set; } = userName;
    public string Email { get; private init; } = email;
    public Guid SecurityStamp { get; private set; } = Guid.CreateVersion7();
    public string GoogleSubject { get; private set; } = googleSubject;
    public bool IsBanned { get; private set; }
    
    public void ChangeUsername(string newName)
    {
        if (Username == newName) return;
        Username = newName;
        SecurityStamp = Guid.CreateVersion7();
    }
}