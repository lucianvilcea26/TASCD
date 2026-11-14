using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Secretariat.Web.Data;

namespace Secretariat.Web.Keycloak;

public sealed class KeycloakAdminOptions
{
    /// <summary>e.g. http://localhost:8080/admin/realms/universitate/ (trailing slash matters)</summary>
    public string BaseUrl { get; set; } = "";
    /// <summary>Where the invitation link sends the student once they finish setup.</summary>
    public string InvitationClientId { get; set; } = "";
    public string InvitationRedirectUri { get; set; } = "";
    public int InvitationLifespanSeconds { get; set; } = 259200;
    /// <summary>Only used to build "open in admin console" links.</summary>
    public string AdminConsoleUrl { get; set; } = "";
}

public sealed record KeycloakUserStatus(
    bool Enabled,
    bool EmailVerified,
    IReadOnlyList<string> RequiredActions,
    bool HasPassword,
    bool HasOtp,
    int ActiveSessions);

public sealed class KeycloakAdminException(string message) : Exception(message);

/// <summary>
/// A thin, hand-written client for the Keycloak Admin REST API.
/// Every call made here is exactly what the Keycloak admin console does when you click
/// around in it (open DevTools → Network in the console to compare).
/// It authenticates as the Secretariat's service account (see AddServiceAccountToken in Program.cs).
/// </summary>
public sealed class KeycloakAdminClient(HttpClient http, IOptions<KeycloakAdminOptions> options)
{
    private readonly KeycloakAdminOptions _options = options.Value;

    /// <summary>Log of the HTTP calls made through this instance (shown in the UI for teaching).</summary>
    public List<string> Calls { get; } = [];

    // POST /users ----------------------------------------------------------------------------
    public async Task<string> CreateUserAsync(Student s, CancellationToken ct = default)
    {
        var body = new
        {
            username = s.StudentId.ToLowerInvariant(),
            enabled = true,
            emailVerified = false,
            firstName = s.FirstName,
            lastName = s.LastName,
            email = s.Email,
            attributes = AttributesFor(s),
        };
        using var response = await SendAsync(HttpMethod.Post, "users", JsonContent.Create(body), ct);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new KeycloakAdminException(
                $"Keycloak already has a user with username '{body.username}' or email '{s.Email}'. " + await ErrorText(response, ct));
        await EnsureSuccess(response, "create user", ct);

        // 201 Created; the new user's id is the last segment of the Location header.
        return response.Headers.Location!.Segments.Last();
    }

    // PUT /users/{id}/groups/{groupId} ----------------------------------------------------------
    public async Task AddToGroupAsync(string userId, string groupName, CancellationToken ct = default)
    {
        using var find = await SendAsync(HttpMethod.Get, $"groups?search={Uri.EscapeDataString(groupName)}&exact=true", null, ct);
        await EnsureSuccess(find, "find group", ct);
        var groups = await find.Content.ReadFromJsonAsync<JsonElement>(ct);
        var groupId = groups.EnumerateArray()
            .Where(g => g.GetProperty("name").GetString() == groupName)
            .Select(g => g.GetProperty("id").GetString())
            .FirstOrDefault() ?? throw new KeycloakAdminException($"Group '{groupName}' does not exist in the realm.");

        using var response = await SendAsync(HttpMethod.Put, $"users/{userId}/groups/{groupId}", null, ct);
        await EnsureSuccess(response, "join group", ct);
    }

    // GET + PUT /users/{id} --------------------------------------------------------------------
    /// <summary>
    /// Read-modify-write: fetch the full user, change our fields, send the whole thing back.
    /// A PUT with only some fields can wipe attributes you did not include.
    /// </summary>
    public async Task UpdateUserAsync(string userId, Student s, CancellationToken ct = default)
    {
        using var get = await SendAsync(HttpMethod.Get, $"users/{userId}", null, ct);
        await EnsureSuccess(get, "read user", ct);
        var user = (await get.Content.ReadFromJsonAsync<JsonObject>(ct))!;

        user["firstName"] = s.FirstName;
        user["lastName"] = s.LastName;
        if (!string.Equals(user["email"]?.GetValue<string>(), s.Email, StringComparison.OrdinalIgnoreCase))
        {
            user["email"] = s.Email;
            user["emailVerified"] = false;
        }
        var attributes = user["attributes"] as JsonObject ?? [];
        foreach (var (key, values) in AttributesFor(s))
            attributes[key] = new JsonArray(values.Select(v => (JsonNode)v!).ToArray());
        user["attributes"] = attributes;

        using var put = await SendAsync(HttpMethod.Put, $"users/{userId}", JsonContent.Create(user), ct);
        await EnsureSuccess(put, "update user", ct);
    }

