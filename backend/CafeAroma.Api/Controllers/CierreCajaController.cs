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
        // HU-21 Resumen: muestra cuanto se ha vendido HOY antes de cerrar caja,
        // para que la encargada/dueno vea el numero antes de contar el efectivo.
        [HttpGet("resumen-dia")]
        public async Task<ActionResult<object>> GetResumenDia()
        {
            decimal totalVentasHoy = await ObtenerTotalVentasDeHoyAsync();
            return new { fecha = DateOnly.FromDateTime(DateTime.Today), totalVentasHoy };
        }

        // POST: api/CierreCaja
        // HU-20 Cierre: registra el cierre de caja del dia, comparando
        // el total de ventas del sistema contra el efectivo contado a mano.
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

            // Volvemos a leer el registro para traer "diferencia",
            // que la calculo Postgres solo (columna generada).
            await _context.Entry(cierre).ReloadAsync();

            return CreatedAtAction(nameof(GetCierre), new { id = cierre.CierreCajaId }, cierre);
        }

        // GET: api/CierreCaja
        // HU-22 (bonus): historial de todos los cierres realizados
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CierreCaja>>> GetHistorial()
        {
            return await _context.CierresCaja
                .OrderByDescending(c => c.FechaHora)
                .ToListAsync();
        }

        // GET: api/CierreCaja/5
        // Detalle/resumen de un cierre puntual
        [HttpGet("{id}")]
        public async Task<ActionResult<CierreCaja>> GetCierre(int id)
        {
            var cierre = await _context.CierresCaja.FindAsync(id);
            if (cierre == null) return NotFound();
            return cierre;
        }

        // Suma el total de las ventas registradas hoy, consultando
        // directamente la tabla "venta" (no hace falta un modelo Venta
        // aparte solo para esta suma).
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