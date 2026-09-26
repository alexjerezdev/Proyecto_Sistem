namespace CafeAroma.Api.Models;

public class HistoricoPrecio
{
    public int HistoricoPrecioId { get; set; }
    public int ProductoId { get; set; }
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
    public int? UsuarioId { get; set; }
    public DateTime Fecha { get; set; }
}