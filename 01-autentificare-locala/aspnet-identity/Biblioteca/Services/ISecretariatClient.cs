using Biblioteca.Models;

namespace Biblioteca.Services;

public interface ISecretariatClient
{
    Task<EnrollmentStatus?> GetEnrollmentAsync(string matricol);

    // La crearea contului: este această adresă cea din dosarul studentului? Este el înmatriculat?
    Task<EmailCheckResult> CheckEmailAsync(string matricol, string email);
}
