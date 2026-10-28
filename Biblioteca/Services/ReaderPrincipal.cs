using System.Security.Claims;
using Biblioteca.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Biblioteca.Services;

// Singurul loc în care o fișă de cititor devine identitatea sesiunii (un ClaimsPrincipal).
public static class ReaderPrincipal
{
    public static ClaimsPrincipal Create(IdentityRecord reader)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, reader.IdentifierIn(Contexts.Biblioteca)!, ClaimValueTypes.String, Contexts.Biblioteca),
            new(ClaimTypes.Name, reader.Attribute("nume")!, ClaimValueTypes.String, Contexts.Biblioteca),
        };

        var matricol = reader.IdentifierIn(Contexts.Secretariat);
        if (matricol is not null)
            claims.Add(new Claim("matricol", matricol, ClaimValueTypes.String, Contexts.Biblioteca));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
