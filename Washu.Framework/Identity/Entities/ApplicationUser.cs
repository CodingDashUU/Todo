namespace Washu.Framework.Identity.Entities;

using System;

public class ApplicationUser(string userName, string email, string googleSubject)
{
    public ApplicationUser() : this(string.Empty, string.Empty, string.Empty) {}
    public Guid Id { get; set; }

    public string Username { get; set; } = userName;
    public string NormalizedUsername { get; set; } = userName.ToUpperInvariant();

    public string Email { get; set; } = email;
    public string NormalizedEmail { get; set; } = email.ToUpperInvariant();

    public Guid SecurityStamp { get; set; } = Guid.CreateVersion7();
    public string GoogleSubject { get; set; } = googleSubject;
    public bool IsBanned { get; set; }
    
    public void ChangeUsername(string newName)
    {
        if (Username == newName) return;
        Username = newName;
        NormalizedUsername = newName.ToUpperInvariant();
        SecurityStamp = Guid.CreateVersion7();
    }
}