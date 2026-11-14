using Biblioteca.Accounts;
using Biblioteca.Data;
using Biblioteca.Models;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Biblioteca.Authorization;

public class CanBorrowHandler(BibliotecaDbContext db, ISecretariatClient secretariat)
    : AuthorizationHandler<CanBorrowRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanBorrowRequirement requirement)
    {
        // Regula 1: împrumută doar studenții înmatriculați, după răspunsul de acum al secretariatului.
        var matricol = context.User.FindFirst(UserClaims.MatricolClaim)?.Value;
        EnrollmentStatus? enrollment;
        try
        {
            enrollment = matricol is null ? null : await secretariat.GetEnrollmentAsync(matricol);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Eșec închis: dacă nu putem afla acum dacă studentul este înmatriculat, nu împrumutăm.
            return;
        }
        if (enrollment is not { Enrolled: true })
            return;

        // Regula 2: cel mult MaxLoans cărți împrumutate în același timp.
        var userId = context.User.Id();
        var active = await db.Loans.CountAsync(l => l.UserId == userId && l.ReturnedUtc == null);
        if (active >= requirement.MaxLoans)
            return;

        context.Succeed(requirement);
    }
}
