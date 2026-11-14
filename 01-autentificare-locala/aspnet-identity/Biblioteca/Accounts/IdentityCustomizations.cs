using System.Security.Claims;
using Biblioteca.Data;
using Biblioteca.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Biblioteca.Accounts;

public static class UserClaims
{
    public const string CardClaim = "permis";
    public const string MatricolClaim = "matricol";
    public const string EnrollmentClaim = "inmatriculare";

    public static string Id(this ClaimsPrincipal principal) => principal.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // Afirmația secretariatului despre înmatriculare, așa cum era în momentul autentificării.
    public static Claim Enrollment(EnrollmentStatus enrollment) =>
        new(EnrollmentClaim, enrollment.ValidUntil.ToString("yyyy-MM-dd"), ClaimValueTypes.Date, "Secretariat");
}

// Construiește identitatea sesiunii. Clasa de bază pune deja identificatorul, numele de utilizator, emailul,
// ștampila de securitate și rolurile; adăugăm afirmațiile bibliotecii.
public class BibliotecaClaimsFactory(UserManager<ApplicationUser> users, RoleManager<IdentityRole> roles,
    IOptions<IdentityOptions> options) : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>(users, roles, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Numele de utilizator din Identity este adresa de email; în bara de navigare vrem numele persoanei.
        if (identity.FindFirst(ClaimTypes.Name) is { } userName)
            identity.RemoveClaim(userName);
        identity.AddClaim(new Claim(ClaimTypes.Name, user.Name));

        identity.AddClaim(new Claim(UserClaims.CardClaim, user.CardNumber));
        if (user.Matricol is not null)
            identity.AddClaim(new Claim(UserClaims.MatricolClaim, user.Matricol));
        return identity;
    }
}

// O regulă proprie, pe lângă cele configurate în PasswordOptions: parola nu conține adresa de email
// și nu depășește 128 de caractere.
public class EmailNotInPasswordValidator : IPasswordValidator<ApplicationUser>
{
    public Task<IdentityResult> ValidateAsync(UserManager<ApplicationUser> manager, ApplicationUser user, string? password)
    {
        password ??= "";
        var errors = new List<IdentityError>();
        if (password.Length > 128)
            errors.Add(new IdentityError { Code = "PasswordTooLong", Description = "Parola poate avea cel mult 128 de caractere." });

        var localPart = (user.Email ?? "").Split('@')[0];
        if (localPart.Length >= 3 && password.Contains(localPart, StringComparison.OrdinalIgnoreCase))
            errors.Add(new IdentityError { Code = "PasswordContainsEmail", Description = "Parola nu poate conține adresa de email." });

        return Task.FromResult(errors.Count == 0 ? IdentityResult.Success : IdentityResult.Failed([.. errors]));
    }
}

// Mesajele lui Identity sunt implicit în engleză; le traducem pe cele pe care le poate vedea utilizatorul.
public class RomanianIdentityErrorDescriber : IdentityErrorDescriber
{
    public override IdentityError PasswordTooShort(int length) =>
        new() { Code = nameof(PasswordTooShort), Description = $"Parola trebuie să aibă cel puțin {length} caractere." };

    public override IdentityError PasswordMismatch() =>
        new() { Code = nameof(PasswordMismatch), Description = "Parola curentă nu este corectă." };

    public override IdentityError InvalidToken() =>
        new() { Code = nameof(InvalidToken), Description = "Linkul nu mai este valabil. Cere un link nou." };

    public override IdentityError DuplicateEmail(string email) =>
        new() { Code = nameof(DuplicateEmail), Description = "Există deja un cont cu această adresă de email." };

    public override IdentityError DuplicateUserName(string userName) =>
        new() { Code = nameof(DuplicateUserName), Description = "Există deja un cont cu această adresă de email." };

    public override IdentityError InvalidEmail(string? email) =>
        new() { Code = nameof(InvalidEmail), Description = "Adresa de email nu este validă." };
}
