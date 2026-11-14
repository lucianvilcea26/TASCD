using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Universitate.Shared;

/// <summary>
/// Runs on every request that carries the login cookie. When the access token is about to
/// expire it uses the refresh token to get new tokens from Keycloak and rebuilds the user's
/// claims from the new ID token.
///
/// Two things to show in class:
///  * claims (e.g. enrollment_status) change only at refresh time, so they can be stale
///    for up to the access token lifespan (120 s in this realm);
///  * if the Keycloak session was revoked, the refresh fails and the user is signed out.
/// </summary>
public sealed class TokenRefresher(
    IHttpClientFactory httpClientFactory,
    IOptions<KeycloakOptions> options,
    EndedSessions endedSessions,
    ILogger<TokenRefresher> logger)
{
    public const string HttpClientName = "keycloak-token-refresh";
    private static readonly TimeSpan RefreshBeforeExpiry = TimeSpan.FromSeconds(15);

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        // Keycloak told us (back-channel logout) that this session is over: sign out now,
        // without waiting for the access token to expire.
        if (endedSessions.Contains(context.Principal?.FindFirst("sid")?.Value))
        {
            logger.LogInformation("Session of {User} was ended in Keycloak. Signing out.", context.Principal?.Identity?.Name);
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return;
        }

        var expiresAtText = context.Properties.GetTokenValue("expires_at");
        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (expiresAtText is null || refreshToken is null) return;

        var expiresAt = DateTimeOffset.Parse(expiresAtText, CultureInfo.InvariantCulture);
        if (expiresAt - DateTimeOffset.UtcNow > RefreshBeforeExpiry) return;

        var kc = options.Value;
        HttpResponseMessage response;
        try
        {
            response = await httpClientFactory.CreateClient(HttpClientName).PostAsync(
                kc.TokenEndpoint,
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = kc.ClientId,
                    ["client_secret"] = kc.ClientSecret,
                    ["refresh_token"] = refreshToken,
                }));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Keycloak is unreachable or too slow (TaskCanceledException = HttpClient timeout):
            // keep the current (stale) principal rather than logging everybody out.
            // Try again on the next request.
            logger.LogWarning(ex, "Token refresh failed: Keycloak unreachable.");
            return;
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation(
                    "Refresh token rejected ({Status}) for {User}: the Keycloak session has ended. Signing out.",
                    (int)response.StatusCode, context.Principal?.Identity?.Name);
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var tokens = await response.Content.ReadFromJsonAsync<JsonElement>();
            var idToken = tokens.TryGetProperty("id_token", out var id)
                ? id.GetString()!
                : context.Properties.GetTokenValue("id_token")!;

            context.Properties.UpdateTokenValue("access_token", tokens.GetProperty("access_token").GetString()!);
            context.Properties.UpdateTokenValue("id_token", idToken);
            if (tokens.TryGetProperty("refresh_token", out var newRefresh))
                context.Properties.UpdateTokenValue("refresh_token", newRefresh.GetString()!);
            context.Properties.UpdateTokenValue("expires_at", DateTimeOffset.UtcNow
                .AddSeconds(tokens.GetProperty("expires_in").GetInt32())
                .ToString("o", CultureInfo.InvariantCulture));

            context.ReplacePrincipal(TokenClaims.PrincipalFromIdToken(idToken));
            context.ShouldRenew = true;
            logger.LogInformation("Tokens refreshed for {User}.", context.Principal?.Identity?.Name);
        }
    }
}
