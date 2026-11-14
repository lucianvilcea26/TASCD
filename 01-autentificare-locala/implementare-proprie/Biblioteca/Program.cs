using Biblioteca.Accounts;
using Biblioteca.Authorization;
using Biblioteca.Data;
using Biblioteca.Email;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cărțile, împrumuturile și conturile stau într-o bază SQLite, administrată prin EF Core.
builder.Services.AddDbContext<BibliotecaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Biblioteca")));

// Conturile le gestionăm noi: AccountService conține toate regulile (parole, blocare, linkuri).
builder.Services.Configure<AccountOptions>(builder.Configuration.GetSection("Accounts"));
builder.Services.AddScoped<AccountService>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        // La fiecare cerere: cookie-ul este respins dacă parola s-a schimbat după emiterea lui.
        options.Events.OnValidatePrincipal = UserPrincipal.ValidateStampAsync;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanBorrow", policy => policy.AddRequirements(new CanBorrowRequirement(maxLoans: 3)));
});
builder.Services.AddScoped<IAuthorizationHandler, CanBorrowHandler>();

builder.Services.AddHttpClient<ISecretariatClient, HttpSecretariatClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Secretariat:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Secretariat:ApiKey"]);
});

var app = builder.Build();

await Seed.InitializeAsync(app.Services, app.Configuration);

// Jurnalul cererilor: vede fiecare cerere la intrare și răspunsul ei la ieșire.
app.Use(async (context, next) =>
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    await next(context);
    Console.WriteLine(
        $"{context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode} ({watch.ElapsedMilliseconds} ms)");
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
