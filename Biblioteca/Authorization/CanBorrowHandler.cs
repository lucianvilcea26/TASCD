using System.Security.Claims;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authorization;

namespace Biblioteca.Authorization;

public class CanBorrowHandler(IBookCatalog catalog, ISecretariatClient secretariat)
    : AuthorizationHandler<CanBorrowRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanBorrowRequirement requirement)
    {
        // Regula 1: împrumută doar studenții înmatriculați, după răspunsul de acum al secretariatului.
        var matricol = context.User.FindFirstValue("matricol");
        var enrollment = matricol is null ? null : await secretariat.GetEnrollmentAsync(matricol);
        if (enrollment is not { Enrolled: true })
            return;

        // Regula 2: cel mult MaxLoans cărți împrumutate în același timp.
        var card = context.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (catalog.LoansOf(card).Count >= requirement.MaxLoans)
            return;

        context.Succeed(requirement);
    }
}
