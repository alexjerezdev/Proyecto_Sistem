using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("merma")]
    public class Merma
    {
        [Key]
        [Column("merma_id")]
        public int MermaId { get; set; }

        [Column("insumo_id")]
        public int InsumoId { get; set; }

        [ForeignKey(nameof(InsumoId))]
        public Insumo Insumo { get; set; } = null!;

        [Column("cantidad")]
        public decimal Cantidad { get; set; }

        [Column("motivo")]
        public string Motivo { get; set; } = string.Empty;

        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Column("fecha")]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;
    }
}