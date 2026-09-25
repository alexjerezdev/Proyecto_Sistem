using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CafeAroma.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuración limpia de la base de datos usando el tipo explícito de opciones
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();

var app = builder.Build();

// === PRUEBA DE CONEXIÓN RÁPIDA A POSTGRESQL (Segura contra errores de tipo) ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = ActivatorUtilities.CreateInstance<AppDbContext>(services);
        var canConnect = dbContext.Database.CanConnect();
        
        if (canConnect)
        {
            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine(" ¡CONEXIÓN EXITOSA A POSTGRESQL!");
            Console.WriteLine("--------------------------------------------------");
        }
        else
        {
            Console.WriteLine("❌ No se pudo conectar a la base de datos.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error al conectar a la base de datos: {ex.Message}");
    }
}
// ===========================================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}