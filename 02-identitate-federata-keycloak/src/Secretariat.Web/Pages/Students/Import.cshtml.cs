using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;
using Secretariat.Web.Keycloak;

namespace Secretariat.Web.Pages.Students;

/// <summary>
/// Bulk onboarding from a CSV export of the enrollment system:
/// student_id,first_name,last_name,email,faculty
/// </summary>
[Authorize(Roles = "secretary")]
public class ImportModel(SecretariatDb db, StudentProvisioning provisioning) : PageModel
{
    public record Result(int Line, string StudentId, string Name, bool Ok, string Outcome, int? Id);

    [BindProperty, Required(ErrorMessage = "Choose a CSV file.")]
    public IFormFile? CsvFile { get; set; }

    [BindProperty] public bool SendInvitations { get; set; } = true;

    public List<Result> Results { get; } = [];

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid || CsvFile is null) return Page();

        using var reader = new StreamReader(CsvFile.OpenReadStream());
        var header = (await reader.ReadLineAsync(ct))?.Trim().ToLowerInvariant();
        if (header != "student_id,first_name,last_name,email,faculty")
        {
            ModelState.AddModelError(nameof(CsvFile), "The first line must be: student_id,first_name,last_name,email,faculty");
            return Page();
        }

        var lineNo = 1;
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            lineNo++;
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Deliberately simple CSV: no quoted fields, so no commas inside values.
            var cols = line.Split(',');
            if (cols.Length != 5)
            {
                Results.Add(new(lineNo, "", "", false, "Expected 5 columns.", null));
                continue;
            }

            var input = new StudentInput
            {
                StudentId = cols[0].Trim().ToUpperInvariant(), FirstName = cols[1], LastName = cols[2],
                Email = cols[3], Faculty = cols[4], Status = EnrollmentStatus.Enrolled,
            };
            var name = $"{input.FirstName.Trim()} {input.LastName.Trim()}";

            var errors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(input, new ValidationContext(input), errors, validateAllProperties: true))
            {
                Results.Add(new(lineNo, input.StudentId, name, false, string.Join(" ", errors.Select(e => e.ErrorMessage)), null));
                continue;
            }

            var student = new Student();
            input.ApplyTo(student);
            if (await db.Students.AnyAsync(s => s.StudentId == student.StudentId || s.Email == student.Email, ct))
            {
                Results.Add(new(lineNo, student.StudentId, name, false, "Skipped: already in the registry.", null));
                continue;
            }

            db.Students.Add(student);
            await db.SaveChangesAsync(ct);
            var ok = await provisioning.ProvisionAsync(student, SendInvitations, ct);
            Results.Add(new(lineNo, student.StudentId, name, ok,
                ok ? (SendInvitations ? "Created in Keycloak, invitation sent." : "Created in Keycloak.")
                   : "Saved in registry; Keycloak failed: " + student.LastSyncError,
                student.Id));
        }

        return Page();
    }
}
