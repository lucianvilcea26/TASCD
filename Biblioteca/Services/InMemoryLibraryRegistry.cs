using Biblioteca.Models;

namespace Biblioteca.Services;

public class InMemoryLibraryRegistry : ILibraryRegistry
{
    private readonly List<IdentityRecord> readers =
    [
        Reader("B-0137", "Ana Popescu", "cititor", "SIM-2026-0412"),
        Reader("B-0142", "Mihai Ionescu", "cititor", "SIM-2026-0388"),
        Reader("B-0099", "Elena Dumitru", "cititor", "SIM-2024-0107"),
        Reader("B-0001", "Ioana Marin", "bibliotecar", null),
    ];

    public IReadOnlyList<IdentityRecord> GetAll() => readers;

    public IdentityRecord? FindByCard(string cardNumber) =>
        readers.FirstOrDefault(r => r.IdentifierIn(Contexts.Biblioteca) == cardNumber);

    private static IdentityRecord Reader(string card, string name, string category, string? matricol)
    {
        var record = new IdentityRecord(Contexts.Biblioteca);
        record.Identifiers.Add(new Identifier(Contexts.Biblioteca, card));
        if (matricol is not null)
            record.Identifiers.Add(new Identifier(Contexts.Secretariat, matricol));
        record.Attributes.Add(new IdentityAttribute("nume", name, Contexts.Biblioteca));
        record.Attributes.Add(new IdentityAttribute("categorie", category, Contexts.Biblioteca));
        return record;
    }
}
