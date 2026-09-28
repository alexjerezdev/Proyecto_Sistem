namespace CafeAroma.Api.DTOs;

// Lo que manda el frontend para registrar el ingreso de insumos por compra
public class RegistrarCompraRequest
{
    public int? UsuarioId { get; set; }
    public List<CompraItemDto> Items { get; set; } = new();
}

public class CompraItemDto
{
    public int InsumoId { get; set; }
    public decimal Cantidad { get; set; }
}

// Lo que devuelve la API: cuanto ingreso y como quedo el stock de cada insumo
public class CompraItemResultadoDto
{
    public int InsumoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadIngresada { get; set; }
    public decimal StockActual { get; set; }
}