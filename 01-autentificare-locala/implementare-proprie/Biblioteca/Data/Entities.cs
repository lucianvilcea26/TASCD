namespace Biblioteca.Data;

public static class Roles
{
    public const string Reader = "cititor";
    public const string Librarian = "bibliotecar";
}

// Contul unui utilizator al bibliotecii. Tot ce ține de autentificare stă aici, în baza noastră.
public class User
{
    public int Id { get; set; }

    // Identificatorul de autentificare. Îl păstrăm cu litere mici, ca „Ana@..." și „ana@..." să fie același cont.
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }

    public string Name { get; set; } = "";
    public string CardNumber { get; set; } = "";   // identificatorul în contextul bibliotecii, de ex. B-0001
    public string? Matricol { get; set; }          // identificatorul în contextul secretariatului; lipsește la personal
    public string Role { get; set; } = Roles.Reader;

    // Niciodată parola: doar rezultatul funcției de derivare (PBKDF2), cu sarea și parametrii incluși.
    public string PasswordHash { get; set; } = "";

    // Se schimbă la fiecare schimbare a parolei; cookie-urile emise cu valoarea veche nu mai sunt acceptate.
    public string SecurityStamp { get; set; } = NewStamp();

    public int FailedLoginCount { get; set; }
    public DateTime? LockoutEndUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public List<Loan> Loans { get; set; } = [];

    public static string NewStamp() => Guid.NewGuid().ToString("N");
}

public static class TokenPurposes
{
    public const string ConfirmEmail = "confirmare-email";
    public const string ResetPassword = "resetare-parola";
}

// Un link trimis prin email. În bază stă doar amprenta SHA-256 a valorii din link.
public class UserToken
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public string Purpose { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresUtc { get; set; }
    public DateTime? UsedUtc { get; set; }
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
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateOnly Due { get; set; }
    public DateTime? ReturnedUtc { get; set; }
}
