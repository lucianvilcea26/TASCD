using Microsoft.AspNetCore.Authentication;
using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class AccountController(ILibraryRegistry library) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var reader = library.FindByCard(model.CardNumber);
        if (reader is null || !library.VerifyPassword(reader, model.Password))
        {
            ModelState.AddModelError("", "Permisul sau parola nu sunt corecte.");
            return View(model);
        }

        await HttpContext.SignInAsync(ReaderPrincipal.Create(reader));
        return RedirectToAction("Index", "Home");
    }
}
