using Microsoft.AspNetCore.Mvc;

namespace luisfrontend.Controllers
{
    public class VentasController : Controller
    {
        // 1. Punto de Venta (POS)
        public IActionResult Index()
        {
            return View();
        }

        // 2. PC-147 y PC-151: Reporte de Más Vendidos con filtro por período (Día / Semana / Mes)
        [HttpGet]
        public IActionResult MasVendidos(string periodo = "dia")
        {
            ViewBag.Periodo = periodo.ToLower();
            return View();
        }

        // 3. PC-105: Reporte Diario Consolidado
        [HttpGet]
        public IActionResult ReporteDiario()
        {
            ViewBag.FechaActual = DateTime.Now.ToString("dd/MM/yyyy");
            return View();
        }

        // 4. PC-87 y PC-91: Vista para Cocina y Gestión de Estado de Pedidos
        [HttpGet]
        public IActionResult Cocina()
        {
            return View();
        }

        // 5. PC-47: Checklist de Inventario
        [HttpGet]
        public IActionResult Checklist()
        {
            return View();
        }

        // --- ENDPOINTS API ---

        [HttpPost]
        public IActionResult ConfirmarVenta([FromBody] VentaDTO venta)
        {
            if (venta == null || venta.Detalles == null || !venta.Detalles.Any())
            {
                return BadRequest(new { mensaje = "El carrito está vacío." });
            }

            // Simulación de respuesta exitosa
            return Json(new { exito = true, mensaje = "Venta y Nota de Cocina generadas con éxito." });
        }

        [HttpPatch]
        public IActionResult CambiarEstadoNota(int id, string nuevoEstado)
        {
            return Json(new { exito = true, mensaje = $"Pedido #{id} actualizado a estado: {nuevoEstado}" });
        }
    }

    // DTOs auxiliares
    public class VentaDTO
    {
        public string MedioPago { get; set; } = "Efectivo";
        public decimal Total { get; set; }
        public List<DetalleVentaDTO> Detalles { get; set; } = new();
    }

    public class DetalleVentaDTO
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
    }
}