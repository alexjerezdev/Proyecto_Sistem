using Microsoft.EntityFrameworkCore;
using luisfrontend.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar la conexión a PostgreSQL usando DefaultConnection de appsettings.json
builder.Services.AddDbContext<CafeDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// 2. CAMBIO AQUÍ: Se cambió "Home" por "Ventas" para que abra directamente el sistema de ventas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Ventas}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();