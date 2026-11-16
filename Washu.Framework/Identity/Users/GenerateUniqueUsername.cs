namespace Washu.Framework.Identity.Users;

using Mediator;
using Microsoft.EntityFrameworkCore;
using Persistence;

public sealed record GenerateUniqueUsernameCommand(string Email) : ICommand<string>;

public sealed class GenerateUniqueUsernameHandler(IDbContextFactory<ApplicationDbContext> factory)
    : ICommandHandler<GenerateUniqueUsernameCommand, string>
{
    public async ValueTask<string> Handle(GenerateUniqueUsernameCommand command, CancellationToken cancellationToken)
    {
        await using var dbContext = await factory.CreateDbContextAsync(cancellationToken);
        var email = command.Email;
        var atIndex = email.IndexOf('@');
        var localPart = email.AsSpan(0, atIndex < 0 ? email.Length : atIndex);

        var validCount = 0;
        foreach (var ch in localPart)
            if (UsernameRules.ValidCharacters.Contains(ch))
                validCount++;

        var clean = string.Create(validCount, localPart.ToString(), static (dest, src) =>
        {
            var i = 0;
            foreach (var ch in src)
                if (UsernameRules.ValidCharacters.Contains(ch))
                    dest[i++] = ch;
        });
        clean = clean.TrimStart('_', '-');

        if (string.IsNullOrEmpty(clean) || !char.IsAsciiLetter(clean[0])) clean = $"u_{clean}".TrimEnd('_', '-');

        if (clean.Length < UsernameRules.MinUsernameLength)
            clean = $"{clean}_{Random.Shared.Next(100, 999)}";

        const int maxBaseLength = UsernameRules.MaxUsernameLength - 5;
        if (clean.Length > maxBaseLength) clean = clean[..maxBaseLength];

        var candidate = clean;
        while (await dbContext.Users.AnyAsync(u => u.Username == candidate, cancellationToken))
            candidate = $"{clean}_{Random.Shared.Next(100, 9999)}";

        return candidate;
    }
}