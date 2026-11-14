namespace Biblioteca.Models;

public record Loan(int BookId, string CardNumber, DateOnly Due);
