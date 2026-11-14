using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Data;

public class BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserToken> UserTokens => Set<UserToken>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        // Unicitatea o garantează baza de date, nu doar codul: două înregistrări simultane nu pot crea același cont.
        model.Entity<User>().HasIndex(u => u.Email).IsUnique();
        model.Entity<User>().HasIndex(u => u.CardNumber).IsUnique();
        model.Entity<User>().HasIndex(u => u.Matricol).IsUnique().HasFilter("Matricol IS NOT NULL");

        model.Entity<UserToken>().HasIndex(t => new { t.UserId, t.Purpose });

        model.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Applied Cryptography", Author = "Bruce Schneier", Year = 1996, Copies = 2 },
            new Book { Id = 2, Title = "Security Engineering", Author = "Ross Anderson", Year = 2020, Copies = 3 },
            new Book { Id = 3, Title = "OAuth 2 in Action", Author = "Justin Richer, Antonio Sanso", Year = 2017, Copies = 4 },
            new Book { Id = 4, Title = "Solving Identity Management in Modern Applications", Author = "Yvonne Wilson, Abhishek Hingnikar", Year = 2019, Copies = 1 });
    }
}
