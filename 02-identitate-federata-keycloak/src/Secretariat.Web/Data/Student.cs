using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Secretariat.Web.Data;

public enum EnrollmentStatus { Enrolled, Suspended, Graduated }

/// <summary>
/// The student registry. THIS database is the source of truth; Keycloak only receives
/// a published copy of the fields needed for login and tokens.
/// </summary>
[Index(nameof(StudentId), IsUnique = true)]
[Index(nameof(Email), IsUnique = true)]
public class Student
{
    public int Id { get; set; }

    /// <summary>Număr matricol, e.g. S2026001. Also used as the Keycloak username (lowercase).</summary>
    [Required, RegularExpression(@"^[A-Z]\d{7}$", ErrorMessage = "One capital letter followed by 7 digits, e.g. S2026001.")]
    [Display(Name = "Student ID")]
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

    /// <summary>The user's id in Keycloak, null until the account is provisioned.</summary>
    public string? KeycloakUserId { get; set; }

    public DateTime? InvitedAtUtc { get; set; }

    /// <summary>Last error while pushing data to Keycloak (the two systems can drift apart).</summary>
    public string? LastSyncError { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}";
}

public static class EnrollmentStatusExtensions
{
    /// <summary>The value published to Keycloak and seen in tokens: "enrolled", "suspended", "graduated".</summary>
    public static string ToClaimValue(this EnrollmentStatus status) => status.ToString().ToLowerInvariant();
}

public class SecretariatDb(DbContextOptions<SecretariatDb> options) : DbContext(options)
{
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.Entity<Student>().Property(s => s.Status).HasConversion<string>();
}
