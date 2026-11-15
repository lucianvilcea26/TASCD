using System.Security.Cryptography;
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
        Account("ioana.marin", "Ioana Marin", "administrator", null),
    ];

    private readonly PasswordHasher<IdentityRecord> hasher = new();
    private readonly Dictionary<string, string> passwordHashes = [];

    // Registrul este Singleton, deci toate cererile îl folosesc în paralel. Cât timp lista nu se schimba, citirea
    // simultană era sigură; de acum, orice acces la listă și la dicționar trece prin același zăvor.
    private readonly Lock gate = new();

    // Un plafon, ca o instalare publică să nu poată fi umplută cu conturi la nesfârșit.
    private const int MaxAccounts = 500;

    public InMemoryPlatformRegistry(IConfiguration configuration)
    {
        // Date de test: toate conturile pornesc cu aceeași parolă. Păstrăm doar rezultatul funcției hash.
        // Administratorul primește parola din configurare, dacă există: pe un server public, parola lui
        // nu poate fi cea publicată în curs.
        var adminPassword = configuration["Platform:AdminPassword"] ?? "Parola.2026";
        foreach (var account in accounts)
        {
            var password = account.Attribute("rol") == "administrator" ? adminPassword : "Parola.2026";
            passwordHashes[account.IdentifierIn(Contexts.Cursuri)!] = hasher.HashPassword(account, password);
        }
    }

    public IReadOnlyList<IdentityRecord> GetAll()
    {
        lock (gate)
            return [.. accounts];
    }

    public IdentityRecord? FindByUsername(string username)
    {
        lock (gate)
            return accounts.FirstOrDefault(a => a.IdentifierIn(Contexts.Cursuri) == username);
    }

    public bool VerifyPassword(IdentityRecord account, string password)
    {
        var username = account.IdentifierIn(Contexts.Cursuri)!;
        string? hash;
        lock (gate)
            passwordHashes.TryGetValue(username, out hash);
        return hash is not null
            && hasher.VerifyHashedPassword(account, hash, password) != PasswordVerificationResult.Failed;
    }

    public IdentityRecord? Register(string username, string fullName, string? matricol, string password)
    {
        var record = new IdentityRecord(Contexts.Cursuri);
        record.Identifiers.Add(new Identifier(Contexts.Cursuri, username));
        // Matricolul este declarat de utilizator: nimeni nu a verificat că este al lui.
        if (!string.IsNullOrWhiteSpace(matricol))
            record.Identifiers.Add(new Identifier(Contexts.Secretariat, matricol.Trim()));
        // Numele îl afirmă utilizatorul; rolul îl decide platforma.
        record.Attributes.Add(new IdentityAttribute("nume", fullName.Trim(), Contexts.SelfAsserted));
        record.Attributes.Add(new IdentityAttribute("rol", "student", Contexts.Cursuri));
        var hash = hasher.HashPassword(record, password);

        lock (gate)
        {
            if (accounts.Count >= MaxAccounts || accounts.Any(a => a.IdentifierIn(Contexts.Cursuri) == username))
                return null;
            accounts.Add(record);
            passwordHashes[username] = hash;
            return record;
        }
    }

    public bool SetRole(string username, string role)
    {
        lock (gate)
        {
            var index = accounts.FindIndex(a => a.IdentifierIn(Contexts.Cursuri) == username);
            if (index < 0)
                return false;
            // Alte cereri pot citi chiar acum înregistrarea veche, deci nu o modificăm: punem în locul ei o copie.
            var copy = new IdentityRecord(Contexts.Cursuri);
            copy.Identifiers.AddRange(accounts[index].Identifiers);
            copy.Attributes.AddRange(accounts[index].Attributes.Where(a => a.Name != "rol"));
            copy.Attributes.Add(new IdentityAttribute("rol", role, Contexts.Cursuri));
            accounts[index] = copy;
            return true;
        }
    }

    public string? ResetPassword(string username)
    {
        var account = FindByUsername(username);
        if (account is null)
            return null;
        // Parolă temporară aleatoare, de 16 caractere, fără caracterele ușor de confundat (0 și O, 1 și l).
        var password = RandomNumberGenerator.GetString("abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789", 16);
        var hash = hasher.HashPassword(account, password);
        lock (gate)
        {
            if (!passwordHashes.ContainsKey(username))
                return null;
            passwordHashes[username] = hash;
            return password;
        }
    }

    public bool Delete(string username)
    {
        lock (gate)
        {
            passwordHashes.Remove(username);
            return accounts.RemoveAll(a => a.IdentifierIn(Contexts.Cursuri) == username) > 0;
        }
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
