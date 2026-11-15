using Cursuri.Models;
using Cursuri.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cursuri.Controllers;

public class PeopleController(IPlatformRegistry platform, ISecretariatRegistry secretariat) : Controller
{
    public IActionResult Index()
    {
        return View(platform.GetAll());
    }

    public IActionResult Details(string id)
    {
        var account = platform.FindByUsername(id);
        if (account is null)
            return NotFound();

        var matricol = account.IdentifierIn(Contexts.Secretariat);
        var student = matricol is null ? null : secretariat.FindByMatricol(matricol);
        return View(new PersonDetailsViewModel(account, student));
    }

    public IActionResult Find(string username)
    {
        if (platform.FindByUsername(username) is null)
        {
            ViewData["Username"] = username;
            return View("NotFound");
        }
        return RedirectToAction(nameof(Details), new { id = username });
    }
}
