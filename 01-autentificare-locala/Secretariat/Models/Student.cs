namespace Secretariat.Models;

// Adresa de email este cea din dosarul de înmatriculare, verificată la ghișeul secretariatului.
public record Student(string Matricol, string Name, string Cnp, string Email, string Program, decimal Average, DateOnly EnrolledUntil);
