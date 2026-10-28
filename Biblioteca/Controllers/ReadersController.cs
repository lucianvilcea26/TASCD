using Microsoft.AspNetCore.Authorization;
using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

[Authorize(Roles = "bibliotecar")]
public class ReadersController(ILibraryRegistry library, ISecretariatClient secretariat) : Controller
{
    public IActionResult Index()
    {
        return View(library.GetAll());
    }

    public async Task<IActionResult> Details(string id)
    {
        var reader = library.FindByCard(id);
        if (reader is null)
            return NotFound();

        var matricol = reader.IdentifierIn(Contexts.Secretariat);
        var enrollment = matricol is null ? null : await secretariat.GetEnrollmentAsync(matricol);
        return View(new ReaderDetailsViewModel(reader, enrollment));
    }

    public IActionResult Find(string card)
    {
        if (library.FindByCard(card) is null)
        {
            ViewData["Card"] = card;
            return View("NotFound");
        }
        return RedirectToAction(nameof(Details), new { id = card });
    }
}
