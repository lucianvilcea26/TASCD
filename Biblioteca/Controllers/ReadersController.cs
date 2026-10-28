using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class ReadersController(ILibraryRegistry library, ISecretariatRegistry secretariat) : Controller
{
    public IActionResult Index()
    {
        return View(library.GetAll());
    }

    public IActionResult Details(string id)
    {
        var reader = library.FindByCard(id);
        if (reader is null)
            return NotFound();

        var matricol = reader.IdentifierIn(Contexts.Secretariat);
        var student = matricol is null ? null : secretariat.FindByMatricol(matricol);
        return View(new ReaderDetailsViewModel(reader, student));
    }
}
