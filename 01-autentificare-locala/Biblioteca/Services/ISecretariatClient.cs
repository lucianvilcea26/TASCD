using Biblioteca.Models;

namespace Biblioteca.Services;

public interface ISecretariatClient
{
    Task<EnrollmentStatus?> GetEnrollmentAsync(string matricol);
}
