using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("alerta_stock")]
    public class AlertaStock
    {
        [Key]
        [Column("alerta_id")]
        public int AlertaId { get; set; }

        [Column("nivel_detectado")]
        public decimal NivelDetectado { get; set; }

        [Column("atendida")]
        public bool Atendida { get; set; }

        [Column("insumo_id")]
        public int InsumoId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; }
    }
}