using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;
using Secretariat.Web.Keycloak;

namespace Secretariat.Web.Pages.Students;

[Authorize(Roles = "secretary")]
public class IndexModel(SecretariatDb db, KeycloakAdminClient keycloak) : PageModel
{
    public record Row(Student Student, KeycloakUserStatus? Account);

    public List<Row> Rows { get; } = [];
    public string? KeycloakError { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var students = await db.Students.AsNoTracking()
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(ct);

        // The registry is local; account state (email verified, MFA, sessions) lives in Keycloak.
        KeycloakUserStatus?[] accounts;
        try
        {
            accounts = await Task.WhenAll(students.Select(s => s.KeycloakUserId is null
                ? Task.FromResult<KeycloakUserStatus?>(null)
                : keycloak.GetStatusAsync(s.KeycloakUserId, ct)));
        }
        catch (Exception ex) when (ex is KeycloakAdminException or HttpRequestException)
        {
            KeycloakError = ex.Message;
            accounts = new KeycloakUserStatus?[students.Count];
        }

        Rows.AddRange(students.Zip(accounts, (s, a) => new Row(s, a)));
    }
}
