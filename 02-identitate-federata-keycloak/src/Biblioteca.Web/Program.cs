using Biblioteca.Web.Data;
using Biblioteca.Web.Services;
using Microsoft.EntityFrameworkCore;
using Universitate.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<BibliotecaDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Biblioteca")));

// Users log in through Keycloak; the ID token carries roles, student_id and enrollment_status.
builder.Services.AddKeycloakLogin(builder.Configuration);

// Server-to-server: Biblioteca → Secretariat API, authenticated as Biblioteca's service account.
builder.Services
    .AddHttpClient<SecretariatApiClient>(c => c.BaseAddress = new Uri(builder.Configuration["SecretariatApi:BaseUrl"]!))
    .AddServiceAccountToken();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<BibliotecaDb>().Database.EnsureCreated();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapAccountEndpoints();

app.Run();
