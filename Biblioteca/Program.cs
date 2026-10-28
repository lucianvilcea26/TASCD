using Biblioteca.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Biblioteca.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IBookCatalog, InMemoryBookCatalog>();
builder.Services.AddSingleton<ILibraryRegistry, InMemoryLibraryRegistry>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CanBorrow", policy => policy.AddRequirements(new CanBorrowRequirement(maxLoans: 3)));
});
builder.Services.AddSingleton<IAuthorizationHandler, CanBorrowHandler>();

builder.Services.AddHttpClient<ISecretariatClient, HttpSecretariatClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Secretariat:BaseUrl"]!);
    client.DefaultRequestHeaders.Add("X-Api-Key", builder.Configuration["Secretariat:ApiKey"]);
});

var app = builder.Build();

// Jurnalul cererilor: vede fiecare cerere la intrare și răspunsul ei la ieșire.
app.Use(async (context, next) =>
{
    var watch = System.Diagnostics.Stopwatch.StartNew();
    await next(context);
    Console.WriteLine(
        $"{context.Request.Method} {context.Request.Path} -> {context.Response.StatusCode} ({watch.ElapsedMilliseconds} ms)");
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
