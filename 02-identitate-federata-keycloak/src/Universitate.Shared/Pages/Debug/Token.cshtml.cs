using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.IdentityModel.Tokens;

namespace Universitate.Shared.Pages.Debug;

/// <summary>Shows what the app actually received from Keycloak. Teaching aid only.</summary>
[Authorize]
public class TokenModel : PageModel
{
    public record DecodedToken(string Name, string Payload, DateTimeOffset? IssuedAt, DateTimeOffset? ExpiresAt, string? Audience);

    public List<DecodedToken> Tokens { get; } = [];
    public bool HasRefreshToken { get; private set; }

    private static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // show diacritics as-is
    };

    public async Task OnGetAsync()
    {
        foreach (var name in new[] { "id_token", "access_token" })
        {
            var jwt = await HttpContext.GetTokenAsync(name);
            if (jwt is not null) Tokens.Add(Decode(name, jwt));
        }
        HasRefreshToken = await HttpContext.GetTokenAsync("refresh_token") is not null;
    }

    private static DecodedToken Decode(string name, string jwt)
    {
        var payload = JsonDocument.Parse(Base64UrlEncoder.DecodeBytes(jwt.Split('.')[1])).RootElement;
        return new DecodedToken(
            name,
            JsonSerializer.Serialize(payload, Pretty),
            Time(payload, "iat"),
            Time(payload, "exp"),
            payload.TryGetProperty("aud", out var aud) ? aud.ToString() : null);
    }

    private static DateTimeOffset? Time(JsonElement payload, string claim) =>
        payload.TryGetProperty(claim, out var v) && v.TryGetInt64(out var s) ? DateTimeOffset.FromUnixTimeSeconds(s) : null;
}
