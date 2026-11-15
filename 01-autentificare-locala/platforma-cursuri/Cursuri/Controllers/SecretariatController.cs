using Cursuri.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cursuri.Controllers;

[Authorize]
public class SecretariatController(ISecretariatRegistry secretariat) : Controller
{
    public IActionResult Index()
    {
        return View(secretariat.GetAll());
    }
}
