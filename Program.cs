using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using luisfrontend.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración del DbContext con PostgreSQL
builder.Services.AddDbContext<luisfrontend.Models.Data.CafeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Agregar servicios de controladores con vistas
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configurar el pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

// Ruta principal apuntando a Ventas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Ventas}/{action=Index}/{id?}");

app.Run();