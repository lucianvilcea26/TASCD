using Cursuri.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cursuri.Controllers;

public class SecretariatController(ISecretariatRegistry secretariat) : Controller
{
    public IActionResult Index()
    {
        return View(secretariat.GetAll());
    }
}
