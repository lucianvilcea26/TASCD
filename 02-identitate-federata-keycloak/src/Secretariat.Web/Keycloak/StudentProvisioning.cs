using Secretariat.Web.Data;

namespace Secretariat.Web.Keycloak;

/// <summary>
/// Keeps Keycloak in step with the student registry. The registry is saved first; if Keycloak
/// fails, the error is stored on the student and the secretary can retry. (Two systems, no
/// distributed transaction: this is the classic provisioning problem SCIM was designed for.)
/// </summary>
public sealed class StudentProvisioning(SecretariatDb db, KeycloakAdminClient keycloak, ILogger<StudentProvisioning> logger)
{
    public const string StudentsGroup = "studenti";

    public IReadOnlyList<string> Calls => keycloak.Calls;

    /// <summary>Creates the Keycloak account (if missing), adds it to the students group, optionally emails the invitation.</summary>
    public Task<bool> ProvisionAsync(Student s, bool sendInvitation, CancellationToken ct = default) =>
        RunAsync(s, async () =>
        {
            if (s.KeycloakUserId is null)
            {
                s.KeycloakUserId = await keycloak.CreateUserAsync(s, ct);
                await db.SaveChangesAsync(ct); // remember the id even if a later step fails
            }
            await keycloak.AddToGroupAsync(s.KeycloakUserId, StudentsGroup, ct);
            if (sendInvitation)
            {
                await keycloak.SendInvitationAsync(s.KeycloakUserId, ct);
                s.InvitedAtUtc = DateTime.UtcNow;
            }
        }, ct);

    /// <summary>Publishes changed registry data (names, email, faculty, status) to Keycloak.</summary>
    public Task<bool> PushChangesAsync(Student s, bool revokeSessions, CancellationToken ct = default) =>
        RunAsync(s, async () =>
        {
            if (s.KeycloakUserId is null)
                throw new KeycloakAdminException("The student has no Keycloak account yet. Use 'Create Keycloak account' first.");
            await keycloak.UpdateUserAsync(s.KeycloakUserId, s, ct);
            if (revokeSessions)
                await keycloak.LogoutAsync(s.KeycloakUserId, ct);
        }, ct);

    public Task<bool> ResendInvitationAsync(Student s, CancellationToken ct = default) =>
        RunAsync(s, async () =>
        {
            if (s.KeycloakUserId is null)
                throw new KeycloakAdminException("The student has no Keycloak account yet.");
            await keycloak.SendInvitationAsync(s.KeycloakUserId, ct);
            s.InvitedAtUtc = DateTime.UtcNow;
        }, ct);

    public async Task DeleteAsync(Student s, CancellationToken ct = default)
    {
        if (s.KeycloakUserId is not null)
            await keycloak.DeleteUserAsync(s.KeycloakUserId, ct);
        db.Students.Remove(s);
        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> RunAsync(Student s, Func<Task> work, CancellationToken ct)
    {
        try
        {
            await work();
            s.LastSyncError = null;
            return true;
        }
        catch (Exception ex) when (ex is KeycloakAdminException or HttpRequestException)
        {
            logger.LogWarning("Keycloak sync failed for {StudentId}: {Error}", s.StudentId, ex.Message);
            s.LastSyncError = ex.Message;
            return false;
        }
        finally
        {
            s.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }
}
