using Cursuri.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cursuri.Controllers;

[Authorize]
public class CoursesController(ICourseCatalog catalog) : Controller
{
    public IActionResult Index()
    {
        return View(catalog.GetAll());
    }
}
