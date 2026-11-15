using Cursuri.Models;
using Microsoft.AspNetCore.Identity;

namespace Cursuri.Services;

public class InMemoryPlatformRegistry : IPlatformRegistry
{
    private readonly List<IdentityRecord> accounts =
    [
        Account("ana.popescu", "Ana Popescu", "student", "S2026001"),
        Account("mihai.dumitrescu", "Mihai Dumitrescu", "student", "S2026002"),
        Account("elena.dumitru", "Elena Dumitru", "student", "S2024017"),
        Account("andreea.vasile", "Andreea Vasile", "profesor", null),
        Account("victor.neagu", "Victor Neagu", "profesor", null),
    ];

    private readonly PasswordHasher<IdentityRecord> hasher = new();
    private readonly Dictionary<string, string> passwordHashes = [];

    public InMemoryPlatformRegistry()
    {
        // Date de test: toate conturile pornesc cu aceeași parolă. Păstrăm doar rezultatul funcției hash.
        foreach (var account in accounts)
            passwordHashes[account.IdentifierIn(Contexts.Cursuri)!] = hasher.HashPassword(account, "Parola.2026");
    }

    public IReadOnlyList<IdentityRecord> GetAll() => accounts;

    public IdentityRecord? FindByUsername(string username) =>
        accounts.FirstOrDefault(a => a.IdentifierIn(Contexts.Cursuri) == username);

    public bool VerifyPassword(IdentityRecord account, string password)
    {
        var username = account.IdentifierIn(Contexts.Cursuri)!;
        return passwordHashes.TryGetValue(username, out var hash)
            && hasher.VerifyHashedPassword(account, hash, password) != PasswordVerificationResult.Failed;
    }

    private static IdentityRecord Account(string username, string name, string role, string? matricol)
    {
        var record = new IdentityRecord(Contexts.Cursuri);
        record.Identifiers.Add(new Identifier(Contexts.Cursuri, username));
        if (matricol is not null)
            record.Identifiers.Add(new Identifier(Contexts.Secretariat, matricol));
        record.Attributes.Add(new IdentityAttribute("nume", name, Contexts.Cursuri));
        record.Attributes.Add(new IdentityAttribute("rol", role, Contexts.Cursuri));
        return record;
    }
}
