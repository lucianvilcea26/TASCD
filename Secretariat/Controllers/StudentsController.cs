using Microsoft.AspNetCore.Mvc;
using Secretariat.Models;
using Secretariat.Services;

namespace Secretariat.Controllers;

[ApiController]
[Route("students")]
public class StudentsController(StudentRegistry registry) : ControllerBase
{
    [HttpGet("{matricol}")]
    public ActionResult<Student> Get(string matricol)
    {
        var student = registry.Find(matricol);
        return student is null ? NotFound() : student;
    }
}
