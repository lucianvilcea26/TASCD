using Biblioteca.Data;

namespace Biblioteca.Models;

public record ReaderDetailsViewModel(User Reader, EnrollmentStatus? Enrollment, IReadOnlyList<Loan> Loans);

public record BookRow(int Id, string Title, string Author, int Year, int Copies, int Available);
