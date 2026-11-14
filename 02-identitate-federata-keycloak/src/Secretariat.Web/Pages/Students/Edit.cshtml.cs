using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;
using Secretariat.Web.Keycloak;

namespace Secretariat.Web.Pages.Students;

[Authorize(Roles = "secretary")]
public class EditModel(SecretariatDb db, StudentProvisioning provisioning, KeycloakAdminClient keycloak) : PageModel
{
    public Student Student { get; private set; } = null!;
    public KeycloakUserStatus? Account { get; private set; }
    public string? AccountError { get; private set; }
    public string? AdminConsoleLink { get; private set; }

    [BindProperty] public StudentInput Input { get; set; } = new();
    [BindProperty] public bool RevokeSessions { get; set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken ct)
    {
        if (!await LoadAsync(id, ct)) return NotFound();
        Input = StudentInput.From(Student);
        return Page();
    }

    /// <summary>Save registry changes, then publish them to Keycloak.</summary>
    public async Task<IActionResult> OnPostSaveAsync(int id, CancellationToken ct)
    {
        var student = await db.Students.FindAsync([id], ct);
        if (student is null) return NotFound();

        Input.StudentId = student.StudentId; // the student ID never changes
        ModelState.Remove("Input.StudentId");
        var email = (Input.Email ?? "").Trim().ToLowerInvariant();
        if (await db.Students.AnyAsync(s => s.Id != id && s.Email == email, ct))
            ModelState.AddModelError("Input.Email", "Another student already uses this email.");
        if (!ModelState.IsValid)
        {
            await LoadAsync(id, ct);
            return Page();
        }

        var oldStatus = student.Status;
        Input.ApplyTo(student);
        await db.SaveChangesAsync(ct);

        var ok = await provisioning.PushChangesAsync(student, RevokeSessions, ct);
        TempData["Message"] = !ok
            ? "Saved in the registry, but Keycloak was not updated. See the error below."
            : oldStatus == student.Status
                ? "Saved and published to Keycloak."
                : $"Status changed {oldStatus.ToClaimValue()} → {student.Status.ToClaimValue()} and published to Keycloak. " +
                  (RevokeSessions
                      ? "Sessions ended: the student's apps must log in again and will get a fresh token."
                      : "Tokens already issued keep the old value until they are refreshed (up to 2 minutes).");
        TempData["Calls"] = string.Join("\n", provisioning.Calls);
        return RedirectToPage(new { id });
    }

    public Task<IActionResult> OnPostProvisionAsync(int id, CancellationToken ct) =>
        RunAsync(id, s => provisioning.ProvisionAsync(s, sendInvitation: true, ct),
            "Keycloak account created and invitation sent. Check Mailpit.");

    public Task<IActionResult> OnPostResendAsync(int id, CancellationToken ct) =>
        RunAsync(id, s => provisioning.ResendInvitationAsync(s, ct), "Invitation sent again. Check Mailpit.");

    public Task<IActionResult> OnPostRevokeAsync(int id, CancellationToken ct) =>
        RunAsync(id, s => provisioning.PushChangesAsync(s, revokeSessions: true, ct),
            "All Keycloak sessions of this student were ended.");

    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken ct)
    {
        var student = await db.Students.FindAsync([id], ct);
        if (student is null) return NotFound();
        try
        {
            await provisioning.DeleteAsync(student, ct);
            TempData["Message"] = $"{student.FullName} was removed from the registry and from Keycloak.";
            return RedirectToPage("Index");
        }
        catch (Exception ex) when (ex is KeycloakAdminException or HttpRequestException)
        {
            TempData["Message"] = "Not deleted: " + ex.Message;
            return RedirectToPage(new { id });
        }
    }

    private async Task<IActionResult> RunAsync(int id, Func<Student, Task<bool>> action, string success)
    {
        var student = await db.Students.FindAsync(id);
        if (student is null) return NotFound();
        var ok = await action(student);
        TempData["Message"] = ok ? success : "The Keycloak call failed. See the error below.";
        TempData["Calls"] = string.Join("\n", provisioning.Calls);
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id, CancellationToken ct)
    {
        var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (student is null) return false;
        Student = student;
        if (student.KeycloakUserId is not null)
        {
            AdminConsoleLink = keycloak.AdminConsoleLink(student.KeycloakUserId);
            try { Account = await keycloak.GetStatusAsync(student.KeycloakUserId, ct); }
            catch (Exception ex) when (ex is KeycloakAdminException or HttpRequestException) { AccountError = ex.Message; }
        }
        return true;
    }
}
