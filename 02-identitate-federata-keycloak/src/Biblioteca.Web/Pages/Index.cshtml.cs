using Biblioteca.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Universitate.Shared;

namespace Biblioteca.Web.Pages;

/// <summary>
/// Catalog + self-service borrowing.
/// VERSION A: enrollment is checked against the enrollment_status claim in the student's own token.
/// Simple and needs no extra call, but the claim is a snapshot taken when the token was issued.
/// </summary>
public class IndexModel(BibliotecaDb db) : PageModel
{
    public record BookRow(Book Book, int Available, bool BorrowedByMe);

    public List<BookRow> Books { get; } = [];

    public async Task OnGetAsync(CancellationToken ct)
    {
        var me = User.FindFirst("student_id")?.Value;
        var books = await db.Books.AsNoTracking()
            .Include(b => b.Loans.Where(l => l.ReturnedAtUtc == null))
            .OrderBy(b => b.Title)
            .ToListAsync(ct);
        Books.AddRange(books.Select(b => new BookRow(b, b.Copies - b.Loans.Count, me is not null && b.Loans.Any(l => l.StudentId == me))));
    }

    public async Task<IActionResult> OnPostBorrowAsync(int bookId, CancellationToken ct)
    {
        // Razor Pages can't put [Authorize] on a single handler, so check here.
        if (User.Identity?.IsAuthenticated != true) return Challenge();
        if (!User.IsInRole("reader")) return Forbid();

        var studentId = User.FindFirst("student_id")?.Value;
        var status = User.FindFirst("enrollment_status")?.Value;
        var issuedAt = User.UnixTime("iat");
        var age = issuedAt is null ? "?" : $"{(int)(DateTimeOffset.UtcNow - issuedAt.Value).TotalSeconds}";
        var evidence = $"token claim enrollment_status=\"{status ?? "(missing)"}\", " +
                       $"token issued {issuedAt?.ToLocalTime():HH:mm:ss} ({age} s before the loan)";

        if (studentId is null)
        {
            TempData["Error"] = "Your token has no student_id claim, so you cannot borrow self-service.";
            return RedirectToPage();
        }
        if (status != "enrolled")
        {
            TempData["Error"] = $"Refused. Your token says you are \"{status ?? "(missing)"}\". [{evidence}]";
            return RedirectToPage();
        }

        var book = await db.Books.Include(b => b.Loans.Where(l => l.ReturnedAtUtc == null))
            .FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book is null) return NotFound();
        if (book.Loans.Any(l => l.StudentId == studentId))
        {
            TempData["Error"] = "You already have this book.";
            return RedirectToPage();
        }
        if (book.Loans.Count >= book.Copies)
        {
            TempData["Error"] = "No copies available.";
            return RedirectToPage();
        }

        db.Loans.Add(new Loan
        {
            BookId = book.Id,
            StudentId = studentId,
            BorrowerName = User.FindFirst("name")?.Value ?? User.Identity.Name ?? studentId,
            LoanedAtUtc = DateTime.UtcNow,
            DueAtUtc = DateTime.UtcNow.AddDays(14),
            Channel = "self-service",
            EnrollmentEvidence = evidence,
        });
        await db.SaveChangesAsync(ct);

        TempData["Message"] = $"Borrowed \"{book.Title}\". Accepted because {evidence}.";
        return RedirectToPage();
    }
}
