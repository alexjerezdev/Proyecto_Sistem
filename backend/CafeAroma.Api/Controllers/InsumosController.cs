using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;

namespace CafeAroma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InsumosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public InsumosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/insumos
        // Lista todos los insumos con su stock actual y mínimo
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Insumo>>> GetInsumos()
        {
            return await _context.Insumos.ToListAsync();
        }

        // GET: api/insumos/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Insumo>> GetInsumo(int id)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null) return NotFound();
            return insumo;
        }

        // POST: api/insumos
        // HU-05: registrar un nuevo insumo
        [HttpPost]
        public async Task<ActionResult<Insumo>> CrearInsumo(Insumo insumo)
        {
            _context.Insumos.Add(insumo);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetInsumo), new { id = insumo.InsumoId }, insumo);
        }

        // PUT: api/insumos/5/stock-minimo
        // HU-07: definir/actualizar el stock mínimo de un insumo
        [HttpPut("{id}/stock-minimo")]
        public async Task<IActionResult> ActualizarStockMinimo(int id, [FromBody] decimal nuevoStockMinimo)
        {
            var insumo = await _context.Insumos.FindAsync(id);
            if (insumo == null) return NotFound();

            insumo.StockMinimo = nuevoStockMinimo;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/insumos/alertas
        // HU-06: alertas de stock critico que aun no fueron atendidas
        // (las alertas las genera automaticamente un trigger en la base de datos
        // cuando una venta deja un insumo por debajo de su stock minimo)
        [HttpGet("alertas")]
        public async Task<ActionResult<IEnumerable<AlertaStock>>> GetAlertas()
        {
            return await _context.AlertasStock
                .Where(a => !a.Atendida)
                .OrderByDescending(a => a.Fecha)
                .ToListAsync();
        }

        // PUT: api/insumos/alertas/5/atender
        // Marca una alerta como resuelta (por ejemplo, tras reponer el insumo)
        [HttpPut("alertas/{id}/atender")]
        public async Task<IActionResult> AtenderAlerta(int id)
        {
            var alerta = await _context.AlertasStock.FindAsync(id);
            if (alerta == null) return NotFound();

            alerta.Atendida = true;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/insumos/5/movimientos
        // Historial de movimientos de un insumo (entradas, salidas, mermas, ajustes).
        // Las salidas por venta las crea el trigger automaticamente (HU-10);
        // las entradas por compra y las mermas las registrara el modulo de Compras/Mermas.
        [HttpGet("{id}/movimientos")]
        public async Task<ActionResult<IEnumerable<MovimientoInventario>>> GetMovimientos(int id)
        {
            return await _context.MovimientosInventario
                .Where(m => m.InsumoId == id)
                .OrderByDescending(m => m.Fecha)
                .ToListAsync();
        }
    }
}