    // PUT /users/{id}/execute-actions-email ----------------------------------------------------
    /// <summary>
    /// Emails the user a one-time link. VERIFY_EMAIL: opening the link proves they control the inbox
    /// (Keycloak sets emailVerified only if this action is in the list). Then they set a password
    /// and register an authenticator app (TOTP).
    /// </summary>
    public async Task SendInvitationAsync(string userId, CancellationToken ct = default)
    {
        var url = $"users/{userId}/execute-actions-email" +
                  $"?client_id={Uri.EscapeDataString(_options.InvitationClientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(_options.InvitationRedirectUri)}" +
                  $"&lifespan={_options.InvitationLifespanSeconds}";
        using var response = await SendAsync(HttpMethod.Put, url,
            JsonContent.Create(new[] { "VERIFY_EMAIL", "UPDATE_PASSWORD", "CONFIGURE_TOTP" }), ct);
        await EnsureSuccess(response, "send invitation email", ct);
    }

    // GET /users/{id}, /credentials, /sessions --------------------------------------------------
    public async Task<KeycloakUserStatus?> GetStatusAsync(string userId, CancellationToken ct = default)
    {
        using var userResponse = await SendAsync(HttpMethod.Get, $"users/{userId}", null, ct, log: false);
        if (userResponse.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureSuccess(userResponse, "read user", ct);
        var user = await userResponse.Content.ReadFromJsonAsync<JsonElement>(ct);

        using var credResponse = await SendAsync(HttpMethod.Get, $"users/{userId}/credentials", null, ct, log: false);
        await EnsureSuccess(credResponse, "read credentials", ct);
        var types = (await credResponse.Content.ReadFromJsonAsync<JsonElement>(ct))
            .EnumerateArray().Select(c => c.GetProperty("type").GetString()).ToHashSet();

        using var sessionResponse = await SendAsync(HttpMethod.Get, $"users/{userId}/sessions", null, ct, log: false);
        await EnsureSuccess(sessionResponse, "read sessions", ct);
        var sessions = (await sessionResponse.Content.ReadFromJsonAsync<JsonElement>(ct)).GetArrayLength();

        return new KeycloakUserStatus(
            user.GetProperty("enabled").GetBoolean(),
            user.TryGetProperty("emailVerified", out var ev) && ev.GetBoolean(),
            user.TryGetProperty("requiredActions", out var ra)
                ? ra.EnumerateArray().Select(a => a.GetString()!).ToList()
                : [],
            types.Contains("password"),
            types.Contains("otp"),
            sessions);
    }

    // POST /users/{id}/logout ------------------------------------------------------------------
    /// <summary>Ends all of the user's Keycloak sessions; their refresh tokens stop working.</summary>
    public async Task LogoutAsync(string userId, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Post, $"users/{userId}/logout", null, ct);
        await EnsureSuccess(response, "log out user", ct);
    }

    // DELETE /users/{id} -----------------------------------------------------------------------
    public async Task DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        using var response = await SendAsync(HttpMethod.Delete, $"users/{userId}", null, ct);
        if (response.StatusCode != HttpStatusCode.NotFound)
            await EnsureSuccess(response, "delete user", ct);
    }

    public string AdminConsoleLink(string userId) =>
        $"{_options.AdminConsoleUrl.TrimEnd('/')}/#/universitate/users/{userId}/settings";

    // ------------------------------------------------------------------------------------------

    private static Dictionary<string, string[]> AttributesFor(Student s) => new()
    {
        ["student_id"] = [s.StudentId],
        ["enrollment_status"] = [s.Status.ToClaimValue()],
        ["faculty"] = [s.Faculty],
    };

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string relativeUrl, HttpContent? content, CancellationToken ct, bool log = true)
    {
        using var request = new HttpRequestMessage(method, relativeUrl) { Content = content };
        var response = await http.SendAsync(request, ct);
        if (log)
        {
            var path = new Uri(http.BaseAddress!, relativeUrl).PathAndQuery;
            Calls.Add($"{method} {Uri.UnescapeDataString(path)} → {(int)response.StatusCode} {response.ReasonPhrase}");
        }
        return response;
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string what, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var hint = response.StatusCode == HttpStatusCode.Forbidden
            ? " The Secretariat service account is missing a realm-management role."
            : "";
        throw new KeycloakAdminException(
            $"Keycloak refused to {what}: {(int)response.StatusCode} {response.ReasonPhrase}. {await ErrorText(response, ct)}{hint}");
    }

    private static async Task<string> ErrorText(HttpResponseMessage response, CancellationToken ct)
    {
        var text = await response.Content.ReadAsStringAsync(ct);
        try
        {
            var json = JsonDocument.Parse(text).RootElement;
            if (json.TryGetProperty("errorMessage", out var m)) return m.GetString() ?? "";
            if (json.TryGetProperty("error", out var e)) return e.GetString() ?? "";
        }
        catch (JsonException) { }
        return text.Length > 300 ? text[..300] : text;
    }
}
