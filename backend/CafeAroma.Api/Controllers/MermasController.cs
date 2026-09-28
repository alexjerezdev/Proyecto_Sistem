using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using CafeAroma.Api.Models;

namespace CafeAroma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MermasController : ControllerBase
{
    private readonly AppDbContext _db;
    public MermasController(AppDbContext db) => _db = db;

    [HttpPost]
    public async Task<IActionResult> Registrar(MermaDto dto)
    {
        if (dto.Cantidad <= 0)
            return BadRequest("La cantidad debe ser mayor a 0.");
        if (string.IsNullOrWhiteSpace(dto.Motivo))
            return BadRequest("El motivo es obligatorio.");

        var insumo = await _db.Insumos.FindAsync(dto.InsumoId);
        if (insumo is null) return NotFound("Insumo no existe.");
        if (insumo.StockActual < dto.Cantidad)
            return BadRequest("La merma supera el stock disponible.");

        insumo.StockActual -= dto.Cantidad;
        var merma = new Merma
        {
            InsumoId = dto.InsumoId,
            Cantidad = dto.Cantidad,
            Motivo = dto.Motivo.Trim(),
            UsuarioId = 0 // TODO: usar el mismo mecanismo que CierreCajaController
        };
        _db.Mermas.Add(merma);
        await _db.SaveChangesAsync();

        return Ok(merma);
    }

    [HttpGet]
    public async Task<IActionResult> Historial() =>
        Ok(await _db.Mermas.Include(m => m.Insumo)
            .OrderByDescending(m => m.Fecha).ToListAsync());
}