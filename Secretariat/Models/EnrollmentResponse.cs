namespace Secretariat.Models;

// Tot ce află biblioteca: dacă studentul este înmatriculat și până când.
public record EnrollmentResponse(string Matricol, bool Enrolled, DateOnly ValidUntil);
