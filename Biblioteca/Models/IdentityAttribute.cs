namespace Biblioteca.Models;

// O proprietate a entității, cu sursa ei (Issuer) și, opțional, cu o dată până la care e valabilă.
public record IdentityAttribute(string Name, string Value, string Issuer, DateOnly? ValidUntil = null)
{
    public bool IsValidOn(DateOnly day) => ValidUntil is null || day <= ValidUntil;
}
