using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using CafeAroma.Api.Services;

var builder = WebApplication.CreateBuilder(args);

<<<<<<< HEAD
// === TODO builder.Services.Add... va ANTES de Build() ===

=======
// Configuración de la base de datos PostgreSQL
>>>>>>> origin/feature/backend-bd
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddControllers();

builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

// === Línea divisoria ===
var app = builder.Build();

<<<<<<< HEAD
// === TODO app.Use... y app.Map... va DESPUÉS de Build() ===

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// === PRUEBA DE CONEXIÓN RÁPIDA A POSTGRESQL ===
=======
// === PRUEBA DE CONEXIÓN CON DETALLE DE ERROR ===
>>>>>>> origin/feature/backend-bd
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
<<<<<<< HEAD
        var dbContext = services.GetRequiredService<AppDbContext>();
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
=======
        var dbContext = ActivatorUtilities.CreateInstance<AppDbContext>(services);
        
        // Intentar abrir la conexión explícitamente para capturar el error exacto si lo hay
        dbContext.Database.OpenConnection();
        dbContext.Database.CloseConnection();

        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(" ¡CONEXIÓN EXITOSA A POSTGRESQL!");
        Console.WriteLine("--------------------------------------------------");
>>>>>>> origin/feature/backend-bd
    }
    catch (Exception ex)
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine($"❌ ERROR DE CONEXIÓN DETALLADO: {ex.Message}");
        Console.WriteLine("--------------------------------------------------");
    }
}
// ===============================================

<<<<<<< HEAD
=======
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

>>>>>>> origin/feature/backend-bd
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