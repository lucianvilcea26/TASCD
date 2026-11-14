using System.Security.Claims;
using Biblioteca.Accounts;
using Biblioteca.Authorization;
using Biblioteca.Data;
using Biblioteca.Email;
using Biblioteca.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Cărțile, împrumuturile și tabelele lui Identity stau într-o bază SQLite, administrată prin EF Core.
builder.Services.AddDbContext<BibliotecaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Biblioteca")));

// ASP.NET Core Identity: aceleași reguli ca în varianta proprie, dar exprimate ca opțiuni.
var accounts = builder.Configuration.GetSection("Accounts");
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;

        // Lungimea contează mai mult decât amestecul de caractere: oprim regulile de compoziție implicite.
        options.Password.RequiredLength = accounts.GetValue<int>("MinPasswordLength");
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = accounts.GetValue<int>("MaxFailedLogins");
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(accounts.GetValue<int>("LockoutMinutes"));
    })
    .AddEntityFrameworkStores<BibliotecaDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<RomanianIdentityErrorDescriber>()
    .AddPasswordValidator<EmailNotInPasswordValidator>()
    .AddClaimsPrincipalFactory<BibliotecaClaimsFactory>();

// Linkurile de confirmare și de resetare expiră după o oră (implicit: o zi).
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromMinutes(accounts.GetValue<int>("TokenLifetimeMinutes")));

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // Implicit, Identity verifică ștampila la 30 de minute. În laborator o verificăm la fiecare cerere,
    // ca închiderea celorlalte sesiuni după o schimbare de parolă să se vadă imediat.
    options.ValidationInterval = TimeSpan.Zero;

    // La reverificare, Identity reconstruiește identitatea din bază; afirmația secretariatului, primită
    // la autentificare, nu este acolo, așa că o copiem din identitatea veche.
    options.OnRefreshingPrincipal = context =>
    {
        var enrollment = context.CurrentPrincipal?.FindFirst(UserClaims.EnrollmentClaim);
        if (enrollment is not null && context.NewPrincipal?.Identity is ClaimsIdentity identity)
            identity.AddClaim(new Claim(enrollment.Type, enrollment.Value, enrollment.ValueType, enrollment.Issuer));
        return Task.CompletedTask;
    };
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();

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
