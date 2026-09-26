using CafeAroma.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CafeAroma.Api.Controllers;

[ApiController]
[Route("api/productos")]
public class HistoricoPrecioController : ControllerBase
{
    private readonly AppDbContext _context;
    public HistoricoPrecioController(AppDbContext context) => _context = context;

    [HttpGet("{id}/historico")]
    public async Task<IActionResult> GetHistorico(int id)
    {
        var existe = await _context.Database
            .SqlQuery<int>($"SELECT producto_id FROM producto WHERE producto_id = {id} LIMIT 1")
            .AnyAsync();

        if (!existe)
            return NotFound(new { mensaje = "Producto no encontrado." });

        var historico = await _context.HistoricosPrecio
            .Where(h => h.ProductoId == id)
            .OrderByDescending(h => h.Fecha)
            .ToListAsync();

        return Ok(historico);
    }
}