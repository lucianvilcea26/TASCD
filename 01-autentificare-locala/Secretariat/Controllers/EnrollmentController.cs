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

    // Folosit de bibliotecă la crearea unui cont: confirmă că adresa de email este cea din dosar.
    // Un matricol inexistent primește același răspuns ca o adresă greșită, ca să nu poată fi ghicite matricolele.
    [HttpPost("{matricol}/email-check")]
    public ActionResult<EmailCheckResponse> CheckEmail(string matricol, EmailCheckRequest request)
    {
        var student = registry.Find(matricol);
        var matches = student is not null
            && string.Equals(student.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!matches)
            return new EmailCheckResponse(false, false, null);

        var today = DateOnly.FromDateTime(DateTime.Today);
        return new EmailCheckResponse(true, student!.EnrolledUntil >= today, student.Name);
    }

    [HttpPost("{matricol}/revoke")]
    public IActionResult Revoke(string matricol)
    {
        return registry.Revoke(matricol) ? NoContent() : NotFound();
    }
}
