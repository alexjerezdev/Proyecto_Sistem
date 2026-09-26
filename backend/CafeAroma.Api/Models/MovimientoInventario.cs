using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("movimiento_inventario")]
    public class MovimientoInventario
    {
        [Key]
        [Column("movimiento_id")]
        public int MovimientoId { get; set; }

        // Valores esperados: "entrada", "salida", "merma", "ajuste"
        [Column("tipo")]
        public string Tipo { get; set; } = string.Empty;

        [Column("cantidad")]
        public decimal Cantidad { get; set; }

        [Column("insumo_id")]
        public int InsumoId { get; set; }

        [Column("usuario_id")]
        public int? UsuarioId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }
    }
}