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

    [Authorize]
    public IActionResult Me()
    {
        return View(User.Claims);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
