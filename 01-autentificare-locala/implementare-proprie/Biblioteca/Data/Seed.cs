using Biblioteca.Accounts;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Data;

public static class Seed
{
    // La pornire: aplică migrările și creează contul bibliotecarului, dacă lipsește.
    // Studenții nu sunt creați aici: își fac singuri cont, iar secretariatul confirmă cine sunt.
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        await db.Database.MigrateAsync();

        var accounts = scope.ServiceProvider.GetRequiredService<AccountService>();
        var email = configuration["Seed:LibrarianEmail"]!;
        if (await accounts.FindByEmailAsync(email) is null)
            await accounts.CreateStaffAsync(email, configuration["Seed:LibrarianName"]!,
                configuration["Seed:LibrarianPassword"]!, Roles.Librarian);
    }
}
