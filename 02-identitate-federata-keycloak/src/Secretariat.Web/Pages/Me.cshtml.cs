using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;

namespace Secretariat.Web.Pages;

/// <summary>The logged-in student's own record, found through the student_id claim in their token.</summary>
[Authorize(Roles = "student")]
public class MeModel(SecretariatDb db) : PageModel
{
    public Student? Student { get; private set; }
    public string? StudentIdClaim { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        StudentIdClaim = User.FindFirst("student_id")?.Value;
        if (StudentIdClaim is not null)
            Student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentId == StudentIdClaim, ct);
    }
}
