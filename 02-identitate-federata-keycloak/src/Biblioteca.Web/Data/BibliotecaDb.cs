using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Web.Data;

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

    /// <summary>The library only knows students by their student ID; the registry lives in the Secretariat.</summary>
    public string StudentId { get; set; } = "";
    public string BorrowerName { get; set; } = "";

    public DateTime LoanedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }

    /// <summary>"self-service" (checked against a token claim) or "desk" (checked against the Secretariat API).</summary>
    public string Channel { get; set; } = "";

    /// <summary>How enrollment was verified, kept as an audit trail.</summary>
    public string EnrollmentEvidence { get; set; } = "";

    public bool IsActive => ReturnedAtUtc is null;
}

public class BibliotecaDb(DbContextOptions<BibliotecaDb> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Ion", Author = "Liviu Rebreanu", Year = 1920, Copies = 3 },
            new Book { Id = 2, Title = "Enigma Otiliei", Author = "George Călinescu", Year = 1938, Copies = 2 },
            new Book { Id = 3, Title = "Moromeții", Author = "Marin Preda", Year = 1955, Copies = 2 },
            new Book { Id = 4, Title = "Amintiri din copilărie", Author = "Ion Creangă", Year = 1892, Copies = 3 },
            new Book { Id = 5, Title = "Maitreyi", Author = "Mircea Eliade", Year = 1933, Copies = 1 },
            new Book { Id = 6, Title = "Structure and Interpretation of Computer Programs", Author = "H. Abelson, G. J. Sussman", Year = 1985, Copies = 2 },
            new Book { Id = 7, Title = "The C Programming Language", Author = "B. W. Kernighan, D. M. Ritchie", Year = 1978, Copies = 2 },
            new Book { Id = 8, Title = "Design Patterns", Author = "E. Gamma, R. Helm, R. Johnson, J. Vlissides", Year = 1994, Copies = 1 },
            new Book { Id = 9, Title = "Introduction to Algorithms", Author = "T. H. Cormen et al.", Year = 1990, Copies = 2 },
            new Book { Id = 10, Title = "Security Engineering", Author = "Ross Anderson", Year = 2001, Copies = 1 });
    }
}
