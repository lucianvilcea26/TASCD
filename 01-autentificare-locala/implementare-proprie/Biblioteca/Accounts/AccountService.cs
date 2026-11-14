using System.Security.Cryptography;
using System.Text;
using Biblioteca.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Biblioteca.Accounts;

public class AccountOptions
{
    public int MinPasswordLength { get; set; } = 10;
    public int MaxFailedLogins { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 5;
    public int TokenLifetimeMinutes { get; set; } = 60;
}

public enum LoginOutcome { Success, InvalidCredentials, LockedOut, EmailNotConfirmed }

public record LoginResult(LoginOutcome Outcome, User? User = null, DateTime? LockedUntilUtc = null);

// Toate regulile despre conturi, într-un singur loc. Controller-ele doar traduc rezultatele în pagini.
public class AccountService(BibliotecaDbContext db, IOptions<AccountOptions> options)
{
    // PBKDF2 cu HMAC-SHA512, sare aleatoare de 128 de biți și 100 000 de iterații, în versiunea curentă a clasei.
    private static readonly PasswordHasher<User> Hasher = new();

    // O amprentă calculată o singură dată, folosită când emailul nu există (vezi LoginAsync).
    private static readonly string DummyHash = Hasher.HashPassword(new User(), "parola-care-nu-se-potriveste");

    private readonly AccountOptions _options = options.Value;

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    public Task<User?> FindByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == Normalize(email));

    public Task<User?> FindByIdAsync(int id) => db.Users.FirstOrDefaultAsync(u => u.Id == id);

    // ---------------------------------------------------------------- politica parolelor

    // Lungimea contează mai mult decât amestecul de caractere: nu impunem majuscule, cifre sau simboluri.
    public IReadOnlyList<string> CheckPassword(string password, string email)
    {
        var problems = new List<string>();
        if (password.Length < _options.MinPasswordLength)
            problems.Add($"Parola trebuie să aibă cel puțin {_options.MinPasswordLength} caractere.");
        if (password.Length > 128)
            problems.Add("Parola poate avea cel mult 128 de caractere.");

        var localPart = Normalize(email).Split('@')[0];
        if (localPart.Length >= 3 && password.Contains(localPart, StringComparison.OrdinalIgnoreCase))
            problems.Add("Parola nu poate conține adresa de email.");
        return problems;
    }

    // ---------------------------------------------------------------- crearea contului

    public async Task<User> CreateReaderAsync(string email, string name, string matricol, string password)
    {
        var user = new User
        {
            Email = Normalize(email),
            Name = name,
            Matricol = matricol,
            Role = Roles.Reader,
            EmailConfirmed = false,
            CardNumber = await NextCardNumberAsync(),
        };
        user.PasswordHash = Hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private async Task<string> NextCardNumberAsync()
    {
        var last = await db.Users.MaxAsync(u => (int?)u.Id) ?? 0;
        return $"B-{last + 1:D4}";
    }

    public async Task<User> CreateStaffAsync(string email, string name, string password, string role)
    {
        // Aceleași reguli și pentru conturile create la pornire: o parolă slabă în configurație oprește aplicația.
        var problems = CheckPassword(password, email);
        if (problems.Count > 0)
            throw new InvalidOperationException(string.Join(" ", problems));

        var user = new User { Email = Normalize(email), Name = name, Role = role, EmailConfirmed = true, CardNumber = await NextCardNumberAsync() };
        user.PasswordHash = Hasher.HashPassword(user, password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ---------------------------------------------------------------- autentificarea

    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        var user = await FindByEmailAsync(email);
        var now = DateTime.UtcNow;

        if (user is null)
        {
            // Verificăm totuși o parolă, ca răspunsul pentru un email inexistent să dureze cât unul
            // pentru o parolă greșită: altfel durata ar spune cine are cont.
            Hasher.VerifyHashedPassword(new User(), DummyHash, password);
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        // Un cont blocat nu primește nicio verificare de parolă până la expirarea blocării.
        if (user.LockoutEndUtc > now)
            return new LoginResult(LoginOutcome.LockedOut, LockedUntilUtc: user.LockoutEndUtc);

        var verification = Hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= _options.MaxFailedLogins)
            {
                user.LockoutEndUtc = now.AddMinutes(_options.LockoutMinutes);
                user.FailedLoginCount = 0;
                await db.SaveChangesAsync();
                return new LoginResult(LoginOutcome.LockedOut, LockedUntilUtc: user.LockoutEndUtc);
            }
            await db.SaveChangesAsync();
            return new LoginResult(LoginOutcome.InvalidCredentials);
        }

        // Parola corectă, dar calculată cu parametri mai vechi (de exemplu mai puține iterații): o recalculăm acum,
        // singurul moment în care avem parola în clar.
        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = Hasher.HashPassword(user, password);

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        await db.SaveChangesAsync();

        // Parola este corectă, dar adresa nu a fost confirmată: nu deschidem sesiunea.
        return user.EmailConfirmed
            ? new LoginResult(LoginOutcome.Success, user)
            : new LoginResult(LoginOutcome.EmailNotConfirmed, user);
    }

    // ---------------------------------------------------------------- schimbarea parolei

    public async Task<IReadOnlyList<string>> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        if (Hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) == PasswordVerificationResult.Failed)
            return ["Parola curentă nu este corectă."];

