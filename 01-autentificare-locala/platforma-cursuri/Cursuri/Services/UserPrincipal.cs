using System.Security.Claims;
using Cursuri.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Cursuri.Services;

// Singurul loc în care un cont al platformei devine identitatea sesiunii (un ClaimsPrincipal).
public static class UserPrincipal
{
    public static ClaimsPrincipal Create(IdentityRecord account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.IdentifierIn(Contexts.Cursuri)!, ClaimValueTypes.String, Contexts.Cursuri),
            new(ClaimTypes.Name, account.Attribute("nume")!, ClaimValueTypes.String, Contexts.Cursuri),
            new(ClaimTypes.Role, account.Attribute("rol")!, ClaimValueTypes.String, Contexts.Cursuri),
        };

        var matricol = account.IdentifierIn(Contexts.Secretariat);
        if (matricol is not null)
            claims.Add(new Claim("matricol", matricol, ClaimValueTypes.String, Contexts.Cursuri));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
