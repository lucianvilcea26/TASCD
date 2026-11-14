using Biblioteca.Data;
using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Controllers;

[Authorize(Roles = Roles.Librarian)]
public class ReadersController(BibliotecaDbContext db, ISecretariatClient secretariat) : Controller
{
    public async Task<IActionResult> Index()
    {
        return View(await db.Users.OrderBy(u => u.CardNumber).ToListAsync());
    }

    public async Task<IActionResult> Details(string id)
    {
        var reader = await db.Users.FirstOrDefaultAsync(u => u.CardNumber == id);
        if (reader is null)
            return NotFound();

        EnrollmentStatus? enrollment = null;
        try
        {
            enrollment = reader.Matricol is null ? null : await secretariat.GetEnrollmentAsync(reader.Matricol);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            ViewData["SecretariatDown"] = true;
        }
        var loans = await db.Loans.Include(l => l.Book).Where(l => l.UserId == reader.Id && l.ReturnedUtc == null).ToListAsync();
        return View(new ReaderDetailsViewModel(reader, enrollment, loans));
    }

    public async Task<IActionResult> Find(string card)
    {
        if (!await db.Users.AnyAsync(u => u.CardNumber == card))
        {
            ViewData["Card"] = card;
            return View("NotFound");
        }
        return RedirectToAction(nameof(Details), new { id = card });
    }
}
