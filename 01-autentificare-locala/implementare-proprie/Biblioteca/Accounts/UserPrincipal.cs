using System.Security.Claims;
using Biblioteca.Data;
using Biblioteca.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Biblioteca.Accounts;

public static class UserPrincipal
{
    public const string CardClaim = "permis";
    public const string MatricolClaim = "matricol";
    public const string EnrollmentClaim = "inmatriculare";
    public const string StampClaim = "stamp";

    private const string Issuer = "Biblioteca";
    private const string SecretariatIssuer = "Secretariat";

    // Singurul loc în care un cont devine identitatea sesiunii (un ClaimsPrincipal), păstrată în cookie.
    public static ClaimsPrincipal Create(User user, EnrollmentStatus? enrollment)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString(), ClaimValueTypes.Integer, Issuer),
            new(ClaimTypes.Name, user.Name, ClaimValueTypes.String, Issuer),
            new(ClaimTypes.Email, user.Email, ClaimValueTypes.String, Issuer),
            new(ClaimTypes.Role, user.Role, ClaimValueTypes.String, Issuer),
            new(CardClaim, user.CardNumber, ClaimValueTypes.String, Issuer),
            new(StampClaim, user.SecurityStamp, ClaimValueTypes.String, Issuer),
        };
        if (user.Matricol is not null)
            claims.Add(new Claim(MatricolClaim, user.Matricol, ClaimValueTypes.String, Issuer));

        // Afirmația secretariatului despre înmatriculare, așa cum era în momentul autentificării.
        if (enrollment is not null)
            claims.Add(new Claim(EnrollmentClaim, enrollment.ValidUntil.ToString("yyyy-MM-dd"), ClaimValueTypes.Date, SecretariatIssuer));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }

    public static int Id(this ClaimsPrincipal principal) =>
        int.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Rulează la fiecare cerere care aduce cookie-ul: dacă parola s-a schimbat între timp (ștampila diferă)
    // sau contul nu mai există, cookie-ul este respins și utilizatorul este scos din aplicație.
    public static async Task ValidateStampAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal?.FindFirstValue(ClaimTypes.NameIdentifier) is null)
            return;

        var accounts = context.HttpContext.RequestServices.GetRequiredService<AccountService>();
        var user = await accounts.FindByIdAsync(principal.Id());
        if (user is null || user.SecurityStamp != principal.FindFirstValue(StampClaim))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
