using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using CafeAroma.Api.Models;
using CafeAroma.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CafeAroma.Tests;

public class AuthControllerTests
{
    // Crea una base de datos en memoria (nueva y aislada por cada test) con un usuario de prueba.
    private AppDbContext CrearContextoConUsuario(string nombre, string passPlano, bool estado = true, string rol = "encargada")
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Usuarios.Add(new Usuario
        {
            Nombre = nombre,
            ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(passPlano),
            Estado = estado,
            Rol = rol
        });
        context.SaveChanges();
        return context;
    }

    // TokenService necesita IConfiguration con Jwt:Key y Jwt:Issuer; se arma en memoria solo para tests.
    private TokenService CrearTokenService()
    {
        var configValues = new Dictionary<string, string?>
        {
            { "Jwt:Key", "clave-super-secreta-solo-para-tests-1234567890" },
            { "Jwt:Issuer", "CafeAromaTests" }
        };

        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues)
            .Build();

        return new TokenService(config);
    }

    [Fact]
    public async Task Login_CredencialesCorrectas_Devuelve200ConToken()
    {
        var context = CrearContextoConUsuario("ana", "1234");
        var controller = new AuthController(context, CrearTokenService());

        var resultado = await controller.Login(new LoginRequestDto { Nombre = "ana", Contrasena = "1234" });

        var ok = Assert.IsType<OkObjectResult>(resultado);
        var body = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.False(string.IsNullOrEmpty(body.Token));
        Assert.Equal("ana", body.Nombre);
    }

    [Fact]
    public async Task Login_ContrasenaIncorrecta_Devuelve401()
    {
        var context = CrearContextoConUsuario("ana", "1234");
        var controller = new AuthController(context, CrearTokenService());

        var resultado = await controller.Login(new LoginRequestDto { Nombre = "ana", Contrasena = "wrong" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public async Task Login_UsuarioInexistente_Devuelve401()
    {
        var context = CrearContextoConUsuario("ana", "1234");
        var controller = new AuthController(context, CrearTokenService());

        var resultado = await controller.Login(new LoginRequestDto { Nombre = "no_existe", Contrasena = "1234" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }

    [Fact]
    public async Task Login_UsuarioInactivo_Devuelve401()
    {
        var context = CrearContextoConUsuario("ana", "1234", estado: false);
        var controller = new AuthController(context, CrearTokenService());

        var resultado = await controller.Login(new LoginRequestDto { Nombre = "ana", Contrasena = "1234" });

        Assert.IsType<UnauthorizedObjectResult>(resultado);
    }
}