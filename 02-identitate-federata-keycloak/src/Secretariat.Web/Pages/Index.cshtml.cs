using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;

namespace Secretariat.Web.Pages;

public class IndexModel(SecretariatDb db) : PageModel
{
    public int Total { get; private set; }
    public int Enrolled { get; private set; }
    public int Suspended { get; private set; }
    public int NotProvisioned { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        // Registry figures for the secretary's dashboard (cheap: a few COUNTs on the local database).
        if (!User.IsInRole("secretary")) return;
        Total = await db.Students.CountAsync(ct);
        Enrolled = await db.Students.CountAsync(s => s.Status == EnrollmentStatus.Enrolled, ct);
        Suspended = await db.Students.CountAsync(s => s.Status == EnrollmentStatus.Suspended, ct);
        NotProvisioned = await db.Students.CountAsync(s => s.KeycloakUserId == null, ct);
    }
}
