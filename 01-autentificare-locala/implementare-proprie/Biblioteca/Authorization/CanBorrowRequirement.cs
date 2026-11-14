using Microsoft.AspNetCore.Authorization;

namespace Biblioteca.Authorization;

// Cerința: cine are voie să împrumute. Regulile stau în CanBorrowHandler.
public class CanBorrowRequirement(int maxLoans) : IAuthorizationRequirement
{
    public int MaxLoans { get; } = maxLoans;
}
