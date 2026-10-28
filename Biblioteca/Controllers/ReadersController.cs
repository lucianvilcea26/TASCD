using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class ReadersController(ILibraryRegistry library) : Controller
{
    public IActionResult Index()
    {
        return View(library.GetAll());
    }
}
