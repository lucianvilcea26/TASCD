using System.ComponentModel.DataAnnotations;
using Secretariat.Web.Data;

namespace Secretariat.Web.Pages.Students;

/// <summary>Form model. Kept separate from the entity so a form can't post KeycloakUserId etc.</summary>
public class StudentInput
{
    [Required, RegularExpression(@"^[A-Z]\d{7}$", ErrorMessage = "One capital letter followed by 7 digits, e.g. S2026001.")]
    [Display(Name = "Student ID (nr. matricol)")]
    public string StudentId { get; set; } = "";

    [Required, StringLength(100), Display(Name = "First name")]
    public string FirstName { get; set; } = "";

    [Required, StringLength(100), Display(Name = "Last name")]
    public string LastName { get; set; } = "";

    [Required, EmailAddress, StringLength(255)]
    public string Email { get; set; } = "";

    [Required, StringLength(100)]
    public string Faculty { get; set; } = "";

    [Display(Name = "Enrollment status")]
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Enrolled;

    public static StudentInput From(Student s) => new()
    {
        StudentId = s.StudentId, FirstName = s.FirstName, LastName = s.LastName,
        Email = s.Email, Faculty = s.Faculty, Status = s.Status,
    };

    public void ApplyTo(Student s)
    {
        s.StudentId = StudentId.Trim().ToUpperInvariant();
        s.FirstName = FirstName.Trim();
        s.LastName = LastName.Trim();
        s.Email = Email.Trim().ToLowerInvariant();
        s.Faculty = Faculty.Trim();
        s.Status = Status;
    }
}
