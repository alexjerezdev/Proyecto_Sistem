namespace CafeAroma.Api.Models
{
    // Una venta que se registro en el dispositivo SIN internet y ahora se envia al servidor.
    public class VentaOfflineRequest
    {
        // Identificador unico generado por el dispositivo al hacer la venta (un GUID).
        // Evita que la misma venta se guarde dos veces si la sincronizacion se reintenta.
        public Guid OfflineId { get; set; }

        // Fecha y hora REAL en que se hizo la venta (no la de la sincronizacion).
        // Con zona horaria, ej: "2026-09-27T10:30:00-04:00" o "2026-09-27T14:30:00Z"
        public DateTimeOffset FechaHora { get; set; }

        public int UsuarioId { get; set; }
        public int? ClienteId { get; set; }
        public List<DetalleOfflineRequest> Detalles { get; set; } = new();
    }

    public class DetalleOfflineRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }

        // Precio que se cobro en el momento de la venta
        public decimal PrecioUnitario { get; set; }
    }

    // Resultado por cada venta enviada, para que el dispositivo sepa cuales borrar de su cola local.
    public class ResultadoSincronizacion
    {
        public Guid OfflineId { get; set; }

        // "sincronizada" | "duplicada" (ya estaba guardada) | "error"
        public string Estado { get; set; } = string.Empty;

        public int? VentaId { get; set; }
        public string? Mensaje { get; set; }
    }
}