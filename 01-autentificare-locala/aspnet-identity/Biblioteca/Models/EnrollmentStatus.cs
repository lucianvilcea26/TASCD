namespace Biblioteca.Models;

// Răspunsul secretariatului, așa cum îl primește biblioteca.
public record EnrollmentStatus(string Matricol, bool Enrolled, DateOnly ValidUntil);
