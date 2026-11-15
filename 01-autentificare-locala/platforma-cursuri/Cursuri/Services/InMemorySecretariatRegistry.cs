using Cursuri.Models;

namespace Cursuri.Services;

public class InMemorySecretariatRegistry : ISecretariatRegistry
{
    private readonly List<IdentityRecord> students =
    [
        Student("S2026001", "Ana Popescu", "6030512400010", "Cybernetics and Economic Informatics", "9,12", new DateOnly(2027, 9, 30)),
        Student("S2026002", "Mihai Dumitrescu", "5021107400021", "Cybernetics and Economic Informatics", "8,47", new DateOnly(2027, 9, 30)),
        Student("S2024017", "Elena Dumitru", "6001224400032", "Cybernetics and Economic Informatics", "9,65", new DateOnly(2026, 7, 31)),
    ];

    public IReadOnlyList<IdentityRecord> GetAll() => students;

    public IdentityRecord? FindByMatricol(string matricol) =>
        students.FirstOrDefault(s => s.IdentifierIn(Contexts.Secretariat) == matricol);

    private static IdentityRecord Student(string matricol, string name, string cnp, string faculty,
        string average, DateOnly enrolledUntil)
    {
        var record = new IdentityRecord(Contexts.Secretariat);
        record.Identifiers.Add(new Identifier(Contexts.Secretariat, matricol));
        record.Attributes.Add(new IdentityAttribute("nume", name, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("cnp", cnp, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("facultate", faculty, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("medie", average, Contexts.Secretariat));
        record.Attributes.Add(new IdentityAttribute("înmatriculare", "activă", Contexts.Secretariat, enrolledUntil));
        return record;
    }
}
