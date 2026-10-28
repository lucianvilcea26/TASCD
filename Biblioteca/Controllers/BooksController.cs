using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class BooksController(IBookCatalog catalog) : Controller
{
    public IActionResult Index()
    {
        return View(catalog.GetAll());
    }

    [HttpPost]
    [Authorize(Policy = "CanBorrow")]
    [ValidateAntiForgeryToken]
    public IActionResult Borrow(int id)
    {
        var book = catalog.Find(id);
        if (book is null)
            return NotFound();

        var card = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        TempData["Message"] = catalog.Borrow(book, card)
            ? $"Ai împrumutat {book.Title}."
            : $"Nu mai există exemplare disponibile din {book.Title}.";
        return RedirectToAction(nameof(Index));
    }
}
