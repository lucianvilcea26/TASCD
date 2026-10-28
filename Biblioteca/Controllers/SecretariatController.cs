using Microsoft.AspNetCore.Authorization;
using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

[Authorize(Roles = "bibliotecar")]
public class SecretariatController(ISecretariatRegistry secretariat) : Controller
{
    public IActionResult Index()
    {
        return View(secretariat.GetAll());
    }
}
