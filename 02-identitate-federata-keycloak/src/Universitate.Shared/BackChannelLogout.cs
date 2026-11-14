using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Universitate.Shared;

/// <summary>
/// Keycloak sessions (by "sid") that ended: logout in another app, "end sessions" in the
/// Secretariat, or the admin console. In memory, so it is per app instance (fine for a lab;
/// a real deployment would use a shared cache).
/// </summary>
public sealed class EndedSessions
{
    // A Keycloak session lives at most "SSO Session Max" (10 hours, the realm default).
    // After that, no app cookie can still belong to it, so the entry can be forgotten.
    private static readonly TimeSpan Retention = TimeSpan.FromHours(10);
    private readonly ConcurrentDictionary<string, DateTimeOffset> _sids = new();

    public void Add(string sid)
    {
        var now = DateTimeOffset.UtcNow;
        _sids[sid] = now;
        foreach (var (oldSid, endedAt) in _sids)
            if (now - endedAt > Retention)
                _sids.TryRemove(oldSid, out _);
    }

    public bool Contains(string? sid) => sid is not null && _sids.ContainsKey(sid);
}

/// <summary>
/// OIDC Back-Channel Logout 1.0. When a Keycloak session ends, Keycloak POSTs a signed
/// "logout token" straight to this endpoint (server to server, no browser involved).
/// We remember the session id; TokenRefresher then rejects any cookie that belongs to it
/// on the user's very next request.
/// </summary>
public static class BackChannelLogout
{
    public const string Path = "/backchannel-logout";
    private const string LogoutEvent = "http://schemas.openid.net/event/backchannel-logout";

    public static IEndpointRouteBuilder MapBackChannelLogout(this IEndpointRouteBuilder app)
    {
        app.MapPost(Path, async (HttpRequest request, EndedSessions ended,
                IOptionsMonitor<OpenIdConnectOptions> oidc, IOptions<KeycloakOptions> keycloak,
                ILoggerFactory loggers) =>
            {
                var logger = loggers.CreateLogger(typeof(BackChannelLogout));
                request.HttpContext.Response.Headers.CacheControl = "no-store"; // required by the spec (2.8)

                var form = await request.ReadFormAsync();
                var logoutToken = form["logout_token"].ToString();
                if (string.IsNullOrEmpty(logoutToken)) return Results.BadRequest();

                // Signed by our realm, addressed to this client, not expired, and typed as a logout token.
                var options = oidc.Get(OpenIdConnectDefaults.AuthenticationScheme);
                var config = await options.ConfigurationManager!.GetConfigurationAsync(request.HttpContext.RequestAborted);
                var result = await new JsonWebTokenHandler().ValidateTokenAsync(logoutToken, new TokenValidationParameters
                {
                    ValidIssuer = config.Issuer,
                    ValidAudience = keycloak.Value.ClientId,
                    IssuerSigningKeys = config.SigningKeys,
                    ValidTypes = ["logout+jwt"], // header "typ": an ID or access token can't pass as a logout token
                });
                if (!result.IsValid)
                {
                    logger.LogWarning("Rejected back-channel logout token: {Error}", result.Exception?.Message);
                    return Results.BadRequest();
                }

                // The claims the spec requires (section 2.6): an "events" object with the back-channel
                // logout event, a "sid" (this realm sends one for every session), and NO "nonce".
                var token = (JsonWebToken)result.SecurityToken;
                using var payload = JsonDocument.Parse(Base64UrlEncoder.Decode(token.EncodedPayload));
                var claims = payload.RootElement;
                var isLogoutEvent = claims.TryGetProperty("events", out var events)
                                    && events.ValueKind == JsonValueKind.Object
                                    && events.TryGetProperty(LogoutEvent, out _);
                if (!isLogoutEvent
                    || claims.TryGetProperty("nonce", out _)
                    || !claims.TryGetProperty("sid", out var sidClaim) || sidClaim.ValueKind != JsonValueKind.String)
                {
                    logger.LogWarning("Rejected back-channel logout token: missing event or sid, or has a nonce.");
                    return Results.BadRequest();
                }

                var sid = sidClaim.GetString()!;
                ended.Add(sid);
                logger.LogInformation("Back-channel logout from Keycloak: session {Sid} of user {Sub} ended.", sid, token.Subject);
                return Results.Ok();
            })
            .AllowAnonymous()       // the caller is Keycloak, not a logged-in user; the signature is the proof
            .DisableAntiforgery();  // server-to-server POST: there is no browser form and no antiforgery token
        return app;
    }
}
