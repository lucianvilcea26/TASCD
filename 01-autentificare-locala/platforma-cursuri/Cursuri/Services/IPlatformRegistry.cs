using Cursuri.Models;

namespace Cursuri.Services;

public interface IPlatformRegistry
{
    IReadOnlyList<IdentityRecord> GetAll();
    IdentityRecord? FindByUsername(string username);
    bool VerifyPassword(IdentityRecord account, string password);
    IdentityRecord? Register(string username, string fullName, string? matricol, string password);
    bool SetRole(string username, string role);
    string? ResetPassword(string username);
    bool Delete(string username);
}
