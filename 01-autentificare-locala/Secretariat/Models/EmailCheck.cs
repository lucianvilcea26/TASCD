namespace Secretariat.Models;

// Biblioteca întreabă: această adresă de email este cea din dosarul studentului cu acest matricol?
public record EmailCheckRequest(string Email);

// Răspunsul spune doar dacă adresa se potrivește și dacă studentul este înmatriculat.
// Numele apare numai când adresa se potrivește: cine nu știe deja adresa din dosar nu află nimic.
public record EmailCheckResponse(bool Matches, bool Enrolled, string? Name);
