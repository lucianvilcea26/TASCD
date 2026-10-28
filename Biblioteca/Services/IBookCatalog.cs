using Biblioteca.Models;

namespace Biblioteca.Services;

public interface IBookCatalog
{
    IReadOnlyList<Book> GetAll();
    Book? Find(int id);
    int Available(Book book);
    IReadOnlyList<Loan> LoansOf(string cardNumber);
    bool Borrow(Book book, string cardNumber);
}
