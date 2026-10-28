using Biblioteca.Models;

namespace Biblioteca.Services;

public interface ISecretariatRegistry
{
    IReadOnlyList<IdentityRecord> GetAll();
    IdentityRecord? FindByMatricol(string matricol);
}
