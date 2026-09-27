using CafeAroma.Api.Controllers;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using CafeAroma.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class UsuariosControllerTests
{
    private AppDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CrearUsuario_DatosValidos_Devuelve201YQuedaActivo()
    {
        var context = CrearContexto();
        var controller = new UsuariosController(context);

        var resultado = await controller.CrearUsuario(new CrearUsuarioDto
        {
            Nombre = "nueva_encargada",
            Contrasena = "clave123",
            Rol = "encargada"
        });

        var creado = Assert.IsType<CreatedAtActionResult>(resultado.Result);
        var body = Assert.IsType<UsuarioResponseDto>(creado.Value);
        Assert.Equal("nueva_encargada", body.Nombre);
        Assert.True(body.Estado); // debe quedar disponible para iniciar sesión

        // Confirmamos que quedó guardado en la BD con contraseña hasheada
        var enBd = await context.Usuarios.FirstAsync(u => u.Nombre == "nueva_encargada");
        Assert.True(BCrypt.Net.BCrypt.Verify("clave123", enBd.ContrasenaHash));
    }

    [Fact]
    public async Task CrearUsuario_NombreDuplicado_Devuelve409()
    {
        var context = CrearContexto();
        context.Usuarios.Add(new Usuario { Nombre = "ana", ContrasenaHash = "x", Rol = "encargada", Estado = true });
        await context.SaveChangesAsync();

        var controller = new UsuariosController(context);

        var resultado = await controller.CrearUsuario(new CrearUsuarioDto
        {
            Nombre = "ana",
            Contrasena = "otra123",
            Rol = "encargada"
        });

        Assert.IsType<ConflictObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task CrearUsuario_SinContrasena_Devuelve400()
    {
        var context = CrearContexto();
        var controller = new UsuariosController(context);

        var resultado = await controller.CrearUsuario(new CrearUsuarioDto
        {
            Nombre = "nuevo",
            Contrasena = "",
            Rol = "encargada"
        });

        Assert.IsType<BadRequestObjectResult>(resultado.Result);
    }

    [Fact]
    public async Task CambiarEstado_DesactivarUsuario_QuedaNoDisponible()
    {
        var context = CrearContexto();
        context.Usuarios.Add(new Usuario { UsuarioId = 1, Nombre = "ana", ContrasenaHash = "x", Rol = "encargada", Estado = true });
        await context.SaveChangesAsync();

        var controller = new UsuariosController(context);

        var resultado = await controller.CambiarEstado(1, false);

        Assert.IsType<NoContentResult>(resultado);
        var actualizado = await context.Usuarios.FindAsync(1);
        Assert.False(actualizado!.Estado); // ya no puede iniciar sesión (validado en AuthController.Login)
    }

    [Fact]
    public async Task GetUsuario_Inexistente_Devuelve404()
    {
        var context = CrearContexto();
        var controller = new UsuariosController(context);

        var resultado = await controller.GetUsuario(999);

        Assert.IsType<NotFoundResult>(resultado.Result);
    }
}