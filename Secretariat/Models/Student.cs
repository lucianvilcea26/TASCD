namespace Secretariat.Models;

public record Student(string Matricol, string Name, string Cnp, string Program, decimal Average, DateOnly EnrolledUntil);
