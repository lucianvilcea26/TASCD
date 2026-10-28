using System.Security.Claims;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authorization;

namespace Biblioteca.Authorization;

public class CanBorrowHandler(IBookCatalog catalog) : AuthorizationHandler<CanBorrowRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanBorrowRequirement requirement)
    {
        // Regula 1: împrumută doar studenții, adică cititorii cu număr matricol.
        if (!context.User.HasClaim(c => c.Type == "matricol"))
            return Task.CompletedTask;

        // Regula 2: cel mult MaxLoans cărți împrumutate în același timp.
        var card = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (catalog.LoansOf(card).Count >= requirement.MaxLoans)
            return Task.CompletedTask;

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
