using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("checklist")]
    public class Checklist
    {
        [Key][Column("checklist_id")] public int ChecklistId { get; set; }
        [Column("usuario_id")] public int UsuarioId { get; set; }
        [Column("fecha_hora")] public DateTime FechaHora { get; set; } = DateTime.UtcNow;
        public List<ChecklistDetalle> Detalles { get; set; } = new();
    }

    [Table("checklist_detalle")]
    public class ChecklistDetalle
    {
        [Key][Column("detalle_id")] public int DetalleId { get; set; }
        [Column("checklist_id")] public int ChecklistId { get; set; }
        [Column("insumo_id")] public int InsumoId { get; set; }
        [ForeignKey(nameof(InsumoId))] public Insumo Insumo { get; set; } = null!;
        [Column("stock_teorico")] public decimal StockTeorico { get; set; }
        [Column("stock_contado")] public decimal StockContado { get; set; }
    }
}