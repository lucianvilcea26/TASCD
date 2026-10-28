namespace Biblioteca.Models;

// Reprezentarea identității: ce știe un context despre o entitate.
public class IdentityRecord(string context)
{
    public string Context { get; } = context;
    public List<Identifier> Identifiers { get; } = [];
    public List<IdentityAttribute> Attributes { get; } = [];

    public string? IdentifierIn(string context) =>
        Identifiers.FirstOrDefault(i => i.Context == context)?.Value;

    public string? Attribute(string name) =>
        Attributes.FirstOrDefault(a => a.Name == name)?.Value;
}
