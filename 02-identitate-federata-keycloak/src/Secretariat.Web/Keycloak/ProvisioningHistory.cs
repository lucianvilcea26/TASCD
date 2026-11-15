using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Data;

namespace Secretariat.Web.Keycloak;

/// <summary>One operation of the Secretariat on a student's Keycloak account, with the Admin API calls it made.</summary>
public sealed class ProvisioningEntry
{
    public int Id { get; set; }
    public int StudentId { get; set; }          // Student.Id in the registry
    public DateTime AtUtc { get; set; }
    public string Summary { get; set; } = "";
    public bool Ok { get; set; }
    public string Calls { get; set; } = "";      // one call per line, as KeycloakAdminClient logs them

    public IReadOnlyList<ApiCall> ParsedCalls =>
        Calls.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(ApiCall.Parse).ToList();
}

/// <summary>"PUT /admin/realms/universitate/users/{id}/logout → 204 No Content", taken apart.</summary>
public sealed record ApiCall(string Method, string Path, int Status, string Reason)
{
    private static readonly Regex Line = new(@"^(?<m>[A-Z]+) (?<p>\S+) → (?<s>\d{3}) ?(?<r>.*)$");

    public bool Ok => Status is >= 200 and < 300;

    public static ApiCall Parse(string line)
    {
        var m = Line.Match(line.Trim());
        return m.Success
            ? new(m.Groups["m"].Value, m.Groups["p"].Value, int.Parse(m.Groups["s"].Value), m.Groups["r"].Value)
            : new("", line.Trim(), 0, "");
    }
}

/// <summary>
/// The history lives in its own table of the Secretariat database. A separate DbContext keeps the
/// registry model (SecretariatDb) as it is, and CREATE TABLE IF NOT EXISTS adds the table to
/// databases created before this feature existed (EnsureCreated does not add tables).
/// </summary>
public sealed class HistoryDb(DbContextOptions<HistoryDb> options) : DbContext(options)
{
    public DbSet<ProvisioningEntry> Entries => Set<ProvisioningEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entry = modelBuilder.Entity<ProvisioningEntry>();
        entry.ToTable("ProvisioningHistory").HasIndex(e => e.StudentId);
        entry.Ignore(e => e.ParsedCalls);
    }
}

public sealed class ProvisioningHistory(HistoryDb db)
{
    private static volatile bool _tableReady;
    private static readonly Regex UserId = new(@"/users/(?<id>[0-9a-f-]{36})");

