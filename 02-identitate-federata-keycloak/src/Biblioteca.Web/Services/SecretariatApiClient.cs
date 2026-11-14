using System.Net;
using System.Text.Json;

namespace Biblioteca.Web.Services;

public sealed record EnrollmentCheck(bool Allowed, string? Status, string? Name, int HttpStatus, string Response);

/// <summary>
/// Calls the Secretariat API as Biblioteca's own service account (client credentials).
/// The token Keycloak issues for it has aud = "secretariat-api" and the role enrollment.read,
/// because that client role is assigned to service-account-biblioteca.
/// </summary>
public sealed class SecretariatApiClient(HttpClient http, ILogger<SecretariatApiClient> logger)
{
    public async Task<EnrollmentCheck> CheckEnrollmentAsync(string studentId, CancellationToken ct = default)
    {
        try
        {
            using var response = await http.GetAsync($"api/students/{Uri.EscapeDataString(studentId)}/enrollment", ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var json = JsonDocument.Parse(body).RootElement;
                var status = json.GetProperty("status").GetString();
                return new EnrollmentCheck(status == "enrolled", status, json.GetProperty("name").GetString(), 200, body);
            }

            var explanation = response.StatusCode switch
            {
                HttpStatusCode.NotFound => body,
                HttpStatusCode.Unauthorized => "401: the Secretariat rejected our token (wrong audience, expired, or not from Keycloak).",
                HttpStatusCode.Forbidden => "403: our token is valid but lacks the role secretariat-api / enrollment.read.",
                _ => body,
            };
            return new EnrollmentCheck(false, null, null, (int)response.StatusCode, explanation);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Fail closed: if we cannot verify enrollment (Secretariat or Keycloak down, a timeout,
            // or an unreadable answer), we do not lend.
            logger.LogWarning(ex, "Enrollment check failed");
            return new EnrollmentCheck(false, null, null, 0, "Could not reach the Secretariat or Keycloak: " + ex.Message);
        }
    }
}
