using System.Security.Claims;
using Cursuri.Models;
using Cursuri.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cursuri.Controllers;

[Authorize(Roles = "administrator")]
public class AdminController(IPlatformRegistry platform, ISecretariatRegistry secretariat) : Controller
{
    public static readonly string[] Roles = ["student", "profesor", "administrator"];

    public IActionResult Users()
    {
        var rows = platform.GetAll()
            .Select(account => new UserRow(account, Origin(account), CheckWithSecretariat(account)))
            .ToList();
        return View(rows);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetRole(string username, string role)
    {
        if (!Roles.Contains(role) || !platform.SetRole(username, role))
            return BadRequest();
        TempData["Message"] = $"{username} is now {role}. The change reaches their session at the next login.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ResetPassword(string username)
    {
        var password = platform.ResetPassword(username);
        if (password is null)
            return NotFound();
        TempData["Message"] = $"Temporary password for {username}: {password} (shown only once).";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(string username)
    {
        if (username == User.FindFirstValue(ClaimTypes.NameIdentifier))
        {
            TempData["Error"] = "You cannot delete your own account.";
            return RedirectToAction(nameof(Users));
        }
        if (!platform.Delete(username))
            return NotFound();
        TempData["Message"] = $"The account {username} was deleted.";
        return RedirectToAction(nameof(Users));
    }

    // Conturile create la pornire au numele afirmat de platformă; cele înregistrate, de utilizator.
    private static string Origin(IdentityRecord account) =>
        account.Attributes.First(a => a.Name == "nume").Issuer == Contexts.SelfAsserted ? "self-registered" : "seeded";

    // Ce ar face un om de la secretariat: compară ce a declarat utilizatorul cu registrul autoritar.
    private string CheckWithSecretariat(IdentityRecord account)
    {
        var matricol = account.IdentifierIn(Contexts.Secretariat);
        if (matricol is null)
            return "-";
        var student = secretariat.FindByMatricol(matricol);
        if (student is null)
            return "Unknown matricol";
        return student.Attribute("nume") == account.Attribute("nume") ? "Matches" : "Name differs";
    }
}
