using System.Data;
using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CafeAroma.Api.Data;
using CafeAroma.Api.Models;

namespace CafeAroma.Api.Controllers
{
    // HU-27 Offline.
    // La captura sin internet ocurre en el FRONTEND (guarda las ventas en el dispositivo).
    // Este controlador es la parte del servidor: recibe esas ventas cuando vuelve la
    // conexion y las guarda en la base de datos.
    [ApiController]
    [Route("api/[controller]")]
    public class SincronizacionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SincronizacionController(AppDbContext context)
        {
            _context = context;
        }

        // POST: api/Sincronizacion/ventas
        // Recibe una lista de ventas hechas offline. Cada venta se guarda en su propia
        // transaccion: si una falla (ej. no alcanza el stock), las demas se guardan igual
        // y la respuesta indica cual fallo y por que.
        [HttpPost("ventas")]
        public async Task<ActionResult<List<ResultadoSincronizacion>>> SincronizarVentas(
            [FromBody] List<VentaOfflineRequest> ventas)
        {
            var resultados = new List<ResultadoSincronizacion>();

            var connection = _context.Database.GetDbConnection();
            bool abrioAqui = connection.State != ConnectionState.Open;
            if (abrioAqui) await connection.OpenAsync();

            try
            {
                foreach (var venta in ventas)
                {
                    resultados.Add(await ProcesarVentaAsync(connection, venta));
                }
            }
            finally
            {
                if (abrioAqui) await connection.CloseAsync();
            }

            return resultados;
        }

        private static async Task<ResultadoSincronizacion> ProcesarVentaAsync(
            DbConnection connection, VentaOfflineRequest venta)
        {
            var resultado = new ResultadoSincronizacion { OfflineId = venta.OfflineId };

            // Validaciones basicas antes de tocar la base de datos
            if (venta.OfflineId == Guid.Empty)
                return Error(resultado, "Falta el OfflineId de la venta.");
            if (venta.Detalles == null || venta.Detalles.Count == 0)
                return Error(resultado, "La venta no tiene detalles.");
            if (venta.Detalles.Any(d => d.Cantidad <= 0 || d.PrecioUnitario < 0))
                return Error(resultado, "Hay detalles con cantidad o precio invalidos.");

            // El total lo calcula el servidor, no se confia en el que mande el dispositivo
            decimal total = venta.Detalles.Sum(d => d.Cantidad * d.PrecioUnitario);

            await using var transaccion = await connection.BeginTransactionAsync();
            try
            {
                // ON CONFLICT: si ese OfflineId ya existe, no inserta nada (no devuelve fila)
                int? ventaId;
                await using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = transaccion;
                    cmd.CommandText = @"
                        INSERT INTO venta (fecha_hora, total, usuario_id, cliente_id, offline_uuid)
                        VALUES (@fecha, @total, @usuario, @cliente, @uuid)
                        ON CONFLICT (offline_uuid) DO NOTHING
                        RETURNING venta_id";
                    AgregarParametro(cmd, "fecha", venta.FechaHora.UtcDateTime); // siempre UTC
                    AgregarParametro(cmd, "total", total);
                    AgregarParametro(cmd, "usuario", venta.UsuarioId);
                    AgregarParametro(cmd, "cliente", venta.ClienteId);
                    AgregarParametro(cmd, "uuid", venta.OfflineId);

                    var id = await cmd.ExecuteScalarAsync();
                    ventaId = id == null || id == DBNull.Value ? null : Convert.ToInt32(id);
                }

                if (ventaId == null)
                {
                    // Ya estaba sincronizada de antes: no se repite
                    await transaccion.RollbackAsync();
                    resultado.Estado = "duplicada";
                    resultado.Mensaje = "Esta venta ya habia sido sincronizada.";
                    return resultado;
                }

                // Al insertar cada detalle, el trigger de la base de datos descuenta el stock
                foreach (var detalle in venta.Detalles)
                {
                    await using var cmdDetalle = connection.CreateCommand();
                    cmdDetalle.Transaction = transaccion;
                    cmdDetalle.CommandText = @"
                        INSERT INTO detalle_venta (cantidad, precio_unitario, venta_id, producto_id)
                        VALUES (@cantidad, @precio, @venta, @producto)";
                    AgregarParametro(cmdDetalle, "cantidad", detalle.Cantidad);
                    AgregarParametro(cmdDetalle, "precio", detalle.PrecioUnitario);
                    AgregarParametro(cmdDetalle, "venta", ventaId.Value);
                    AgregarParametro(cmdDetalle, "producto", detalle.ProductoId);
                    await cmdDetalle.ExecuteNonQueryAsync();
                }

                await transaccion.CommitAsync();
                resultado.Estado = "sincronizada";
                resultado.VentaId = ventaId;
                return resultado;
            }
            catch (Exception ex)
            {
                await transaccion.RollbackAsync();
                return Error(resultado, TraducirError(ex));
            }
        }

        private static ResultadoSincronizacion Error(ResultadoSincronizacion resultado, string mensaje)
        {
            resultado.Estado = "error";
            resultado.Mensaje = mensaje;
            return resultado;
        }

        // Convierte errores tecnicos de PostgreSQL en mensajes que se entiendan
        private static string TraducirError(Exception ex)
        {
            var mensaje = ex.GetBaseException().Message;

            if (mensaje.Contains("insumo_stock_actual_check"))
                return "No alcanza el stock de algun insumo para esta venta.";
            if (mensaje.Contains("fk_venta_usuario"))
                return "El usuario indicado no existe.";
            if (mensaje.Contains("fk_detalle_producto"))
                return "Alguno de los productos indicados no existe.";
            if (mensaje.Contains("fk_venta_cliente"))
                return "El cliente indicado no existe.";

            return mensaje;
        }

        private static void AgregarParametro(DbCommand cmd, string nombre, object? valor)
        {
            var parametro = cmd.CreateParameter();
            parametro.ParameterName = nombre;
            parametro.Value = valor ?? DBNull.Value;
            cmd.Parameters.Add(parametro);
        }
    }
}