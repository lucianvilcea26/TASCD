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

    private readonly List<Loan> loans = [];
    private readonly Lock gate = new();

    public InMemoryBookCatalog()
    {
        Console.WriteLine("InMemoryBookCatalog: instanță nouă");
    }

    public IReadOnlyList<Book> GetAll() => books;

    public Book? Find(int id) => books.FirstOrDefault(b => b.Id == id);

    public int Available(Book book)
    {
        lock (gate)
            return book.Copies - loans.Count(l => l.BookId == book.Id);
    }

    public IReadOnlyList<Loan> LoansOf(string cardNumber)
    {
        lock (gate)
            return loans.Where(l => l.CardNumber == cardNumber).ToList();
    }

    public bool Borrow(Book book, string cardNumber)
    {
        // Verificarea și adăugarea se fac împreună, ca două cereri simultane să nu ia ultimul exemplar.
        lock (gate)
        {
            if (book.Copies - loans.Count(l => l.BookId == book.Id) == 0)
                return false;

            loans.Add(new Loan(book.Id, cardNumber, DateOnly.FromDateTime(DateTime.Today).AddDays(14)));
            return true;
        }
    }
}
