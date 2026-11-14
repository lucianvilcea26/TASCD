using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Data;

// IdentityDbContext adaugă tabelele lui Identity: AspNetUsers, AspNetRoles, AspNetUserRoles,
// AspNetUserClaims, AspNetUserLogins, AspNetUserTokens și AspNetRoleClaims.
public class BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Loan> Loans => Set<Loan>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model); // modelul tabelelor lui Identity, cu indecșii lui unici pe nume și email normalizate

        model.Entity<ApplicationUser>().HasIndex(u => u.CardNumber).IsUnique();
        model.Entity<ApplicationUser>().HasIndex(u => u.Matricol).IsUnique().HasFilter("Matricol IS NOT NULL");

        model.Entity<Book>().HasData(
            new Book { Id = 1, Title = "Applied Cryptography", Author = "Bruce Schneier", Year = 1996, Copies = 2 },
            new Book { Id = 2, Title = "Security Engineering", Author = "Ross Anderson", Year = 2020, Copies = 3 },
            new Book { Id = 3, Title = "OAuth 2 in Action", Author = "Justin Richer, Antonio Sanso", Year = 2017, Copies = 4 },
            new Book { Id = 4, Title = "Solving Identity Management in Modern Applications", Author = "Yvonne Wilson, Abhishek Hingnikar", Year = 2019, Copies = 1 });
    }
}
