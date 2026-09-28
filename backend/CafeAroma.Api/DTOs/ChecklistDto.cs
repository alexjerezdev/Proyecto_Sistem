namespace CafeAroma.Api.DTOs;

public class LineaConteoDto { public int InsumoId { get; set; } public decimal StockContado { get; set; } }
public class ChecklistRequest { public int UsuarioId { get; set; } public List<LineaConteoDto> Lineas { get; set; } = new(); }

public class DiferenciaDto
{
    public int InsumoId { get; set; }
    public string Insumo { get; set; } = "";
    public decimal StockTeorico { get; set; }
    public decimal StockContado { get; set; }
    public decimal Diferencia { get; set; }
    public string Estado { get; set; } = "";   // Cuadra / Faltante / Sobrante
}

public class ChecklistResultado { public int ChecklistId { get; set; } public List<DiferenciaDto> Diferencias { get; set; } = new(); }