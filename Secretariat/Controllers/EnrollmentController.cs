using Microsoft.AspNetCore.Mvc;
using Secretariat.Models;
using Secretariat.Services;

namespace Secretariat.Controllers;

[ApiController]
[Route("enrollment")]
public class EnrollmentController(StudentRegistry registry) : ControllerBase
{
    [HttpGet("{matricol}")]
    public ActionResult<EnrollmentResponse> Get(string matricol)
    {
        var student = registry.Find(matricol);
        if (student is null)
            return NotFound();

        var today = DateOnly.FromDateTime(DateTime.Today);
        return new EnrollmentResponse(student.Matricol, student.EnrolledUntil >= today, student.EnrolledUntil);
    }

    [HttpPost("{matricol}/revoke")]
    public IActionResult Revoke(string matricol)
    {
        return registry.Revoke(matricol) ? NoContent() : NotFound();
    }
}
