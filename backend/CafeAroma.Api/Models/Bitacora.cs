using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("bitacora")]
    public class Bitacora
    {
        [Key]
        [Column("bitacora_id")]
        public int BitacoraId { get; set; }

        [Column("usuario_id")]
        public int? UsuarioId { get; set; }

        // Ej.: CIERRE_CAJA, CAMBIO_PRECIO, ANULAR_VENTA
        [Column("accion")]
        public string Accion { get; set; } = string.Empty;

        // Ej.: cierre_caja, producto, venta
        [Column("entidad_afectada")]
        public string EntidadAfectada { get; set; } = string.Empty;

        [Column("entidad_id")]
        public int? EntidadId { get; set; }

        [Column("detalle")]
        public string? Detalle { get; set; }

        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; }
    }

    // Lo que manda otro modulo (o el frontend) para dejar una marca en la bitacora.
    // La fecha la pone el servidor, no el cliente.
    public class BitacoraRequest
    {
        public int? UsuarioId { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string EntidadAfectada { get; set; } = string.Empty;
        public int? EntidadId { get; set; }
        public string? Detalle { get; set; }
    }
}