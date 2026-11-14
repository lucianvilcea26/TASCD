using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;
using Secretariat.Web.Keycloak;

namespace Secretariat.Web.Pages.Students;

[Authorize(Roles = "secretary")]
public class CreateModel(SecretariatDb db, StudentProvisioning provisioning) : PageModel
{
    [BindProperty] public StudentInput Input { get; set; } = new();
    [BindProperty] public bool SendInvitation { get; set; } = true;

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        var student = new Student();
        Input.ApplyTo(student);

        if (await db.Students.AnyAsync(s => s.StudentId == student.StudentId, ct))
            ModelState.AddModelError("Input.StudentId", "This student ID is already in the registry.");
        if (await db.Students.AnyAsync(s => s.Email == student.Email, ct))
            ModelState.AddModelError("Input.Email", "This email is already in the registry.");
        if (!ModelState.IsValid) return Page();

        // 1. The registry first (source of truth) ...
        db.Students.Add(student);
        await db.SaveChangesAsync(ct);

        // 2. ... then publish to the identity provider.
        var ok = await provisioning.ProvisionAsync(student, SendInvitation, ct);

        TempData["Message"] = ok
            ? $"{student.FullName} was added to the registry and to Keycloak." +
              (SendInvitation ? " The invitation email is in Mailpit." : "")
            : $"{student.FullName} was saved in the registry, but Keycloak provisioning failed. See below.";
        TempData["Calls"] = string.Join("\n", provisioning.Calls);
        return RedirectToPage("Edit", new { id = student.Id });
    }
}
