using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class BooksController(IBookCatalog catalog) : Controller
{
    public IActionResult Index()
    {
        return View(catalog.GetAll());
    }
}