    private async Task EnsureTableAsync(CancellationToken ct)
    {
        if (_tableReady) return;
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ProvisioningHistory" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ProvisioningHistory" PRIMARY KEY AUTOINCREMENT,
                "StudentId" INTEGER NOT NULL,
                "AtUtc" TEXT NOT NULL,
                "Summary" TEXT NOT NULL,
                "Ok" INTEGER NOT NULL,
                "Calls" TEXT NOT NULL);
            CREATE INDEX IF NOT EXISTS "IX_ProvisioningHistory_StudentId" ON "ProvisioningHistory" ("StudentId");
            """, ct);
        _tableReady = true;
    }

    public async Task<List<ProvisioningEntry>> ForStudentAsync(int studentId, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        return await db.Entries.AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .OrderByDescending(e => e.Id)
            .Take(30)
            .ToListAsync(ct);
    }

    public async Task AddAsync(int studentId, IReadOnlyList<string> calls, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        var parsed = calls.Select(ApiCall.Parse).ToList();
        db.Entries.Add(new ProvisioningEntry
        {
            StudentId = studentId,
            AtUtc = DateTime.UtcNow,
            Summary = Summarize(parsed),
            Ok = parsed.All(c => c.Ok),
            Calls = string.Join("\n", calls),
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteForStudentAsync(int studentId, CancellationToken ct = default)
    {
        await EnsureTableAsync(ct);
        await db.Entries.Where(e => e.StudentId == studentId).ExecuteDeleteAsync(ct);
    }

    /// <summary>A short name for an operation, from the calls it made.</summary>
    public static string Summarize(IReadOnlyList<ApiCall> calls)
    {
        var parts = new List<string>();
        if (calls.Any(c => c.Method == "POST" && c.Path.EndsWith("/users"))) parts.Add("account created");
        if (calls.Any(c => c.Method == "PUT" && c.Path.Contains("/groups/"))) parts.Add("added to studenti");
        if (calls.Any(c => c.Method == "PUT" && Regex.IsMatch(c.Path, @"/users/[0-9a-f-]{36}$"))) parts.Add("registry data published");
        if (calls.Any(c => c.Path.Contains("/execute-actions-email"))) parts.Add("invitation sent");
        if (calls.Any(c => c.Method == "POST" && c.Path.EndsWith("/logout"))) parts.Add("sessions ended");
        var text = parts.Count == 0 ? "Admin API calls" : string.Join(", ", parts);
        return char.ToUpperInvariant(text[0]) + text[1..];
    }

    /// <summary>
    /// Splits one request's calls by student. A create starts with POST /users (no id yet); the calls
    /// after it carry the new user's id. Returns (Keycloak user id or null, calls) per operation.
    /// </summary>
    public static List<(string? UserId, List<string> Calls)> SplitByUser(IReadOnlyList<string> calls)
    {
        var groups = new List<(string? UserId, List<string> Calls)>();
        foreach (var call in calls)
        {
            var id = UserId.Match(call) is { Success: true } m ? m.Groups["id"].Value : null;
            var isCreate = call.StartsWith("POST ") && call.Contains("/users →");
            var last = groups.Count > 0 ? groups[^1] : default;
            var startsNew = groups.Count == 0 || isCreate || (id is not null && last.UserId is not null && last.UserId != id);
            if (startsNew)
                groups.Add((id, [call]));
            else
            {
                last.Calls.Add(call);
                if (last.UserId is null && id is not null) groups[^1] = (id, last.Calls);
            }
        }
        return groups;
    }
}

/// <summary>
/// After every Secretariat page handler, stores the Admin API calls it made in the student's history.
/// The pages and StudentProvisioning stay unchanged: the filter reads the same call log that the
/// "calls just made" panel shows.
/// </summary>
public sealed class ProvisioningHistoryFilter(
    StudentProvisioning provisioning, ProvisioningHistory history, SecretariatDb db, ILogger<ProvisioningHistoryFilter> logger)
    : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var before = provisioning.Calls.Count;
        var executed = await next();
        var calls = provisioning.Calls.Skip(before).ToList();
        if (calls.Count == 0) return;

        // The history is a teaching aid: if it cannot be stored, the operation itself must still succeed.
        try { await RecordAsync(context, executed, calls); }
        catch (Exception ex) { logger.LogWarning(ex, "Could not store the provisioning history."); }
    }

    private async Task RecordAsync(PageHandlerExecutingContext context, PageHandlerExecutedContext executed, List<string> calls)
    {
        var ct = context.HttpContext.RequestAborted;
        var pageStudent = RouteId(executed.Result) ?? RouteId(context.RouteData.Values["id"]);

        if (calls.Any(c => c.StartsWith("DELETE ")) && pageStudent is int deleted)
        {
            await history.DeleteForStudentAsync(deleted, ct);
            return;
        }

        foreach (var (userId, group) in ProvisioningHistory.SplitByUser(calls))
        {
            var studentId = userId is null
                ? pageStudent
                : await db.Students.AsNoTracking().Where(s => s.KeycloakUserId == userId).Select(s => (int?)s.Id).FirstOrDefaultAsync(ct)
                  ?? pageStudent;
            if (studentId is int id)
                await history.AddAsync(id, group, ct);
        }
    }

    private static int? RouteId(object? value) => value switch
    {
        RedirectToPageResult r when r.RouteValues?.TryGetValue("id", out var v) == true => RouteId(v),
        int i => i,
        string s when int.TryParse(s, out var i) => i,
        _ => null,
    };
}

public static class ProvisioningHistoryExtensions
{
    public static IServiceCollection AddProvisioningHistory(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<HistoryDb>(o => o.UseSqlite(connectionString));
        services.AddScoped<ProvisioningHistory>();
        services.Configure<MvcOptions>(o => o.Filters.Add<ProvisioningHistoryFilter>());
        return services;
    }
}
