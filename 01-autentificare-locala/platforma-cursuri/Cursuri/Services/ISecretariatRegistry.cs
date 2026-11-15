using Cursuri.Models;

namespace Cursuri.Services;

public interface ISecretariatRegistry
{
    IReadOnlyList<IdentityRecord> GetAll();
    IdentityRecord? FindByMatricol(string matricol);
}
