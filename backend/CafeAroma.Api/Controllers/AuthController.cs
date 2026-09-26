using BCrypt.Net;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using CafeAroma.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeAroma.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        var usuario = await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Nombre == dto.Nombre);

        // mensaje genérico: no revela si falló el usuario o la contraseña
        if (usuario is null || !usuario.Estado)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

        bool valido = BCrypt.Net.BCrypt.Verify(dto.Contrasena, usuario.ContrasenaHash);
        if (!valido)
            return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });

        var token = _tokenService.GenerarToken(usuario);

        return Ok(new LoginResponseDto
        {
            Token = token,
            Rol = usuario.Rol,
            Nombre = usuario.Nombre
        });
    }
}