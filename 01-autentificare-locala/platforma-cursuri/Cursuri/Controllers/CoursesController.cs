using Cursuri.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cursuri.Controllers;

public class CoursesController(ICourseCatalog catalog) : Controller
{
    public IActionResult Index()
    {
        return View(catalog.GetAll());
    }
}
