using Biblioteca.Services;
using Microsoft.AspNetCore.Mvc;

namespace Biblioteca.Controllers;

public class SecretariatController(ISecretariatRegistry secretariat) : Controller
{
    public IActionResult Index()
    {
        return View(secretariat.GetAll());
    }
}
