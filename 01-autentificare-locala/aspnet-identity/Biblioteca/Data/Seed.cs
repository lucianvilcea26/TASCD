using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Data;

public static class Seed
{
    // La pornire: aplică migrările, creează cele două roluri și contul bibliotecarului, dacă lipsesc.
    // Studenții nu sunt creați aici: își fac singuri cont, iar secretariatul confirmă cine sunt.
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>().Database.MigrateAsync();

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { Roles.Reader, Roles.Librarian })
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var email = configuration["Seed:LibrarianEmail"]!;
        if (await users.FindByEmailAsync(email) is null)
        {
            var librarian = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Name = configuration["Seed:LibrarianName"]!,
                CardNumber = "B-0001",
            };
            var created = await users.CreateAsync(librarian, configuration["Seed:LibrarianPassword"]!);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(librarian, Roles.Librarian);
        }
    }
}
