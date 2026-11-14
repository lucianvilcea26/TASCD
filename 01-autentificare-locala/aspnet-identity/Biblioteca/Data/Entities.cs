using Microsoft.AspNetCore.Identity;

namespace Biblioteca.Data;

public static class Roles
{
    public const string Reader = "cititor";
    public const string Librarian = "bibliotecar";
}

// Contul unui utilizator al bibliotecii. IdentityUser aduce deja tot ce ține de autentificare:
// Email, EmailConfirmed, PasswordHash, SecurityStamp, AccessFailedCount, LockoutEnd și altele.
// Adăugăm doar ce este specific bibliotecii.
public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = "";
    public string CardNumber { get; set; } = "";   // identificatorul în contextul bibliotecii, de ex. B-0001
    public string? Matricol { get; set; }          // identificatorul în contextul secretariatului; lipsește la personal
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public List<Loan> Loans { get; set; } = [];
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public int Year { get; set; }
    public int Copies { get; set; }
    public List<Loan> Loans { get; set; } = [];
}

public class Loan
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public DateOnly Due { get; set; }
    public DateTime? ReturnedUtc { get; set; }
}
