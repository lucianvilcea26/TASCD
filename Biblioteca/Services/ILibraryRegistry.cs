using Biblioteca.Models;

namespace Biblioteca.Services;

public interface ILibraryRegistry
{
    IReadOnlyList<IdentityRecord> GetAll();
    IdentityRecord? FindByCard(string cardNumber);
}
