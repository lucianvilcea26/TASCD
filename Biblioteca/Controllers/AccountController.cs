using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class AccountController(ILibraryRegistry library) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
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
        if (Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public IActionResult Me()
    {
        return View(User.Claims);
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
