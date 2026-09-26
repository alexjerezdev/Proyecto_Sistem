using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("insumo")]
    public class Insumo
    {
        [Key]
        [Column("insumo_id")]
        public int InsumoId { get; set; }

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("stock_actual")]
        public decimal StockActual { get; set; }

        [Column("stock_minimo")]
        public decimal StockMinimo { get; set; }
    }
}