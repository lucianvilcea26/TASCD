using System.Security.Claims;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Universitate.Shared;

/// <summary>
/// Turns Keycloak tokens into a ClaimsPrincipal, keeping the claim names exactly as
/// Keycloak issues them (no renaming to long XML-namespace URIs).
/// </summary>
public static class TokenClaims
{
    public const string NameClaim = "preferred_username";
    public const string RoleClaim = "roles";

    private static readonly HashSet<string> Ignored = ["nonce", "at_hash", "c_hash", "s_hash"];

    /// <summary>
    /// Builds the user principal from the ID token. Called at login and again on every
    /// token refresh, so claims such as enrollment_status are only as fresh as the last refresh.
    /// </summary>
    public static ClaimsPrincipal PrincipalFromIdToken(string idToken)
    {
        var jwt = new JsonWebToken(idToken);
        var claims = jwt.Claims
            .Where(c => !Ignored.Contains(c.Type))
            .Select(c => new Claim(c.Type, c.Value, c.ValueType, c.Issuer));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "keycloak", NameClaim, RoleClaim));
    }

    /// <summary>Unix-time claim (iat, exp, auth_time) as a DateTimeOffset.</summary>
    public static DateTimeOffset? UnixTime(this ClaimsPrincipal user, string claimType) =>
        long.TryParse(user.FindFirst(claimType)?.Value, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds)
            : null;

    /// <summary>
    /// Checks Keycloak's standard access-token claim
    /// "resource_access": { "&lt;client&gt;": { "roles": [ ... ] } }.
    /// </summary>
    public static bool HasClientRole(this ClaimsPrincipal user, string clientId, string role)
    {
        var json = user.FindFirst("resource_access")?.Value;
        if (string.IsNullOrEmpty(json)) return false;

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty(clientId, out var client)
            && client.TryGetProperty("roles", out var roles)
            && roles.EnumerateArray().Any(r => r.GetString() == role);
    }
}
