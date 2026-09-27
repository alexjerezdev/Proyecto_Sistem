using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;

namespace CafeAroma.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CierreCajaController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CierreCajaController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/CierreCaja/resumen-dia
        // HU-21 Resumen: cuanto se ha vendido HOY antes de cerrar caja.
        [HttpGet("resumen-dia")]
        public async Task<ActionResult<object>> GetResumenDia()
        {
            decimal totalVentasHoy = await ObtenerTotalVentasDeHoyAsync();
            return new { fecha = DateOnly.FromDateTime(DateTime.Today), totalVentasHoy };
        }

        // POST: api/CierreCaja
        // HU-20 Cierre: compara el total de ventas del sistema contra el efectivo contado.
        // Ademas deja constancia en la bitacora (HU-26).
        [HttpPost]
        public async Task<ActionResult<CierreCaja>> CerrarCaja(CierreCajaRequest request)
        {
            decimal totalVentas = await ObtenerTotalVentasDeHoyAsync();

            var cierre = new CierreCaja
            {
                Fecha = DateOnly.FromDateTime(DateTime.Today),
                UsuarioId = request.UsuarioId,
                TotalVentas = totalVentas,
                EfectivoContado = request.EfectivoContado,
                FechaHora = DateTime.UtcNow
            };

            _context.CierresCaja.Add(cierre);
            await _context.SaveChangesAsync();

            // Traemos de vuelta "diferencia", que la calcula Postgres (columna generada).
            await _context.Entry(cierre).ReloadAsync();

            _context.Bitacoras.Add(new Bitacora
            {
                UsuarioId = request.UsuarioId,
                Accion = "CIERRE_CAJA",
                EntidadAfectada = "cierre_caja",
                EntidadId = cierre.CierreCajaId,
                Detalle = $"Total ventas: {cierre.TotalVentas}, efectivo contado: {cierre.EfectivoContado}, diferencia: {cierre.Diferencia}",
                FechaHora = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCierre), new { id = cierre.CierreCajaId }, cierre);
        }

        // GET: api/CierreCaja
        // GET: api/CierreCaja?desde=2026-09-01&hasta=2026-09-30&usuarioId=1&soloConDiferencia=true
        // HU-22 Historial: todos los cierres, del mas reciente al mas antiguo, con filtros opcionales.
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CierreCaja>>> GetHistorial(
            [FromQuery] DateOnly? desde,
            [FromQuery] DateOnly? hasta,
            [FromQuery] int? usuarioId,
            [FromQuery] bool soloConDiferencia = false)
        {
            var consulta = _context.CierresCaja.AsQueryable();

            if (desde.HasValue)
                consulta = consulta.Where(c => c.Fecha >= desde.Value);

            if (hasta.HasValue)
                consulta = consulta.Where(c => c.Fecha <= hasta.Value);

            if (usuarioId.HasValue)
                consulta = consulta.Where(c => c.UsuarioId == usuarioId.Value);

            // Util para que el dueno revise solo los cierres donde no cuadro la caja
            if (soloConDiferencia)
                consulta = consulta.Where(c => c.Diferencia != 0);

            return await consulta
                .OrderByDescending(c => c.FechaHora)
                .ToListAsync();
        }

        // GET: api/CierreCaja/5
        [HttpGet("{id}")]
        public async Task<ActionResult<CierreCaja>> GetCierre(int id)
        {
            var cierre = await _context.CierresCaja.FindAsync(id);
            if (cierre == null) return NotFound();
            return cierre;
        }

        // Suma las ventas de hoy consultando directamente la tabla "venta"
        // (asi no dependemos del modelo Venta del modulo de ventas).
        private async Task<decimal> ObtenerTotalVentasDeHoyAsync()
        {
            var connection = _context.Database.GetDbConnection();
            bool abrioAqui = connection.State != System.Data.ConnectionState.Open;
            if (abrioAqui) await connection.OpenAsync();

            try
            {
                using var cmd = connection.CreateCommand();
                cmd.CommandText = "SELECT COALESCE(SUM(total), 0) FROM venta WHERE fecha_hora::date = CURRENT_DATE";
                var resultado = await cmd.ExecuteScalarAsync();
                return resultado != null && resultado != DBNull.Value ? Convert.ToDecimal(resultado) : 0m;
            }
            finally
            {
                if (abrioAqui) await connection.CloseAsync();
            }
        }
    }
}