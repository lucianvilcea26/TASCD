using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;
using Universitate.Shared;

namespace Secretariat.Web.Api;

/// <summary>
/// The Secretariat API, used by Biblioteca at the lending desk. It answers from the registry
/// database, so the answer is always current (unlike a claim inside a token).
///
/// Callers must send a Keycloak access token that
///   * is addressed to this API (aud contains "secretariat-api"), and
///   * carries the client role secretariat-api / enrollment.read.
/// </summary>
public static class EnrollmentApi
{
    public const string Audience = "secretariat-api";
    public const string ReaderPolicy = "EnrollmentReader";

    public static void AddEnrollmentApiPolicy(this Microsoft.AspNetCore.Authorization.AuthorizationOptions options) =>
        options.AddPolicy(ReaderPolicy, policy => policy
            .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()
            .RequireAssertion(ctx => ctx.User.HasClientRole(Audience, "enrollment.read")));

    public static void MapEnrollmentApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/students/{studentId}/enrollment", async (string studentId, HttpContext http, SecretariatDb db) =>
            {
                var student = await db.Students.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.StudentId == studentId.ToUpperInvariant());

                if (student is null)
                    return Results.NotFound(new { studentId, error = "No such student in the registry." });

                return Results.Ok(new
                {
                    studentId = student.StudentId,
                    name = student.FullName,
                    faculty = student.Faculty,
                    status = student.Status.ToClaimValue(),
                    checkedAt = DateTime.UtcNow,
                    askedBy = http.User.FindFirst("azp")?.Value, // which client called us
                });
            })
            .RequireAuthorization(ReaderPolicy);
    }
}
