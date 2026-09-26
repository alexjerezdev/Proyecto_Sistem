using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CafeAroma.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// === PRUEBA DE CONEXIÓN CON DETALLE DE ERROR ===
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = ActivatorUtilities.CreateInstance<AppDbContext>(services);
        
        // Intentar abrir la conexión explícitamente para capturar el error exacto si lo hay
        dbContext.Database.OpenConnection();
        dbContext.Database.CloseConnection();

        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(" ¡CONEXIÓN EXITOSA A POSTGRESQL!");
        Console.WriteLine("--------------------------------------------------");
    }
    catch (Exception ex)
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($"❌ ERROR DE CONEXIÓN DETALLADO: {ex.Message}");
        Console.WriteLine("--------------------------------------------------");
    }
}
// ===============================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

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