using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CafeAroma.Api.Models
{
    [Table("cierre_caja")]
    public class CierreCaja
    {
        [Key]
        [Column("cierre_caja_id")]
        public int CierreCajaId { get; set; }

        [Column("fecha")]
        public DateOnly Fecha { get; set; }

        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Column("total_ventas")]
        public decimal TotalVentas { get; set; }

        [Column("efectivo_contado")]
        public decimal EfectivoContado { get; set; }

        // Columna GENERATED en la base de datos: la calcula Postgres solo,
        // aqui solo la leemos, nunca la escribimos.
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column("diferencia")]
        public decimal Diferencia { get; set; }

        [Column("fecha_hora")]
        public DateTime FechaHora { get; set; }
    }

    // DTO: lo que manda el frontend para cerrar caja.
    // No se expone la entidad completa porque total_ventas y diferencia
    // los calcula el sistema, no los escribe el usuario.
    public class CierreCajaRequest
    {
        public int UsuarioId { get; set; }
        public decimal EfectivoContado { get; set; }
    }
}