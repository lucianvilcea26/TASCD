namespace Biblioteca.Models;

// Răspunsul secretariatului la verificarea adresei: se potrivește cu dosarul? Este studentul înmatriculat?
public record EmailCheckResult(bool Matches, bool Enrolled, string? Name);
