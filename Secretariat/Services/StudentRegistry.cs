using Secretariat.Models;

namespace Secretariat.Services;

public class StudentRegistry
{
    private readonly List<Student> students =
    [
        new("SIM-2026-0412", "Ana Popescu", "6030512400010", "Securitate informatică", 9.12m, new DateOnly(2027, 9, 30)),
        new("SIM-2026-0388", "Mihai Ionescu", "5021107400021", "Securitate informatică", 8.47m, new DateOnly(2027, 9, 30)),
        new("SIM-2024-0107", "Elena Dumitru", "6001224400032", "Securitate informatică", 9.65m, new DateOnly(2026, 7, 31)),
    ];

    private readonly Lock gate = new();

    public Student? Find(string matricol) => students.FirstOrDefault(s => s.Matricol == matricol);

    // Retragerea înmatriculării: valabilitatea se încheie ieri.
    public bool Revoke(string matricol)
    {
        lock (gate)
        {
            var index = students.FindIndex(s => s.Matricol == matricol);
            if (index < 0)
                return false;

            var yesterday = DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
            students[index] = students[index] with { EnrolledUntil = yesterday };
            return true;
        }
    }
}
