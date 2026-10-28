using Biblioteca.Models;

namespace Biblioteca.Services;

public class InMemorySecretariatRegistry : ISecretariatRegistry
{
    private readonly List<IdentityRecord> students =
    [
        Student("SIM-2026-0412", "Ana Popescu", "6030512400010", "Securitate informatică", "9,12", new DateOnly(2027, 9, 30)),
        Student("SIM-2026-0388", "Mihai Ionescu", "5021107400021", "Securitate informatică", "8,47", new DateOnly(2027, 9, 30)),
        Student("SIM-2024-0107", "Elena Dumitru", "6001224400032", "Securitate informatică", "9,65", new DateOnly(2026, 7, 31)),
    ];

    public IReadOnlyList<IdentityRecord> GetAll() => students;

    public IdentityRecord? FindByMatricol(string matricol) =>
        students.FirstOrDefault(s => s.IdentifierIn(Contexts.Secretariat) == matricol);

    private static IdentityRecord Student(string matricol, string name, string cnp, string program,
        string average, DateOnly enrolledUntil)
    {
        var record = new IdentityRecord(Contexts.Secretariat);
        record.Identifiers.Add(new Identifier(Contexts.Secretariat, matricol));
        record.Attributes.Add(new IdentityAttribute("nume", name, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("cnp", cnp, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("program", program, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("medie", average, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("înmatriculare", "activă", Contexts.Secretariat, enrolledUntil));
        return record;
    }
}
