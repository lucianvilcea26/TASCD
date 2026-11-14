using Biblioteca.Accounts;
using Biblioteca.Data;
using Biblioteca.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

public class BooksController(BibliotecaDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var books = await db.Books
            .OrderBy(b => b.Title)
            .Select(b => new BookRow(b.Id, b.Title, b.Author, b.Year, b.Copies,
                b.Copies - b.Loans.Count(l => l.ReturnedUtc == null)))
            .ToListAsync();
        return View(books);
    }

    [HttpPost]
    [Authorize(Policy = "CanBorrow")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Borrow(int id)
    {
        // Verificarea și adăugarea se fac în aceeași tranzacție, ca două cereri simultane să nu ia ultimul exemplar.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var book = await db.Books.FindAsync(id);
        if (book is null)
            return NotFound();

        var borrowed = await db.Loans.CountAsync(l => l.BookId == id && l.ReturnedUtc == null);
        if (borrowed >= book.Copies)
        {
            TempData["Message"] = $"Nu mai există exemplare disponibile din {book.Title}.";
            return RedirectToAction(nameof(Index));
        }

        db.Loans.Add(new Loan { BookId = id, UserId = User.Id(), Due = DateOnly.FromDateTime(DateTime.Today).AddDays(14) });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["Message"] = $"Ai împrumutat {book.Title}.";
        return RedirectToAction(nameof(Index));
    }
}
