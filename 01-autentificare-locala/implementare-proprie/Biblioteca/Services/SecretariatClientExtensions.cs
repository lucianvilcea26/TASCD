using Biblioteca.Models;

namespace Biblioteca.Services;

public static class SecretariatClientExtensions
{
    // Pentru afirmația adăugată la autentificare: este utilă, dar nu obligatorie. Dacă secretariatul nu răspunde,
    // sesiunea se deschide fără ea. Decizia de împrumut nu se bazează pe această afirmație, ci întreabă din nou.
    public static async Task<EnrollmentStatus?> TryGetEnrollmentAsync(this ISecretariatClient secretariat, string? matricol, ILogger logger)
    {
        if (matricol is null)
            return null;
        try
        {
            return await secretariat.GetEnrollmentAsync(matricol);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Secretariatul nu a răspuns pentru {Matricol}", matricol);
            return null;
        }
    }
}
