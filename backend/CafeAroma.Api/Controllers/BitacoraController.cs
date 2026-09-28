using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;

namespace CafeAroma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BitacoraController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BitacoraController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Bitacora
        // GET: api/Bitacora?usuarioId=1&accion=CIERRE_CAJA&desde=2026-09-01&hasta=2026-09-30&limite=50
        // HU-26: consulta de la bitacora de auditoria (solo lectura, con filtros opcionales).
        // Los cambios de precio los registra solo un trigger de la base de datos;
        // los cierres de caja los registra CierreCajaController.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Bitacora>>> GetBitacora(
            [FromQuery] int? usuarioId,
            [FromQuery] string? accion,
            [FromQuery] string? entidad,
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] int limite = 100)
        {
            limite = Math.Clamp(limite, 1, 500);

            var consulta = _context.Bitacoras.AsQueryable();

            if (usuarioId.HasValue)
                consulta = consulta.Where(b => b.UsuarioId == usuarioId.Value);

            if (!string.IsNullOrWhiteSpace(accion))
            {
                var accionNormalizada = accion.Trim().ToUpper();
                consulta = consulta.Where(b => b.Accion == accionNormalizada);
            }

            if (!string.IsNullOrWhiteSpace(entidad))
            {
                var entidadNormalizada = entidad.Trim().ToLower();
                consulta = consulta.Where(b => b.EntidadAfectada == entidadNormalizada);
            }

            // Las fechas se arman en UTC porque PostgreSQL (timestamptz) no acepta otro tipo.
            if (desde.HasValue)
            {
                var inicio = desde.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                consulta = consulta.Where(b => b.FechaHora >= inicio);
            }

            if (hasta.HasValue)
            {
                // "hasta" incluye todo ese dia, por eso se compara contra el dia siguiente
                var finExclusivo = hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                consulta = consulta.Where(b => b.FechaHora < finExclusivo);
            }

            return await consulta
                .OrderByDescending(b => b.FechaHora)
                .Take(limite)
                .ToListAsync();
        }

        // GET: api/Bitacora/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Bitacora>> GetRegistro(int id)
        {
            var registro = await _context.Bitacoras.FindAsync(id);
            if (registro == null) return NotFound();
            return registro;
        }

        // POST: api/Bitacora
        // Permite que otros modulos (ej. anulacion de ventas) dejen una marca en la bitacora.
        [HttpPost]
        public async Task<ActionResult<Bitacora>> RegistrarEvento(BitacoraRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Accion) || string.IsNullOrWhiteSpace(request.EntidadAfectada))
                return BadRequest("Accion y EntidadAfectada son obligatorias.");

            var registro = new Bitacora
            {
                UsuarioId = request.UsuarioId,
                Accion = request.Accion.Trim().ToUpper(),
                EntidadAfectada = request.EntidadAfectada.Trim().ToLower(),
                EntidadId = request.EntidadId,
                Detalle = request.Detalle,
                FechaHora = DateTime.UtcNow
            };

            _context.Bitacoras.Add(registro);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetRegistro), new { id = registro.BitacoraId }, registro);
        }
    }
}