using Microsoft.EntityFrameworkCore;
using Secretariat.Web.Api;
using Secretariat.Web.Data;
using Secretariat.Web.Keycloak;
using Universitate.Shared;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddDbContext<SecretariatDb>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Secretariat")));

// 1) Users log in through Keycloak (cookie + OpenID Connect).
// 2) The /api endpoints accept Keycloak access tokens from other services (JWT bearer).
var keycloak = builder.Configuration.GetSection(KeycloakOptions.Section).Get<KeycloakOptions>()!;
builder.Services.AddKeycloakLogin(builder.Configuration)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloak.Authority;
        options.Audience = EnrollmentApi.Audience;
        options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
        options.MapInboundClaims = false;
        // The default tolerance for clock differences is 5 minutes, longer than the whole
        // lifetime of a token in this realm (120 s). 30 s is enough for clocks on one machine.
        options.TokenValidationParameters.ClockSkew = TimeSpan.FromSeconds(30);
    });
builder.Services.AddAuthorization(options => options.AddEnrollmentApiPolicy());

// 3) The app manages Keycloak users through the Admin REST API, as its own service account.
builder.Services.Configure<KeycloakAdminOptions>(builder.Configuration.GetSection("KeycloakAdmin"));
builder.Services
    .AddHttpClient<KeycloakAdminClient>(c => c.BaseAddress = new Uri(builder.Configuration["KeycloakAdmin:BaseUrl"]!))
    .AddServiceAccountToken();
builder.Services.AddScoped<StudentProvisioning>();
// Keeps every operation's Admin API calls per student (the history panel on the student page).
builder.Services.AddProvisioningHistory(builder.Configuration.GetConnectionString("Secretariat")!);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<SecretariatDb>().Database.EnsureCreated();

if (!app.Environment.IsDevelopment())
    app.UseExceptionHandler("/Error");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapAccountEndpoints();
app.MapEnrollmentApi();

app.Run();
