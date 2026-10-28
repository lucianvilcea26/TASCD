using Microsoft.AspNetCore.Authorization;

namespace Biblioteca.Authorization;

public class CanBorrowHandler : AuthorizationHandler<CanBorrowRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, CanBorrowRequirement requirement)
    {
        // Regula 1: împrumută doar studenții, adică cititorii cu număr matricol.
        if (!context.User.HasClaim(c => c.Type == "matricol"))
            return Task.CompletedTask;

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
