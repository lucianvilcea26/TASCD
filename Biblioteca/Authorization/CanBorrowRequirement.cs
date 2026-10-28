using Microsoft.AspNetCore.Authorization;

namespace Biblioteca.Authorization;

// Cerința: cine are voie să împrumute. Regulile stau în CanBorrowHandler.
public class CanBorrowRequirement : IAuthorizationRequirement
{
}
