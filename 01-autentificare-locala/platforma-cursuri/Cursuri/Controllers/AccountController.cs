using Cursuri.Models;
using Cursuri.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cursuri.Controllers;

public class AccountController(IPlatformRegistry platform) : Controller
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

        var account = platform.FindByUsername(model.Username);
        if (account is null || !platform.VerifyPassword(account, model.Password))
        {
            ModelState.AddModelError("", "The username or the password is not correct.");
            return View(model);
        }

        await HttpContext.SignInAsync(UserPrincipal.Create(account));
        if (Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var account = platform.Register(model.Username, model.FullName, model.Matricol, model.Password);
        if (account is null)
        {
            ModelState.AddModelError(nameof(model.Username), platform.FindByUsername(model.Username) is null
                ? "Registration is closed: the platform has too many accounts."
                : "This username is already taken.");
            return View(model);
        }

        await HttpContext.SignInAsync(UserPrincipal.Create(account));
        return RedirectToAction(nameof(Me));
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
