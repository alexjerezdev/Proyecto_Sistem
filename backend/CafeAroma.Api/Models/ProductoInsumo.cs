using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("producto_insumo")]
    public class ProductoInsumo
    {
        [Column("producto_id")]
        public int ProductoId { get; set; }

        [Column("insumo_id")]
        public int InsumoId { get; set; }

        [Column("cantidad_necesaria")]
        public decimal CantidadNecesaria { get; set; }
    }
}