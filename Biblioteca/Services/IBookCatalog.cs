using Biblioteca.Models;

namespace Biblioteca.Services;

public interface IBookCatalog
{
    IReadOnlyList<Book> GetAll();
}
