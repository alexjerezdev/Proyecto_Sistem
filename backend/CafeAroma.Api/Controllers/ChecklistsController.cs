using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Data;
using CafeAroma.Api.DTOs;
using CafeAroma.Api.Models;

namespace CafeAroma.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChecklistsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ChecklistsController(AppDbContext db) => _db = db;

    // Completa el checklist: guarda StockTeorico (foto del sistema) y StockContado por insumo
    [HttpPost]
    public async Task<IActionResult> Completar(ChecklistRequest req)
    {
        if (req.Lineas.Count == 0) return BadRequest("El checklist no tiene insumos.");
        if (req.Lineas.Any(l => l.StockContado < 0)) return BadRequest("El stock contado no puede ser negativo.");
        if (req.Lineas.GroupBy(l => l.InsumoId).Any(g => g.Count() > 1)) return BadRequest("Hay insumos repetidos.");

        var ids = req.Lineas.Select(l => l.InsumoId).ToList();
        var insumos = await _db.Insumos.Where(i => ids.Contains(i.InsumoId)).ToDictionaryAsync(i => i.InsumoId);
        if (insumos.Count != ids.Count) return NotFound("Algún insumo no existe.");

        var checklist = new Checklist
        {
            UsuarioId = req.UsuarioId,
            Detalles = req.Lineas.Select(l => new ChecklistDetalle
            {
                InsumoId = l.InsumoId,
                Insumo = insumos[l.InsumoId],
                StockTeorico = insumos[l.InsumoId].StockActual,
                StockContado = l.StockContado
            }).ToList()
        };
        _db.Checklists.Add(checklist);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetDiferencias), new { id = checklist.ChecklistId }, Mapear(checklist));
    }

    // Diferencia por insumo de un checklist completado
    [HttpGet("{id}/diferencias")]
    public async Task<IActionResult> GetDiferencias(int id)
    {
        var c = await _db.Checklists.Include(x => x.Detalles).ThenInclude(d => d.Insumo)
            .SingleOrDefaultAsync(x => x.ChecklistId == id);
        return c is null ? NotFound() : Ok(Mapear(c));
    }

    private static ChecklistResultado Mapear(Checklist c) => new()
    {
        ChecklistId = c.ChecklistId,
        Diferencias = c.Detalles.Select(d =>
        {
            var dif = d.StockContado - d.StockTeorico;
            return new DiferenciaDto
            {
                InsumoId = d.InsumoId,
                Insumo = d.Insumo.Nombre,
                StockTeorico = d.StockTeorico,
                StockContado = d.StockContado,
                Diferencia = dif,
                Estado = dif == 0 ? "Cuadra" : dif < 0 ? "Faltante" : "Sobrante"
            };
        }).ToList()
    };
}