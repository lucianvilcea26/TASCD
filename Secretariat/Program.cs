using System.Security.Cryptography;
using System.Text;
using Secretariat.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<StudentRegistry>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

var apiKey = app.Configuration["ApiKey"]
    ?? throw new InvalidOperationException("Lipsește ApiKey. Rulează: dotnet user-secrets set ApiKey <cheie> --project Secretariat");

// Numai clienții care prezintă cheia pot întreba secretariatul.
app.Use(async (context, next) =>
{
    var presented = context.Request.Headers["X-Api-Key"].ToString();
    if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presented), Encoding.UTF8.GetBytes(apiKey)))
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }
    await next(context);
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
