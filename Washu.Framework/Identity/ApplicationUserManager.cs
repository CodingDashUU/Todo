namespace Washu.Framework.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Notifications;

public class ApplicationUserManager<TDbContext>(IDbContextFactory<TDbContext> factory)
    where TDbContext : ApplicationDbContext
{
    public async Task<Message> AddUserLoginAsync(
        Guid userId, 
        ExternalLoginInfo info,
        CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        var userLogin = new UserLogin
        {
            UserId = userId,
            LoginProvider = info.LoginProvider,
            ProviderKey = info.ProviderKey,
        };

// Check if it's already in the DB on this transaction context
        var existsInDb = await dbContext.UserLogins
            .AnyAsync(l => l.LoginProvider == userLogin.LoginProvider 
                           && l.ProviderKey == userLogin.ProviderKey, ct);

        if (existsInDb) return Message.Error("Login Error", "User already exists");
        // Add directly to the DbSet — bypasses UserManager's graph attacher completely!
        await dbContext.UserLogins.AddAsync(userLogin, ct);
        await dbContext.SaveChangesAsync(ct);
        return Message.Success();
    }
    public async Task<Message> CreateAsync(
        ApplicationUser user,
        CancellationToken ct = default)
    {
        
        await using var context = await factory.CreateDbContextAsync(ct);

        // Ensure normalized fields are set for clean lookups
        user.NormalizedUsername = user.Username.ToUpperInvariant();
        user.NormalizedEmail = user.Email.ToUpperInvariant();

        // Check for duplicate username or email directly
        var exists = await context.Users
            .AnyAsync(u => u.NormalizedUsername == user.NormalizedUsername, ct);

        if (exists) return Message.Error("Login Error", "User already exists");

        // Direct insert — no graph traversal or ambient tracking conflicts
        await context.Users.AddAsync(user, ct);
        await context.SaveChangesAsync(ct);

        return Message.Success();
    }

    public async Task<ApplicationUser?> FindByUsernameAsync(string userName, CancellationToken ct = default)
    {
        var normalizedUsername = userName.ToUpperInvariant();
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.FirstOrDefaultAsync(u => u.NormalizedUsername == normalizedUsername, ct);
    }

    public async Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        await using var dbContext = await factory.CreateDbContextAsync(ct);
        return await dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
    }
}