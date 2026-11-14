using System.Text.Json;
using Biblioteca.Web.Data;
using Biblioteca.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Pages.Desk;

/// <summary>
/// The lending desk, used by a librarian.
/// VERSION B: the token here belongs to the LIBRARIAN, so it says nothing about the student.
/// Biblioteca asks the source of truth (Secretariat API) at the moment of lending.
/// </summary>
[Authorize(Roles = "librarian")]
public class IndexModel(BibliotecaDb db, SecretariatApiClient secretariat) : PageModel
{
    [BindProperty] public string StudentId { get; set; } = "";
    [BindProperty] public int BookId { get; set; }

    public List<Book> Books { get; private set; } = [];
    public List<Loan> ActiveLoans { get; private set; } = [];
    public EnrollmentCheck? LastCheck { get; private set; }
    public string? CheckedStudentId { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        if (TempData["Check"] is string json)
        {
            LastCheck = JsonSerializer.Deserialize<EnrollmentCheck>(json);
            CheckedStudentId = TempData["CheckedStudentId"] as string;
            StudentId = CheckedStudentId ?? "";
        }
        Books = await db.Books.AsNoTracking().OrderBy(b => b.Title).ToListAsync(ct);
        ActiveLoans = await db.Loans.AsNoTracking().Include(l => l.Book)
            .Where(l => l.ReturnedAtUtc == null)
            .OrderBy(l => l.DueAtUtc)
            .ToListAsync(ct);
    }

    /// <summary>Only asks the Secretariat, lends nothing.</summary>
    public async Task<IActionResult> OnPostCheckAsync(CancellationToken ct)
    {
        var id = Normalize(StudentId);
        Remember(id, await secretariat.CheckEnrollmentAsync(id, ct));
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLendAsync(CancellationToken ct)
    {
        var id = Normalize(StudentId);
        var check = await secretariat.CheckEnrollmentAsync(id, ct);
        Remember(id, check);

        if (!check.Allowed)
        {
            TempData["Error"] = check.Status is null
                ? $"Not lent: enrollment could not be verified ({check.HttpStatus})."
                : $"Not lent: the Secretariat says {id} is \"{check.Status}\".";
            return RedirectToPage();
        }

        var book = await db.Books.Include(b => b.Loans.Where(l => l.ReturnedAtUtc == null))
            .FirstOrDefaultAsync(b => b.Id == BookId, ct);
        if (book is null) return NotFound();
        if (book.Loans.Count >= book.Copies)
        {
            TempData["Error"] = $"No copies of \"{book.Title}\" available.";
            return RedirectToPage();
        }
        if (book.Loans.Any(l => l.StudentId == id))
        {
            TempData["Error"] = $"{id} already has \"{book.Title}\".";
            return RedirectToPage();
        }

        db.Loans.Add(new Loan
        {
            BookId = book.Id,
            StudentId = id,
            BorrowerName = check.Name ?? id,
            LoanedAtUtc = DateTime.UtcNow,
            DueAtUtc = DateTime.UtcNow.AddDays(14),
            Channel = "desk",
            EnrollmentEvidence = $"Secretariat API said \"{check.Status}\" at {DateTime.Now:HH:mm:ss} " +
                                 $"(lent by {User.Identity?.Name})",
        });
        await db.SaveChangesAsync(ct);
        TempData["Message"] = $"\"{book.Title}\" lent to {check.Name} ({id}).";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReturnAsync(int loanId, CancellationToken ct)
    {
        var loan = await db.Loans.Include(l => l.Book).FirstOrDefaultAsync(l => l.Id == loanId, ct);
        if (loan is null) return NotFound();
        loan.ReturnedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        TempData["Message"] = $"\"{loan.Book.Title}\" returned by {loan.BorrowerName}.";
        return RedirectToPage();
    }

    private static string Normalize(string? id) => (id ?? "").Trim().ToUpperInvariant();

    private void Remember(string id, EnrollmentCheck check)
    {
        TempData["Check"] = JsonSerializer.Serialize(check);
        TempData["CheckedStudentId"] = id;
    }
}
