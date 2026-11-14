using Biblioteca.Data;

namespace Biblioteca.Models;

public record ReaderDetailsViewModel(ApplicationUser Reader, string Role, EnrollmentStatus? Enrollment, IReadOnlyList<Loan> Loans);

public record ReaderRow(ApplicationUser Reader, string Role);

public record BookRow(int Id, string Title, string Author, int Year, int Copies, int Available);