        var problems = CheckPassword(newPassword, user.Email);
        if (problems.Count > 0)
            return problems;

        SetPassword(user, newPassword);
        await db.SaveChangesAsync();
        return [];
    }

    private static void SetPassword(User user, string newPassword)
    {
        user.PasswordHash = Hasher.HashPassword(user, newPassword);
        // Ștampila nouă invalidează sesiunile deschise cu parola veche, pe toate dispozitivele.
        user.SecurityStamp = User.NewStamp();
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
    }

    // ---------------------------------------------------------------- linkurile trimise prin email

    // Valoarea din link este aleatoare (256 de biți); baza păstrează doar amprenta ei, ca o copie a bazei
    // să nu conțină linkuri folosibile. Un link nou anulează linkurile anterioare cu același scop.
    public async Task<string> CreateTokenAsync(User user, string purpose)
    {
        var pending = await db.UserTokens
            .Where(t => t.UserId == user.Id && t.Purpose == purpose && t.UsedUtc == null)
            .ToListAsync();
        foreach (var old in pending)
            old.UsedUtc = DateTime.UtcNow;

        var raw = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        db.UserTokens.Add(new UserToken
        {
            UserId = user.Id,
            Purpose = purpose,
            TokenHash = Fingerprint(raw),
            ExpiresUtc = DateTime.UtcNow.AddMinutes(_options.TokenLifetimeMinutes),
        });
        await db.SaveChangesAsync();
        return raw;
    }

    // Un link este valabil o singură dată și numai până la expirare.
    private async Task<bool> ConsumeTokenAsync(User user, string purpose, string raw)
    {
        var expected = Encoding.ASCII.GetBytes(Fingerprint(raw));
        var now = DateTime.UtcNow;
        var candidates = await db.UserTokens
            .Where(t => t.UserId == user.Id && t.Purpose == purpose && t.UsedUtc == null)
            .ToListAsync();

        var token = candidates.FirstOrDefault(t =>
            t.ExpiresUtc > now && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(t.TokenHash), expected));
        if (token is null)
            return false;

        token.UsedUtc = now;
        return true;
    }

    private static string Fingerprint(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    public async Task<bool> ConfirmEmailAsync(User user, string token)
    {
        if (!await ConsumeTokenAsync(user, TokenPurposes.ConfirmEmail, token))
            return false;
        user.EmailConfirmed = true;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<string>> ResetPasswordAsync(User user, string token, string newPassword)
    {
        // Verificăm parola nouă înainte să consumăm linkul, ca o parolă respinsă să nu irosească linkul.
        var problems = CheckPassword(newPassword, user.Email);
        if (problems.Count > 0)
            return problems;

        if (!await ConsumeTokenAsync(user, TokenPurposes.ResetPassword, token))
            return ["Linkul nu mai este valabil. Cere un link nou."];

        SetPassword(user, newPassword);
        await db.SaveChangesAsync();
        return [];
    }
}
