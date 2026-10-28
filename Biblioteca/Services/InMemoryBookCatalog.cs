using Biblioteca.Models;

namespace Biblioteca.Services;

public class InMemoryBookCatalog : IBookCatalog
{
    private readonly List<Book> books =
    [
        new(1, "Applied Cryptography", "Bruce Schneier", 1996, 2),
        new(2, "Security Engineering", "Ross Anderson", 2020, 3),
        new(3, "OAuth 2 in Action", "Justin Richer, Antonio Sanso", 2017, 4),
        new(4, "Solving Identity Management in Modern Applications", "Yvonne Wilson, Abhishek Hingnikar", 2019, 1),
    ];

    public InMemoryBookCatalog()
    {
        Console.WriteLine("InMemoryBookCatalog: instanță nouă");
    }

    public IReadOnlyList<Book> GetAll() => books;
}
