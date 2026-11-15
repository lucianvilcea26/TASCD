using Cursuri.Models;

namespace Cursuri.Services;

public interface IPlatformRegistry
{
    IReadOnlyList<IdentityRecord> GetAll();
    IdentityRecord? FindByUsername(string username);
    bool VerifyPassword(IdentityRecord account, string password);
}
