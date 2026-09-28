using BCrypt.Net;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using CafeAroma.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeAroma.Api.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "dueno")] // Solo el dueño autenticado puede gestionar usuarios
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    // POST: api/usuarios
    // HU-02, criterio 1: el dueño crea un usuario nuevo y queda disponible para iniciar sesión
    [HttpPost]
    public async Task<ActionResult<UsuarioResponseDto>> CrearUsuario(CrearUsuarioDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre) || string.IsNullOrWhiteSpace(dto.Contrasena))
            return BadRequest(new { mensaje = "Nombre y contraseña son obligatorios." });

        var existe = await _context.Usuarios.AnyAsync(u => u.Nombre == dto.Nombre);
        if (existe)
            return Conflict(new { mensaje = "Ya existe un usuario con ese nombre." });

        var usuario = new Usuario
        {
            Nombre = dto.Nombre,
            Rol = dto.Rol,
            ContrasenaHash = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena),
            Estado = true // queda activo y disponible para login de inmediato
        };

        _context.Usuarios.Add(usuario);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUsuario), new { id = usuario.UsuarioId }, new UsuarioResponseDto
        {
            UsuarioId = usuario.UsuarioId,
            Nombre = usuario.Nombre,
            Rol = usuario.Rol,
            Estado = usuario.Estado
        });
    }

    // GET: api/usuarios/5
    [HttpGet("{id}")]
    public async Task<ActionResult<UsuarioResponseDto>> GetUsuario(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        return new UsuarioResponseDto
        {
            UsuarioId = usuario.UsuarioId,
            Nombre = usuario.Nombre,
            Rol = usuario.Rol,
            Estado = usuario.Estado
        };
    }

    // PUT: api/usuarios/5/estado
    // Para desactivar/reactivar un usuario (soporta el criterio 2, ya validado en Login)
    [HttpPut("{id}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] bool nuevoEstado)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario == null) return NotFound();

        usuario.Estado = nuevoEstado;
        await _context.SaveChangesAsync();
        return NoContent();
    }
}